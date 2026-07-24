using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class SolicitacaoDevolucaoEscala : BaseEntity
{
    public Guid EscalaId { get; private set; }
    public Guid SolicitanteUsuarioId { get; private set; }
    public string Justificativa { get; private set; } = null!;
    public StatusSolicitacaoDevolucao Status { get; private set; } = StatusSolicitacaoDevolucao.Pendente;
    public string? RespondidoPor { get; private set; }
    public DateTime? RespostaEm { get; private set; }
    public string? ObservacaoResposta { get; private set; }

    public Escala Escala { get; private set; } = null!;
    public Usuario SolicitanteUsuario { get; private set; } = null!;

    private SolicitacaoDevolucaoEscala()
    {
    }

    public static SolicitacaoDevolucaoEscala Create(
        Guid escalaId,
        Guid solicitanteUsuarioId,
        string justificativa,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(justificativa))
        {
            throw new ArgumentException("Justificativa é obrigatória.");
        }

        var entity = new SolicitacaoDevolucaoEscala
        {
            EscalaId = escalaId,
            SolicitanteUsuarioId = solicitanteUsuarioId,
            Justificativa = justificativa.Trim(),
            Status = StatusSolicitacaoDevolucao.Pendente,
        };

        entity.MarkCreated(createdBy);
        return entity;
    }

    public void Aprovar(string respondidoPor, string? observacaoResposta = null)
    {
        EnsurePendente();
        Status = StatusSolicitacaoDevolucao.Aprovada;
        RespondidoPor = respondidoPor.Trim();
        RespostaEm = DateTime.UtcNow;
        ObservacaoResposta = string.IsNullOrWhiteSpace(observacaoResposta) ? null : observacaoResposta.Trim();
        MarkUpdated(respondidoPor);
    }

    public void Recusar(string respondidoPor, string? observacaoResposta = null)
    {
        EnsurePendente();
        Status = StatusSolicitacaoDevolucao.Recusada;
        RespondidoPor = respondidoPor.Trim();
        RespostaEm = DateTime.UtcNow;
        ObservacaoResposta = string.IsNullOrWhiteSpace(observacaoResposta) ? null : observacaoResposta.Trim();
        MarkUpdated(respondidoPor);
    }

    private void EnsurePendente()
    {
        if (Status != StatusSolicitacaoDevolucao.Pendente)
        {
            throw new InvalidOperationException("Somente solicitações pendentes podem ser respondidas.");
        }
    }
}
