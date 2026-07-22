using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Nucleo : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Sigla { get; private set; } = null!;
    public Guid? ChefeServidorId { get; private set; }

    public Servidor? ChefeServidor { get; private set; }
    public ICollection<Setor> Setores { get; private set; } = [];

    private Nucleo()
    {
    }

    public static Nucleo Create(
        string nome,
        string sigla,
        Guid? chefeServidorId = null,
        string? createdBy = null,
        Guid? id = null)
    {
        var nucleo = new Nucleo
        {
            Nome = nome.Trim(),
            Sigla = NormalizeSigla(sigla),
            ChefeServidorId = chefeServidorId,
        };

        if (id.HasValue)
        {
            nucleo.SetId(id.Value);
        }

        nucleo.MarkCreated(createdBy);
        return nucleo;
    }

    public void Atualizar(string nome, string sigla, Guid? chefeServidorId, string? updatedBy = null)
    {
        Nome = nome.Trim();
        Sigla = NormalizeSigla(sigla);
        ChefeServidorId = chefeServidorId;
        MarkUpdated(updatedBy);
    }

    public static string NormalizeSigla(string sigla) => sigla.Trim();
}
