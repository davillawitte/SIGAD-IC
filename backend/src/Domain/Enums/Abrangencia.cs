namespace TemplateSistema.Domain.Enums;

/// <summary>
/// Alcance de um módulo dentro de um perfil. A ausência de registro equivale a
/// <see cref="MeusSetores"/> — o default mais restritivo.
/// </summary>
public enum Abrangencia
{
    MeusSetores = 1,
    TodosOsSetores = 2,
}
