namespace TemplateSistema.Application.Calendario;

public record MarcacaoDto(
    Guid Id,
    Guid CalendarioAnoId,
    string Tipo,
    string Origem,
    DateOnly Data,
    DateOnly? DataFim,
    string Nome,
    string? Descricao,
    string? Local,
    string? PublicoAlvo,
    DateTime CreatedAt);

public record CalendarioAnoDto(
    Guid Id,
    int Ano,
    string Status,
    DateTime? PublicadoEm,
    string? PublicadoPor,
    string? Observacao,
    DateTime CreatedAt,
    IReadOnlyList<MarcacaoDto> Marcacoes);

public record CalendarioAnoResumoDto(
    Guid Id,
    int Ano,
    string Status,
    DateTime? PublicadoEm,
    int TotalFeriados,
    int TotalPontosFacultativos,
    int TotalEventos);

public record DiaNaoUtilDto(DateOnly Data, string Tipo, string Nome);

public record GerarAnoRequest(int Ano);

public record AtualizarAnoRequest(string? Observacao);

public record CriarMarcacaoRequest(
    Guid CalendarioAnoId,
    string Tipo,
    DateOnly Data,
    DateOnly? DataFim,
    string Nome,
    string? Descricao,
    string? Local,
    string? PublicoAlvo);

public record AtualizarMarcacaoRequest(
    string Tipo,
    DateOnly Data,
    DateOnly? DataFim,
    string Nome,
    string? Descricao,
    string? Local,
    string? PublicoAlvo);
