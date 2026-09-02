namespace TemplateSistema.Domain.Enums;

public enum RecorrenciaTipo
{
    Nenhuma = 1,
    TodosOsDias = 2,
    DiasSemana = 3,
    ACadaXDias = 4,
    CicloPlantao = 5,

    /// <summary>Ciclo com mais de 2 fases (ex.: 24h trabalho, 72h folga, 12h laudo, 36h folga),
    /// definido como sequência explícita de códigos de ocorrência em <c>SequenciaCiclo</c>,
    /// um por unidade de dia do ciclo, expandido pela mesma técnica de âncora+módulo do
    /// <see cref="CicloPlantao"/>.</summary>
    CicloPersonalizado = 6,
}
