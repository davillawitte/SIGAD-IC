using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permutas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Permutas da Gestão do Setor. Registra quem chefia a lotação dos dois servidores (chefe de
/// núcleo: qualquer setor do núcleo); exclui quem chefia a de um deles. A troca nas escalas é feita
/// pelo <see cref="PermutaAplicador"/>, na mesma transação.
/// </summary>
public class PermutaService(ApplicationDbContext db) : IPermutaService
{
    public async Task<IReadOnlyList<PermutaDto>> ListAsync(
        PermutaListQuery query,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var (setores, nucleos) = LotacoesChefiadas(actor);
        if (setores.Count == 0 && nucleos.Count == 0)
        {
            return [];
        }

        var q = db.Permutas
            .AsNoTracking()
            .Include(x => x.Solicitante)
            .Include(x => x.Permutante)
            .Where(x =>
                (x.Solicitante.SetorId != null && setores.Contains(x.Solicitante.SetorId.Value))
                || (x.Solicitante.NucleoId != null && nucleos.Contains(x.Solicitante.NucleoId.Value))
                || (x.Permutante.SetorId != null && setores.Contains(x.Permutante.SetorId.Value))
                || (x.Permutante.NucleoId != null && nucleos.Contains(x.Permutante.NucleoId.Value)));

        if (query.ServidorId is Guid servidorId)
        {
            q = q.Where(x => x.SolicitanteServidorId == servidorId || x.PermutanteServidorId == servidorId);
        }

        if (query.Ano is int ano && query.Mes is int mes && mes is >= 1 and <= 12)
        {
            var inicio = new DateOnly(ano, mes, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);
            q = q.Where(x => (x.DataIda >= inicio && x.DataIda <= fim) || (x.DataVolta >= inicio && x.DataVolta <= fim));
        }

        var items = await q
            .OrderByDescending(x => x.DataIda)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var nomes = await NomesPorLoginAsync(items.Select(x => x.CreatedBy), cancellationToken);
        return items.Select(x => Map(x, nomes)).ToList();
    }

    public async Task<Result<PermutaDto>> CreateAsync(
        CreatePermutaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var servidores = await db.Servidores
            .AsNoTracking()
            .Where(x => x.Id == request.SolicitanteServidorId || x.Id == request.PermutanteServidorId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (!servidores.TryGetValue(request.SolicitanteServidorId, out var solicitante))
        {
            return Result<PermutaDto>.Failure("Servidor solicitante não encontrado.");
        }

        if (!servidores.TryGetValue(request.PermutanteServidorId, out var permutante))
        {
            return Result<PermutaDto>.Failure("Servidor permutante não encontrado.");
        }

        if (!Chefia(actor, solicitante) || !Chefia(actor, permutante))
        {
            return Result<PermutaDto>.Failure(
                "Só é possível registrar permuta entre servidores do setor ou núcleo em que você é chefe.");
        }

        Permuta entity;
        try
        {
            entity = Permuta.Create(
                request.SolicitanteServidorId,
                request.PermutanteServidorId,
                request.DataIda,
                request.DataVolta,
                request.Observacao,
                actorLogin);
        }
        catch (ArgumentException ex)
        {
            return Result<PermutaDto>.Failure(ex.Message);
        }

        var afastado = await BuscarAfastamentoAsync(
            [solicitante, permutante], [request.DataIda, request.DataVolta], cancellationToken);
        if (afastado is not null)
        {
            return Result<PermutaDto>.Failure(afastado);
        }

        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Permutas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await new PermutaAplicador(db).RecalcularDatasAsync(
            [entity.DataIda, entity.DataVolta], actorLogin, cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        var criada = await db.Permutas
            .AsNoTracking()
            .Include(x => x.Solicitante)
            .Include(x => x.Permutante)
            .FirstAsync(x => x.Id == entity.Id, cancellationToken);
        var nomes = await NomesPorLoginAsync([criada.CreatedBy], cancellationToken);
        return Result<PermutaDto>.Success(Map(criada, nomes));
    }

    public async Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var entity = await db.Permutas
            .Include(x => x.Solicitante)
            .Include(x => x.Permutante)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure("Permuta não encontrada.");
        }

        if (!Chefia(actor, entity.Solicitante) && !Chefia(actor, entity.Permutante))
        {
            return Result.Failure("Sem permissão para excluir esta permuta.");
        }

        // Recalcula a partir da base: as permutas posteriores no mesmo dia (permuta da permuta)
        // continuam valendo, só esta deixa de existir.
        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Permutas.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await new PermutaAplicador(db).RecalcularDatasAsync(
            [entity.DataIda, entity.DataVolta], actorLogin, cancellationToken);
        await transacao.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<PermutaServidoresDto> ListServidoresAsync(
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var (setores, nucleos) = LotacoesChefiadas(actor);

        var todos = await db.Servidores
            .AsNoTracking()
            .Where(x => x.Status != StatusServidor.Aposentado)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.Matricula,
                x.SetorId,
                x.NucleoId,
                SetorSigla = x.Setor != null ? x.Setor.Sigla : null,
                NucleoSigla = x.Nucleo != null ? x.Nucleo.Sigla : null,
            })
            .ToListAsync(cancellationToken);

        var opcoes = todos
            .Select(x => (
                Servidor: x,
                Opcao: new PermutaServidorOpcaoDto(x.Id, x.Nome, x.Matricula, x.SetorSigla ?? x.NucleoSigla)))
            .ToList();

        // Os dois lados seguem a chefia: chefe de setor vê o setor; chefe de núcleo, todos os
        // setores do núcleo e quem está lotado direto nele.
        var daChefia = opcoes
            .Where(x => (x.Servidor.SetorId is Guid s && setores.Contains(s))
                        || (x.Servidor.NucleoId is Guid n && nucleos.Contains(n)))
            .Select(x => x.Opcao)
            .ToList();

        return new PermutaServidoresDto(daChefia, daChefia);
    }

    public async Task<PermutaEscalaServidorDto> GetEscalaServidorAsync(
        Guid servidorId,
        int ano,
        int mes,
        CancellationToken cancellationToken = default)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var linhas = await db.EscalaServidores
            .AsNoTracking()
            .Where(x => x.ServidorId == servidorId && x.Escala.Ano == ano && x.Escala.Mes == mes)
            .Select(x => new
            {
                x.Id,
                x.EscalaId,
                x.Escala.Status,
                x.Escala.CreatedAt,
                SetorNome = x.Escala.Setor != null ? x.Escala.Setor.Nome : null,
                NucleoNome = x.Escala.Nucleo != null ? x.Escala.Nucleo.Nome : null,
            })
            .ToListAsync(cancellationToken);

        // Mesma escolha do PermutaAplicador: publicada > finalizada > rascunho, a mais recente.
        var linha = linhas
            .OrderByDescending(x => x.Status switch
            {
                StatusEscala.Publicada or StatusEscala.DevolucaoSolicitada => 3,
                StatusEscala.Finalizada => 2,
                _ => 1,
            })
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        var celulas = linha is null
            ? []
            : await db.EscalaOcorrencias
                .AsNoTracking()
                .Where(x => x.EscalaServidorId == linha.Id)
                .Select(x => new
                {
                    x.Data,
                    x.TipoOcorrenciaCodigo,
                    Nome = x.TipoOcorrencia.Nome,
                    Marca = x.Permutada && x.PermutaRecebida,
                })
                .ToListAsync(cancellationToken);
        var porData = celulas.ToDictionary(x => x.Data);

        var dias = new List<PermutaDiaDto>();
        for (var data = inicio; data <= fim; data = data.AddDays(1))
        {
            dias.Add(porData.TryGetValue(data, out var c)
                ? new PermutaDiaDto(data, c.TipoOcorrenciaCodigo, c.Nome, c.Marca)
                : new PermutaDiaDto(data, null, null, false));
        }

        return new PermutaEscalaServidorDto(
            linha?.EscalaId,
            linha is null ? null : Escala.FormatIdentificacao(mes, ano, linha.SetorNome ?? linha.NucleoNome ?? string.Empty),
            linha?.Status,
            dias,
            linha?.SetorNome ?? linha?.NucleoNome);
    }

    /// <summary>Recusa a permuta quando um dos dois está de férias/licença numa das datas — pelo
    /// cadastro de Afastamentos ou por ocorrência de afastamento lançada na escala.</summary>
    private async Task<string?> BuscarAfastamentoAsync(
        IReadOnlyList<Servidor> servidores,
        IReadOnlyList<DateOnly> datas,
        CancellationToken cancellationToken)
    {
        var ids = servidores.Select(x => x.Id).ToList();
        var menor = datas.Min();
        var maior = datas.Max();

        var afastamentos = await db.Afastamentos
            .AsNoTracking()
            .Where(x => ids.Contains(x.ServidorId) && x.DataInicio <= maior && x.DataFim >= menor)
            .Select(x => new { x.ServidorId, x.DataInicio, x.DataFim })
            .ToListAsync(cancellationToken);

        var codigosAfastamento = await db.TiposOcorrencia
            .AsNoTracking()
            .Where(x => x.Categoria == CategoriaOcorrencia.Afastamento)
            .Select(x => x.Codigo)
            .ToListAsync(cancellationToken);
        var naEscala = await db.EscalaOcorrencias
            .AsNoTracking()
            .Where(x => ids.Contains(x.EscalaServidor.ServidorId)
                        && datas.Contains(x.Data)
                        && codigosAfastamento.Contains(x.TipoOcorrenciaCodigo))
            .Select(x => new { x.EscalaServidor.ServidorId, x.Data })
            .ToListAsync(cancellationToken);

        foreach (var data in datas)
        {
            foreach (var servidor in servidores)
            {
                var afastado = afastamentos.Any(x => x.ServidorId == servidor.Id && x.DataInicio <= data && x.DataFim >= data)
                    || naEscala.Any(x => x.ServidorId == servidor.Id && x.Data == data);
                if (afastado)
                {
                    return $"{servidor.Nome} está afastado (férias/licença) em {data:dd/MM/yyyy}.";
                }
            }
        }

        return null;
    }

    private static (List<Guid> Setores, IReadOnlyList<Guid> Nucleos) LotacoesChefiadas(ActorContext actor) =>
        (actor.SetoresGerenciadosIds.Concat(actor.SetoresDosNucleosGerenciadosIds).Distinct().ToList(),
            actor.NucleosGerenciadosIds);

    /// <summary>Chefia de verdade da lotação do servidor — mesma regra de
    /// <c>EscalaService.IsChefiaDireta</c>.</summary>
    private static bool Chefia(ActorContext actor, Servidor servidor) =>
        servidor.SetorId is Guid s
            ? actor.SetoresGerenciadosIds.Contains(s) || actor.GerenciaSetorViaNucleo(s)
            : servidor.NucleoId is Guid n && actor.GerenciaNucleo(n);

    private async Task<Dictionary<string, string>> NomesPorLoginAsync(
        IEnumerable<string?> logins,
        CancellationToken cancellationToken)
    {
        var lista = logins
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (lista.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return await db.Usuarios
            .AsNoTracking()
            .Where(u => lista.Contains(u.Login))
            .Select(u => new { u.Login, u.Servidor.Nome })
            .ToDictionaryAsync(x => x.Login, x => x.Nome, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private static PermutaDto Map(Permuta x, IReadOnlyDictionary<string, string> nomes) =>
        new(
            x.Id,
            x.SolicitanteServidorId,
            x.Solicitante.Nome,
            x.Solicitante.Matricula,
            x.PermutanteServidorId,
            x.Permutante.Nome,
            x.Permutante.Matricula,
            x.DataIda,
            x.DataVolta,
            x.Observacao,
            x.CreatedBy,
            x.CreatedAt,
            x.CreatedBy is not null && nomes.TryGetValue(x.CreatedBy.Trim(), out var nome) ? nome : x.CreatedBy);

    private Task<ActorContext> ResolveActorAsync(string login, CancellationToken cancellationToken) =>
        ActorContextLoader.LoadAsync(db, login, cancellationToken);
}
