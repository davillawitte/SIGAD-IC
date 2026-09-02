using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

/// <summary>Um dos setores do núcleo incluído nesta escala resumida — define a ordem
/// das colunas (agrupadas por setor) na grade. `SetorId` nulo representa o grupo especial
/// "Agentes": servidores lotados direto no núcleo, à disposição, sem setor específico.</summary>
public class EscalaResumidaSetor : BaseEntity
{
    /// <summary>Nome/sigla usados no snapshot do grupo "Agentes" (SetorId nulo).</summary>
    public const string AgentesLabel = "Agentes";

    public Guid EscalaResumidaId { get; private set; }
    public Guid? SetorId { get; private set; }
    public int Ordem { get; private set; }
    public string SetorNomeSnapshot { get; private set; } = null!;
    public string SetorSiglaSnapshot { get; private set; } = null!;

    public EscalaResumida EscalaResumida { get; private set; } = null!;
    public Setor? Setor { get; private set; }
    public ICollection<EscalaResumidaEquipe> Equipes { get; private set; } = [];

    private EscalaResumidaSetor()
    {
    }

    public static EscalaResumidaSetor Create(
        Guid escalaResumidaId,
        Guid? setorId,
        int ordem,
        string setorNome,
        string setorSigla,
        string? createdBy = null)
    {
        var item = new EscalaResumidaSetor
        {
            EscalaResumidaId = escalaResumidaId,
            SetorId = setorId,
            Ordem = ordem,
            SetorNomeSnapshot = setorNome.Trim(),
            SetorSiglaSnapshot = setorSigla.Trim(),
        };

        item.MarkCreated(createdBy);
        return item;
    }

    public void AtualizarOrdem(int ordem, string? updatedBy = null)
    {
        Ordem = ordem;
        MarkUpdated(updatedBy);
    }
}
