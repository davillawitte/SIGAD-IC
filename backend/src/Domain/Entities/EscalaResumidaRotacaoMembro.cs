using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

/// <summary>
/// Uma posição do pool de rodízio de uma equipe. <see cref="ServidorId"/> nulo representa
/// uma vaga de folga fixa no ciclo (renderiza "DO") — não é caso especial, é só mais uma
/// posição do pool. <see cref="ServidorId2"/> é um reforço opcional na MESMA posição (ex.:
/// vaga de Agentes com duas pessoas) — nulo simplesmente significa "sem reforço nesta
/// posição", sem distinção de DO (não faz sentido marcar reforço como folga: equivale a não
/// ter reforço nenhum).
/// </summary>
public class EscalaResumidaRotacaoMembro : BaseEntity
{
    public Guid EscalaResumidaEquipeId { get; private set; }
    public int Posicao { get; private set; }
    public Guid? ServidorId { get; private set; }
    public Guid? ServidorId2 { get; private set; }

    public EscalaResumidaEquipe EscalaResumidaEquipe { get; private set; } = null!;
    public Servidor? Servidor { get; private set; }
    public Servidor? Servidor2 { get; private set; }

    private EscalaResumidaRotacaoMembro()
    {
    }

    public static EscalaResumidaRotacaoMembro Create(
        Guid escalaResumidaEquipeId,
        int posicao,
        Guid? servidorId,
        Guid? servidorId2 = null,
        string? createdBy = null)
    {
        if (posicao < 0)
        {
            throw new ArgumentException("Posição inválida.");
        }

        var membro = new EscalaResumidaRotacaoMembro
        {
            EscalaResumidaEquipeId = escalaResumidaEquipeId,
            Posicao = posicao,
            ServidorId = servidorId,
            ServidorId2 = servidorId2,
        };

        membro.MarkCreated(createdBy);
        return membro;
    }
}
