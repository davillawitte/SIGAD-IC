using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Permissao : BaseEntity
{
    public string Codigo { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public string Modulo { get; private set; } = null!;
    public bool Sistema { get; private set; }
    public bool Ativo { get; private set; } = true;

    public ICollection<PerfilPermissao> PerfilPermissoes { get; private set; } = [];

    private Permissao()
    {
    }

    public static Permissao Create(
        string codigo,
        string nome,
        string modulo,
        string? descricao = null,
        bool sistema = true,
        string? createdBy = null,
        Guid? id = null)
    {
        var permissao = new Permissao
        {
            Codigo = NormalizeCodigo(codigo),
            Nome = nome.Trim(),
            Modulo = modulo.Trim().ToLowerInvariant(),
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            Sistema = sistema,
            Ativo = true,
        };

        if (id.HasValue)
        {
            permissao.SetId(id.Value);
        }

        permissao.MarkCreated(createdBy);
        return permissao;
    }

    public void Atualizar(string nome, string modulo, string? descricao, string? updatedBy = null)
    {
        Nome = nome.Trim();
        Modulo = modulo.Trim().ToLowerInvariant();
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
            throw new InvalidOperationException("Permissões de sistema não podem ser desativadas.");
        }

        Ativo = false;
        MarkUpdated(updatedBy);
    }

    public static string NormalizeCodigo(string codigo) =>
        codigo.Trim().ToLowerInvariant();
}
