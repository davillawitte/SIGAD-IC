using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Afastamentos;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
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
            .AsQueryable();

        var escopo = (query.Escopo ?? string.Empty).Trim().ToLowerInvariant();
        if (escopo is "setor" or "meus")
        {
            var meus = actor.SetoresGerenciadosIds;
            if (meus.Count == 0)
            {
                return [];
            }

            q = q.Where(x => meus.Contains(x.Servidor.SetorId));
        }
        else if (escopo is "institucional" or "outros")
        {
            if (!actor.TemVisaoGlobal(PermissionModules.Afastamentos))
            {
                return [];
            }

            // Institucional: todos os setores, exceto a Direção do IC.
            var direcaoIds = await LoadDirecaoIcSetorIdsAsync(cancellationToken);
            if (direcaoIds.Count > 0)
            {
                q = q.Where(x => !direcaoIds.Contains(x.Servidor.SetorId));
            }
        }
        else
        {
            var setoresVisiveis = actor.SetoresVisiveis(PermissionModules.Afastamentos);
            if (setoresVisiveis is not null)
            {
                q = q.Where(x => setoresVisiveis.Contains(x.Servidor.SetorId));
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
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result<AfastamentoDto>.Failure("Afastamento não encontrado.");
        }

        if (!CanView(actor, entity.Servidor.SetorId))
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
            .FirstOrDefaultAsync(x => x.Id == request.ServidorId, cancellationToken);

        if (servidor is null)
        {
            return Result<AfastamentoDto>.Failure("Servidor não encontrado.");
        }

        if (!actor.PodeAcessar(PermissionCodes.AfastamentosCriar, servidor.SetorId))
        {
            return Result<AfastamentoDto>.Failure(
                "Só é possível cadastrar afastamento para servidores do setor em que você é chefe.");
        }

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

    public async Task<Result<AfastamentoDto>> UpdateAsync(
        Guid id,
        UpdateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var entity = await db.Afastamentos
            .Include(x => x.Servidor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result<AfastamentoDto>.Failure("Afastamento não encontrado.");
        }

        if (!actor.PodeAcessar(PermissionCodes.AfastamentosEditar, entity.Servidor.SetorId))
        {
            return Result<AfastamentoDto>.Failure("Sem permissão para alterar afastamento neste setor.");
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

    public async Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var entity = await db.Afastamentos
            .Include(x => x.Servidor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure("Afastamento não encontrado.");
        }

        if (!actor.PodeAcessar(PermissionCodes.AfastamentosExcluir, entity.Servidor.SetorId))
        {
            return Result.Failure("Sem permissão para excluir afastamento neste setor.");
        }

        db.Afastamentos.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static AfastamentoDto Map(Afastamento x, IReadOnlyDictionary<string, string> tipoNomes) =>
        new(
            x.Id,
            x.ServidorId,
            x.Servidor.Nome,
            x.Servidor.Matricula,
            x.Servidor.SetorId,
            x.Servidor.Setor.Nome,
            x.Servidor.Setor.Sigla,
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

    private static bool CanView(ActorContext actor, Guid setorId) =>
        actor.PodeVer(PermissionCodes.AfastamentosListar, setorId);
}
