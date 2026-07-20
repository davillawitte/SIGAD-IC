namespace TemplateSistema.Domain.Common;

/// <summary>
/// Metadados básicos de rastreabilidade (não é auditoria imutável).
/// TODO: Auditoria imutável, event sourcing e cadeia de custódia serão
/// reintroduzidos quando a equipe crescer — ver ADR-0001.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public string? UpdatedBy { get; protected set; }
}
