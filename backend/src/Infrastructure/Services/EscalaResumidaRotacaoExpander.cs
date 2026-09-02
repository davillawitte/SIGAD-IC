using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Expande o pool de rodízio das equipes de um setor em valores por dia. Cada equipe avança
/// uma posição do pool por dia, ancorado em <c>DataInicioCiclo</c> (fase módulo com correção
/// de negativo — mesma técnica usada em <see cref="EscalaJornadaExpander"/> para ciclos de
/// plantão).
///
/// Quando várias equipes do MESMO setor compartilham tamanho de pool e âncora, elas formam um
/// "grupo de rodízio": a cada ciclo completo (uma volta inteira do pool), as equipes trocam de
/// pool inteiro entre si, caminhando pra frente — quem estava na Equipe 1 passa pra Equipe 2,
/// Equipe 2 pra Equipe 3, ..., e a última equipe volta pra Equipe 1. Depois de N ciclos (N = nº
/// de equipes do grupo), cada pool volta pra equipe original. Uma equipe sem nenhuma irmã com o
/// mesmo tamanho+âncora forma um grupo de 1 e se comporta exatamente como antes (sem troca) — a
/// fórmula abaixo degenera pra isso automaticamente quando N=1, sem precisar de caso especial.
/// </summary>
public static class EscalaResumidaRotacaoExpander
{
    public static IEnumerable<(Guid EquipeId, DateOnly Data, Guid? ServidorId, Guid? ServidorId2, Guid RotacaoMembroId)> ExpandSetor(
        IReadOnlyList<EscalaResumidaEquipe> equipesDoSetor,
        DateOnly inicio,
        DateOnly fim)
    {
        var grupos = equipesDoSetor
            .Where(e => e.Rotacao.Count > 0 && e.DataInicioCiclo is not null)
            .OrderBy(e => e.Ordem)
            .GroupBy(e => (Tamanho: e.Rotacao.Count, Ancora: e.DataInicioCiclo!.Value));

        foreach (var grupo in grupos)
        {
            var equipesDoGrupo = grupo.ToList();
            var tamanho = grupo.Key.Tamanho;
            var ancora = grupo.Key.Ancora;
            var n = equipesDoGrupo.Count;

            var poolsPorEquipe = equipesDoGrupo
                .Select(e => e.Rotacao.ToDictionary(m => m.Posicao))
                .ToList();

            for (var d = inicio; d <= fim; d = d.AddDays(1))
            {
                var diasDesdeAncora = d.DayNumber - ancora.DayNumber;
                var ciclo = FloorDiv(diasDesdeAncora, tamanho);
                var posNoPool = Mod(diasDesdeAncora, tamanho);

                for (var i = 0; i < n; i++)
                {
                    var donoIndex = Mod(i - ciclo, n);
                    if (!poolsPorEquipe[donoIndex].TryGetValue(posNoPool, out var membro))
                    {
                        continue;
                    }

                    yield return (equipesDoGrupo[i].Id, d, membro.ServidorId, membro.ServidorId2, membro.Id);
                }
            }
        }
    }

    /// <summary>Posição do pool (0-based) ocupada na data informada, dada uma âncora e o
    /// tamanho do ciclo — sem considerar troca entre equipes (uso isolado, ex.: validações
    /// simples). Permite datas antes da âncora (fase negativa) mantendo continuidade.</summary>
    public static int PosicaoNaData(DateOnly data, DateOnly ancora, int tamanhoPool)
    {
        if (tamanhoPool <= 0)
        {
            throw new ArgumentException("Pool de rodízio vazio.", nameof(tamanhoPool));
        }

        return Mod(data.DayNumber - ancora.DayNumber, tamanhoPool);
    }

    private static int Mod(int a, int b) => ((a % b) + b) % b;

    private static int FloorDiv(int a, int b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0))
        {
            q--;
        }

        return q;
    }
}
