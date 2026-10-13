using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class EscalaOcorrencia : BaseEntity
{
    public Guid EscalaServidorId { get; private set; }
    public DateOnly Data { get; private set; }
    public string TipoOcorrenciaCodigo { get; private set; } = null!;
    public TimeOnly? HoraInicio { get; private set; }
    public TimeOnly? HoraFim { get; private set; }
    public decimal? Horas { get; private set; }
    public OrigemOcorrencia Origem { get; private set; }
    public Guid? EscalaJornadaId { get; private set; }
    public string? Observacao { get; private set; }

    /// <summary>Célula alterada por permuta (exibida com "*"). Os campos <c>*Original</c> guardam
    /// o valor de antes da permuta para recalcular/desfazer; <see cref="CodigoOriginal"/> nulo numa
    /// célula permutada = a célula não existia e foi criada pela permuta.</summary>
    public bool Permutada { get; private set; }
    public Guid? PermutaId { get; private set; }
    public string? CodigoOriginal { get; private set; }
    public TimeOnly? HoraInicioOriginal { get; private set; }
    public TimeOnly? HoraFimOriginal { get; private set; }
    public decimal? HorasOriginais { get; private set; }
    public string? ObservacaoOriginal { get; private set; }
    public OrigemOcorrencia? OrigemOriginal { get; private set; }
    public Guid? EscalaJornadaIdOriginal { get; private set; }

    public EscalaServidor EscalaServidor { get; private set; } = null!;
    public TipoOcorrencia TipoOcorrencia { get; private set; } = null!;
    public EscalaJornada? EscalaJornada { get; private set; }

    private EscalaOcorrencia()
    {
    }

    public static EscalaOcorrencia Create(
        Guid escalaServidorId,
        DateOnly data,
        string tipoOcorrenciaCodigo,
        OrigemOcorrencia origem,
        TimeOnly? horaInicio = null,
        TimeOnly? horaFim = null,
        decimal? horas = null,
        Guid? escalaJornadaId = null,
        string? observacao = null,
        string? createdBy = null)
    {
        var ocorrencia = new EscalaOcorrencia
        {
            EscalaServidorId = escalaServidorId,
            Data = data,
            TipoOcorrenciaCodigo = tipoOcorrenciaCodigo.Trim().ToUpperInvariant(),
            HoraInicio = horaInicio,
            HoraFim = horaFim,
            Horas = horas,
            Origem = origem,
            EscalaJornadaId = escalaJornadaId,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
        };

        ocorrencia.MarkCreated(createdBy);
        return ocorrencia;
    }

    public void AtualizarManual(
        string tipoOcorrenciaCodigo,
        TimeOnly? horaInicio,
        TimeOnly? horaFim,
        decimal? horas,
        string? observacao,
        string? updatedBy = null)
    {
        TipoOcorrenciaCodigo = tipoOcorrenciaCodigo.Trim().ToUpperInvariant();
        HoraInicio = horaInicio;
        HoraFim = horaFim;
        Horas = horas;
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        Origem = OrigemOcorrencia.Manual;
        EscalaJornadaId = null;
        // Valor gravado por edição vira a nova base; a permuta é reaplicada por cima depois.
        LimparPermuta();
        MarkUpdated(updatedBy);
    }

    public void AtualizarPorRegra(
        string tipoOcorrenciaCodigo,
        TimeOnly? horaInicio,
        TimeOnly? horaFim,
        decimal? horas,
        Guid escalaJornadaId,
        string? observacao,
        string? updatedBy = null)
    {
        TipoOcorrenciaCodigo = tipoOcorrenciaCodigo.Trim().ToUpperInvariant();
        HoraInicio = horaInicio;
        HoraFim = horaFim;
        Horas = horas;
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        Origem = OrigemOcorrencia.Regra;
        EscalaJornadaId = escalaJornadaId;
        LimparPermuta();
        MarkUpdated(updatedBy);
    }

    /// <summary>Valor atual da célula.</summary>
    public CelulaEscala Valor => new(TipoOcorrenciaCodigo, HoraInicio, HoraFim, Horas, Observacao);

    /// <summary>Valor sem nenhuma permuta aplicada; nulo quando a célula foi criada pela permuta.</summary>
    public CelulaEscala? ValorBase => !Permutada
        ? Valor
        : CodigoOriginal is null
            ? null
            : new CelulaEscala(CodigoOriginal, HoraInicioOriginal, HoraFimOriginal, HorasOriginais, ObservacaoOriginal);

    /// <summary>Célula que não existia e passa a existir por causa de uma permuta.</summary>
    public static EscalaOcorrencia CriarPorPermuta(
        Guid escalaServidorId,
        DateOnly data,
        CelulaEscala valor,
        Guid permutaId,
        string? createdBy = null)
    {
        var ocorrencia = Create(
            escalaServidorId,
            data,
            valor.Codigo,
            OrigemOcorrencia.Manual,
            valor.HoraInicio,
            valor.HoraFim,
            valor.Horas,
            observacao: valor.Observacao,
            createdBy: createdBy);
        ocorrencia.Permutada = true;
        ocorrencia.PermutaId = permutaId;
        return ocorrencia;
    }

    /// <summary>Grava o valor resultante da permuta, guardando o original só na primeira vez (numa
    /// permuta da permuta o original continua sendo o de antes de qualquer troca).</summary>
    public void AplicarPermuta(CelulaEscala valor, Guid permutaId, string? updatedBy = null)
    {
        if (!Permutada)
        {
            CodigoOriginal = TipoOcorrenciaCodigo;
            HoraInicioOriginal = HoraInicio;
            HoraFimOriginal = HoraFim;
            HorasOriginais = Horas;
            ObservacaoOriginal = Observacao;
            OrigemOriginal = Origem;
            EscalaJornadaIdOriginal = EscalaJornadaId;
            Permutada = true;
        }

        PermutaId = permutaId;
        TipoOcorrenciaCodigo = valor.Codigo.Trim().ToUpperInvariant();
        HoraInicio = valor.HoraInicio;
        HoraFim = valor.HoraFim;
        Horas = valor.Horas;
        Observacao = string.IsNullOrWhiteSpace(valor.Observacao) ? null : valor.Observacao.Trim();
        // Manual: regenerar a jornada não sobrescreve a célula permutada.
        Origem = OrigemOcorrencia.Manual;
        EscalaJornadaId = null;
        MarkUpdated(updatedBy);
    }

    /// <summary>Volta ao valor de antes da permuta. Não serve para célula criada pela permuta
    /// (<see cref="CodigoOriginal"/> nulo) — essa deve ser removida.</summary>
    public void RestaurarOriginal(string? updatedBy = null)
    {
        if (!Permutada)
        {
            return;
        }

        if (CodigoOriginal is null)
        {
            throw new InvalidOperationException("Célula criada pela permuta deve ser removida, não restaurada.");
        }

        TipoOcorrenciaCodigo = CodigoOriginal;
        HoraInicio = HoraInicioOriginal;
        HoraFim = HoraFimOriginal;
        Horas = HorasOriginais;
        Observacao = ObservacaoOriginal;
        Origem = OrigemOriginal ?? OrigemOcorrencia.Manual;
        EscalaJornadaId = EscalaJornadaIdOriginal;
        LimparPermuta();
        MarkUpdated(updatedBy);
    }

    private void LimparPermuta()
    {
        Permutada = false;
        PermutaId = null;
        CodigoOriginal = null;
        HoraInicioOriginal = null;
        HoraFimOriginal = null;
        HorasOriginais = null;
        ObservacaoOriginal = null;
        OrigemOriginal = null;
        EscalaJornadaIdOriginal = null;
    }
}

/// <summary>Conteúdo de uma célula da escala (o que a permuta troca entre os servidores).</summary>
public sealed record CelulaEscala(
    string Codigo,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    decimal? Horas,
    string? Observacao)
{
    public bool MesmoConteudo(CelulaEscala? outra) =>
        outra is not null
        && string.Equals(Codigo, outra.Codigo, StringComparison.OrdinalIgnoreCase)
        && HoraInicio == outra.HoraInicio
        && HoraFim == outra.HoraFim
        && Horas == outra.Horas;
}
