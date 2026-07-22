using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Cargo : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Codigo { get; private set; } = null!;
    public bool Ativo { get; private set; } = true;

    public ICollection<Servidor> Servidores { get; private set; } = [];

    private Cargo()
    {
    }

    public static Cargo Create(
        string nome,
        string codigo,
        string? createdBy = null,
        Guid? id = null)
    {
        var cargo = new Cargo
        {
            Nome = nome.Trim(),
            Codigo = NormalizeCodigo(codigo),
            Ativo = true,
        };

        if (id.HasValue)
        {
            cargo.SetId(id.Value);
        }

        cargo.MarkCreated(createdBy);
        return cargo;
    }

    public void Atualizar(string nome, string? updatedBy = null)
    {
        Nome = nome.Trim();
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

    public static string NormalizeCodigo(string codigo) =>
        codigo.Trim().ToUpperInvariant().Replace(' ', '_');
}
