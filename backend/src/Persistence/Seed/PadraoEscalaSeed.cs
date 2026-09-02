using Microsoft.EntityFrameworkCore;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Persistence.Seed;

public static class PadraoEscalaSeed
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var existing = await context.PadroesEscala.AsNoTracking().Select(x => x.Codigo).ToListAsync(cancellationToken);

        void AddIfMissing(PadraoEscala padrao)
        {
            if (!existing.Contains(padrao.Codigo))
            {
                context.PadroesEscala.Add(padrao);
            }
        }

        AddIfMissing(PadraoEscala.Create(
            "EXP_ADM",
            "Expediente Administrativo",
            TipoFuncionamento.Expediente,
            TipoJornada.Expediente,
            RecorrenciaTipo.DiasSemana,
            tipoOcorrenciaTrabalho: "M",
            tipoOcorrenciaFolga: "D",
            diasSemana: "1,2,3,4,5",
            horaInicioPadrao: new TimeOnly(8, 0),
            horaFimPadrao: new TimeOnly(14, 0),
            horasPadrao: 6,
            sistema: true,
            createdBy: "seed"));

        AddIfMissing(PadraoEscala.Create(
            "24X72",
            "24x72",
            TipoFuncionamento.VinteQuatroHoras,
            TipoJornada.Plantao,
            RecorrenciaTipo.CicloPlantao,
            tipoOcorrenciaTrabalho: "PT",
            tipoOcorrenciaFolga: "D",
            diasTrabalho: 1,
            diasFolga: 3,
            horaInicioPadrao: new TimeOnly(7, 0),
            horaFimPadrao: new TimeOnly(7, 0),
            horasPadrao: 24,
            sistema: true,
            createdBy: "seed"));

        AddIfMissing(PadraoEscala.Create(
            "24X48",
            "24x48",
            TipoFuncionamento.VinteQuatroHoras,
            TipoJornada.Plantao,
            RecorrenciaTipo.CicloPlantao,
            tipoOcorrenciaTrabalho: "PT",
            tipoOcorrenciaFolga: "D",
            diasTrabalho: 1,
            diasFolga: 2,
            horaInicioPadrao: new TimeOnly(7, 0),
            horaFimPadrao: new TimeOnly(7, 0),
            horasPadrao: 24,
            sistema: true,
            createdBy: "seed"));

        AddIfMissing(PadraoEscala.Create(
            "12X36",
            "12x36",
            TipoFuncionamento.VinteQuatroHoras,
            TipoJornada.Plantao,
            RecorrenciaTipo.CicloPlantao,
            tipoOcorrenciaTrabalho: "PD",
            tipoOcorrenciaFolga: "D",
            diasTrabalho: 1,
            diasFolga: 1,
            horaInicioPadrao: new TimeOnly(7, 0),
            horaFimPadrao: new TimeOnly(19, 0),
            horasPadrao: 12,
            sistema: true,
            createdBy: "seed"));

        AddIfMissing(PadraoEscala.Create(
            "PLANTAO_DIURNO",
            "Plantão Diurno",
            TipoFuncionamento.VinteQuatroHoras,
            TipoJornada.Plantao,
            RecorrenciaTipo.CicloPlantao,
            tipoOcorrenciaTrabalho: "PD",
            tipoOcorrenciaFolga: "D",
            diasTrabalho: 1,
            diasFolga: 1,
            horaInicioPadrao: new TimeOnly(7, 0),
            horaFimPadrao: new TimeOnly(19, 0),
            horasPadrao: 12,
            sistema: true,
            createdBy: "seed"));

        AddIfMissing(PadraoEscala.Create(
            "PLANTAO_NOTURNO",
            "Plantão Noturno",
            TipoFuncionamento.VinteQuatroHoras,
            TipoJornada.Plantao,
            RecorrenciaTipo.CicloPlantao,
            tipoOcorrenciaTrabalho: "PN",
            tipoOcorrenciaFolga: "D",
            diasTrabalho: 1,
            diasFolga: 1,
            horaInicioPadrao: new TimeOnly(19, 0),
            horaFimPadrao: new TimeOnly(7, 0),
            horasPadrao: 12,
            sistema: true,
            createdBy: "seed"));

        AddIfMissing(PadraoEscala.Create(
            "PT24_TL12",
            "Plantão 24h + Laudo 12h",
            TipoFuncionamento.VinteQuatroHoras,
            TipoJornada.Plantao,
            RecorrenciaTipo.CicloPersonalizado,
            tipoOcorrenciaTrabalho: "PT",
            tipoOcorrenciaFolga: "D",
            sequenciaCiclo: "PT,D,D,D,TL12,D",
            horaInicioPadrao: new TimeOnly(7, 0),
            horaFimPadrao: new TimeOnly(7, 0),
            horasPadrao: 24,
            sistema: true,
            createdBy: "seed"));

        AddIfMissing(PadraoEscala.Create(
            "PERSONALIZADO",
            "Personalizado",
            TipoFuncionamento.VinteQuatroHoras,
            TipoJornada.Outro,
            RecorrenciaTipo.CicloPlantao,
            tipoOcorrenciaTrabalho: "PT",
            tipoOcorrenciaFolga: "D",
            diasTrabalho: 1,
            diasFolga: 3,
            sistema: true,
            createdBy: "seed"));

        await context.SaveChangesAsync(cancellationToken);
    }
}
