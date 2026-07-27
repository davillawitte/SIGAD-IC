namespace TemplateSistema.Application.Afastamentos;

public record AfastamentoDto(
    Guid Id,
    Guid ServidorId,
    string ServidorNome,
    string Matricula,
    Guid SetorId,
    string SetorNome,
    string SetorSigla,
    DateOnly DataInicio,
    DateOnly DataFim,
    string TipoOcorrenciaCodigo,
    string TipoOcorrenciaNome,
    string? Observacao,
    string? Sei,
    DateTime CreatedAt);

public record CreateAfastamentoRequest(
    Guid ServidorId,
    DateOnly DataInicio,
    DateOnly DataFim,
    string TipoOcorrenciaCodigo,
    string? Observacao,
    string? Sei = null);

public record UpdateAfastamentoRequest(
    DateOnly DataInicio,
    DateOnly DataFim,
    string TipoOcorrenciaCodigo,
    string? Observacao,
    string? Sei = null);

public record AfastamentoListQuery
{
    public Guid? SetorId { get; init; }
    public Guid? ServidorId { get; init; }
    public int? Ano { get; init; }
    public int? Mes { get; init; }
    public string? TipoOcorrenciaCodigo { get; init; }
    public IReadOnlyList<Guid>? ServidorIds { get; init; }

    /// <summary>
    /// <c>setor</c>: só setores gerenciados. <c>institucional</c>: demais setores (visão global).
    /// </summary>
    public string? Escopo { get; init; }
}
