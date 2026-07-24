using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Infrastructure.Services;

public static class EscalaJornadaExpander
{
    public static IEnumerable<(DateOnly Data, string Codigo, bool IsTrabalho)> Expand(EscalaJornada jornada)
    {
        var folgaCodigo = string.IsNullOrWhiteSpace(jornada.TipoOcorrenciaFolgaCodigo)
            ? "D"
            : jornada.TipoOcorrenciaFolgaCodigo;

        return jornada.RecorrenciaTipo switch
        {
            RecorrenciaTipo.Nenhuma => ExpandNenhuma(jornada),
            RecorrenciaTipo.TodosOsDias => ExpandTodos(jornada, jornada.TipoOcorrenciaCodigo),
            RecorrenciaTipo.DiasSemana => ExpandDiasSemana(jornada),
            RecorrenciaTipo.ACadaXDias => ExpandACadaX(jornada),
            RecorrenciaTipo.CicloPlantao => ExpandCiclo(jornada, folgaCodigo),
            _ => [],
        };
    }

    private static IEnumerable<(DateOnly, string, bool)> ExpandNenhuma(EscalaJornada jornada)
    {
        yield return (jornada.DataInicio, jornada.TipoOcorrenciaCodigo, true);
    }

    private static IEnumerable<(DateOnly, string, bool)> ExpandTodos(EscalaJornada jornada, string codigo)
    {
        for (var d = jornada.DataInicio; d <= jornada.DataFim; d = d.AddDays(1))
        {
            yield return (d, codigo, true);
        }
    }

    private static IEnumerable<(DateOnly, string, bool)> ExpandDiasSemana(EscalaJornada jornada)
    {
        var days = ParseDiasSemana(jornada.DiasSemana);
        for (var d = jornada.DataInicio; d <= jornada.DataFim; d = d.AddDays(1))
        {
            var iso = d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;
            if (days.Contains(iso))
            {
                yield return (d, jornada.TipoOcorrenciaCodigo, true);
            }
        }
    }

    private static IEnumerable<(DateOnly, string, bool)> ExpandACadaX(EscalaJornada jornada)
    {
        var step = Math.Max(1, jornada.IntervaloDias ?? 1);
        for (var d = jornada.DataInicio; d <= jornada.DataFim; d = d.AddDays(step))
        {
            yield return (d, jornada.TipoOcorrenciaCodigo, true);
        }
    }

    private static IEnumerable<(DateOnly, string, bool)> ExpandCiclo(EscalaJornada jornada, string folgaCodigo)
    {
        var trabalho = Math.Max(1, jornada.DiasTrabalho ?? 1);
        var folga = Math.Max(0, jornada.DiasFolga ?? 3);
        var ciclo = trabalho + folga;
        var ancora = jornada.DataInicioCiclo ?? jornada.DataInicio;

        for (var d = jornada.DataInicio; d <= jornada.DataFim; d = d.AddDays(1))
        {
            var diasDesdeAncora = d.DayNumber - ancora.DayNumber;
            // Permite datas antes da âncora (fase negativa) mantendo continuidade.
            var pos = ((diasDesdeAncora % ciclo) + ciclo) % ciclo;
            if (pos < trabalho)
            {
                yield return (d, jornada.TipoOcorrenciaCodigo, true);
            }
            else
            {
                yield return (d, folgaCodigo, false);
            }
        }
    }

    private static HashSet<int> ParseDiasSemana(string? diasSemana)
    {
        if (string.IsNullOrWhiteSpace(diasSemana))
        {
            return [1, 2, 3, 4, 5];
        }

        return diasSemana
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var n) ? n : 0)
            .Where(x => x is >= 1 and <= 7)
            .ToHashSet();
    }
}
