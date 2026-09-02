using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class CalendarioAno : BaseEntity
{
    public int Ano { get; private set; }
    public StatusCalendarioAno Status { get; private set; } = StatusCalendarioAno.Rascunho;
    public DateTime? PublicadoEm { get; private set; }
    public string? PublicadoPor { get; private set; }
    public string? Observacao { get; private set; }

    private CalendarioAno()
    {
    }

    public static CalendarioAno Create(int ano, string? createdBy = null)
    {
        ValidateAno(ano);

        var entity = new CalendarioAno
        {
            Ano = ano,
            Status = StatusCalendarioAno.Rascunho,
        };

        entity.MarkCreated(createdBy);
        return entity;
    }

    public void Atualizar(string? observacao, string? updatedBy = null)
    {
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        MarkUpdated(updatedBy);
    }

    /// <summary>
    /// Publicar não trava edição: assim como a escala finalizada, o calendário publicado
    /// continua editável e pode ser republicado sem precisar "despublicar" antes.
    /// </summary>
    public void Publicar(string? updatedBy = null)
    {
        Status = StatusCalendarioAno.Publicado;
        PublicadoEm = DateTime.UtcNow;
        PublicadoPor = string.IsNullOrWhiteSpace(updatedBy) ? null : updatedBy.Trim();
        MarkUpdated(updatedBy);
    }

    private static void ValidateAno(int ano)
    {
        if (ano is < 2000 or > 2100)
        {
            throw new ArgumentException("Ano inválido.");
        }
    }
}
