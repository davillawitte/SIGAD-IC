using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class PadraoEscala : BaseEntity
{
    public string Codigo { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public TipoFuncionamento TipoFuncionamento { get; private set; }
    public TipoJornada TipoJornada { get; private set; }
    public RecorrenciaTipo RecorrenciaTipo { get; private set; }
    public int? DiasTrabalho { get; private set; }
    public int? DiasFolga { get; private set; }
    public string? DiasSemana { get; private set; }
    public string TipoOcorrenciaTrabalho { get; private set; } = null!;
    public string TipoOcorrenciaFolga { get; private set; } = null!;

    /// <summary>Só usado quando <c>RecorrenciaTipo == CicloPersonalizado</c>: sequência de
    /// códigos de ocorrência, um por dia do ciclo (ex.: "PT,D,D,D,TL12,D").</summary>
    public string? SequenciaCiclo { get; private set; }
    public TimeOnly? HoraInicioPadrao { get; private set; }
    public TimeOnly? HoraFimPadrao { get; private set; }
    public decimal? HorasPadrao { get; private set; }
    public bool Sistema { get; private set; }
    public bool Ativo { get; private set; } = true;
    public Guid? SetorId { get; private set; }

    public Setor? Setor { get; private set; }

    private PadraoEscala()
    {
    }

    public static PadraoEscala Create(
        string codigo,
        string nome,
        TipoFuncionamento tipoFuncionamento,
        TipoJornada tipoJornada,
        RecorrenciaTipo recorrenciaTipo,
        string tipoOcorrenciaTrabalho,
        string tipoOcorrenciaFolga = "D",
        int? diasTrabalho = null,
        int? diasFolga = null,
        string? diasSemana = null,
        string? sequenciaCiclo = null,
        TimeOnly? horaInicioPadrao = null,
        TimeOnly? horaFimPadrao = null,
        decimal? horasPadrao = null,
        bool sistema = false,
        Guid? setorId = null,
        string? createdBy = null,
        Guid? id = null)
    {
        var padrao = new PadraoEscala
        {
            Codigo = codigo.Trim().ToUpperInvariant(),
            Nome = nome.Trim(),
            TipoFuncionamento = tipoFuncionamento,
            TipoJornada = tipoJornada,
            RecorrenciaTipo = recorrenciaTipo,
            DiasTrabalho = diasTrabalho,
            DiasFolga = diasFolga,
            DiasSemana = string.IsNullOrWhiteSpace(diasSemana) ? null : diasSemana.Trim(),
            TipoOcorrenciaTrabalho = tipoOcorrenciaTrabalho.Trim().ToUpperInvariant(),
            TipoOcorrenciaFolga = tipoOcorrenciaFolga.Trim().ToUpperInvariant(),
            SequenciaCiclo = string.IsNullOrWhiteSpace(sequenciaCiclo) ? null : sequenciaCiclo.Trim().ToUpperInvariant(),
            HoraInicioPadrao = horaInicioPadrao,
            HoraFimPadrao = horaFimPadrao,
            HorasPadrao = horasPadrao,
            Sistema = sistema,
            Ativo = true,
            SetorId = setorId,
        };

        if (id.HasValue)
        {
            padrao.SetId(id.Value);
        }

        padrao.MarkCreated(createdBy);
        return padrao;
    }
}
