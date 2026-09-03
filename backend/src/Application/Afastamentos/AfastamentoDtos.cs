namespace TemplateSistema.Application.Afastamentos;

/// <summary>Servidor lotado em setor traz <c>SetorId</c>/<c>SetorNome</c>/<c>SetorSigla</c>;
/// lotado direto no núcleo traz <c>NucleoId</c>/<c>NucleoNome</c>/<c>NucleoSigla</c> — nunca os
/// dois, igual à lotação de <c>Servidor</c>.</summary>
public record AfastamentoDto(
    Guid Id,
    Guid ServidorId,
    string ServidorNome,
    string Matricula,
    Guid? SetorId,
    string? SetorNome,
    string? SetorSigla,
    Guid? NucleoId,
    string? NucleoNome,
    string? NucleoSigla,
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
