using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Um mesmo servidor não pode estar escalado em duas escalas por setor/núcleo no mesmo mês.
/// A escala resumida NÃO entra nessa checagem em nenhum sentido — ela é só uma etapa de
/// planejamento/visualização anterior à escala de fato, não gera nem sofre "atrito": um
/// servidor pode estar no rodízio de uma escala resumida e, ao mesmo tempo, numa escala real
/// de outro setor, sem conflito. Só a escala de verdade (por setor ou núcleo) é considerada
/// aqui.
/// </summary>
public static class EscalaConflitoChecker
{
    public static async Task<IReadOnlyList<ConflitoServidorDto>> FindServidoresJaEscaladosAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<Guid> servidorIds,
        int ano,
        int mes,
        Guid? excluirEscalaId,
        CancellationToken cancellationToken)
    {
        if (servidorIds.Count == 0)
        {
            return [];
        }

        var origemPorServidor = await db.EscalaServidores
            .AsNoTracking()
            .Where(x => servidorIds.Contains(x.ServidorId)
                && x.Escala.Ano == ano
                && x.Escala.Mes == mes
                && x.EscalaId != excluirEscalaId)
            .Select(x => new
            {
                x.ServidorId,
                Origem = x.Escala.SetorId != null
                    ? "escala do setor " + x.Escala.Setor!.Sigla
                    : "escala do núcleo " + x.Escala.Nucleo!.Sigla,
            })
            .GroupBy(x => x.ServidorId)
            .Select(g => new { ServidorId = g.Key, Origem = g.First().Origem })
            .ToDictionaryAsync(x => x.ServidorId, x => x.Origem, cancellationToken);

        if (origemPorServidor.Count == 0)
        {
            return [];
        }

        var nomes = await db.Servidores
            .AsNoTracking()
            .Where(x => origemPorServidor.Keys.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome })
            .ToListAsync(cancellationToken);

        return nomes
            .Select(x => new ConflitoServidorDto(x.Id, x.Nome, origemPorServidor[x.Id]))
            .OrderBy(x => x.ServidorNome)
            .ToList();
    }

    /// <summary>Mensagem de erro pronta pra exibir — nome de cada servidor junto de onde ele já
    /// está escalado, não só o nome sozinho.</summary>
    public static string FormatarMensagem(IReadOnlyList<ConflitoServidorDto> conflitos) =>
        "Já escalado(s) em outra escala neste mês: "
        + string.Join("; ", conflitos.Select(x => $"{x.ServidorNome} ({x.Origem})"))
        + ".";
}
