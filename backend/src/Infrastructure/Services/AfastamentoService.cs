using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Afastamentos;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class AfastamentoService(ApplicationDbContext db) : IAfastamentoService
{
    public async Task<IReadOnlyList<AfastamentoDto>> ListAsync(
        AfastamentoListQuery query,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var q = db.Afastamentos
            .AsNoTracking()
            .Include(x => x.Servidor).ThenInclude(x => x.Setor)
            .Include(x => x.Servidor).ThenInclude(x => x.Nucleo)
            .AsQueryable();

        var escopo = (query.Escopo ?? string.Empty).Trim().ToLowerInvariant();
        if (escopo is "setor" or "meus")
        {
            // Chefe de núcleo também enxerga afastamentos dos servidores lotados nos setores
            // que aquele núcleo engloba (não só dos setores geridos diretamente), e dos
            // servidores lotados direto no próprio núcleo que chefia.
            var meusSetores = actor.SetoresGerenciadosIds
                .Concat(actor.SetoresDosNucleosGerenciadosIds)
                .Distinct()
                .ToList();
            var meusNucleos = actor.NucleosGerenciadosIds;
            if (meusSetores.Count == 0 && meusNucleos.Count == 0)
            {
                return [];
            }

            q = q.Where(x =>
                (x.Servidor.SetorId != null && meusSetores.Contains(x.Servidor.SetorId.Value))
                || (x.Servidor.NucleoId != null && meusNucleos.Contains(x.Servidor.NucleoId.Value)));
        }
        else if (escopo is "institucional" or "outros")
        {
            if (!actor.TemVisaoGlobal(PermissionModules.Afastamentos))
            {
                return [];
            }

            // Institucional: todos os setores e núcleos, exceto a Direção do IC.
            var direcaoIds = await LoadDirecaoIcSetorIdsAsync(cancellationToken);
            if (direcaoIds.Count > 0)
            {
                q = q.Where(x => x.Servidor.SetorId == null || !direcaoIds.Contains(x.Servidor.SetorId.Value));
            }
        }
        else
        {
            var setoresVisiveis = actor.SetoresVisiveis(PermissionModules.Afastamentos);
            if (setoresVisiveis is not null)
            {
                var setoresIncluidos = setoresVisiveis
                    .Concat(actor.SetoresDosNucleosGerenciadosIds)
                    .Distinct()
                    .ToList();
                var nucleosIncluidos = actor.NucleosGerenciadosIds;
                q = q.Where(x =>
                    (x.Servidor.SetorId != null && setoresIncluidos.Contains(x.Servidor.SetorId.Value))
                    || (x.Servidor.NucleoId != null && nucleosIncluidos.Contains(x.Servidor.NucleoId.Value)));
            }
        }

        if (query.SetorId is Guid setorId)
        {
            q = q.Where(x => x.Servidor.SetorId == setorId);
        }

        if (query.ServidorId is Guid servidorId)
        {
            q = q.Where(x => x.ServidorId == servidorId);
        }

        if (query.ServidorIds is { Count: > 0 } ids)
        {
            q = q.Where(x => ids.Contains(x.ServidorId));
        }

        if (query.TipoOcorrenciaCodigo is { Length: > 0 } tipo)
        {
            var codigo = tipo.Trim().ToUpperInvariant();
            q = q.Where(x => x.TipoOcorrenciaCodigo == codigo);
        }

        if (query.Ano is int ano && query.Mes is int mes && mes is >= 1 and <= 12)
        {
            var inicio = new DateOnly(ano, mes, 1);
            var fim = DateOnly.FromDateTime(new DateTime(ano, mes, 1).AddMonths(1).AddDays(-1));
            q = q.Where(x => x.DataInicio <= fim && x.DataFim >= inicio);
        }
        else if (query.Ano is int anoOnly)
        {
            var inicio = new DateOnly(anoOnly, 1, 1);
            var fim = new DateOnly(anoOnly, 12, 31);
            q = q.Where(x => x.DataInicio <= fim && x.DataFim >= inicio);
        }

        var items = await q
            .OrderByDescending(x => x.DataInicio)
            .ThenBy(x => x.Servidor.Nome)
            .ToListAsync(cancellationToken);

        var tipoNomes = await db.TiposOcorrencia
            .AsNoTracking()
            .Where(x => x.Ativo)
            .ToDictionaryAsync(x => x.Codigo, x => x.Nome, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return items.Select(x => Map(x, tipoNomes)).ToList();
    }

    public async Task<Result<AfastamentoDto>> GetByIdAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var entity = await db.Afastamentos
            .AsNoTracking()
            .Include(x => x.Servidor).ThenInclude(x => x.Setor)
            .Include(x => x.Servidor).ThenInclude(x => x.Nucleo)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result<AfastamentoDto>.Failure("Afastamento não encontrado.");
        }

        if (!CanView(actor, entity.Servidor.SetorId, entity.Servidor.NucleoId))
        {
            return Result<AfastamentoDto>.Failure("Sem permissão para este afastamento.");
        }

        var tipoNome = await db.TiposOcorrencia
            .AsNoTracking()
            .Where(x => x.Codigo == entity.TipoOcorrenciaCodigo)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(cancellationToken) ?? entity.TipoOcorrenciaCodigo;

        return Result<AfastamentoDto>.Success(Map(entity, new Dictionary<string, string>
        {
            [entity.TipoOcorrenciaCodigo] = tipoNome,
        }));
    }

    public async Task<Result<AfastamentoDto>> CreateAsync(
        CreateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var servidor = await db.Servidores
            .Include(x => x.Setor)
            .Include(x => x.Nucleo)
            .FirstOrDefaultAsync(x => x.Id == request.ServidorId, cancellationToken);

        if (servidor is null)
        {
            return Result<AfastamentoDto>.Failure("Servidor não encontrado.");
        }

        if (!PodeAcessarLotacao(actor, PermissionCodes.AfastamentosCriar, servidor.SetorId, servidor.NucleoId))
        {
            return Result<AfastamentoDto>.Failure(
                "Só é possível cadastrar afastamento para servidores do setor ou núcleo em que você é chefe.");
        }

        return await PersistirNovoAsync(request, actorLogin, cancellationToken);
    }

    public async Task<Result<AfastamentoDto>> CreateInstitucionalAsync(
        CreateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!actor.TemVisaoGlobal(PermissionModules.Afastamentos))
        {
            return Result<AfastamentoDto>.Failure(SemGestaoInstitucional);
        }

        var servidor = await db.Servidores
            .AsNoTracking()
            .Include(x => x.Setor)
            .FirstOrDefaultAsync(x => x.Id == request.ServidorId, cancellationToken);

        if (servidor is null)
        {
            return Result<AfastamentoDto>.Failure("Servidor não encontrado.");
        }

        var erro = ValidarInstitucional(actor, servidor);
        if (erro is not null)
        {
            return Result<AfastamentoDto>.Failure(erro);
        }

        return await PersistirNovoAsync(request, actorLogin, cancellationToken);
    }

    public async Task<IReadOnlyList<AfastamentoServidorOpcaoDto>> ListServidoresInstitucionaisAsync(
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!actor.TemVisaoGlobal(PermissionModules.Afastamentos))
        {
            return [];
        }

        var direcaoIds = await LoadDirecaoIcSetorIdsAsync(cancellationToken);
        return await db.Servidores
            .AsNoTracking()
            .Where(x => x.Status != StatusServidor.Aposentado)
            .Where(x => x.SetorId == null || !direcaoIds.Contains(x.SetorId.Value))
            .OrderBy(x => x.Nome)
            .Select(x => new AfastamentoServidorOpcaoDto(x.Id, x.Nome, x.Matricula))
            .ToListAsync(cancellationToken);
    }

    private async Task<Result<AfastamentoDto>> PersistirNovoAsync(
        CreateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        Afastamento entity;
        try
        {
            entity = Afastamento.Create(
                request.ServidorId,
                request.DataInicio,
                request.DataFim,
                request.TipoOcorrenciaCodigo,
                request.Observacao,
                request.Sei,
                actorLogin);
        }
        catch (Exception ex)
        {
            return Result<AfastamentoDto>.Failure(ex.Message);
        }

        db.Afastamentos.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, actorLogin, cancellationToken);
    }

    public Task<Result<AfastamentoDto>> UpdateAsync(
        Guid id,
        UpdateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default) =>
        UpdateCoreAsync(id, request, actorLogin, institucional: false, cancellationToken);

    public Task<Result<AfastamentoDto>> UpdateInstitucionalAsync(
        Guid id,
        UpdateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default) =>
        UpdateCoreAsync(id, request, actorLogin, institucional: true, cancellationToken);

    private async Task<Result<AfastamentoDto>> UpdateCoreAsync(
        Guid id,
        UpdateAfastamentoRequest request,
        string actorLogin,
        bool institucional,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var entity = await db.Afastamentos
            .Include(x => x.Servidor).ThenInclude(x => x.Setor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result<AfastamentoDto>.Failure("Afastamento não encontrado.");
        }

        if (institucional)
        {
            var erro = ValidarInstitucional(actor, entity.Servidor);
            if (erro is not null)
            {
                return Result<AfastamentoDto>.Failure(erro);
            }
        }
        else if (!PodeAcessarLotacao(actor, PermissionCodes.AfastamentosEditar, entity.Servidor.SetorId, entity.Servidor.NucleoId))
        {
            return Result<AfastamentoDto>.Failure("Sem permissão para alterar afastamento desta lotação.");
        }

        try
        {
            entity.Atualizar(
                request.DataInicio,
                request.DataFim,
                request.TipoOcorrenciaCodigo,
                request.Observacao,
                request.Sei,
                actorLogin);
        }
        catch (Exception ex)
        {
            return Result<AfastamentoDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default) =>
        DeleteCoreAsync(id, actorLogin, institucional: false, cancellationToken);

    public Task<Result> DeleteInstitucionalAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default) =>
        DeleteCoreAsync(id, actorLogin, institucional: true, cancellationToken);

    private async Task<Result> DeleteCoreAsync(
        Guid id,
        string actorLogin,
        bool institucional,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var entity = await db.Afastamentos
            .Include(x => x.Servidor).ThenInclude(x => x.Setor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure("Afastamento não encontrado.");
        }

        if (institucional)
        {
            var erro = ValidarInstitucional(actor, entity.Servidor);
            if (erro is not null)
            {
                return Result.Failure(erro);
            }
        }
        else if (!PodeAcessarLotacao(actor, PermissionCodes.AfastamentosExcluir, entity.Servidor.SetorId, entity.Servidor.NucleoId))
        {
            return Result.Failure("Sem permissão para excluir afastamento desta lotação.");
        }

        db.Afastamentos.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private const string SemGestaoInstitucional =
        "Só quem tem acesso à Gestão Institucional pode cadastrar afastamento por esta tela.";

    /// <summary>Gestão Institucional (visão global de afastamentos) cadastra, altera e exclui
    /// afastamento de qualquer servidor, exceto os da Direção do IC — mesmo recorte da listagem
    /// institucional; a Direção do IC fica com a Gestão do Setor. <c>servidor.Setor</c> precisa
    /// estar carregado.</summary>
    private static string? ValidarInstitucional(ActorContext actor, Servidor servidor)
    {
        if (!actor.TemVisaoGlobal(PermissionModules.Afastamentos))
        {
            return "Só quem tem acesso à Gestão Institucional pode alterar ou excluir afastamentos por esta tela.";
        }

        return SetorSiglas.IsDirecaoIc(servidor.Setor?.Sigla)
            ? "Afastamentos de servidores da Direção do IC são cadastrados pela Gestão do Setor."
            : null;
    }

    private static AfastamentoDto Map(Afastamento x, IReadOnlyDictionary<string, string> tipoNomes) =>
        new(
            x.Id,
            x.ServidorId,
            x.Servidor.Nome,
            x.Servidor.Matricula,
            x.Servidor.SetorId,
            x.Servidor.Setor?.Nome,
            x.Servidor.Setor?.Sigla,
            x.Servidor.NucleoId,
            x.Servidor.Nucleo?.Nome,
            x.Servidor.Nucleo?.Sigla,
            x.DataInicio,
            x.DataFim,
            x.TipoOcorrenciaCodigo,
            tipoNomes.TryGetValue(x.TipoOcorrenciaCodigo, out var nome) ? nome : x.TipoOcorrenciaCodigo,
            x.Observacao,
            x.Sei,
            x.CreatedAt);

    private Task<ActorContext> ResolveActorAsync(string login, CancellationToken cancellationToken) =>
        ActorContextLoader.LoadAsync(db, login, cancellationToken);

    private async Task<HashSet<Guid>> LoadDirecaoIcSetorIdsAsync(CancellationToken cancellationToken)
    {
        var setores = await db.Setores
            .AsNoTracking()
            .Select(x => new { x.Id, x.Sigla })
            .ToListAsync(cancellationToken);
        return setores
            .Where(x => SetorSiglas.IsDirecaoIc(x.Sigla))
            .Select(x => x.Id)
            .ToHashSet();
    }

    /// <summary>Servidor lotado direto no núcleo: quem chefia aquele núcleo, ou tem visão
    /// institucional do módulo. Servidor lotado em setor: mesma regra de sempre (abrangência da
    /// permissão, ou chefia de núcleo que engloba o setor).</summary>
    private static bool PodeAcessarLotacao(ActorContext actor, string code, Guid? setorId, Guid? nucleoId)
    {
        if (nucleoId.HasValue)
        {
            return actor.GerenciaNucleo(nucleoId.Value)
                || (actor.TemVisaoGlobal(PermissionModules.Afastamentos) && actor.TemPermissao(code));
        }

        return setorId.HasValue
            && (actor.PodeAcessar(code, setorId) || actor.GerenciaSetorViaNucleo(setorId.Value));
    }

    private static bool CanView(ActorContext actor, Guid? setorId, Guid? nucleoId)
    {
        if (nucleoId.HasValue)
        {
            return actor.GerenciaNucleo(nucleoId.Value) || actor.TemVisaoGlobal(PermissionModules.Afastamentos);
        }

        return setorId.HasValue
            && (actor.PodeVer(PermissionCodes.AfastamentosListar, setorId.Value) || actor.GerenciaSetorViaNucleo(setorId.Value));
    }
}
