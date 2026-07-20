namespace TemplateSistema.Domain.Entities;

public class UsuarioPerfil
{
    public Guid UsuarioId { get; private set; }
    public Guid PerfilId { get; private set; }

    public Usuario Usuario { get; private set; } = null!;
    public Perfil Perfil { get; private set; } = null!;

    private UsuarioPerfil()
    {
    }

    public static UsuarioPerfil Create(Guid usuarioId, Guid perfilId) =>
        new()
        {
            UsuarioId = usuarioId,
            PerfilId = perfilId,
        };
}
