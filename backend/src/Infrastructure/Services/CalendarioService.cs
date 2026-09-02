using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Calendario;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class CalendarioService(ApplicationDbContext db) : ICalendarioService
{
    public async Task<IReadOnlyList<CalendarioAnoResumoDto>> ListarAnosAsync(
        CancellationToken cancellationToken = default)
    {
        var anos = await db.CalendariosAno
            .AsNoTracking()
            .OrderByDescending(x => x.Ano)
            .ToListAsync(cancellationToken);

        var anoIds = anos.Select(x => x.Id).ToList();
        var marcacoes = await db.MarcacoesCalendario
            .AsNoTracking()
            .Where(x => anoIds.Contains(x.CalendarioAnoId))
            .ToListAsync(cancellationToken);

        return anos
            .Select(a => MapResumo(a, marcacoes.Where(m => m.CalendarioAnoId == a.Id).ToList()))
            .ToList();
    }

    public async Task<Result<CalendarioAnoDto>> ObterAnoAsync(
        int ano,
        CancellationToken cancellationToken = default)
    {
        var calendarioAno = await db.CalendariosAno
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ano == ano, cancellationToken);

        if (calendarioAno is null)
        {
            return Result<CalendarioAnoDto>.Failure($"Calendário de {ano} não encontrado.");
        }

        var marcacoes = await MarcacoesDoAnoAsync(calendarioAno.Id, cancellationToken);
        return Result<CalendarioAnoDto>.Success(MapDetail(calendarioAno, marcacoes));
    }

    public async Task<Result<CalendarioAnoDto>> GerarAnoAsync(
        GerarAnoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        if (await db.CalendariosAno.AnyAsync(x => x.Ano == request.Ano, cancellationToken))
        {
            return Result<CalendarioAnoDto>.Failure($"O calendário de {request.Ano} já foi gerado.");
        }

        CalendarioAno calendarioAno;
        try
        {
            calendarioAno = CalendarioAno.Create(request.Ano, actorLogin);
        }
        catch (Exception ex)
        {
            return Result<CalendarioAnoDto>.Failure(ex.Message);
        }

        db.CalendariosAno.Add(calendarioAno);

        foreach (var (mes, dia, nome) in FeriadoFixoCatalogo.Nacionais)
        {
            db.MarcacoesCalendario.Add(MarcacaoCalendario.Create(
                calendarioAno.Id,
                TipoMarcacaoCalendario.FeriadoNacional,
                OrigemMarcacao.Fixo,
                new DateOnly(request.Ano, mes, dia),
                dataFim: null,
                nome,
                createdBy: actorLogin));
        }

        foreach (var (mes, dia, nome) in FeriadoFixoCatalogo.EstaduaisRn)
        {
            db.MarcacoesCalendario.Add(MarcacaoCalendario.Create(
                calendarioAno.Id,
                TipoMarcacaoCalendario.FeriadoEstadual,
                OrigemMarcacao.Fixo,
                new DateOnly(request.Ano, mes, dia),
                dataFim: null,
                nome,
                createdBy: actorLogin));
        }

        foreach (var (mes, dia, nome) in FeriadoFixoCatalogo.MunicipaisNatal)
        {
            db.MarcacoesCalendario.Add(MarcacaoCalendario.Create(
                calendarioAno.Id,
                TipoMarcacaoCalendario.FeriadoMunicipal,
                OrigemMarcacao.Fixo,
                new DateOnly(request.Ano, mes, dia),
                dataFim: null,
                nome,
                createdBy: actorLogin));
        }

        foreach (var (data, nome, tipo) in FeriadoMovelCalculator.Moveis(request.Ano))
        {
            db.MarcacoesCalendario.Add(MarcacaoCalendario.Create(
                calendarioAno.Id,
                tipo,
                OrigemMarcacao.Calculado,
                data,
                dataFim: null,
                nome,
                createdBy: actorLogin));
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Corrida: outra requisição criou o mesmo ano entre o AnyAsync acima e este save.
            return Result<CalendarioAnoDto>.Failure($"O calendário de {request.Ano} já foi gerado.");
        }

        return await ObterAnoAsync(request.Ano, cancellationToken);
    }

    public async Task<Result> ExcluirAnoAsync(
        Guid calendarioAnoId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var calendarioAno = await db.CalendariosAno
            .FirstOrDefaultAsync(x => x.Id == calendarioAnoId, cancellationToken);

        if (calendarioAno is null)
        {
            return Result.Failure("Calendário não encontrado.");
        }

        if (calendarioAno.Status != StatusCalendarioAno.Rascunho)
        {
            return Result.Failure("Somente calendários em rascunho podem ser excluídos.");
        }

        db.CalendariosAno.Remove(calendarioAno);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<CalendarioAnoDto>> AtualizarAnoAsync(
        Guid calendarioAnoId,
        AtualizarAnoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var calendarioAno = await db.CalendariosAno
            .FirstOrDefaultAsync(x => x.Id == calendarioAnoId, cancellationToken);

        if (calendarioAno is null)
        {
            return Result<CalendarioAnoDto>.Failure("Calendário não encontrado.");
        }

        calendarioAno.Atualizar(request.Observacao, actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return await ObterAnoAsync(calendarioAno.Ano, cancellationToken);
    }

    public async Task<Result<CalendarioAnoDto>> PublicarAsync(
        Guid calendarioAnoId,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var calendarioAno = await db.CalendariosAno
            .FirstOrDefaultAsync(x => x.Id == calendarioAnoId, cancellationToken);

        if (calendarioAno is null)
        {
            return Result<CalendarioAnoDto>.Failure("Calendário não encontrado.");
        }

        calendarioAno.Publicar(actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return await ObterAnoAsync(calendarioAno.Ano, cancellationToken);
    }

    public async Task<Result<MarcacaoDto>> AdicionarMarcacaoAsync(
        CriarMarcacaoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var ano = await db.CalendariosAno
            .Where(x => x.Id == request.CalendarioAnoId)
            .Select(x => (int?)x.Ano)
            .FirstOrDefaultAsync(cancellationToken);
        if (ano is null)
        {
            return Result<MarcacaoDto>.Failure("Calendário não encontrado.");
        }

        if (!TryParseTipo(request.Tipo, out var tipo))
        {
            return Result<MarcacaoDto>.Failure("Tipo de marcação inválido.");
        }

        if (!DentroDoAno(ano.Value, request.Data, request.DataFim))
        {
            return Result<MarcacaoDto>.Failure("A data da marcação deve estar dentro do ano do calendário.");
        }

        MarcacaoCalendario entity;
        try
        {
            entity = MarcacaoCalendario.Create(
                request.CalendarioAnoId,
                tipo,
                OrigemMarcacao.Manual,
                request.Data,
                request.DataFim,
                request.Nome,
                request.Descricao,
                request.Local,
                request.PublicoAlvo,
                actorLogin);
        }
        catch (Exception ex)
        {
            return Result<MarcacaoDto>.Failure(ex.Message);
        }

        db.MarcacoesCalendario.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Result<MarcacaoDto>.Success(MapMarcacao(entity));
    }

    public async Task<Result<MarcacaoDto>> AtualizarMarcacaoAsync(
        Guid id,
        AtualizarMarcacaoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.MarcacoesCalendario.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result<MarcacaoDto>.Failure("Marcação não encontrada.");
        }

        if (!TryParseTipo(request.Tipo, out var tipo))
        {
            return Result<MarcacaoDto>.Failure("Tipo de marcação inválido.");
        }

        var ano = await db.CalendariosAno
            .Where(x => x.Id == entity.CalendarioAnoId)
            .Select(x => x.Ano)
            .FirstAsync(cancellationToken);
        if (!DentroDoAno(ano, request.Data, request.DataFim))
        {
            return Result<MarcacaoDto>.Failure("A data da marcação deve estar dentro do ano do calendário.");
        }

        try
        {
            entity.Atualizar(
                tipo,
                request.Data,
                request.DataFim,
                request.Nome,
                request.Descricao,
                request.Local,
                request.PublicoAlvo,
                actorLogin);
        }
        catch (Exception ex)
        {
            return Result<MarcacaoDto>.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<MarcacaoDto>.Success(MapMarcacao(entity));
    }

    public async Task<Result> ExcluirMarcacaoAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.MarcacoesCalendario.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure("Marcação não encontrada.");
        }

        db.MarcacoesCalendario.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Só considera marcações de calendários publicados — um rascunho em edição não deve
    /// afetar cálculo de dias úteis em outras partes do sistema. Eventos institucionais não
    /// contam como dia não útil (são só uma marcação, não implicam folga).
    /// </summary>
    public async Task<IReadOnlyList<DiaNaoUtilDto>> ConsultarDiasNaoUteisAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancellationToken = default)
    {
        var query =
            from m in db.MarcacoesCalendario.AsNoTracking()
            join c in db.CalendariosAno.AsNoTracking() on m.CalendarioAnoId equals c.Id
            where c.Status == StatusCalendarioAno.Publicado
            where m.Tipo != TipoMarcacaoCalendario.EventoInstitucional
            where m.Data <= fim && (m.DataFim ?? m.Data) >= inicio
            orderby m.Data
            select new { m.Data, m.Tipo, m.Nome };

        var itens = await query.ToListAsync(cancellationToken);
        return itens.Select(x => new DiaNaoUtilDto(x.Data, x.Tipo.ToString(), x.Nome)).ToList();
    }

    private async Task<IReadOnlyList<MarcacaoCalendario>> MarcacoesDoAnoAsync(
        Guid calendarioAnoId,
        CancellationToken cancellationToken) =>
        await db.MarcacoesCalendario
            .AsNoTracking()
            .Where(x => x.CalendarioAnoId == calendarioAnoId)
            .OrderBy(x => x.Data)
            .ThenBy(x => x.Tipo)
            .ToListAsync(cancellationToken);

    private static bool DentroDoAno(int ano, DateOnly data, DateOnly? dataFim) =>
        data.Year == ano && (dataFim is null || dataFim.Value.Year == ano);

    private static bool TryParseTipo(string? tipo, out TipoMarcacaoCalendario result) =>
        Enum.TryParse(tipo, ignoreCase: true, out result) && Enum.IsDefined(result);

    private static MarcacaoDto MapMarcacao(MarcacaoCalendario m) => new(
        m.Id,
        m.CalendarioAnoId,
        m.Tipo.ToString(),
        m.Origem.ToString(),
        m.Data,
        m.DataFim,
        m.Nome,
        m.Descricao,
        m.Local,
        m.PublicoAlvo,
        m.CreatedAt);

    private static CalendarioAnoDto MapDetail(CalendarioAno c, IReadOnlyList<MarcacaoCalendario> marcacoes) => new(
        c.Id,
        c.Ano,
        c.Status.ToString(),
        c.PublicadoEm,
        c.PublicadoPor,
        c.Observacao,
        c.CreatedAt,
        marcacoes.Select(MapMarcacao).ToList());

    private static CalendarioAnoResumoDto MapResumo(
        CalendarioAno c,
        IReadOnlyList<MarcacaoCalendario> marcacoes) => new(
        c.Id,
        c.Ano,
        c.Status.ToString(),
        c.PublicadoEm,
        marcacoes.Count(x => x.Tipo is TipoMarcacaoCalendario.FeriadoNacional
            or TipoMarcacaoCalendario.FeriadoEstadual
            or TipoMarcacaoCalendario.FeriadoMunicipal),
        marcacoes.Count(x => x.Tipo == TipoMarcacaoCalendario.PontoFacultativo),
        marcacoes.Count(x => x.Tipo == TipoMarcacaoCalendario.EventoInstitucional));
}
