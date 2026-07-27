namespace TemplateSistema.Application.Abstractions;

/// <summary>Recurso cujo acesso depende do setor.</summary>
public interface ISetorScoped
{
    Guid SetorId { get; }
}
