using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

/// <summary>
/// Uma posição do pool de rodízio de uma equipe. <see cref="ServidorId"/> nulo representa
/// uma vaga em aberto no ciclo (renderiza "DO" — Diária Operacional disponível) — não é caso
/// especial, é só mais uma posição do pool. <see cref="ServidorId2"/> é a segunda função
/// opcional na MESMA posição (ex.: vaga de Agentes com motorista + fotógrafo): nula sem
/// <see cref="IsFolga2"/> = sem segunda função; nula com <see cref="IsFolga2"/> = a segunda função
/// fica em aberto ("DO"), mesmo com a primeira ocupada.
/// </summary>
public class EscalaResumidaRotacaoMembro : BaseEntity
{
    public Guid EscalaResumidaEquipeId { get; private set; }
    public int Posicao { get; private set; }
    public Guid? ServidorId { get; private set; }
    public Guid? ServidorId2 { get; private set; }
    public bool IsFolga2 { get; private set; }

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
        string? createdBy = null,
        bool isFolga2 = false)
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
            IsFolga2 = servidorId2 is null && isFolga2,
        };

        membro.MarkCreated(createdBy);
        return membro;
    }
}
