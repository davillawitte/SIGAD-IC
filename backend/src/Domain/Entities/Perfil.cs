using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Perfil : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Codigo { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public bool Sistema { get; private set; }
    public bool Ativo { get; private set; } = true;

    public ICollection<PerfilPermissao> PerfilPermissoes { get; private set; } = [];
    public ICollection<UsuarioPerfil> UsuarioPerfis { get; private set; } = [];

    private Perfil()
    {
    }

    public static Perfil Create(
        string nome,
        string codigo,
        string? descricao = null,
        bool sistema = false,
        string? createdBy = null,
        Guid? id = null)
    {
        var perfil = new Perfil
        {
            Nome = nome.Trim(),
            Codigo = NormalizeCodigo(codigo),
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            Sistema = sistema,
            Ativo = true,
        };

        if (id.HasValue)
        {
            perfil.SetId(id.Value);
        }

        perfil.MarkCreated(createdBy);
        return perfil;
    }

    public void Atualizar(string nome, string? descricao, string? updatedBy = null)
    {
        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        MarkUpdated(updatedBy);
    }

    public void Ativar(string? updatedBy = null)
    {
        Ativo = true;
        MarkUpdated(updatedBy);
    }

    public void Desativar(string? updatedBy = null)
    {
        if (Sistema)
        {
            throw new InvalidOperationException("Perfis de sistema não podem ser desativados.");
        }

        Ativo = false;
        MarkUpdated(updatedBy);
    }

    public void DefinirPermissoes(IEnumerable<Guid> permissaoIds, string? updatedBy = null)
    {
        PerfilPermissoes.Clear();
        foreach (var permissaoId in permissaoIds.Distinct())
        {
            PerfilPermissoes.Add(PerfilPermissao.Create(Id, permissaoId));
        }

        MarkUpdated(updatedBy);
    }

    public static string NormalizeCodigo(string codigo) =>
        codigo.Trim().ToUpperInvariant().Replace(' ', '_');
}
