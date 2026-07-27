using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Common;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class EscalaService(ApplicationDbContext db) : IEscalaService
{
    private static readonly HashSet<string> FolgaCodes =
        new(StringComparer.OrdinalIgnoreCase) { "D", "F", "FR", "LP", "LM", "LO" };

    public async Task<PagedResult<EscalaListItemDto>> ListAsync(
        EscalaListQuery query,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var normalized = query.Normalize();

        var q = db.Escalas.AsNoTracking().Include(x => x.Setor).AsQueryable();

        var escopo = (query.Escopo ?? string.Empty).Trim().ToLowerInvariant();
        if (escopo is "setor" or "meus")
        {
            var meus = actor.SetoresGerenciadosIds;
            if (meus.Count == 0)
            {
                return PagedResult<EscalaListItemDto>.Empty(normalized.Page, normalized.PageSize);
            }

            q = q.Where(x => meus.Contains(x.SetorId));
        }
        else if (escopo is "institucional" or "outros")
        {
            if (!actor.TemVisaoGlobal(PermissionModules.Escalas))
            {
                return PagedResult<EscalaListItemDto>.Empty(normalized.Page, normalized.PageSize);
            }

            // Institucional: todos os setores, exceto a Direção do IC (gerida em Gestão do Setor).
            var direcaoIds = await LoadDirecaoIcSetorIdsAsync(cancellationToken);
            if (direcaoIds.Count > 0)
            {
                q = q.Where(x => !direcaoIds.Contains(x.SetorId));
            }
        }
        else
        {
            var setoresVisiveis = actor.SetoresVisiveis(PermissionModules.Escalas);
            if (setoresVisiveis is not null)
            {
                q = q.Where(x => setoresVisiveis.Contains(x.SetorId));
            }
        }

        if (query.SetorId is Guid setorId)
        {
            q = q.Where(x => x.SetorId == setorId);
        }

        if (query.Status is StatusEscala status)
        {
            q = q.Where(x => x.Status == status);
        }

        if (query.Ano is int ano)
        {
            q = q.Where(x => x.Ano == ano);
        }

        if (query.Mes is int mes)
        {
            q = q.Where(x => x.Mes == mes);
        }

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var term = normalized.Search.ToLowerInvariant();
            q = q.Where(x =>
                x.Setor.Nome.ToLower().Contains(term) ||
                x.Setor.Sigla.ToLower().Contains(term));
        }

        q = q.OrderByDescending(x => x.Ano).ThenByDescending(x => x.Mes).ThenBy(x => x.Setor.Nome);

        var totalItems = await q.CountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<EscalaListItemDto>.Empty(normalized.Page, normalized.PageSize);
        }

        var pageItems = await q
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(x => new
            {
                x.Id,
                x.SetorId,
                SetorNome = x.Setor.Nome,
                SetorSigla = x.Setor.Sigla,
                x.Ano,
                x.Mes,
                x.TipoFuncionamento,
                x.Status,
                x.PublicadaEm,
                x.PublicadaPor,
                x.CreatedAt,
                x.CreatedBy,
            })
            .ToListAsync(cancellationToken);

        var createdByLogins = pageItems
            .Select(x => x.CreatedBy)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var responsavelByLogin = await ResolveResponsavelNomesAsync(createdByLogins, cancellationToken);

        var items = pageItems.Select(x =>
        {
            var inicio = new DateOnly(x.Ano, x.Mes, 1);
            var fim = DateOnly.FromDateTime(new DateTime(x.Ano, x.Mes, 1).AddMonths(1).AddDays(-1));
            var responsavelNome = ResolveResponsavelFromMap(x.CreatedBy, responsavelByLogin);
            return new EscalaListItemDto(
                x.Id,
                Escala.FormatIdentificacao(x.Mes, x.Ano, x.SetorNome),
                x.SetorId,
                x.SetorNome,
                x.SetorSigla,
                x.Ano,
                x.Mes,
                inicio,
                fim,
                x.TipoFuncionamento,
                x.Status,
                x.PublicadaEm,
                x.PublicadaPor,
                x.CreatedAt,
                responsavelNome);
        }).ToList();

        return PagedResult<EscalaListItemDto>.Create(items, normalized.Page, normalized.PageSize, totalItems);
    }

    public async Task<Result<EscalaDetailDto>> GetByIdAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await LoadDetailQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaDetailDto>.Failure("Escala não encontrada.");
        }

        if (!CanView(actor, escala.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        var detail = MapDetail(escala);
        var responsavelNome = await ResolveResponsavelNomeAsync(escala.CreatedBy, cancellationToken);
        return Result<EscalaDetailDto>.Success(detail with { CreatedBy = responsavelNome });
    }

    public async Task<Result<EscalaCalendarioDto>> GetCalendarioAsync(
        Guid id,
        Guid? servidorId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var detail = await GetByIdAsync(id, actorLogin, cancellationToken);
        if (!detail.Succeeded)
        {
            return Result<EscalaCalendarioDto>.Failure(detail.Error!);
        }

        var servidores = detail.Value!.Servidores
            .Where(s => servidorId is null || s.ServidorId == servidorId)
            .Select(s => new EscalaServidorCalendarioDto(
                s.Id,
                s.ServidorId,
                s.ServidorNome,
                s.Matricula,
                s.CargoNome,
                s.CargoCodigo,
                s.Ocorrencias))
            .ToList();

        return Result<EscalaCalendarioDto>.Success(new EscalaCalendarioDto(
            detail.Value.Id,
            detail.Value.Ano,
            detail.Value.Mes,
            detail.Value.DataInicio,
            detail.Value.DataFim,
            servidores));
    }

    public async Task<Result<EscalaDetailDto>> CreateAsync(
        CreateEscalaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!actor.PodeAcessar(PermissionCodes.EscalasCriar, request.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para criar escala neste setor.");
        }

        if (!await db.Setores.AnyAsync(x => x.Id == request.SetorId, cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Setor inválido.");
        }

        if (await db.Escalas.AnyAsync(
                x => x.SetorId == request.SetorId && x.Ano == request.Ano && x.Mes == request.Mes,
                cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Já existe escala para este setor neste mês/ano.");
        }

        Escala escala;
        try
        {
            escala = Escala.Create(
                request.SetorId,
                request.Ano,
                request.Mes,
                request.TipoFuncionamento,
                request.Observacao,
                actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        db.Escalas.Add(escala);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(escala.Id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaDetailDto>> UpdateAsync(
        Guid id,
        UpdateEscalaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken);
        if (error is not null)
        {
            return Result<EscalaDetailDto>.Failure(error);
        }

        if (await db.Escalas.AnyAsync(
                x => x.Id != id && x.SetorId == escala!.SetorId && x.Ano == request.Ano && x.Mes == request.Mes,
                cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Já existe escala para este setor neste mês/ano.");
        }

        try
        {
            escala!.Atualizar(request.Ano, request.Mes, request.TipoFuncionamento, request.Observacao, actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaDetailDto>> AddServidoresAsync(
        Guid id,
        AddEscalaServidoresRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escalaInfo = await db.Escalas
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.SetorId, x.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (escalaInfo is null)
        {
            return Result<EscalaDetailDto>.Failure("Escala não encontrada.");
        }

        if (!CanMutate(actor, escalaInfo.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        if (!IsEditableStatus(escalaInfo.Status))
        {
            return Result<EscalaDetailDto>.Failure(
                "A escala só pode ser alterada enquanto estiver em rascunho ou finalizada.");
        }

        var ids = (request.ServidorIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
        {
            return Result<EscalaDetailDto>.Failure("Informe ao menos um servidor.");
        }

        var jaNaEscala = await db.EscalaServidores
            .AsNoTracking()
            .Where(x => x.EscalaId == id)
            .Select(x => x.ServidorId)
            .ToListAsync(cancellationToken);

        var novosIds = ids.Where(x => !jaNaEscala.Contains(x)).ToList();
        if (novosIds.Count == 0)
        {
            return await GetByIdAsync(id, actorLogin, cancellationToken);
        }

        // Projeção evita tracking de Servidor/Cargo (causa comum de DbUpdateConcurrencyException).
        var servidores = await db.Servidores
            .AsNoTracking()
            .Where(x => novosIds.Contains(x.Id) && x.SetorId == escalaInfo.SetorId)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.CargoId,
                x.Nome,
                x.Matricula,
                CargoNome = x.Cargo.Nome,
                CargoCodigo = x.Cargo.Codigo,
            })
            .ToListAsync(cancellationToken);

        if (servidores.Count != novosIds.Count)
        {
            return Result<EscalaDetailDto>.Failure("Há servidores inválidos ou de outro setor.");
        }

        var maxOrdem = await db.EscalaServidores
            .AsNoTracking()
            .Where(x => x.EscalaId == id)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(cancellationToken) ?? 0;

        var ordem = maxOrdem + 1;
        foreach (var servidor in servidores)
        {
            db.EscalaServidores.Add(EscalaServidor.Create(
                escalaInfo.Id,
                servidor.Id,
                servidor.CargoId,
                ordem++,
                servidor.Nome,
                servidor.Matricula,
                servidor.CargoNome,
                servidor.CargoCodigo,
                actorLogin));
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result> RemoveServidorAsync(
        Guid id,
        Guid servidorId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken, includeServidores: true);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        var item = escala!.Servidores.FirstOrDefault(x => x.ServidorId == servidorId);
        if (item is null)
        {
            return Result.Failure("Servidor não está na escala.");
        }

        db.EscalaServidores.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<EscalaDetailDto>> AddJornadaAsync(
        Guid id,
        Guid servidorId,
        CreateEscalaJornadaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(
            id,
            actorLogin,
            cancellationToken,
            includeServidores: true,
            includeDeep: true);
        if (error is not null)
        {
            return Result<EscalaDetailDto>.Failure(error);
        }

        var escalaServidor = escala!.Servidores.FirstOrDefault(x => x.ServidorId == servidorId);
        if (escalaServidor is null)
        {
            return Result<EscalaDetailDto>.Failure("Servidor não está na escala.");
        }

        if (request.DataInicio < escala.DataInicio || request.DataFim > escala.DataFim)
        {
            return Result<EscalaDetailDto>.Failure("O período da jornada deve estar dentro da escala.");
        }

        if (!await db.TiposOcorrencia.AnyAsync(x => x.Codigo == request.TipoOcorrenciaCodigo && x.Ativo, cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Tipo de ocorrência inválido.");
        }

        if (request.PadraoEscalaId is Guid padraoEscalaId &&
            !await db.PadroesEscala.AnyAsync(x => x.Id == padraoEscalaId && x.Ativo, cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Padrão de escala inválido.");
        }

        EscalaJornada jornada;
        try
        {
            jornada = EscalaJornada.Create(
                escalaServidor.Id,
                request.TipoJornada,
                request.DataInicio,
                request.DataFim,
                request.TipoOcorrenciaCodigo,
                request.RecorrenciaTipo,
                request.HoraInicio,
                request.HoraFim,
                request.Horas,
                request.DiasSemana,
                request.IntervaloDias,
                request.DiasTrabalho,
                request.DiasFolga,
                request.TipoOcorrenciaFolgaCodigo,
                request.Observacao,
                request.PadraoEscalaId,
                request.DataInicioCiclo,
                actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        db.EscalaJornadas.Add(jornada);
        await db.SaveChangesAsync(cancellationToken);

        await ApplyJornadaAsync(escalaServidor, jornada, actorLogin, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result> DeleteJornadaAsync(
        Guid id,
        Guid jornadaId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken, includeDeep: true);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        var jornada = await db.EscalaJornadas
            .Include(x => x.EscalaServidor)
            .FirstOrDefaultAsync(x => x.Id == jornadaId && x.EscalaServidor.EscalaId == id, cancellationToken);

        if (jornada is null)
        {
            return Result.Failure("Jornada não encontrada.");
        }

        var ocorrencias = await db.EscalaOcorrencias
            .Where(x => x.EscalaJornadaId == jornadaId && x.Origem == OrigemOcorrencia.Regra)
            .ToListAsync(cancellationToken);

        db.EscalaOcorrencias.RemoveRange(ocorrencias);
        db.EscalaJornadas.Remove(jornada);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<EscalaDetailDto>> UpsertOcorrenciaAsync(
        Guid id,
        Guid servidorId,
        UpsertOcorrenciaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken, includeDeep: true);
        if (error is not null)
        {
            return Result<EscalaDetailDto>.Failure(error);
        }

        var escalaServidor = escala!.Servidores.FirstOrDefault(x => x.ServidorId == servidorId);
        if (escalaServidor is null)
        {
            return Result<EscalaDetailDto>.Failure("Servidor não está na escala.");
        }

        if (request.Data < escala.DataInicio || request.Data > escala.DataFim)
        {
            return Result<EscalaDetailDto>.Failure("Data fora do período da escala.");
        }

        if (!await db.TiposOcorrencia.AnyAsync(x => x.Codigo == request.TipoOcorrenciaCodigo && x.Ativo, cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Tipo de ocorrência inválido.");
        }

        var existing = escalaServidor.Ocorrencias.FirstOrDefault(x => x.Data == request.Data);
        if (existing is null)
        {
            escalaServidor.Ocorrencias.Add(EscalaOcorrencia.Create(
                escalaServidor.Id,
                request.Data,
                request.TipoOcorrenciaCodigo,
                OrigemOcorrencia.Manual,
                request.HoraInicio,
                request.HoraFim,
                request.Horas,
                observacao: request.Observacao,
                createdBy: actorLogin));
        }
        else
        {
            existing.AtualizarManual(
                request.TipoOcorrenciaCodigo,
                request.HoraInicio,
                request.HoraFim,
                request.Horas,
                request.Observacao,
                actorLogin);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result> DeleteOcorrenciaAsync(
        Guid id,
        Guid ocorrenciaId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        var ocorrencia = await db.EscalaOcorrencias
            .Include(x => x.EscalaServidor)
            .FirstOrDefaultAsync(x => x.Id == ocorrenciaId && x.EscalaServidor.EscalaId == id, cancellationToken);

        if (ocorrencia is null)
        {
            return Result.Failure("Ocorrência não encontrada.");
        }

        db.EscalaOcorrencias.Remove(ocorrencia);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<EscalaDetailDto>> PublicarAsync(
        Guid id,
        PublicarEscalaRequest? request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.Escalas
            .Include(x => x.Servidores).ThenInclude(x => x.Ocorrencias)
            .Include(x => x.Servidores).ThenInclude(x => x.Jornadas)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (escala is null)
        {
            return Result<EscalaDetailDto>.Failure("Escala não encontrada.");
        }

        if (!CanMutate(actor, escala.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        if (escala.Status != StatusEscala.Finalizada)
        {
            return Result<EscalaDetailDto>.Failure("Somente escalas finalizadas podem ser publicadas.");
        }

        var overlap = await db.Escalas.AnyAsync(
            x => x.Id != id
                 && x.SetorId == escala.SetorId
                 && x.Status == StatusEscala.Publicada
                 && x.Ano == escala.Ano
                 && x.Mes == escala.Mes,
            cancellationToken);

        if (overlap)
        {
            return Result<EscalaDetailDto>.Failure("Já existe escala publicada sobreposta neste setor e período.");
        }

        if (request?.ConfirmarConflitos != true)
        {
            var conflitos = await BuildConflitosAsync(escala, cancellationToken);
            if (conflitos.TotalCriticos > 0)
            {
                return Result<EscalaDetailDto>.Failure(
                    $"Existem {conflitos.TotalCriticos} conflito(s) crítico(s) na escala. Confirme para publicar mesmo assim.");
            }
        }

        try
        {
            escala.Publicar(actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaDetailDto>> FinalizarAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.Escalas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaDetailDto>.Failure("Escala não encontrada.");
        }

        if (!CanMutate(actor, escala.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        try
        {
            escala.Finalizar(actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaDetailDto>> ReabrirAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.Escalas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaDetailDto>.Failure("Escala não encontrada.");
        }

        if (!CanMutate(actor, escala.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        try
        {
            escala.ReabrirParaRascunho(actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.Escalas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result.Failure("Escala não encontrada.");
        }

        if (!CanMutate(actor, escala.SetorId))
        {
            return Result.Failure("Sem permissão para esta escala.");
        }

        if (!IsEditableStatus(escala.Status))
        {
            return Result.Failure("Somente escalas em rascunho ou finalizadas podem ser excluídas.");
        }

        db.Escalas.Remove(escala);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<SolicitacaoDevolucaoEscalaDto>> SolicitarDevolucaoAsync(
        Guid id,
        SolicitarDevolucaoEscalaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (actor.UsuarioId == Guid.Empty)
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure("Usuário não encontrado.");
        }

        var escala = await db.Escalas
            .Include(x => x.Setor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure("Escala não encontrada.");
        }

        if (!CanSolicitarDevolucao(actor, escala.SetorId))
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure("Sem permissão para esta escala.");
        }

        if (SetorSiglas.IsDirecaoIc(escala.Setor.Sigla))
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure(
                "A escala da Direção do IC não solicita devolução. Use a ação Devolver.");
        }

        if (escala.Status != StatusEscala.Publicada)
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure("Somente escalas publicadas podem solicitar devolução.");
        }

        var pendente = await db.SolicitacoesDevolucaoEscala.AnyAsync(
            x => x.EscalaId == id && x.Status == StatusSolicitacaoDevolucao.Pendente,
            cancellationToken);
        if (pendente)
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure("Já existe uma solicitação de devolução pendente para esta escala.");
        }

        SolicitacaoDevolucaoEscala solicitacao;
        try
        {
            solicitacao = SolicitacaoDevolucaoEscala.Create(id, actor.UsuarioId, request.Justificativa, actorLogin);
            escala.MarcarDevolucaoSolicitada(actorLogin);
        }
        catch (Exception ex)
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure(ex.Message);
        }

        db.SolicitacoesDevolucaoEscala.Add(solicitacao);
        await db.SaveChangesAsync(cancellationToken);

        var solicitanteNome = await db.Usuarios
            .AsNoTracking()
            .Where(x => x.Id == actor.UsuarioId)
            .Select(x => x.Servidor.Nome)
            .FirstOrDefaultAsync(cancellationToken) ?? actorLogin;

        return Result<SolicitacaoDevolucaoEscalaDto>.Success(new SolicitacaoDevolucaoEscalaDto(
            solicitacao.Id,
            escala.Id,
            Escala.FormatIdentificacao(escala.Mes, escala.Ano, escala.Setor.Nome),
            escala.SetorId,
            escala.Setor.Nome,
            escala.Setor.Sigla,
            escala.Ano,
            escala.Mes,
            solicitacao.SolicitanteUsuarioId,
            solicitanteNome,
            solicitacao.Justificativa,
            solicitacao.Status,
            solicitacao.RespondidoPor,
            solicitacao.RespostaEm,
            solicitacao.ObservacaoResposta,
            solicitacao.CreatedAt));
    }

    public async Task<Result<EscalaDetailDto>> DevolverAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);

        var escala = await db.Escalas
            .Include(x => x.Setor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaDetailDto>.Failure("Escala não encontrada.");
        }

        if (!actor.PodeAcessar(PermissionCodes.EscalasDevolver, escala.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para devolver esta escala.");
        }

        if (!SetorSiglas.IsDirecaoIc(escala.Setor.Sigla))
        {
            return Result<EscalaDetailDto>.Failure(
                "A devolução direta só é permitida para a escala da Direção do IC.");
        }

        if (escala.Status is not (StatusEscala.Publicada or StatusEscala.DevolucaoSolicitada))
        {
            return Result<EscalaDetailDto>.Failure(
                "Somente escalas publicadas ou com devolução solicitada podem ser devolvidas.");
        }

        var pendentes = await db.SolicitacoesDevolucaoEscala
            .Where(x => x.EscalaId == id && x.Status == StatusSolicitacaoDevolucao.Pendente)
            .ToListAsync(cancellationToken);
        foreach (var pendente in pendentes)
        {
            pendente.Recusar(actorLogin, "Devolução direta pela Direção do IC.");
        }

        try
        {
            escala.DevolverParaFinalizada(actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<IReadOnlyList<SolicitacaoDevolucaoEscalaDto>> ListDevolucoesPendentesAsync(
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!actor.TemPermissao(PermissionCodes.EscalasDevolver))
        {
            return [];
        }

        var items = await db.SolicitacoesDevolucaoEscala
            .AsNoTracking()
            .Include(x => x.Escala).ThenInclude(x => x.Setor)
            .Include(x => x.SolicitanteUsuario).ThenInclude(x => x.Servidor)
            .Where(x => x.Status == StatusSolicitacaoDevolucao.Pendente)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        // Devoluções institucionais: setores operacionais (não a própria Direção IC).
        var direcaoIds = await LoadDirecaoIcSetorIdsAsync(cancellationToken);
        if (direcaoIds.Count > 0)
        {
            items = items.Where(x => !direcaoIds.Contains(x.Escala.SetorId)).ToList();
        }

        return items.Select(x => new SolicitacaoDevolucaoEscalaDto(
            x.Id,
            x.EscalaId,
            Escala.FormatIdentificacao(x.Escala.Mes, x.Escala.Ano, x.Escala.Setor.Nome),
            x.Escala.SetorId,
            x.Escala.Setor.Nome,
            x.Escala.Setor.Sigla,
            x.Escala.Ano,
            x.Escala.Mes,
            x.SolicitanteUsuarioId,
            x.SolicitanteUsuario.Servidor.Nome,
            x.Justificativa,
            x.Status,
            x.RespondidoPor,
            x.RespostaEm,
            x.ObservacaoResposta,
            x.CreatedAt)).ToList();
    }

    public async Task<Result<SolicitacaoDevolucaoEscalaDto>> AprovarDevolucaoAsync(
        Guid solicitacaoId,
        ResponderDevolucaoEscalaRequest? request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        return await ResponderDevolucaoAsync(solicitacaoId, aprovar: true, request, actorLogin, cancellationToken);
    }

    public async Task<Result<SolicitacaoDevolucaoEscalaDto>> RecusarDevolucaoAsync(
        Guid solicitacaoId,
        ResponderDevolucaoEscalaRequest? request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        return await ResponderDevolucaoAsync(solicitacaoId, aprovar: false, request, actorLogin, cancellationToken);
    }

    private async Task<Result<SolicitacaoDevolucaoEscalaDto>> ResponderDevolucaoAsync(
        Guid solicitacaoId,
        bool aprovar,
        ResponderDevolucaoEscalaRequest? request,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);

        var solicitacao = await db.SolicitacoesDevolucaoEscala
            .Include(x => x.Escala).ThenInclude(x => x.Setor)
            .Include(x => x.SolicitanteUsuario).ThenInclude(x => x.Servidor)
            .FirstOrDefaultAsync(x => x.Id == solicitacaoId, cancellationToken);

        if (solicitacao is null)
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure("Solicitação não encontrada.");
        }

        if (!actor.PodeAcessar(PermissionCodes.EscalasDevolver, solicitacao.Escala.SetorId))
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure("Sem permissão para responder devoluções.");
        }

        try
        {
            if (aprovar)
            {
                solicitacao.Aprovar(actorLogin, request?.ObservacaoResposta);
                solicitacao.Escala.DevolverParaFinalizada(actorLogin);
            }
            else
            {
                solicitacao.Recusar(actorLogin, request?.ObservacaoResposta);
                solicitacao.Escala.CancelarSolicitacaoDevolucao(actorLogin);
            }
        }
        catch (Exception ex)
        {
            return Result<SolicitacaoDevolucaoEscalaDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result<SolicitacaoDevolucaoEscalaDto>.Success(new SolicitacaoDevolucaoEscalaDto(
            solicitacao.Id,
            solicitacao.EscalaId,
            Escala.FormatIdentificacao(solicitacao.Escala.Mes, solicitacao.Escala.Ano, solicitacao.Escala.Setor.Nome),
            solicitacao.Escala.SetorId,
            solicitacao.Escala.Setor.Nome,
            solicitacao.Escala.Setor.Sigla,
            solicitacao.Escala.Ano,
            solicitacao.Escala.Mes,
            solicitacao.SolicitanteUsuarioId,
            solicitacao.SolicitanteUsuario.Servidor.Nome,
            solicitacao.Justificativa,
            solicitacao.Status,
            solicitacao.RespondidoPor,
            solicitacao.RespostaEm,
            solicitacao.ObservacaoResposta,
            solicitacao.CreatedAt));
    }

    public async Task<Result<EscalaDetailDto>> CopiarAsync(
        Guid id,
        CopiarEscalaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var origem = await LoadDetailQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (origem is null)
        {
            return Result<EscalaDetailDto>.Failure("Escala de origem não encontrada.");
        }

        if (!CanMutate(actor, origem.SetorId))
        {
            return Result<EscalaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        if (await db.Escalas.AnyAsync(
                x => x.SetorId == origem.SetorId && x.Ano == request.Ano && x.Mes == request.Mes,
                cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Já existe escala para este setor no mês/ano de destino.");
        }

        Escala nova;
        try
        {
            nova = Escala.Create(origem.SetorId, request.Ano, request.Mes, origem.TipoFuncionamento, origem.Observacao, actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaDetailDto>.Failure(ex.Message);
        }

        db.Escalas.Add(nova);
        await db.SaveChangesAsync(cancellationToken);

        var deltaMonths = (request.Ano - origem.Ano) * 12 + (request.Mes - origem.Mes);
        var destinoInicio = nova.DataInicio;
        var destinoFim = nova.DataFim;
        var servidorIds = origem.Servidores.Select(x => x.ServidorId).ToList();
        var servidoresAtuais = await db.Servidores
            .Include(x => x.Cargo)
            .Where(x => servidorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var src in origem.Servidores.OrderBy(x => x.Ordem))
        {
            if (!servidoresAtuais.TryGetValue(src.ServidorId, out var servidor))
            {
                continue;
            }

            var dest = EscalaServidor.Create(
                nova.Id,
                servidor.Id,
                servidor.CargoId,
                src.Ordem,
                servidor.Nome,
                servidor.Matricula,
                servidor.Cargo.Nome,
                servidor.Cargo.Codigo,
                actorLogin);

            db.EscalaServidores.Add(dest);
            await db.SaveChangesAsync(cancellationToken);

            // Preserva a âncora do ciclo (DataInicioCiclo) e o padrão de origem; a janela da
            // jornada é regenerada para cobrir integralmente o mês de destino.
            foreach (var jornadaSrc in src.Jornadas)
            {
                var jornada = EscalaJornada.Create(
                    dest.Id,
                    jornadaSrc.TipoJornada,
                    destinoInicio,
                    destinoFim,
                    jornadaSrc.TipoOcorrenciaCodigo,
                    jornadaSrc.RecorrenciaTipo,
                    jornadaSrc.HoraInicio,
                    jornadaSrc.HoraFim,
                    jornadaSrc.Horas,
                    jornadaSrc.DiasSemana,
                    jornadaSrc.IntervaloDias,
                    jornadaSrc.DiasTrabalho,
                    jornadaSrc.DiasFolga,
                    jornadaSrc.TipoOcorrenciaFolgaCodigo,
                    jornadaSrc.Observacao,
                    jornadaSrc.PadraoEscalaId,
                    jornadaSrc.DataInicioCiclo,
                    actorLogin);

                db.EscalaJornadas.Add(jornada);
                await db.SaveChangesAsync(cancellationToken);
                await ApplyJornadaAsync(dest, jornada, actorLogin, cancellationToken);
            }

            if (!request.SobrescreverManuais)
            {
                foreach (var oc in src.Ocorrencias.Where(x => x.Origem == OrigemOcorrencia.Manual))
                {
                    var data = ShiftMonth(oc.Data, deltaMonths, destinoInicio, destinoFim);
                    if (data is null)
                    {
                        continue;
                    }

                    var existing = await db.EscalaOcorrencias
                        .FirstOrDefaultAsync(x => x.EscalaServidorId == dest.Id && x.Data == data, cancellationToken);

                    if (existing is null)
                    {
                        db.EscalaOcorrencias.Add(EscalaOcorrencia.Create(
                            dest.Id,
                            data.Value,
                            oc.TipoOcorrenciaCodigo,
                            OrigemOcorrencia.Manual,
                            oc.HoraInicio,
                            oc.HoraFim,
                            oc.Horas,
                            observacao: oc.Observacao,
                            createdBy: actorLogin));
                    }
                    else
                    {
                        existing.AtualizarManual(
                            oc.TipoOcorrenciaCodigo,
                            oc.HoraInicio,
                            oc.HoraFim,
                            oc.Horas,
                            oc.Observacao,
                            actorLogin);
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(nova.Id, actorLogin, cancellationToken);
    }

    public async Task<IReadOnlyList<TipoOcorrenciaDto>> ListTiposOcorrenciaAsync(CancellationToken cancellationToken = default) =>
        await db.TiposOcorrencia
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Codigo)
            .Select(x => new TipoOcorrenciaDto(x.Codigo, x.Nome, x.HorasPadrao, x.Categoria, x.Ativo))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PadraoEscalaDto>> ListPadroesAsync(
        TipoFuncionamento? tipo,
        CancellationToken cancellationToken = default)
    {
        var q = db.PadroesEscala.AsNoTracking().Where(x => x.Ativo);
        if (tipo is TipoFuncionamento tipoFuncionamento)
        {
            q = q.Where(x => x.TipoFuncionamento == tipoFuncionamento);
        }

        return await q
            .OrderBy(x => x.Nome)
            .Select(x => new PadraoEscalaDto(
                x.Id,
                x.Codigo,
                x.Nome,
                x.TipoFuncionamento,
                x.TipoJornada,
                x.RecorrenciaTipo,
                x.DiasTrabalho,
                x.DiasFolga,
                x.DiasSemana,
                x.TipoOcorrenciaTrabalho,
                x.TipoOcorrenciaFolga,
                x.HoraInicioPadrao,
                x.HoraFimPadrao,
                x.HorasPadrao,
                x.Sistema,
                x.Ativo))
            .ToListAsync(cancellationToken);
    }

    public async Task<EscalaAnteriorInfoDto?> GetEscalaAnteriorAsync(
        Guid setorId,
        int ano,
        int mes,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!CanMutate(actor, setorId))
        {
            return null;
        }

        var mesAnterior = new DateOnly(ano, mes, 1).AddMonths(-1);
        var anterior = await db.Escalas
            .AsNoTracking()
            .Include(x => x.Setor)
            .Include(x => x.Servidores)
            .FirstOrDefaultAsync(
                x => x.SetorId == setorId && x.Ano == mesAnterior.Year && x.Mes == mesAnterior.Month,
                cancellationToken);

        if (anterior is null)
        {
            return null;
        }

        return new EscalaAnteriorInfoDto(
            anterior.Id,
            anterior.Ano,
            anterior.Mes,
            Escala.FormatIdentificacao(anterior.Mes, anterior.Ano, anterior.Setor.Nome),
            anterior.TipoFuncionamento,
            anterior.Status,
            anterior.Servidores.Count);
    }

    public async Task<Result<EscalaDetailDto>> GerarEscalaAsync(
        Guid id,
        GerarEscalaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken, includeDeep: true);
        if (error is not null)
        {
            return Result<EscalaDetailDto>.Failure(error);
        }

        var itens = request.Itens ?? [];
        if (itens.Count == 0)
        {
            return Result<EscalaDetailDto>.Failure("Informe ao menos um servidor para gerar a escala.");
        }

        var padraoIds = itens.Select(x => x.PadraoEscalaId).Distinct().ToList();
        var padroes = await db.PadroesEscala
            .AsNoTracking()
            .Where(x => padraoIds.Contains(x.Id) && x.Ativo)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (padroes.Count != padraoIds.Count)
        {
            return Result<EscalaDetailDto>.Failure("Há padrões de escala inválidos ou inativos.");
        }

        var servidorIdsNecessarios = itens.Select(x => x.ServidorId).Distinct().ToList();
        var jaNaEscala = escala!.Servidores.Select(x => x.ServidorId).ToHashSet();
        var faltantes = servidorIdsNecessarios.Where(x => !jaNaEscala.Contains(x)).ToList();

        if (faltantes.Count > 0)
        {
            var novos = await db.Servidores
                .AsNoTracking()
                .Where(x => faltantes.Contains(x.Id) && x.SetorId == escala.SetorId)
                .Select(x => new
                {
                    x.Id,
                    x.CargoId,
                    x.Nome,
                    x.Matricula,
                    CargoNome = x.Cargo.Nome,
                    CargoCodigo = x.Cargo.Codigo,
                })
                .ToListAsync(cancellationToken);

            if (novos.Count != faltantes.Count)
            {
                return Result<EscalaDetailDto>.Failure("Há servidores inválidos ou de outro setor.");
            }

            var maxOrdem = escala.Servidores.Count == 0 ? 0 : escala.Servidores.Max(x => x.Ordem);
            var ordem = maxOrdem + 1;
            foreach (var novo in novos)
            {
                var criado = EscalaServidor.Create(
                    escala.Id,
                    novo.Id,
                    novo.CargoId,
                    ordem++,
                    novo.Nome,
                    novo.Matricula,
                    novo.CargoNome,
                    novo.CargoCodigo,
                    actorLogin);

                db.EscalaServidores.Add(criado);
                escala.Servidores.Add(criado);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        for (var i = 0; i < itens.Count; i++)
        {
            var itemRequest = itens[i];
            var escalaServidor = escala.Servidores.First(x => x.ServidorId == itemRequest.ServidorId);
            var padrao = padroes[itemRequest.PadraoEscalaId];

            var jornadasAntigas = escalaServidor.Jornadas.ToList();
            if (jornadasAntigas.Count > 0)
            {
                var idsAntigas = jornadasAntigas.Select(x => x.Id).ToHashSet();
                var ocorrenciasRegra = escalaServidor.Ocorrencias
                    .Where(x => x.EscalaJornadaId.HasValue
                                && idsAntigas.Contains(x.EscalaJornadaId.Value)
                                && x.Origem == OrigemOcorrencia.Regra)
                    .ToList();

                db.EscalaOcorrencias.RemoveRange(ocorrenciasRegra);
                foreach (var ocorrencia in ocorrenciasRegra)
                {
                    escalaServidor.Ocorrencias.Remove(ocorrencia);
                }

                db.EscalaJornadas.RemoveRange(jornadasAntigas);
                foreach (var jornadaAntiga in jornadasAntigas)
                {
                    escalaServidor.Jornadas.Remove(jornadaAntiga);
                }
            }

            // Escalona a âncora do ciclo por índice para distribuir automaticamente os servidores
            // ao longo do ciclo de trabalho/folga, evitando que todos iniciem no mesmo dia.
            var dataInicioCiclo = request.DistribuirAutomaticamente
                ? (request.DataBaseDistribuicao ?? escala.DataInicio).AddDays(i)
                : itemRequest.DataInicioCiclo;

            EscalaJornada jornada;
            try
            {
                jornada = EscalaJornada.Create(
                    escalaServidor.Id,
                    padrao.TipoJornada,
                    escala.DataInicio,
                    escala.DataFim,
                    padrao.TipoOcorrenciaTrabalho,
                    padrao.RecorrenciaTipo,
                    itemRequest.HoraInicio ?? padrao.HoraInicioPadrao,
                    itemRequest.HoraFim ?? padrao.HoraFimPadrao,
                    padrao.HorasPadrao,
                    padrao.DiasSemana,
                    null,
                    padrao.DiasTrabalho,
                    padrao.DiasFolga,
                    padrao.TipoOcorrenciaFolga,
                    null,
                    padrao.Id,
                    dataInicioCiclo,
                    actorLogin);
            }
            catch (Exception ex)
            {
                return Result<EscalaDetailDto>.Failure(ex.Message);
            }

            db.EscalaJornadas.Add(jornada);
            escalaServidor.Jornadas.Add(jornada);
            await db.SaveChangesAsync(cancellationToken);
            await ApplyJornadaAsync(escalaServidor, jornada, actorLogin, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await ApplyAfastamentosToEscalaAsync(escala, actorLogin, cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaDetailDto>> AplicarAfastamentosAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken, includeDeep: true);
        if (error is not null)
        {
            return Result<EscalaDetailDto>.Failure(error);
        }

        await ApplyAfastamentosToEscalaAsync(escala!, actorLogin, cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    private async Task ApplyAfastamentosToEscalaAsync(
        Escala escala,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        var servidorIds = escala.Servidores.Select(x => x.ServidorId).ToList();
        if (servidorIds.Count == 0)
        {
            return;
        }

        var afastamentos = await db.Afastamentos
            .AsNoTracking()
            .Where(x => servidorIds.Contains(x.ServidorId)
                        && x.DataInicio <= escala.DataFim
                        && x.DataFim >= escala.DataInicio)
            .ToListAsync(cancellationToken);

        if (afastamentos.Count == 0)
        {
            return;
        }

        var tipos = await db.TiposOcorrencia
            .AsNoTracking()
            .Where(x => x.Ativo)
            .ToDictionaryAsync(x => x.Codigo, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var afastamento in afastamentos)
        {
            var escalaServidor = escala.Servidores.FirstOrDefault(x => x.ServidorId == afastamento.ServidorId);
            if (escalaServidor is null)
            {
                continue;
            }

            var inicio = afastamento.DataInicio < escala.DataInicio ? escala.DataInicio : afastamento.DataInicio;
            var fim = afastamento.DataFim > escala.DataFim ? escala.DataFim : afastamento.DataFim;
            var horas = tipos.GetValueOrDefault(afastamento.TipoOcorrenciaCodigo)?.HorasPadrao;
            // Observacao da ocorrência guarda o SEI para exibição no PDF: "Tipo conforme SEI {n}"
            var seiObs = string.IsNullOrWhiteSpace(afastamento.Sei) ? null : afastamento.Sei.Trim();

            for (var data = inicio; data <= fim; data = data.AddDays(1))
            {
                var existing = escalaServidor.Ocorrencias.FirstOrDefault(x => x.Data == data);
                if (existing is null)
                {
                    var criada = EscalaOcorrencia.Create(
                        escalaServidor.Id,
                        data,
                        afastamento.TipoOcorrenciaCodigo,
                        OrigemOcorrencia.Manual,
                        null,
                        null,
                        horas,
                        null,
                        seiObs,
                        actorLogin);
                    db.EscalaOcorrencias.Add(criada);
                    escalaServidor.Ocorrencias.Add(criada);
                }
                else
                {
                    existing.AtualizarManual(
                        afastamento.TipoOcorrenciaCodigo,
                        null,
                        null,
                        horas,
                        seiObs,
                        actorLogin);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<EscalaCoberturaDto>> GetCoberturaAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadReadOnlyDeepAsync(id, actorLogin, cancellationToken);
        if (error is not null)
        {
            return Result<EscalaCoberturaDto>.Failure(error);
        }

        var categorias = await LoadCategoriasAsync(cancellationToken);
        var dias = new List<EscalaCoberturaDiaDto>();

        for (var data = escala!.DataInicio; data <= escala.DataFim; data = data.AddDays(1))
        {
            var nomesTrabalho = escala.Servidores
                .Where(s => s.Ocorrencias.Any(o => o.Data == data && IsTrabalho(o.TipoOcorrenciaCodigo, categorias)))
                .Select(s => s.ServidorNome)
                .OrderBy(x => x)
                .ToList();

            var temCobertura = escala.TipoFuncionamento != TipoFuncionamento.VinteQuatroHoras || nomesTrabalho.Count > 0;
            dias.Add(new EscalaCoberturaDiaDto(data, nomesTrabalho, temCobertura));
        }

        return Result<EscalaCoberturaDto>.Success(new EscalaCoberturaDto(escala.Id, escala.TipoFuncionamento, dias));
    }

    public async Task<Result<EscalaConflitosDto>> GetConflitosAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadReadOnlyDeepAsync(id, actorLogin, cancellationToken);
        if (error is not null)
        {
            return Result<EscalaConflitosDto>.Failure(error);
        }

        var dto = await BuildConflitosAsync(escala!, cancellationToken);
        return Result<EscalaConflitosDto>.Success(dto);
    }

    public async Task<Result<EscalaDetailDto>> UpsertOcorrenciasLoteAsync(
        Guid id,
        UpsertOcorrenciasLoteRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken, includeDeep: true);
        if (error is not null)
        {
            return Result<EscalaDetailDto>.Failure(error);
        }

        if (request.DataFim < request.DataInicio)
        {
            return Result<EscalaDetailDto>.Failure("Período inválido.");
        }

        if (request.DataInicio < escala!.DataInicio || request.DataFim > escala.DataFim)
        {
            return Result<EscalaDetailDto>.Failure("O período deve estar dentro da escala.");
        }

        var servidorIds = (request.ServidorIds ?? []).Distinct().ToList();
        if (servidorIds.Count == 0)
        {
            return Result<EscalaDetailDto>.Failure("Informe ao menos um servidor.");
        }

        var escalaServidores = escala.Servidores.Where(x => servidorIds.Contains(x.ServidorId)).ToList();
        if (escalaServidores.Count != servidorIds.Count)
        {
            return Result<EscalaDetailDto>.Failure("Há servidores que não estão na escala.");
        }

        if (!await db.TiposOcorrencia.AnyAsync(x => x.Codigo == request.TipoOcorrenciaCodigo && x.Ativo, cancellationToken))
        {
            return Result<EscalaDetailDto>.Failure("Tipo de ocorrência inválido.");
        }

        var codigo = request.TipoOcorrenciaCodigo.Trim().ToUpperInvariant();

        if (!request.ConfirmarSobrescrita)
        {
            foreach (var escalaServidor in escalaServidores)
            {
                for (var data = request.DataInicio; data <= request.DataFim; data = data.AddDays(1))
                {
                    var existing = escalaServidor.Ocorrencias.FirstOrDefault(x => x.Data == data);
                    if (existing is not null && OcorrenciaDifere(existing, codigo, request))
                    {
                        return Result<EscalaDetailDto>.Failure(
                            "Existem ocorrências já lançadas no período que serão sobrescritas. Confirme para continuar.");
                    }
                }
            }
        }

        foreach (var escalaServidor in escalaServidores)
        {
            for (var data = request.DataInicio; data <= request.DataFim; data = data.AddDays(1))
            {
                var existing = escalaServidor.Ocorrencias.FirstOrDefault(x => x.Data == data);
                if (existing is null)
                {
                    escalaServidor.Ocorrencias.Add(EscalaOcorrencia.Create(
                        escalaServidor.Id,
                        data,
                        codigo,
                        OrigemOcorrencia.Manual,
                        request.HoraInicio,
                        request.HoraFim,
                        request.Horas,
                        observacao: request.Observacao,
                        createdBy: actorLogin));
                }
                else
                {
                    existing.AtualizarManual(codigo, request.HoraInicio, request.HoraFim, request.Horas, request.Observacao, actorLogin);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaDetailDto>> SyncOcorrenciasAsync(
        Guid id,
        SyncOcorrenciasRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await SyncOcorrenciasCoreAsync(id, request, actorLogin, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt == 0)
            {
                db.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<EscalaDetailDto>.Failure(
                    "Não foi possível salvar as ocorrências por conflito de concorrência. Tente novamente.");
            }
        }

        return Result<EscalaDetailDto>.Failure(
            "Não foi possível salvar as ocorrências por conflito de concorrência. Tente novamente.");
    }

    private async Task<Result<EscalaDetailDto>> SyncOcorrenciasCoreAsync(
        Guid id,
        SyncOcorrenciasRequest request,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        // Só servidores (sem ocorrências tracked) — replace via ExecuteDelete + Add.
        var (escala, error) = await LoadEditableAsync(id, actorLogin, cancellationToken, includeServidores: true);
        if (error is not null)
        {
            return Result<EscalaDetailDto>.Failure(error);
        }

        var itens = request.Itens ?? [];
        var porServidor = escala!.Servidores.ToDictionary(x => x.ServidorId);

        // Lista vazia = escala em branco (ex.: multi-regime): limpa o período.
        if (itens.Count == 0)
        {
            var allIds = porServidor.Values.Select(x => x.Id).ToList();
            if (allIds.Count > 0)
            {
                await db.EscalaOcorrencias
                    .Where(x => allIds.Contains(x.EscalaServidorId)
                                && x.Data >= escala.DataInicio
                                && x.Data <= escala.DataFim)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            return await GetByIdAsync(id, actorLogin, cancellationToken);
        }

        var codigos = itens.Select(x => x.TipoOcorrenciaCodigo.Trim().ToUpperInvariant()).Distinct().ToList();
        var tiposValidos = await db.TiposOcorrencia
            .AsNoTracking()
            .Where(x => x.Ativo && codigos.Contains(x.Codigo))
            .Select(x => x.Codigo)
            .ToListAsync(cancellationToken);
        var tiposSet = tiposValidos.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Último item vence em caso de duplicata (servidorId + data).
        var dedup = new Dictionary<(Guid ServidorId, DateOnly Data), SyncOcorrenciaItemRequest>();
        foreach (var item in itens)
        {
            if (!porServidor.ContainsKey(item.ServidorId))
            {
                return Result<EscalaDetailDto>.Failure("Há servidores que não estão na escala.");
            }

            if (item.Data < escala.DataInicio || item.Data > escala.DataFim)
            {
                return Result<EscalaDetailDto>.Failure($"Data {item.Data:dd/MM/yyyy} fora do período da escala.");
            }

            var codigo = item.TipoOcorrenciaCodigo.Trim().ToUpperInvariant();
            if (!tiposSet.Contains(codigo))
            {
                return Result<EscalaDetailDto>.Failure($"Tipo de ocorrência inválido: {codigo}.");
            }

            dedup[(item.ServidorId, item.Data)] = item;
        }

        var touchedServidorIds = dedup.Keys.Select(k => k.ServidorId).Distinct().ToList();
        var escalaServidorIds = touchedServidorIds.Select(sid => porServidor[sid].Id).ToList();
        await db.EscalaOcorrencias
            .Where(x => escalaServidorIds.Contains(x.EscalaServidorId)
                        && x.Data >= escala.DataInicio
                        && x.Data <= escala.DataFim)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var item in dedup.Values)
        {
            var escalaServidor = porServidor[item.ServidorId];
            var codigo = item.TipoOcorrenciaCodigo.Trim().ToUpperInvariant();
            db.EscalaOcorrencias.Add(EscalaOcorrencia.Create(
                escalaServidor.Id,
                item.Data,
                codigo,
                OrigemOcorrencia.Manual,
                item.HoraInicio,
                item.HoraFim,
                item.Horas,
                observacao: item.Observacao,
                createdBy: actorLogin));
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    private static bool OcorrenciaDifere(EscalaOcorrencia existing, string codigo, UpsertOcorrenciasLoteRequest request) =>
        !string.Equals(existing.TipoOcorrenciaCodigo, codigo, StringComparison.OrdinalIgnoreCase)
        || existing.HoraInicio != request.HoraInicio
        || existing.HoraFim != request.HoraFim
        || existing.Horas != request.Horas;

    private async Task<EscalaConflitosDto> BuildConflitosAsync(Escala escala, CancellationToken cancellationToken)
    {
        var categorias = await LoadCategoriasAsync(cancellationToken);
        var itens = new List<EscalaConflitoDto>();

        for (var data = escala.DataInicio; data <= escala.DataFim; data = data.AddDays(1))
        {
            var temTrabalho = escala.Servidores
                .Any(s => s.Ocorrencias.Any(o => o.Data == data && IsTrabalho(o.TipoOcorrenciaCodigo, categorias)));

            if (escala.TipoFuncionamento == TipoFuncionamento.VinteQuatroHoras && !temTrabalho)
            {
                itens.Add(new EscalaConflitoDto(
                    "LacunaCobertura",
                    true,
                    null,
                    null,
                    data,
                    $"Nenhum servidor em cobertura em {data:dd/MM/yyyy}."));
            }

            foreach (var servidor in escala.Servidores.Where(s => s.Jornadas.Count > 0))
            {
                var ocorrencia = servidor.Ocorrencias.FirstOrDefault(o => o.Data == data);
                if (ocorrencia is null)
                {
                    itens.Add(new EscalaConflitoDto(
                        "DiaSemOcorrencia",
                        false,
                        servidor.ServidorId,
                        servidor.ServidorNome,
                        data,
                        $"{servidor.ServidorNome} está sem ocorrência definida em {data:dd/MM/yyyy}."));
                }
            }
        }

        var servidorIds = escala.Servidores.Select(x => x.ServidorId).Distinct().ToList();
        if (servidorIds.Count > 0)
        {
            var ocorrenciasOutrasEscalas = await db.EscalaOcorrencias
                .AsNoTracking()
                .Where(o => o.EscalaServidor.EscalaId != escala.Id
                            && o.EscalaServidor.Escala.Status == StatusEscala.Publicada
                            && servidorIds.Contains(o.EscalaServidor.ServidorId)
                            && o.Data >= escala.DataInicio
                            && o.Data <= escala.DataFim)
                .Select(o => new { o.EscalaServidor.ServidorId, o.Data, o.TipoOcorrenciaCodigo })
                .ToListAsync(cancellationToken);

            var trabalhoEmOutrasEscalas = ocorrenciasOutrasEscalas
                .Where(o => IsTrabalho(o.TipoOcorrenciaCodigo, categorias))
                .Select(o => (o.ServidorId, o.Data))
                .ToHashSet();

            if (trabalhoEmOutrasEscalas.Count > 0)
            {
                foreach (var servidor in escala.Servidores)
                {
                    foreach (var ocorrencia in servidor.Ocorrencias.Where(o => IsTrabalho(o.TipoOcorrenciaCodigo, categorias)))
                    {
                        if (trabalhoEmOutrasEscalas.Contains((servidor.ServidorId, ocorrencia.Data)))
                        {
                            itens.Add(new EscalaConflitoDto(
                                "ConflitoCruzado",
                                true,
                                servidor.ServidorId,
                                servidor.ServidorNome,
                                ocorrencia.Data,
                                $"{servidor.ServidorNome} já está em outra escala publicada em {ocorrencia.Data:dd/MM/yyyy}."));
                        }
                    }
                }
            }
        }

        var ordenados = itens
            .OrderByDescending(x => x.Critico)
            .ThenBy(x => x.Data)
            .ToList();

        return new EscalaConflitosDto(escala.Id, ordenados, ordenados.Count(x => x.Critico));
    }

    private async Task<Dictionary<string, CategoriaOcorrencia>> LoadCategoriasAsync(CancellationToken cancellationToken) =>
        await db.TiposOcorrencia
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Codigo, x => x.Categoria, cancellationToken);

    private static bool IsTrabalho(string codigo, IReadOnlyDictionary<string, CategoriaOcorrencia> categorias) =>
        categorias.TryGetValue(codigo, out var categoria)
            ? categoria == CategoriaOcorrencia.Trabalho
            : !FolgaCodes.Contains(codigo);

    private async Task ApplyJornadaAsync(
        EscalaServidor escalaServidor,
        EscalaJornada jornada,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        var existing = await db.EscalaOcorrencias
            .Where(x => x.EscalaServidorId == escalaServidor.Id)
            .ToListAsync(cancellationToken);

        foreach (var (data, codigo, isTrabalho) in EscalaJornadaExpander.Expand(jornada))
        {
            var current = existing.FirstOrDefault(x => x.Data == data);
            if (current is not null && current.Origem == OrigemOcorrencia.Manual)
            {
                continue;
            }

            var horaInicio = isTrabalho ? jornada.HoraInicio : null;
            var horaFim = isTrabalho ? jornada.HoraFim : null;
            var horas = isTrabalho ? jornada.Horas : null;

            if (current is null)
            {
                var created = EscalaOcorrencia.Create(
                    escalaServidor.Id,
                    data,
                    codigo,
                    OrigemOcorrencia.Regra,
                    horaInicio,
                    horaFim,
                    horas,
                    jornada.Id,
                    jornada.Observacao,
                    actorLogin);
                db.EscalaOcorrencias.Add(created);
                existing.Add(created);
            }
            else
            {
                current.AtualizarPorRegra(codigo, horaInicio, horaFim, horas, jornada.Id, jornada.Observacao, actorLogin);
            }
        }
    }

    private static DateOnly? ShiftMonth(DateOnly original, int deltaMonths, DateOnly min, DateOnly max)
    {
        var shifted = original.AddMonths(deltaMonths);
        if (shifted < min || shifted > max)
        {
            return null;
        }

        return shifted;
    }

    private async Task<(Escala? Escala, string? Error)> LoadEditableAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken,
        bool includeServidores = false,
        bool includeDeep = false)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        IQueryable<Escala> query = db.Escalas;
        if (includeDeep)
        {
            query = query
                .Include(x => x.Servidores).ThenInclude(x => x.Ocorrencias)
                .Include(x => x.Servidores).ThenInclude(x => x.Jornadas);
        }
        else if (includeServidores)
        {
            query = query.Include(x => x.Servidores);
        }

        var escala = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return (null, "Escala não encontrada.");
        }

        if (!CanMutate(actor, escala.SetorId))
        {
            return (null, "Sem permissão para esta escala.");
        }

        if (!IsEditableStatus(escala.Status))
        {
            return (null, "A escala só pode ser alterada enquanto estiver em rascunho ou finalizada.");
        }

        return (escala, null);
    }

    private async Task<(Escala? Escala, string? Error)> LoadReadOnlyDeepAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.Escalas
            .AsNoTracking()
            .Include(x => x.Servidores).ThenInclude(x => x.Jornadas)
            .Include(x => x.Servidores).ThenInclude(x => x.Ocorrencias)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (escala is null)
        {
            return (null, "Escala não encontrada.");
        }

        if (!CanView(actor, escala.SetorId))
        {
            return (null, "Sem permissão para esta escala.");
        }

        return (escala, null);
    }

    private IQueryable<Escala> LoadDetailQuery() =>
        db.Escalas
            .AsNoTracking()
            .Include(x => x.Setor)
            .Include(x => x.Servidores).ThenInclude(x => x.Cargo)
            .Include(x => x.Servidores).ThenInclude(x => x.Jornadas).ThenInclude(x => x.PadraoEscala)
            .Include(x => x.Servidores).ThenInclude(x => x.Ocorrencias).ThenInclude(x => x.TipoOcorrencia);

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

    private static bool CanMutate(ActorContext actor, Guid setorId) =>
        actor.PodeAcessar(PermissionCodes.EscalasEditar, setorId);

    private static bool CanSolicitarDevolucao(ActorContext actor, Guid setorId) =>
        actor.PodeAcessar(PermissionCodes.EscalasSolicitarDevolucao, setorId);

    private static bool CanView(ActorContext actor, Guid setorId) =>
        actor.PodeVer(PermissionCodes.EscalasListar, setorId);

    private static bool IsEditableStatus(StatusEscala status) =>
        status is StatusEscala.Rascunho or StatusEscala.Finalizada;

    private async Task<string?> ResolveResponsavelNomeAsync(
        string? createdByLogin,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(createdByLogin))
        {
            return createdByLogin;
        }

        var map = await ResolveResponsavelNomesAsync(
            [createdByLogin.Trim().ToLowerInvariant()],
            cancellationToken);
        return ResolveResponsavelFromMap(createdByLogin, map);
    }

    private async Task<Dictionary<string, string>> ResolveResponsavelNomesAsync(
        IReadOnlyList<string> logins,
        CancellationToken cancellationToken)
    {
        if (logins.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return await db.Usuarios
            .AsNoTracking()
            .Where(u => logins.Contains(u.Login))
            .Select(u => new { u.Login, Nome = u.Servidor.Nome })
            .ToDictionaryAsync(x => x.Login, x => x.Nome, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private static string? ResolveResponsavelFromMap(
        string? createdByLogin,
        IReadOnlyDictionary<string, string> responsavelByLogin)
    {
        if (string.IsNullOrWhiteSpace(createdByLogin))
        {
            return createdByLogin;
        }

        var key = createdByLogin.Trim().ToLowerInvariant();
        return responsavelByLogin.TryGetValue(key, out var nome) ? nome : createdByLogin;
    }

    private static EscalaDetailDto MapDetail(Escala escala)
    {
        static decimal HorasDe(EscalaOcorrencia o) =>
            o.Horas ?? o.TipoOcorrencia?.HorasPadrao ?? 0m;

        static bool IsRemota(string codigo) =>
            codigo.StartsWith("TL", StringComparison.OrdinalIgnoreCase);

        static bool ContaCarga(string codigo, EscalaOcorrencia o) =>
            HorasDe(o) > 0 && !string.Equals(codigo, "D", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(codigo, "F", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(codigo, "FR", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(codigo, "LP", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(codigo, "LM", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(codigo, "LO", StringComparison.OrdinalIgnoreCase);

        var servidores = escala.Servidores
            .OrderBy(x => x.Ordem)
            .Select(s =>
            {
                var presencial = s.Ocorrencias
                    .Where(o => ContaCarga(o.TipoOcorrenciaCodigo, o) && !IsRemota(o.TipoOcorrenciaCodigo))
                    .Sum(HorasDe);
                var remota = s.Ocorrencias
                    .Where(o => ContaCarga(o.TipoOcorrenciaCodigo, o) && IsRemota(o.TipoOcorrenciaCodigo))
                    .Sum(HorasDe);

                // Preferir sigla atual do Cargo (ATF); snapshot antigo pode ter o nome completo.
                var cargoCodigo = !string.IsNullOrWhiteSpace(s.Cargo?.Codigo)
                    ? s.Cargo.Codigo
                    : s.CargoCodigo;
                var cargoNome = !string.IsNullOrWhiteSpace(s.Cargo?.Nome)
                    ? s.Cargo.Nome
                    : s.CargoNome;

                return new EscalaServidorDto(
                    s.Id,
                    s.ServidorId,
                    s.CargoId,
                    s.Ordem,
                    s.ServidorNome,
                    s.Matricula,
                    cargoNome,
                    cargoCodigo,
                    presencial,
                    remota,
                    s.Jornadas
                        .OrderBy(j => j.DataInicio)
                        .Select(j => new EscalaJornadaDto(
                            j.Id,
                            j.PadraoEscalaId,
                            j.PadraoEscala?.Nome,
                            j.TipoJornada,
                            j.DataInicio,
                            j.DataFim,
                            j.DataInicioCiclo,
                            j.HoraInicio,
                            j.HoraFim,
                            j.Horas,
                            j.TipoOcorrenciaCodigo,
                            j.RecorrenciaTipo,
                            j.DiasSemana,
                            j.IntervaloDias,
                            j.DiasTrabalho,
                            j.DiasFolga,
                            j.TipoOcorrenciaFolgaCodigo,
                            j.Observacao))
                        .ToList(),
                    s.Ocorrencias
                        .OrderBy(o => o.Data)
                        .Select(o => new EscalaOcorrenciaDto(
                            o.Id,
                            o.Data,
                            o.TipoOcorrenciaCodigo,
                            o.TipoOcorrencia?.Nome,
                            o.HoraInicio,
                            o.HoraFim,
                            o.Horas,
                            o.Origem,
                            o.EscalaJornadaId,
                            o.Observacao))
                        .ToList());
            })
            .ToList();

        return new(
            escala.Id,
            Escala.FormatIdentificacao(escala.Mes, escala.Ano, escala.Setor.Nome),
            escala.SetorId,
            escala.Setor.Nome,
            escala.Setor.Sigla,
            escala.Ano,
            escala.Mes,
            escala.DataInicio,
            escala.DataFim,
            escala.TipoFuncionamento,
            escala.Status,
            escala.Observacao,
            escala.PublicadaEm,
            escala.PublicadaPor,
            escala.CreatedAt,
            escala.CreatedBy,
            servidores.Sum(x => x.CargaHorariaPresencial),
            servidores.Sum(x => x.CargaHorariaRemota),
            servidores);
    }
}
