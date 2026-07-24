using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class EscalaJornada : BaseEntity
{
    public Guid EscalaServidorId { get; private set; }
    public Guid? PadraoEscalaId { get; private set; }
    public TipoJornada TipoJornada { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public DateOnly? DataInicioCiclo { get; private set; }
    public TimeOnly? HoraInicio { get; private set; }
    public TimeOnly? HoraFim { get; private set; }
    public decimal? Horas { get; private set; }
    public string TipoOcorrenciaCodigo { get; private set; } = null!;
    public RecorrenciaTipo RecorrenciaTipo { get; private set; }
    public string? DiasSemana { get; private set; }
    public int? IntervaloDias { get; private set; }
    public int? DiasTrabalho { get; private set; }
    public int? DiasFolga { get; private set; }
    public string? TipoOcorrenciaFolgaCodigo { get; private set; }
    public string? Observacao { get; private set; }

    public EscalaServidor EscalaServidor { get; private set; } = null!;
    public PadraoEscala? PadraoEscala { get; private set; }
    public TipoOcorrencia TipoOcorrencia { get; private set; } = null!;
    public ICollection<EscalaOcorrencia> Ocorrencias { get; private set; } = [];

    private EscalaJornada()
    {
    }

    public static EscalaJornada Create(
        Guid escalaServidorId,
        TipoJornada tipoJornada,
        DateOnly dataInicio,
        DateOnly dataFim,
        string tipoOcorrenciaCodigo,
        RecorrenciaTipo recorrenciaTipo,
        TimeOnly? horaInicio = null,
        TimeOnly? horaFim = null,
        decimal? horas = null,
        string? diasSemana = null,
        int? intervaloDias = null,
        int? diasTrabalho = null,
        int? diasFolga = null,
        string? tipoOcorrenciaFolgaCodigo = null,
        string? observacao = null,
        Guid? padraoEscalaId = null,
        DateOnly? dataInicioCiclo = null,
        string? createdBy = null)
    {
        if (dataFim < dataInicio)
        {
            throw new ArgumentException("Data fim deve ser maior ou igual à data início.");
        }

        var jornada = new EscalaJornada
        {
            EscalaServidorId = escalaServidorId,
            PadraoEscalaId = padraoEscalaId,
            TipoJornada = tipoJornada,
            DataInicio = dataInicio,
            DataFim = dataFim,
            DataInicioCiclo = dataInicioCiclo ?? (recorrenciaTipo == RecorrenciaTipo.CicloPlantao ? dataInicio : null),
            HoraInicio = horaInicio,
            HoraFim = horaFim,
            Horas = horas,
            TipoOcorrenciaCodigo = tipoOcorrenciaCodigo.Trim().ToUpperInvariant(),
            RecorrenciaTipo = recorrenciaTipo,
            DiasSemana = string.IsNullOrWhiteSpace(diasSemana) ? null : diasSemana.Trim(),
            IntervaloDias = intervaloDias,
            DiasTrabalho = diasTrabalho,
            DiasFolga = diasFolga,
            TipoOcorrenciaFolgaCodigo = string.IsNullOrWhiteSpace(tipoOcorrenciaFolgaCodigo)
                ? "D"
                : tipoOcorrenciaFolgaCodigo.Trim().ToUpperInvariant(),
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
        };

        jornada.MarkCreated(createdBy);
        return jornada;
    }
}
