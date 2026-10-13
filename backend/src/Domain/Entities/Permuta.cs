using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

/// <summary>
/// Troca de dias entre dois servidores, presa às datas (não a uma escala): na <see cref="DataIda"/>
/// o solicitante passa o dia ao permutante e na <see cref="DataVolta"/> o permutante devolve um dia
/// ao solicitante. Em cada data as células dos dois são trocadas — sem distinção do tipo de
/// expediente. Vale para escala em qualquer status e também para mês que ainda não tem escala: é
/// aplicada quando a escala daquele mês é criada/gerada/copiada (ver <c>PermutaAplicador</c>).
/// </summary>
public class Permuta : BaseEntity
{
    public Guid SolicitanteServidorId { get; private set; }
    public Guid PermutanteServidorId { get; private set; }
    public DateOnly DataIda { get; private set; }
    public DateOnly DataVolta { get; private set; }
    public string? Observacao { get; private set; }

    public Servidor Solicitante { get; private set; } = null!;
    public Servidor Permutante { get; private set; } = null!;

    private Permuta()
    {
    }

    public static Permuta Create(
        Guid solicitanteServidorId,
        Guid permutanteServidorId,
        DateOnly dataIda,
        DateOnly dataVolta,
        string? observacao = null,
        string? createdBy = null)
    {
        if (solicitanteServidorId == permutanteServidorId)
        {
            throw new ArgumentException("O solicitante e o permutante devem ser servidores diferentes.");
        }

        if (dataIda == dataVolta)
        {
            throw new ArgumentException("As datas do solicitante e do permutante devem ser diferentes.");
        }

        var entity = new Permuta
        {
            SolicitanteServidorId = solicitanteServidorId,
            PermutanteServidorId = permutanteServidorId,
            DataIda = dataIda,
            DataVolta = dataVolta,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
        };

        entity.MarkCreated(createdBy);
        return entity;
    }
}
