using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Setor : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Sigla { get; private set; } = null!;
    public Guid? ChefeServidorId { get; private set; }
    public bool Ativo { get; private set; } = true;

    public Servidor? ChefeServidor { get; private set; }
    public ICollection<Servidor> Servidores { get; private set; } = [];

    private Setor()
    {
    }

    public static Setor Create(string nome, string sigla, string? createdBy = null, Guid? id = null)
    {
        var setor = new Setor
        {
            Nome = nome.Trim(),
            Sigla = sigla.Trim().ToUpperInvariant(),
            Ativo = true,
        };

        if (id.HasValue)
        {
            setor.SetId(id.Value);
        }

        setor.MarkCreated(createdBy);
        return setor;
    }

    public void Atualizar(string nome, string sigla, string? updatedBy = null)
    {
        Nome = nome.Trim();
        Sigla = sigla.Trim().ToUpperInvariant();
        MarkUpdated(updatedBy);
    }

    public void DefinirChefe(Guid? chefeServidorId, string? updatedBy = null)
    {
        ChefeServidorId = chefeServidorId;
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
}
