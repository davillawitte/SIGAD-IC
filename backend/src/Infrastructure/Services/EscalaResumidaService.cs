using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class EscalaResumidaService(ApplicationDbContext db) : IEscalaResumidaService
{
    public async Task<PagedResult<EscalaResumidaListItemDto>> ListAsync(
        EscalaResumidaListQuery query,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var normalized = query.Normalize();

        var q = db.EscalasResumidas.AsNoTracking().Include(x => x.Nucleo).Include(x => x.Setor).AsQueryable();

        if (!actor.TemVisaoGlobal(PermissionModules.Escalas))
        {
            var meusNucleos = actor.NucleosGerenciadosIds;
            var meusSetores = actor.SetoresGerenciadosIds
                .Concat(actor.SetoresDosNucleosGerenciadosIds)
                .Distinct()
                .ToList();
            if (meusNucleos.Count == 0 && meusSetores.Count == 0)
            {
                return PagedResult<EscalaResumidaListItemDto>.Empty(normalized.Page, normalized.PageSize);
            }

            q = q.Where(x =>
                (x.NucleoId != null && meusNucleos.Contains(x.NucleoId.Value)) ||
                (x.SetorId != null && meusSetores.Contains(x.SetorId.Value)));
        }

        if (query.NucleoId is Guid nucleoId)
        {
            q = q.Where(x => x.NucleoId == nucleoId);
        }

        if (query.SetorId is Guid setorIdFilter)
        {
            q = q.Where(x => x.SetorId == setorIdFilter);
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
                (x.Nucleo != null && (x.Nucleo.Nome.ToLower().Contains(term) || x.Nucleo.Sigla.ToLower().Contains(term))) ||
                (x.Setor != null && (x.Setor.Nome.ToLower().Contains(term) || x.Setor.Sigla.ToLower().Contains(term))));
        }

        q = q.OrderByDescending(x => x.Ano).ThenByDescending(x => x.Mes);

        var totalItems = await q.CountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<EscalaResumidaListItemDto>.Empty(normalized.Page, normalized.PageSize);
        }

        var pageItems = await q
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(x => new
            {
                x.Id,
                x.NucleoId,
                NucleoNome = x.Nucleo != null ? x.Nucleo.Nome : null,
                NucleoSigla = x.Nucleo != null ? x.Nucleo.Sigla : null,
                x.SetorId,
                SetorNome = x.Setor != null ? x.Setor.Nome : null,
                SetorSigla = x.Setor != null ? x.Setor.Sigla : null,
                x.Ano,
                x.Mes,
                x.Status,
                x.CreatedAt,
                x.CreatedBy,
                QuantidadeSetores = x.Setores.Count,
                SetoresSiglas = x.Setores
                    .OrderBy(s => s.Ordem)
                    .Select(s => s.SetorSiglaSnapshot)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var items = pageItems.Select(x =>
        {
            var inicio = new DateOnly(x.Ano, x.Mes, 1);
            var fim = DateOnly.FromDateTime(new DateTime(x.Ano, x.Mes, 1).AddMonths(1).AddDays(-1));
            var containerNome = x.SetorNome ?? x.NucleoNome ?? string.Empty;
            return new EscalaResumidaListItemDto(
                x.Id,
                EscalaResumida.FormatIdentificacao(x.Mes, x.Ano, containerNome),
                x.NucleoId,
                x.NucleoNome,
                x.NucleoSigla,
                x.Ano,
                x.Mes,
                inicio,
                fim,
                x.Status,
                x.CreatedAt,
                x.CreatedBy,
                x.QuantidadeSetores,
                x.SetoresSiglas,
                x.SetorId,
                x.SetorNome,
                x.SetorSigla);
        }).ToList();

        return PagedResult<EscalaResumidaListItemDto>.Create(items, normalized.Page, normalized.PageSize, totalItems);
    }

    public async Task<Result<EscalaResumidaDetailDto>> GetByIdAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await LoadDetailQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Escala resumida não encontrada.");
        }

        if (!CanView(actor, escala.NucleoId, escala.SetorId))
        {
            return Result<EscalaResumidaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        return Result<EscalaResumidaDetailDto>.Success(MapDetail(escala));
    }

    public async Task<Result<EscalaResumidaDetailDto>> CreateAsync(
        CreateEscalaResumidaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        if (request.NucleoId is null == request.SetorId is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Informe exatamente um: núcleo ou setor.");
        }

        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!CanMutate(actor, request.NucleoId, request.SetorId))
        {
            return Result<EscalaResumidaDetailDto>.Failure("Sem permissão para criar escala resumida.");
        }

        if (request.NucleoId is Guid nucleoId)
        {
            if (!await db.Nucleos.AnyAsync(x => x.Id == nucleoId, cancellationToken))
            {
                return Result<EscalaResumidaDetailDto>.Failure("Núcleo inválido.");
            }

            if (await db.EscalasResumidas.AnyAsync(
                    x => x.NucleoId == nucleoId && x.Ano == request.Ano && x.Mes == request.Mes,
                    cancellationToken))
            {
                return Result<EscalaResumidaDetailDto>.Failure("Já existe escala resumida para este núcleo no período.");
            }

            EscalaResumida escalaNucleo;
            try
            {
                escalaNucleo = EscalaResumida.Create(nucleoId, request.Ano, request.Mes, request.Observacao, actorLogin);
            }
            catch (Exception ex)
            {
                return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
            }

            db.EscalasResumidas.Add(escalaNucleo);
            await db.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(escalaNucleo.Id, actorLogin, cancellationToken);
        }

        var setorId = request.SetorId!.Value;
        var setor = await db.Setores.FirstOrDefaultAsync(x => x.Id == setorId, cancellationToken);
        if (setor is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Setor inválido.");
        }

        if (await db.EscalasResumidas.AnyAsync(
                x => x.SetorId == setorId && x.Ano == request.Ano && x.Mes == request.Mes,
                cancellationToken))
        {
            return Result<EscalaResumidaDetailDto>.Failure("Já existe escala resumida para este setor no período.");
        }

        EscalaResumida escalaSetor;
        try
        {
            escalaSetor = EscalaResumida.Create(
                null, request.Ano, request.Mes, request.Observacao, actorLogin, setorId: setorId);
        }
        catch (Exception ex)
        {
            return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
        }

        db.EscalasResumidas.Add(escalaSetor);

        // Resumida de setor único não tem etapa de "setores participantes" — o próprio setor
        // já é o único container, criado automaticamente aqui (ver ConfigurarSetoresAsync, que
        // continua sendo o fluxo pra resumida de núcleo).
        db.EscalaResumidaSetores.Add(EscalaResumidaSetor.Create(
            escalaSetor.Id, setor.Id, 0, setor.Nome, setor.Sigla, actorLogin));

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(escalaSetor.Id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> UpdateAsync(
        Guid id,
        UpdateEscalaResumidaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, actor, error) = await LoadEditableAsync(id, actorLogin, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        try
        {
            escala.Atualizar(request.Observacao, actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> ConfigurarSetoresAsync(
        Guid id,
        ConfigurarSetoresRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, actor, error) = await LoadEditableAsync(
            id, actorLogin, cancellationToken, x => x.Include(e => e.Setores));
        if (escala is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        if (escala.SetorId is not null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(
                "Escala resumida de setor único não usa configuração de setores participantes.");
        }

        if (request.Setores.Count(x => x.SetorId is null) > 1)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Só pode haver um grupo de Agentes.");
        }

        var setorIdsRequisitados = request.Setores
            .Where(x => x.SetorId is not null)
            .Select(x => x.SetorId!.Value)
            .ToList();
        var setoresValidos = await db.Setores
            .Where(x => setorIdsRequisitados.Contains(x.Id) && x.NucleoId == escala.NucleoId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (setoresValidos.Count != setorIdsRequisitados.Distinct().Count())
        {
            return Result<EscalaResumidaDetailDto>.Failure(
                "Há setores inválidos ou que não pertencem a este núcleo.");
        }

        // `Guid?` como chave de Dictionary/ToDictionary dispara CS8714 (falso positivo do
        // analisador de nulidade) — trata o grupo Agentes (SetorId nulo) à parte, já que só
        // pode haver um por escala.
        var existentesPorSetor = escala.Setores
            .Where(x => x.SetorId is not null)
            .ToDictionary(x => x.SetorId!.Value);
        var existenteAgentes = escala.Setores.FirstOrDefault(x => x.SetorId is null);
        var requisitadosIds = request.Setores.Select(x => x.SetorId).ToHashSet();

        foreach (var remover in escala.Setores.Where(x => !requisitadosIds.Contains(x.SetorId)).ToList())
        {
            db.EscalaResumidaSetores.Remove(remover);
        }

        foreach (var item in request.Setores)
        {
            if (item.SetorId is null)
            {
                if (existenteAgentes is not null)
                {
                    existenteAgentes.AtualizarOrdem(item.Ordem, actorLogin);
                    continue;
                }

                db.EscalaResumidaSetores.Add(EscalaResumidaSetor.Create(
                    escala.Id, null, item.Ordem, EscalaResumidaSetor.AgentesLabel, EscalaResumidaSetor.AgentesLabel, actorLogin));
                continue;
            }

            if (existentesPorSetor.TryGetValue(item.SetorId.Value, out var existente))
            {
                existente.AtualizarOrdem(item.Ordem, actorLogin);
                continue;
            }

            var setor = setoresValidos[item.SetorId.Value];
            db.EscalaResumidaSetores.Add(EscalaResumidaSetor.Create(
                escala.Id, setor.Id, item.Ordem, setor.Nome, setor.Sigla, actorLogin));
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> ConfigurarEquipeAsync(
        Guid id,
        ConfigurarEquipeRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (escala, actor, error) = await LoadEditableAsync(
            id, actorLogin, cancellationToken, x => x.Include(e => e.Setores).ThenInclude(s => s.Equipes));
        if (escala is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        var setor = escala.Setores.FirstOrDefault(x => x.Id == request.EscalaResumidaSetorId);
        if (setor is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Setor não pertence a esta escala.");
        }

        // Numeração é sempre por setor, começando em 1 — nunca confia em nome/ordem calculados
        // pelo cliente (já causou "Equipe 03" nascer no primeiro time de um setor porque a
        // contagem veio de outro setor da mesma escala resumida).
        var ordem = setor.Equipes.Count + 1;
        var nome = $"Equipe {ordem:00}";

        EscalaResumidaEquipe equipe;
        try
        {
            equipe = EscalaResumidaEquipe.Create(setor.Id, nome, ordem, actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
        }

        db.EscalaResumidaEquipes.Add(equipe);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> AtualizarEquipeAsync(
        Guid id,
        Guid equipeId,
        AtualizarEquipeRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (equipe, error) = await LoadEquipeEditavelAsync(id, equipeId, actorLogin, cancellationToken);
        if (equipe is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        try
        {
            equipe.Renomear(request.Nome, request.Ordem, actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> RemoverEquipeAsync(
        Guid id,
        Guid equipeId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (equipe, error) = await LoadEquipeEditavelAsync(
            id, equipeId, actorLogin, cancellationToken,
            x => x.Include(e => e.EscalaResumidaSetor).ThenInclude(s => s.Equipes).ThenInclude(eq => eq.Rotacao));
        if (equipe is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        var irmas = equipe.EscalaResumidaSetor.Equipes
            .Where(x => x.Id != equipe.Id)
            .OrderBy(x => x.Ordem)
            .ToList();

        db.EscalaResumidaEquipes.Remove(equipe);

        // Fecha o buraco na numeração (Equipe 02/03 viram 01/02) — só renomeia quem ainda
        // tem o nome auto-gerado padrão da própria ordem antiga, sem mexer em nome customizado.
        for (var i = 0; i < irmas.Count; i++)
        {
            var irma = irmas[i];
            var novaOrdem = i + 1;
            var nomeAutoGeradoAntigo = $"Equipe {irma.Ordem:00}";
            var novoNome = irma.Nome == nomeAutoGeradoAntigo ? $"Equipe {novaOrdem:00}" : irma.Nome;
            if (irma.Ordem != novaOrdem || irma.Nome != novoNome)
            {
                irma.Renomear(novoNome, novaOrdem, actorLogin);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (irmas.Count > 0)
        {
            // A ordem entre equipes-irmãs decide quem "dono" de cada ciclo na troca de pool
            // entre equipes (ver `EscalaResumidaRotacaoExpander`) — renumerar exige regerar.
            var escalaResumida = await db.EscalasResumidas.FirstAsync(x => x.Id == id, cancellationToken);
            await RegerarSetorAsync(
                irmas, escalaResumida.DataInicio, escalaResumida.DataFim, actorLogin, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> ConfigurarRotacaoAsync(
        Guid id,
        Guid equipeId,
        ConfigurarRotacaoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (equipe, error) = await LoadEquipeEditavelAsync(
            id, equipeId, actorLogin, cancellationToken,
            x => x.Include(e => e.EscalaResumidaSetor).ThenInclude(s => s.Equipes).ThenInclude(eq => eq.Rotacao));
        if (equipe is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        if (request.Membros.Count == 0)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Informe ao menos uma posição no rodízio.");
        }

        var posicoesEsperadas = Enumerable.Range(0, request.Membros.Count).ToHashSet();
        var posicoesRecebidas = request.Membros.Select(x => x.Posicao).ToHashSet();
        if (!posicoesEsperadas.SetEquals(posicoesRecebidas))
        {
            return Result<EscalaResumidaDetailDto>.Failure(
                "As posições do rodízio devem ser sequenciais, começando em 0, sem repetição.");
        }

        var servidorIds = request.Membros
            .SelectMany(x => new[] { x.ServidorId, x.ServidorId2 })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var servidoresValidos = await db.Servidores.Where(x => servidorIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        if (servidoresValidos.Count != servidorIds.Count)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Há servidores inválidos no rodízio.");
        }

        var escalaResumida = await db.EscalasResumidas.FirstAsync(x => x.Id == id, cancellationToken);

        var atuais = await db.EscalaResumidaRotacaoMembros
            .Where(x => x.EscalaResumidaEquipeId == equipe.Id)
            .ToListAsync(cancellationToken);
        db.EscalaResumidaRotacaoMembros.RemoveRange(atuais);
        equipe.Rotacao.Clear();

        foreach (var item in request.Membros.OrderBy(x => x.Posicao))
        {
            var membro = EscalaResumidaRotacaoMembro.Create(
                equipe.Id, item.Posicao, item.ServidorId, item.ServidorId2, actorLogin);
            // Só Add no DbSet: como `equipe` já está rastreado pelo EF, o fixup automático
            // já inclui `membro` em equipe.Rotacao — adicionar aqui também duplicaria o item
            // na coleção em memória e quebraria o ToDictionary por Posicao no expander.
            db.EscalaResumidaRotacaoMembros.Add(membro);
        }

        equipe.DefinirAncora(request.DataInicioCiclo, actorLogin);

        await db.SaveChangesAsync(cancellationToken);
        await RegerarSetorAsync(
            equipe.EscalaResumidaSetor.Equipes.ToList(),
            escalaResumida.DataInicio, escalaResumida.DataFim, actorLogin, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> UpsertDiaAsync(
        Guid id,
        Guid equipeId,
        UpsertDiaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (equipe, error) = await LoadEquipeEditavelAsync(id, equipeId, actorLogin, cancellationToken);
        if (equipe is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        var escalaResumida = await db.EscalasResumidas.FirstAsync(x => x.Id == id, cancellationToken);
        if (request.Data < escalaResumida.DataInicio || request.Data > escalaResumida.DataFim)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Data fora do período da escala.");
        }

        string? servidorNome = null;
        if (request.ServidorId is Guid servidorId)
        {
            servidorNome = await db.Servidores
                .Where(x => x.Id == servidorId)
                .Select(x => x.Nome)
                .FirstOrDefaultAsync(cancellationToken);
            if (servidorNome is null)
            {
                return Result<EscalaResumidaDetailDto>.Failure("Servidor inválido.");
            }
        }

        string? servidorNome2 = null;
        if (request.ServidorId2 is Guid servidorId2)
        {
            servidorNome2 = await db.Servidores
                .Where(x => x.Id == servidorId2)
                .Select(x => x.Nome)
                .FirstOrDefaultAsync(cancellationToken);
            if (servidorNome2 is null)
            {
                return Result<EscalaResumidaDetailDto>.Failure("Segundo servidor inválido.");
            }
        }

        var dia = await db.EscalaResumidaDias
            .FirstOrDefaultAsync(x => x.EscalaResumidaEquipeId == equipe.Id && x.Data == request.Data, cancellationToken);

        if (dia is null)
        {
            db.EscalaResumidaDias.Add(EscalaResumidaDia.CriarManual(
                equipe.Id, request.Data, request.ServidorId, servidorNome, request.TextoLivre, request.IsFolga,
                request.ServidorId2, servidorNome2, request.IsFolga2, actorLogin));
        }
        else
        {
            dia.AtualizarManual(
                request.ServidorId, servidorNome, request.TextoLivre, request.IsFolga,
                request.ServidorId2, servidorNome2, request.IsFolga2, actorLogin);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> ReverterDiaParaRegraAsync(
        Guid id,
        Guid equipeId,
        DateOnly data,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var (equipe, error) = await LoadEquipeEditavelAsync(
            id, equipeId, actorLogin, cancellationToken,
            x => x.Include(e => e.EscalaResumidaSetor).ThenInclude(s => s.Equipes).ThenInclude(eq => eq.Rotacao));
        if (equipe is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        if (equipe.DataInicioCiclo is null || equipe.Rotacao.Count == 0)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Equipe sem rodízio configurado.");
        }

        var dia = await db.EscalaResumidaDias
            .FirstOrDefaultAsync(x => x.EscalaResumidaEquipeId == equipe.Id && x.Data == data, cancellationToken);
        if (dia is null || dia.Origem != OrigemOcorrencia.Manual)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Não há valor manual para reverter nesta data.");
        }

        // Considera possível troca de pool com equipes irmãs (mesmo tamanho+âncora) — ver
        // `EscalaResumidaRotacaoExpander`; por isso não dá pra olhar só o pool desta equipe.
        var regra = EscalaResumidaRotacaoExpander
            .ExpandSetor(equipe.EscalaResumidaSetor.Equipes.ToList(), data, data)
            .First(x => x.EquipeId == equipe.Id);
        var nome = regra.ServidorId is Guid servidorId
            ? await db.Servidores.Where(x => x.Id == servidorId).Select(x => x.Nome).FirstOrDefaultAsync(cancellationToken)
            : null;
        var nome2 = regra.ServidorId2 is Guid servidorId2
            ? await db.Servidores.Where(x => x.Id == servidorId2).Select(x => x.Nome).FirstOrDefaultAsync(cancellationToken)
            : null;

        dia.AtualizarPorRegra(regra.ServidorId, nome, regra.ServidorId2, nome2, regra.RotacaoMembroId, actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<IReadOnlyList<EscalaResumidaServidorElegivelDto>> ListServidoresElegiveisAsync(
        Guid? nucleoId,
        Guid? setorId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!CanView(actor, nucleoId, setorId))
        {
            return [];
        }

        var servidores = await db.Servidores
            .AsNoTracking()
            .Include(x => x.Setor)
            .Where(x => x.Status == StatusServidor.Ativo
                && (setorId.HasValue
                    ? x.SetorId == setorId.Value
                    : (x.NucleoId == nucleoId || (x.SetorId != null && x.Setor!.NucleoId == nucleoId))))
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);

        return servidores
            .Select(x => new EscalaResumidaServidorElegivelDto(x.Id, x.Nome, x.Matricula, x.SetorId, x.Setor?.Nome))
            .ToList();
    }

    public async Task<EscalaResumidaAnteriorInfoDto?> GetAnteriorAsync(
        Guid? nucleoId,
        Guid? setorId,
        int ano,
        int mes,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!CanMutate(actor, nucleoId, setorId))
        {
            return null;
        }

        var mesAnterior = new DateOnly(ano, mes, 1).AddMonths(-1);
        var anterior = await db.EscalasResumidas
            .AsNoTracking()
            .Include(x => x.Nucleo)
            .Include(x => x.Setor)
            .Include(x => x.Setores)
            .FirstOrDefaultAsync(
                x => setorId.HasValue
                    ? x.SetorId == setorId.Value && x.Ano == mesAnterior.Year && x.Mes == mesAnterior.Month
                    : x.NucleoId == nucleoId && x.Ano == mesAnterior.Year && x.Mes == mesAnterior.Month,
                cancellationToken);

        if (anterior is null)
        {
            return null;
        }

        return new EscalaResumidaAnteriorInfoDto(
            anterior.Id,
            anterior.Ano,
            anterior.Mes,
            EscalaResumida.FormatIdentificacao(
                anterior.Mes, anterior.Ano, anterior.Setor?.Nome ?? anterior.Nucleo?.Nome ?? string.Empty),
            anterior.Status,
            anterior.Setores.Count);
    }

    public async Task<Result<EscalaResumidaDetailDto>> CopiarAsync(
        Guid origemId,
        CopiarEscalaResumidaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var origem = await LoadDetailQuery().FirstOrDefaultAsync(x => x.Id == origemId, cancellationToken);
        if (origem is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Escala resumida de origem não encontrada.");
        }

        if (!CanMutate(actor, origem.NucleoId, origem.SetorId))
        {
            return Result<EscalaResumidaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        if (await db.EscalasResumidas.AnyAsync(
                x => origem.SetorId.HasValue
                    ? x.SetorId == origem.SetorId.Value && x.Ano == request.Ano && x.Mes == request.Mes
                    : x.NucleoId == origem.NucleoId && x.Ano == request.Ano && x.Mes == request.Mes,
                cancellationToken))
        {
            return Result<EscalaResumidaDetailDto>.Failure("Já existe escala resumida para este período de destino.");
        }

        EscalaResumida nova;
        try
        {
            nova = EscalaResumida.Create(
                origem.NucleoId, request.Ano, request.Mes, origem.Observacao, actorLogin, setorId: origem.SetorId);
        }
        catch (Exception ex)
        {
            return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
        }

        db.EscalasResumidas.Add(nova);

        var novasEquipes = new List<EscalaResumidaEquipe>();

        foreach (var setorOrigem in origem.Setores.OrderBy(x => x.Ordem))
        {
            var setorNovo = EscalaResumidaSetor.Create(
                nova.Id, setorOrigem.SetorId, setorOrigem.Ordem,
                setorOrigem.SetorNomeSnapshot, setorOrigem.SetorSiglaSnapshot, actorLogin);
            db.EscalaResumidaSetores.Add(setorNovo);

            foreach (var equipeOrigem in setorOrigem.Equipes.OrderBy(x => x.Ordem))
            {
                var equipeNova = EscalaResumidaEquipe.Create(setorNovo.Id, equipeOrigem.Nome, equipeOrigem.Ordem, actorLogin);
                db.EscalaResumidaEquipes.Add(equipeNova);

                var poolNovo = new List<EscalaResumidaRotacaoMembro>();
                foreach (var membroOrigem in equipeOrigem.Rotacao.OrderBy(x => x.Posicao))
                {
                    var membroNovo = EscalaResumidaRotacaoMembro.Create(
                        equipeNova.Id, membroOrigem.Posicao, membroOrigem.ServidorId, membroOrigem.ServidorId2, actorLogin);
                    // Só Add no DbSet — ver comentário equivalente em ConfigurarRotacaoAsync
                    // (fixup automático do EF já popula equipeNova.Rotacao).
                    db.EscalaResumidaRotacaoMembros.Add(membroNovo);
                    poolNovo.Add(membroNovo);
                }

                var novaAncora = ReancorarRodizio(equipeOrigem, poolNovo, origem.DataInicio, origem.DataFim);
                if (novaAncora is DateOnly ancora)
                {
                    equipeNova.DefinirAncora(ancora, actorLogin);
                }

                novasEquipes.Add(equipeNova);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Por setor (não por equipe): equipes irmãs entram na mesma leva de regeneração, já
        // que uma pode depender do pool da outra quando há troca entre equipes.
        foreach (var grupoSetor in novasEquipes.GroupBy(x => x.EscalaResumidaSetorId))
        {
            await RegerarSetorAsync(grupoSetor.ToList(), nova.DataInicio, nova.DataFim, actorLogin, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(nova.Id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> FinalizarAsync(
        Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var (escala, actor, error) = await LoadEditableRootAsync(id, actorLogin, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        try
        {
            escala.Finalizar(actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> ReabrirAsync(
        Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var (escala, actor, error) = await LoadEditableRootAsync(id, actorLogin, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure(error!);
        }

        try
        {
            escala.ReabrirParaRascunho(actorLogin);
        }
        catch (Exception ex)
        {
            return Result<EscalaResumidaDetailDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<EscalaResumidaDetailDto>> VincularEscalaAsync(
        Guid id, Guid escalaId, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.EscalasResumidas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result<EscalaResumidaDetailDto>.Failure("Escala resumida não encontrada.");
        }

        if (!CanMutate(actor, escala.NucleoId, escala.SetorId))
        {
            return Result<EscalaResumidaDetailDto>.Failure("Sem permissão para esta escala.");
        }

        if (!await db.Escalas.AnyAsync(x => x.Id == escalaId, cancellationToken))
        {
            return Result<EscalaResumidaDetailDto>.Failure("Escala inválida.");
        }

        escala.VincularEscala(escalaId, actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.EscalasResumidas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return Result.Failure("Escala resumida não encontrada.");
        }

        if (!CanMutate(actor, escala.NucleoId, escala.SetorId))
        {
            return Result.Failure("Sem permissão para esta escala.");
        }

        if (!IsEditableStatus(escala.Status))
        {
            return Result.Failure("Somente escalas em rascunho ou finalizadas podem ser excluídas.");
        }

        if (escala.EscalaId is not null)
        {
            return Result.Failure("Escala resumida já vinculada a uma escala salva — não pode ser excluída.");
        }

        db.EscalasResumidas.Remove(escala);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ---- helpers ----

    /// <summary>Regera os dias de TODAS as equipes de um setor de uma vez — necessário porque,
    /// quando equipes trocam de pool entre si a cada ciclo (ver
    /// <see cref="EscalaResumidaRotacaoExpander"/>), o valor de uma equipe num dado dia pode
    /// depender do pool configurado em uma equipe IRMÃ, não só do dela mesma.</summary>
    private async Task RegerarSetorAsync(
        IReadOnlyList<EscalaResumidaEquipe> equipesDoSetor,
        DateOnly inicio,
        DateOnly fim,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        var expandido = EscalaResumidaRotacaoExpander.ExpandSetor(equipesDoSetor, inicio, fim).ToList();
        if (expandido.Count == 0)
        {
            return;
        }

        var servidorIds = expandido
            .SelectMany(x => new[] { x.ServidorId, x.ServidorId2 })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var nomes = await db.Servidores
            .AsNoTracking()
            .Where(x => servidorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Nome, cancellationToken);

        var equipeIds = equipesDoSetor.Select(x => x.Id).ToList();
        var existentes = await db.EscalaResumidaDias
            .Where(x => equipeIds.Contains(x.EscalaResumidaEquipeId) && x.Data >= inicio && x.Data <= fim)
            .ToDictionaryAsync(x => (x.EscalaResumidaEquipeId, x.Data), cancellationToken);

        foreach (var (equipeId, data, servidorId, servidorId2, rotacaoMembroId) in expandido)
        {
            var nome = servidorId.HasValue && nomes.TryGetValue(servidorId.Value, out var n) ? n : null;
            var nome2 = servidorId2.HasValue && nomes.TryGetValue(servidorId2.Value, out var n2) ? n2 : null;

            if (existentes.TryGetValue((equipeId, data), out var dia))
            {
                if (dia.Origem == OrigemOcorrencia.Manual)
                {
                    continue;
                }

                dia.AtualizarPorRegra(servidorId, nome, servidorId2, nome2, rotacaoMembroId, actorLogin);
            }
            else
            {
                db.EscalaResumidaDias.Add(
                    EscalaResumidaDia.CriarPorRegra(equipeId, data, servidorId, nome, servidorId2, nome2, rotacaoMembroId, actorLogin));
            }
        }
    }

    /// <summary>
    /// Acha o ponto de ancoragem do novo mês a partir dos últimos 4 dias reais do mês de
    /// origem: procura, do mais recente pro mais antigo, um dia "limpo" (sem texto livre —
    /// origem de regra ou override manual de identidade única) cujo servidor bate com alguma
    /// posição do pool clonado. Achando, a nova âncora é aquele dia menos a posição encontrada,
    /// o que faz o rodízio continuar em fase a partir dali. Não achando em nenhum dos 4,
    /// mantém a âncora antiga (mesmo fallback do <see cref="EscalaJornada"/>).
    /// </summary>
    private static DateOnly? ReancorarRodizio(
        EscalaResumidaEquipe equipeOrigem,
        IReadOnlyList<EscalaResumidaRotacaoMembro> poolNovo,
        DateOnly origemInicio,
        DateOnly origemFim)
    {
        if (equipeOrigem.DataInicioCiclo is not DateOnly ancoraAntiga || poolNovo.Count == 0)
        {
            return equipeOrigem.DataInicioCiclo;
        }

        for (var i = 0; i < 4; i++)
        {
            var dia = origemFim.AddDays(-i);
            if (dia < origemInicio)
            {
                break;
            }

            var diaOrigem = equipeOrigem.Dias.FirstOrDefault(x => x.Data == dia);
            if (diaOrigem is null || diaOrigem.TextoLivre is not null)
            {
                continue;
            }

            var match = poolNovo.FirstOrDefault(x => x.ServidorId == diaOrigem.ServidorId);
            if (match is not null)
            {
                return dia.AddDays(-match.Posicao);
            }
        }

        return ancoraAntiga;
    }

    private async Task<(EscalaResumida? Escala, ActorContext Actor, string? Error)> LoadEditableAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken,
        Func<IQueryable<EscalaResumida>, IQueryable<EscalaResumida>>? include = null)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var query = db.EscalasResumidas.AsQueryable();
        if (include is not null)
        {
            query = include(query);
        }

        var escala = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return (null, actor, "Escala resumida não encontrada.");
        }

        if (!CanMutate(actor, escala.NucleoId, escala.SetorId))
        {
            return (null, actor, "Sem permissão para esta escala.");
        }

        try
        {
            escala.EnsureEditable();
        }
        catch (Exception ex)
        {
            return (null, actor, ex.Message);
        }

        return (escala, actor, null);
    }

    private async Task<(EscalaResumida? Escala, ActorContext Actor, string? Error)> LoadEditableRootAsync(
        Guid id, string actorLogin, CancellationToken cancellationToken)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var escala = await db.EscalasResumidas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (escala is null)
        {
            return (null, actor, "Escala resumida não encontrada.");
        }

        if (!CanMutate(actor, escala.NucleoId, escala.SetorId))
        {
            return (null, actor, "Sem permissão para esta escala.");
        }

        return (escala, actor, null);
    }

    private async Task<(EscalaResumidaEquipe? Equipe, string? Error)> LoadEquipeEditavelAsync(
        Guid id,
        Guid equipeId,
        string actorLogin,
        CancellationToken cancellationToken,
        Func<IQueryable<EscalaResumidaEquipe>, IQueryable<EscalaResumidaEquipe>>? include = null)
    {
        var (escala, _, error) = await LoadEditableAsync(id, actorLogin, cancellationToken);
        if (escala is null)
        {
            return (null, error);
        }

        var query = db.EscalaResumidaEquipes
            .Where(x => x.Id == equipeId && x.EscalaResumidaSetor.EscalaResumidaId == id);
        if (include is not null)
        {
            query = include(query);
        }

        var equipe = await query.FirstOrDefaultAsync(cancellationToken);
        if (equipe is null)
        {
            return (null, "Equipe não encontrada nesta escala.");
        }

        return (equipe, null);
    }

    private IQueryable<EscalaResumida> LoadDetailQuery() =>
        db.EscalasResumidas
            .AsNoTracking()
            .Include(x => x.Nucleo)
            .Include(x => x.Setor)
            .Include(x => x.Setores).ThenInclude(x => x.Equipes).ThenInclude(x => x.Rotacao).ThenInclude(x => x.Servidor)
            .Include(x => x.Setores).ThenInclude(x => x.Equipes).ThenInclude(x => x.Rotacao).ThenInclude(x => x.Servidor2)
            .Include(x => x.Setores).ThenInclude(x => x.Equipes).ThenInclude(x => x.Dias);

    private Task<ActorContext> ResolveActorAsync(string login, CancellationToken cancellationToken) =>
        ActorContextLoader.LoadAsync(db, login, cancellationToken);

    private static bool CanMutate(ActorContext actor, Guid? nucleoId, Guid? setorId) =>
        setorId is Guid s
            ? actor.PodeAcessar(PermissionCodes.EscalasEditar, s) || actor.GerenciaSetorViaNucleo(s)
            : nucleoId is Guid n
                && (actor.GerenciaNucleo(n)
                    || (actor.TemVisaoGlobal(PermissionModules.Escalas) && actor.TemPermissao(PermissionCodes.EscalasEditar)));

    private static bool CanView(ActorContext actor, Guid? nucleoId, Guid? setorId) =>
        CanMutate(actor, nucleoId, setorId)
        || (actor.TemVisaoGlobal(PermissionModules.Escalas) && actor.TemPermissao(PermissionCodes.EscalasListar));

    private static bool IsEditableStatus(StatusEscala status) =>
        status is StatusEscala.Rascunho or StatusEscala.Finalizada;

    private static EscalaResumidaDetailDto MapDetail(EscalaResumida escala) =>
        new(
            escala.Id,
            EscalaResumida.FormatIdentificacao(
                escala.Mes, escala.Ano, escala.Setor?.Nome ?? escala.Nucleo?.Nome ?? string.Empty),
            escala.NucleoId,
            escala.Nucleo?.Nome,
            escala.Nucleo?.Sigla,
            escala.Ano,
            escala.Mes,
            escala.DataInicio,
            escala.DataFim,
            escala.Status,
            escala.Observacao,
            escala.EscalaId,
            escala.CreatedAt,
            escala.CreatedBy,
            escala.Setores
                .OrderBy(s => s.Ordem)
                .Select(s => new EscalaResumidaSetorDto(
                    s.Id,
                    s.SetorId,
                    s.SetorNomeSnapshot,
                    s.SetorSiglaSnapshot,
                    s.Ordem,
                    s.Equipes
                        .OrderBy(e => e.Ordem)
                        .Select(e => new EscalaResumidaEquipeDto(
                            e.Id,
                            e.Nome,
                            e.Ordem,
                            e.DataInicioCiclo,
                            e.Rotacao
                                .OrderBy(m => m.Posicao)
                                .Select(m => new EscalaResumidaRotacaoMembroDto(
                                    m.Id, m.Posicao, m.ServidorId, m.Servidor?.Nome, m.ServidorId2, m.Servidor2?.Nome))
                                .ToList(),
                            e.Dias
                                .OrderBy(d => d.Data)
                                .Select(d => new EscalaResumidaDiaDto(
                                    d.Id, d.Data, d.ServidorId, d.ServidorNomeSnapshot,
                                    d.ServidorId2, d.ServidorNomeSnapshot2, d.IsFolga2,
                                    d.TextoLivre, d.IsFolga, d.Rotulo, d.Origem, d.RotacaoMembroId))
                                .ToList()))
                        .ToList()))
                .ToList(),
            escala.SetorId,
            escala.Setor?.Nome,
            escala.Setor?.Sigla);
}
