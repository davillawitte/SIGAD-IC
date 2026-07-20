namespace TemplateSistema.Domain.Entities;

public class PerfilPermissao
{
    public Guid PerfilId { get; private set; }
    public Guid PermissaoId { get; private set; }

    public Perfil Perfil { get; private set; } = null!;
    public Permissao Permissao { get; private set; } = null!;

    private PerfilPermissao()
    {
    }

    public static PerfilPermissao Create(Guid perfilId, Guid permissaoId) =>
        new()
        {
            PerfilId = perfilId,
            PermissaoId = permissaoId,
        };
}
