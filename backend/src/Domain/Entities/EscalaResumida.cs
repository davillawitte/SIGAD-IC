using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

/// <summary>
/// Escala de rodízio por núcleo — usada por servidores chefes de núcleo ou lotados
/// diretamente no núcleo, que circulam entre todos os setores que ele engloba. Diferente
/// da <see cref="Escala"/> (por setor, servidor fixo x dia com código de ocorrência), aqui
/// a coluna é uma vaga de equipe e a célula é quem ocupa aquela vaga naquele dia.
/// </summary>
public class EscalaResumida : BaseEntity
{
    /// <summary>Exatamente um entre <see cref="NucleoId"/> e <see cref="SetorId"/> é
    /// preenchido: escala resumida de núcleo (compartilhada entre os setores que ele engloba,
    /// mais o grupo "Agentes") ou de um único setor sem núcleo (sem grupo de setores
    /// participantes — o próprio setor já é o único container).</summary>
    public Guid? NucleoId { get; private set; }

    public Guid? SetorId { get; private set; }
    public int Ano { get; private set; }
    public int Mes { get; private set; }
    public StatusEscala Status { get; private set; } = StatusEscala.Rascunho;
    public string? Observacao { get; private set; }

    /// <summary>Escala (por setor) cujo wizard originou esta escala resumida — meramente
    /// informativo: a escala resumida continua compartilhada por núcleo+período entre
    /// setores, então isto não é usado como escopo de nenhuma consulta.</summary>
    public Guid? EscalaId { get; private set; }

    public Nucleo? Nucleo { get; private set; }
    public Setor? Setor { get; private set; }
    public Escala? Escala { get; private set; }
    public ICollection<EscalaResumidaSetor> Setores { get; private set; } = [];

    public DateOnly DataInicio => new(Ano, Mes, 1);

    public DateOnly DataFim =>
        DateOnly.FromDateTime(new DateTime(Ano, Mes, 1).AddMonths(1).AddDays(-1));

    private EscalaResumida()
    {
    }

    public static EscalaResumida Create(
        Guid? nucleoId,
        int ano,
        int mes,
        string? observacao = null,
        string? createdBy = null,
        Guid? setorId = null)
    {
        if (nucleoId is null == setorId is null)
        {
            throw new ArgumentException("Informe exatamente um: núcleo ou setor.");
        }

        ValidatePeriodo(ano, mes);

        var escala = new EscalaResumida
        {
            NucleoId = nucleoId,
            SetorId = setorId,
            Ano = ano,
            Mes = mes,
            Status = StatusEscala.Rascunho,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
        };

        escala.MarkCreated(createdBy);
        return escala;
    }

    public void Atualizar(string? observacao, string? updatedBy = null)
    {
        EnsureEditable();
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        MarkUpdated(updatedBy);
    }

    public void Finalizar(string? updatedBy = null)
    {
        // Escala finalizada continua editável; "Finalizar" de novo só confirma a edição.
        if (Status == StatusEscala.Finalizada)
        {
            MarkUpdated(updatedBy);
            return;
        }

        if (Status != StatusEscala.Rascunho)
        {
            throw new InvalidOperationException("Somente escalas em rascunho podem ser finalizadas.");
        }

        Status = StatusEscala.Finalizada;
        MarkUpdated(updatedBy);
    }

    public void ReabrirParaRascunho(string? updatedBy = null)
    {
        if (Status != StatusEscala.Finalizada)
        {
            throw new InvalidOperationException("Somente escalas finalizadas podem voltar para rascunho.");
        }

        Status = StatusEscala.Rascunho;
        MarkUpdated(updatedBy);
    }

    /// <summary>Grava o vínculo com a escala de origem só se ainda não houver um — a escala
    /// resumida é compartilhada por núcleo+período entre vários setores, então o primeiro
    /// wizard a resolvê-la é quem "ganha" a atribuição; os demais reaproveitam sem sobrescrever.</summary>
    public void VincularEscala(Guid escalaId, string? updatedBy = null)
    {
        if (EscalaId.HasValue)
        {
            return;
        }

        EscalaId = escalaId;
        MarkUpdated(updatedBy);
    }

    /// <summary>Escala resumida nunca é publicada nem tem devolução — <see cref="StatusEscala.Publicada"/>
    /// e <see cref="StatusEscala.DevolucaoSolicitada"/> ficam inatingíveis para este agregado
    /// (o enum é compartilhado com <see cref="Escala"/>, que usa os dois).</summary>
    public void EnsureEditable()
    {
        if (Status is not (StatusEscala.Rascunho or StatusEscala.Finalizada))
        {
            throw new InvalidOperationException(
                "A escala só pode ser alterada enquanto estiver em rascunho ou finalizada.");
        }
    }

    public static string FormatIdentificacao(int mes, int ano, string nucleoNome) =>
        $"Escala Resumida de {Escala.MesAbrev(mes)}/{ano} - {nucleoNome}";

    private static void ValidatePeriodo(int ano, int mes)
    {
        if (ano is < 2000 or > 2100)
        {
            throw new ArgumentException("Ano inválido.");
        }

        if (mes is < 1 or > 12)
        {
            throw new ArgumentException("Mês inválido.");
        }
    }
}
