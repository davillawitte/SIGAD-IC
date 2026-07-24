using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class Escala : BaseEntity
{
    public Guid SetorId { get; private set; }
    public int Ano { get; private set; }
    public int Mes { get; private set; }
    public TipoFuncionamento TipoFuncionamento { get; private set; } = TipoFuncionamento.Expediente;
    public StatusEscala Status { get; private set; } = StatusEscala.Rascunho;
    public string? Observacao { get; private set; }
    public DateTime? PublicadaEm { get; private set; }
    public string? PublicadaPor { get; private set; }

    public Setor Setor { get; private set; } = null!;
    public ICollection<EscalaServidor> Servidores { get; private set; } = [];
    public ICollection<SolicitacaoDevolucaoEscala> SolicitacoesDevolucao { get; private set; } = [];

    public DateOnly DataInicio => new(Ano, Mes, 1);

    public DateOnly DataFim =>
        DateOnly.FromDateTime(new DateTime(Ano, Mes, 1).AddMonths(1).AddDays(-1));

    private Escala()
    {
    }

    public static Escala Create(
        Guid setorId,
        int ano,
        int mes,
        TipoFuncionamento tipoFuncionamento = TipoFuncionamento.Expediente,
        string? observacao = null,
        string? createdBy = null)
    {
        ValidatePeriodo(ano, mes);

        var escala = new Escala
        {
            SetorId = setorId,
            Ano = ano,
            Mes = mes,
            TipoFuncionamento = tipoFuncionamento,
            Status = StatusEscala.Rascunho,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
        };

        escala.MarkCreated(createdBy);
        return escala;
    }

    public void Atualizar(
        int ano,
        int mes,
        TipoFuncionamento tipoFuncionamento,
        string? observacao,
        string? updatedBy = null)
    {
        EnsureEditable();
        ValidatePeriodo(ano, mes);
        Ano = ano;
        Mes = mes;
        TipoFuncionamento = tipoFuncionamento;
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        MarkUpdated(updatedBy);
    }

    public void Finalizar(string? updatedBy = null)
    {
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

    public void Publicar(string actorLogin, string? updatedBy = null)
    {
        if (Status != StatusEscala.Finalizada)
        {
            throw new InvalidOperationException("Somente escalas finalizadas podem ser publicadas.");
        }

        Status = StatusEscala.Publicada;
        PublicadaEm = DateTime.UtcNow;
        PublicadaPor = string.IsNullOrWhiteSpace(actorLogin) ? null : actorLogin.Trim();
        MarkUpdated(updatedBy ?? actorLogin);
    }

    public void MarcarDevolucaoSolicitada(string? updatedBy = null)
    {
        if (Status != StatusEscala.Publicada)
        {
            throw new InvalidOperationException(
                "Somente escalas publicadas podem ter devolução solicitada.");
        }

        Status = StatusEscala.DevolucaoSolicitada;
        MarkUpdated(updatedBy);
    }

    public void CancelarSolicitacaoDevolucao(string? updatedBy = null)
    {
        if (Status != StatusEscala.DevolucaoSolicitada)
        {
            throw new InvalidOperationException(
                "Somente escalas com devolução solicitada podem voltar a publicada.");
        }

        Status = StatusEscala.Publicada;
        MarkUpdated(updatedBy);
    }

    public void DevolverParaFinalizada(string? updatedBy = null)
    {
        if (Status is not (StatusEscala.Publicada or StatusEscala.DevolucaoSolicitada))
        {
            throw new InvalidOperationException(
                "Somente escalas publicadas ou com devolução solicitada podem ser devolvidas.");
        }

        Status = StatusEscala.Finalizada;
        PublicadaEm = null;
        PublicadaPor = null;
        MarkUpdated(updatedBy);
    }

    public void EnsureEditable()
    {
        if (Status is not (StatusEscala.Rascunho or StatusEscala.Finalizada))
        {
            throw new InvalidOperationException(
                "A escala só pode ser alterada enquanto estiver em rascunho ou finalizada.");
        }
    }

    public static string FormatIdentificacao(int mes, int ano, string setorNome) =>
        $"Escala de {MesAbrev(mes)}/{ano} - {setorNome}";

    public static string NomeMes(int mes) => mes switch
    {
        1 => "Janeiro",
        2 => "Fevereiro",
        3 => "Março",
        4 => "Abril",
        5 => "Maio",
        6 => "Junho",
        7 => "Julho",
        8 => "Agosto",
        9 => "Setembro",
        10 => "Outubro",
        11 => "Novembro",
        12 => "Dezembro",
        _ => mes.ToString(),
    };

    public static string MesAbrev(int mes) => mes switch
    {
        1 => "Jan",
        2 => "Fev",
        3 => "Mar",
        4 => "Abr",
        5 => "Mai",
        6 => "Jun",
        7 => "Jul",
        8 => "Ago",
        9 => "Set",
        10 => "Out",
        11 => "Nov",
        12 => "Dez",
        _ => mes.ToString(),
    };

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
