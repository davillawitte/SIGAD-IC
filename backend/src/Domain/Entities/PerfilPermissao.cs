using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class PerfilPermissao
{
    public Guid PerfilId { get; private set; }
    public Guid PermissaoId { get; private set; }
    public Abrangencia Abrangencia { get; private set; } = Abrangencia.MeusSetores;

    public Perfil Perfil { get; private set; } = null!;
    public Permissao Permissao { get; private set; } = null!;

    private PerfilPermissao()
    {
    }

    public static PerfilPermissao Create(
        Guid perfilId,
        Guid permissaoId,
        Abrangencia abrangencia = Abrangencia.MeusSetores) =>
        new()
        {
            PerfilId = perfilId,
            PermissaoId = permissaoId,
            Abrangencia = Enum.IsDefined(abrangencia) ? abrangencia : Abrangencia.MeusSetores,
        };

    public void DefinirAbrangencia(Abrangencia abrangencia) =>
        Abrangencia = Enum.IsDefined(abrangencia) ? abrangencia : Abrangencia.MeusSetores;
}
