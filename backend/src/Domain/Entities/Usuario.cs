using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Usuario : BaseEntity
{
    public Guid ServidorId { get; private set; }
    public string Login { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public DateTime? UltimoLogin { get; private set; }
    public bool Bloqueado { get; private set; }
    public bool Ativo { get; private set; } = true;
    public bool DeveAlterarSenha { get; private set; }

    public Servidor Servidor { get; private set; } = null!;
    public ICollection<UsuarioPerfil> UsuarioPerfis { get; private set; } = [];

    private Usuario()
    {
    }

    public static Usuario Create(
        Guid servidorId,
        string login,
        string senhaHash,
        string? createdBy = null,
        Guid? id = null,
        bool deveAlterarSenha = true)
    {
        var usuario = new Usuario
        {
            ServidorId = servidorId,
            Login = login.Trim().ToLowerInvariant(),
            SenhaHash = senhaHash,
            Bloqueado = false,
            Ativo = true,
            DeveAlterarSenha = deveAlterarSenha,
        };

        if (id.HasValue)
        {
            usuario.SetId(id.Value);
        }

        usuario.MarkCreated(createdBy);
        return usuario;
    }

    public void AlterarSenha(string senhaHash, bool exigirTrocaNoProximoLogin, string? updatedBy = null)
    {
        SenhaHash = senhaHash;
        DeveAlterarSenha = exigirTrocaNoProximoLogin;
        MarkUpdated(updatedBy);
    }

    public void ConfirmarSenhaAlterada(string senhaHash, string? updatedBy = null)
    {
        SenhaHash = senhaHash;
        DeveAlterarSenha = false;
        MarkUpdated(updatedBy);
    }

    public void RegistrarLogin()
    {
        UltimoLogin = DateTime.UtcNow;
        MarkUpdated(Login);
    }

    public void Bloquear(string? updatedBy = null)
    {
        Bloqueado = true;
        MarkUpdated(updatedBy);
    }

    public void Desbloquear(string? updatedBy = null)
    {
        Bloqueado = false;
        MarkUpdated(updatedBy);
    }

    public void Ativar(string? updatedBy = null)
    {
        Ativo = true;
        MarkUpdated(updatedBy);
    }

    public void Desativar(string? updatedBy = null)
    {
        Ativo = false;
        MarkUpdated(updatedBy);
    }

    public void DefinirPerfis(IEnumerable<Guid> perfilIds, string? updatedBy = null)
    {
        UsuarioPerfis.Clear();
        foreach (var perfilId in perfilIds.Distinct())
        {
            UsuarioPerfis.Add(UsuarioPerfil.Create(Id, perfilId));
        }

        MarkUpdated(updatedBy);
    }
}
