using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

/// <summary>
/// Uma vaga/coluna de rodízio dentro de um setor da escala resumida (ex.: "Equipe 01",
/// "Plantonista 24h"). O pool ordenado de <see cref="EscalaResumidaRotacaoMembro"/> mais a
/// âncora <see cref="DataInicioCiclo"/> definem quem ocupa a vaga em cada dia — mesmo
/// espírito do <c>DataInicioCiclo</c> usado em <see cref="EscalaJornada"/> para plantões.
/// </summary>
public class EscalaResumidaEquipe : BaseEntity
{
    public Guid EscalaResumidaSetorId { get; private set; }
    public string Nome { get; private set; } = null!;
    public int Ordem { get; private set; }
    public DateOnly? DataInicioCiclo { get; private set; }

    public EscalaResumidaSetor EscalaResumidaSetor { get; private set; } = null!;
    public ICollection<EscalaResumidaRotacaoMembro> Rotacao { get; private set; } = [];
    public ICollection<EscalaResumidaDia> Dias { get; private set; } = [];

    private EscalaResumidaEquipe()
    {
    }

    public static EscalaResumidaEquipe Create(
        Guid escalaResumidaSetorId,
        string nome,
        int ordem,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome da equipe é obrigatório.");
        }

        var equipe = new EscalaResumidaEquipe
        {
            EscalaResumidaSetorId = escalaResumidaSetorId,
            Nome = nome.Trim(),
            Ordem = ordem,
        };

        equipe.MarkCreated(createdBy);
        return equipe;
    }

    public void Renomear(string nome, int ordem, string? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome da equipe é obrigatório.");
        }

        Nome = nome.Trim();
        Ordem = ordem;
        MarkUpdated(updatedBy);
    }

    public void DefinirAncora(DateOnly dataInicioCiclo, string? updatedBy = null)
    {
        DataInicioCiclo = dataInicioCiclo;
        MarkUpdated(updatedBy);
    }
}
