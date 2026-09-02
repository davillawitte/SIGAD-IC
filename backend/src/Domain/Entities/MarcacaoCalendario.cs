using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class MarcacaoCalendario : BaseEntity
{
    public Guid CalendarioAnoId { get; private set; }
    public TipoMarcacaoCalendario Tipo { get; private set; }
    public OrigemMarcacao Origem { get; private set; }
    public DateOnly Data { get; private set; }
    public DateOnly? DataFim { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public string? Local { get; private set; }
    public string? PublicoAlvo { get; private set; }

    private MarcacaoCalendario()
    {
    }

    public static MarcacaoCalendario Create(
        Guid calendarioAnoId,
        TipoMarcacaoCalendario tipo,
        OrigemMarcacao origem,
        DateOnly data,
        DateOnly? dataFim,
        string nome,
        string? descricao = null,
        string? local = null,
        string? publicoAlvo = null,
        string? createdBy = null)
    {
        Validate(data, dataFim, nome);

        var entity = new MarcacaoCalendario
        {
            CalendarioAnoId = calendarioAnoId,
            Tipo = tipo,
            Origem = origem,
            Data = data,
            DataFim = dataFim,
            Nome = nome.Trim(),
            Descricao = NormalizeEventoField(tipo, descricao),
            Local = NormalizeEventoField(tipo, local),
            PublicoAlvo = NormalizeEventoField(tipo, publicoAlvo),
        };

        entity.MarkCreated(createdBy);
        return entity;
    }

    /// <summary>
    /// A origem (Fixo/Calculado/Manual) não muda na edição — registra como a marcação
    /// nasceu, mesmo quando o conteúdo é ajustado depois pela Direção IC.
    /// </summary>
    public void Atualizar(
        TipoMarcacaoCalendario tipo,
        DateOnly data,
        DateOnly? dataFim,
        string nome,
        string? descricao,
        string? local,
        string? publicoAlvo,
        string? updatedBy = null)
    {
        Validate(data, dataFim, nome);

        Tipo = tipo;
        Data = data;
        DataFim = dataFim;
        Nome = nome.Trim();
        Descricao = NormalizeEventoField(tipo, descricao);
        Local = NormalizeEventoField(tipo, local);
        PublicoAlvo = NormalizeEventoField(tipo, publicoAlvo);
        MarkUpdated(updatedBy);
    }

    private static string? NormalizeEventoField(TipoMarcacaoCalendario tipo, string? value)
    {
        if (tipo != TipoMarcacaoCalendario.EventoInstitucional)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void Validate(DateOnly data, DateOnly? dataFim, string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome é obrigatório.");
        }

        if (dataFim is DateOnly fim && fim < data)
        {
            throw new ArgumentException("Data fim deve ser maior ou igual à data.");
        }
    }
}
