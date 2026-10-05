using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Nucleo : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Sigla { get; private set; } = null!;
    public Guid? ChefeServidorId { get; private set; }

    /// <summary>O "chefe" cadastrado só elabora a escala, não chefia de fato: na assinatura das
    /// escalas impressas sai apenas o nome dele, sem o título "Chefe do ...".</summary>
    public bool SomenteElaboraEscala { get; private set; }

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
        Guid? id = null,
        bool somenteElaboraEscala = false)
    {
        var nucleo = new Nucleo
        {
            Nome = nome.Trim(),
            Sigla = NormalizeSigla(sigla),
            ChefeServidorId = chefeServidorId,
            SomenteElaboraEscala = somenteElaboraEscala,
        };

        if (id.HasValue)
        {
            nucleo.SetId(id.Value);
        }

        nucleo.MarkCreated(createdBy);
        return nucleo;
    }

    public void Atualizar(
        string nome,
        string sigla,
        Guid? chefeServidorId,
        string? updatedBy = null,
        bool somenteElaboraEscala = false)
    {
        Nome = nome.Trim();
        Sigla = NormalizeSigla(sigla);
        ChefeServidorId = chefeServidorId;
        SomenteElaboraEscala = somenteElaboraEscala;
        MarkUpdated(updatedBy);
    }

    public static string NormalizeSigla(string sigla) => sigla.Trim();
}
