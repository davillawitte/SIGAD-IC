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
        MarkUpdated(updatedBy);
    }
}
