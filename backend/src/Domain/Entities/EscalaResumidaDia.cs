using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

/// <summary>
/// A célula materializada da grade (uma equipe, um dia). Mesmo padrão Regra/Manual de
/// <see cref="EscalaOcorrencia"/>: <see cref="Origem"/> = Regra quando veio do rodízio
/// (<see cref="RotacaoMembroId"/> aponta pra posição do pool que gerou), Manual quando foi
/// sobrescrita à mão (troca pontual, combinação de dois nomes, ou "DO" avulso).
/// </summary>
public class EscalaResumidaDia : BaseEntity
{
    public Guid EscalaResumidaEquipeId { get; private set; }
    public DateOnly Data { get; private set; }
    public Guid? ServidorId { get; private set; }
    public string? ServidorNomeSnapshot { get; private set; }
    /// <summary>Reforço opcional na mesma vaga/dia (ex.: Agentes com duas pessoas). Nulo
    /// simplesmente significa "sem reforço" — só <see cref="IsFolga2"/> marca "DO" explícito
    /// pra esse segundo lugar (usado só em override manual; dia de regra nunca marca DO2
    /// automaticamente, já que "sem reforço" e "reforço é DO" têm o mesmo efeito visual).</summary>
    public Guid? ServidorId2 { get; private set; }
    public string? ServidorNomeSnapshot2 { get; private set; }
    public bool IsFolga2 { get; private set; }
    public string? TextoLivre { get; private set; }
    public bool IsFolga { get; private set; }
    public OrigemOcorrencia Origem { get; private set; }
    public Guid? RotacaoMembroId { get; private set; }

    public EscalaResumidaEquipe EscalaResumidaEquipe { get; private set; } = null!;
    public Servidor? Servidor { get; private set; }
    public Servidor? Servidor2 { get; private set; }
    public EscalaResumidaRotacaoMembro? RotacaoMembro { get; private set; }

    /// <summary>Texto exibido na célula: texto livre tem prioridade (override total); senão
    /// combina pessoa principal + reforço (quando houver), cada um podendo ser um nome ou
    /// "DO".</summary>
    public string Rotulo
    {
        get
        {
            if (TextoLivre is not null)
            {
                return TextoLivre;
            }

            var principal = ServidorNomeSnapshot ?? (IsFolga ? "DO" : null);
            var segunda = ServidorNomeSnapshot2 ?? (IsFolga2 ? "DO" : null);
            if (principal is null)
            {
                return segunda ?? string.Empty;
            }

            return segunda is null ? principal : $"{principal} + {segunda}";
        }
    }

    private EscalaResumidaDia()
    {
    }

    public static EscalaResumidaDia CriarPorRegra(
        Guid escalaResumidaEquipeId,
        DateOnly data,
        Guid? servidorId,
        string? servidorNome,
        Guid? servidorId2,
        string? servidorNome2,
        Guid rotacaoMembroId,
        string? createdBy = null)
    {
        var dia = new EscalaResumidaDia
        {
            EscalaResumidaEquipeId = escalaResumidaEquipeId,
            Data = data,
            ServidorId = servidorId,
            ServidorNomeSnapshot = servidorNome,
            ServidorId2 = servidorId2,
            ServidorNomeSnapshot2 = servidorNome2,
            IsFolga2 = false,
            TextoLivre = null,
            IsFolga = servidorId is null,
            Origem = OrigemOcorrencia.Regra,
            RotacaoMembroId = rotacaoMembroId,
        };

        dia.MarkCreated(createdBy);
        return dia;
    }

    public static EscalaResumidaDia CriarManual(
        Guid escalaResumidaEquipeId,
        DateOnly data,
        Guid? servidorId,
        string? servidorNome,
        string? textoLivre,
        bool isFolga,
        Guid? servidorId2 = null,
        string? servidorNome2 = null,
        bool isFolga2 = false,
        string? createdBy = null)
    {
        var dia = new EscalaResumidaDia
        {
            EscalaResumidaEquipeId = escalaResumidaEquipeId,
            Data = data,
            ServidorId = servidorId,
            ServidorNomeSnapshot = servidorNome,
            ServidorId2 = servidorId2,
            ServidorNomeSnapshot2 = servidorNome2,
            IsFolga2 = servidorId2 is null && isFolga2,
            TextoLivre = string.IsNullOrWhiteSpace(textoLivre) ? null : textoLivre.Trim(),
            IsFolga = isFolga,
            Origem = OrigemOcorrencia.Manual,
            RotacaoMembroId = null,
        };

        dia.MarkCreated(createdBy);
        return dia;
    }

    public void AtualizarPorRegra(
        Guid? servidorId,
        string? servidorNome,
        Guid? servidorId2,
        string? servidorNome2,
        Guid rotacaoMembroId,
        string? updatedBy = null)
    {
        ServidorId = servidorId;
        ServidorNomeSnapshot = servidorNome;
        ServidorId2 = servidorId2;
        ServidorNomeSnapshot2 = servidorNome2;
        IsFolga2 = false;
        TextoLivre = null;
        IsFolga = servidorId is null;
        Origem = OrigemOcorrencia.Regra;
        RotacaoMembroId = rotacaoMembroId;
        MarkUpdated(updatedBy);
    }

    public void AtualizarManual(
        Guid? servidorId,
        string? servidorNome,
        string? textoLivre,
        bool isFolga,
        Guid? servidorId2 = null,
        string? servidorNome2 = null,
        bool isFolga2 = false,
        string? updatedBy = null)
    {
        ServidorId = servidorId;
        ServidorNomeSnapshot = servidorNome;
        ServidorId2 = servidorId2;
        ServidorNomeSnapshot2 = servidorNome2;
        IsFolga2 = servidorId2 is null && isFolga2;
        TextoLivre = string.IsNullOrWhiteSpace(textoLivre) ? null : textoLivre.Trim();
        IsFolga = isFolga;
        Origem = OrigemOcorrencia.Manual;
        RotacaoMembroId = null;
        MarkUpdated(updatedBy);
    }
}
