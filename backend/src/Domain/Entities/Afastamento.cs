using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Afastamento : BaseEntity
{
    public static readonly HashSet<string> TiposPermitidos =
        new(StringComparer.OrdinalIgnoreCase) { "FR", "LM", "LP", "LO" };

    public Guid ServidorId { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public string TipoOcorrenciaCodigo { get; private set; } = null!;
    public string? Observacao { get; private set; }
    public string? Sei { get; private set; }

    public Servidor Servidor { get; private set; } = null!;

    private Afastamento()
    {
    }

    public static Afastamento Create(
        Guid servidorId,
        DateOnly dataInicio,
        DateOnly dataFim,
        string tipoOcorrenciaCodigo,
        string? observacao = null,
        string? sei = null,
        string? createdBy = null)
    {
        Validate(dataInicio, dataFim, tipoOcorrenciaCodigo);

        var entity = new Afastamento
        {
            ServidorId = servidorId,
            DataInicio = dataInicio,
            DataFim = dataFim,
            TipoOcorrenciaCodigo = tipoOcorrenciaCodigo.Trim().ToUpperInvariant(),
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            Sei = NormalizeSei(sei),
        };

        entity.MarkCreated(createdBy);
        return entity;
    }

    public void Atualizar(
        DateOnly dataInicio,
        DateOnly dataFim,
        string tipoOcorrenciaCodigo,
        string? observacao,
        string? sei = null,
        string? updatedBy = null)
    {
        Validate(dataInicio, dataFim, tipoOcorrenciaCodigo);
        DataInicio = dataInicio;
        DataFim = dataFim;
        TipoOcorrenciaCodigo = tipoOcorrenciaCodigo.Trim().ToUpperInvariant();
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        Sei = NormalizeSei(sei);
        MarkUpdated(updatedBy);
    }

    private static string? NormalizeSei(string? sei) =>
        string.IsNullOrWhiteSpace(sei) ? null : sei.Trim();

    private static void Validate(DateOnly dataInicio, DateOnly dataFim, string tipoOcorrenciaCodigo)
    {
        if (dataFim < dataInicio)
        {
            throw new ArgumentException("Data fim deve ser maior ou igual à data início.");
        }

        if (string.IsNullOrWhiteSpace(tipoOcorrenciaCodigo)
            || !TiposPermitidos.Contains(tipoOcorrenciaCodigo.Trim()))
        {
            throw new ArgumentException("Tipo de afastamento inválido. Use FR, LM, LP ou LO.");
        }
    }
}
