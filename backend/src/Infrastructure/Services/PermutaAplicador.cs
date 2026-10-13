using Microsoft.EntityFrameworkCore;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Aplica as permutas nas células das escalas. O recálculo é sempre feito a partir do valor
/// <b>base</b> de cada célula (o de antes de qualquer permuta, guardado em
/// <c>EscalaOcorrencia.*Original</c>), então rodar de novo não troca duas vezes; permuta da permuta,
/// exclusão de uma permuta no meio da cadeia e escala criada depois da permuta caem no mesmo caminho.
/// </summary>
/// <remarks>
/// Regras: numa data, as permutas daquele dia são simuladas em ordem de cadastro, trocando o
/// conteúdo das células do solicitante e do permutante. Quem RECEBE o dia (o permutante na data
/// do solicitante; o solicitante na data do permutante) fica com a célula marcada com "*"; numa
/// permuta da permuta, a marca fica com quem recebeu por último. A base de um servidor vem da escala dele
/// naquele mês (havendo mais de uma: publicada &gt; finalizada &gt; rascunho, a mais recente). Quem
/// recebe de alguém sem escala/célula no dia fica com descanso ("D") até a escala do outro existir.
/// Data em que um dos dois está de férias/licença não é trocada. O resultado é gravado em todas
/// as escalas do mês que têm o servidor.
/// </remarks>
public sealed class PermutaAplicador(ApplicationDbContext db)
{
    private static readonly CelulaEscala Descanso = new("D", null, null, null, null);

    /// <summary>Restaura o valor base das células permutadas da escala (e remove as criadas pela
    /// permuta). Usado antes de operações que reescrevem a base — depois, chamar
    /// <see cref="RecalcularMesAsync"/>.</summary>
    public async Task DesaplicarEscalaAsync(Guid escalaId, string? actorLogin, CancellationToken cancellationToken)
    {
        var celulas = await db.EscalaOcorrencias
            .Where(x => x.EscalaServidor.EscalaId == escalaId && x.Permutada)
            .ToListAsync(cancellationToken);
        if (celulas.Count == 0)
        {
            return;
        }

        foreach (var celula in celulas)
        {
            if (celula.CodigoOriginal is null)
            {
                db.EscalaOcorrencias.Remove(celula);
                // Escala carregada pelo chamador (Include) mantém a coleção; tira daqui também
                // para a operação seguinte não enxergar a célula removida.
                celula.EscalaServidor?.Ocorrencias.Remove(celula);
            }
            else
            {
                celula.RestaurarOriginal(actorLogin);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Recalcula todas as datas do mês que têm permuta (ou célula ainda marcada).</summary>
    public async Task RecalcularMesAsync(int ano, int mes, string? actorLogin, CancellationToken cancellationToken)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var datasIda = await db.Permutas.AsNoTracking()
            .Where(x => x.DataIda >= inicio && x.DataIda <= fim)
            .Select(x => x.DataIda)
            .ToListAsync(cancellationToken);
        var datasVolta = await db.Permutas.AsNoTracking()
            .Where(x => x.DataVolta >= inicio && x.DataVolta <= fim)
            .Select(x => x.DataVolta)
            .ToListAsync(cancellationToken);
        var datasMarcadas = await db.EscalaOcorrencias.AsNoTracking()
            .Where(x => x.Permutada && x.Data >= inicio && x.Data <= fim)
            .Select(x => x.Data)
            .ToListAsync(cancellationToken);

        await RecalcularDatasAsync(datasIda.Concat(datasVolta).Concat(datasMarcadas), actorLogin, cancellationToken);
    }

    public async Task RecalcularDatasAsync(
        IEnumerable<DateOnly> datas,
        string? actorLogin,
        CancellationToken cancellationToken)
    {
        var distintas = datas.Distinct().OrderBy(x => x).ToList();
        if (distintas.Count == 0)
        {
            return;
        }

        var categorias = await db.TiposOcorrencia
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Codigo, x => x.Categoria, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var data in distintas)
        {
            await RecalcularDataAsync(data, categorias, actorLogin, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RecalcularDataAsync(
        DateOnly data,
        IReadOnlyDictionary<string, CategoriaOcorrencia> categorias,
        string? actorLogin,
        CancellationToken cancellationToken)
    {
        var permutas = await db.Permutas
            .AsNoTracking()
            .Where(x => x.DataIda == data || x.DataVolta == data)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        // Células ainda marcadas entram mesmo sem permuta no dia: é o caso de permuta excluída,
        // cuja troca precisa ser desfeita.
        var marcados = await db.EscalaOcorrencias
            .AsNoTracking()
            .Where(x => x.Data == data && x.Permutada)
            .Select(x => x.EscalaServidor.ServidorId)
            .ToListAsync(cancellationToken);

        var servidorIds = permutas
            .SelectMany(x => new[] { x.SolicitanteServidorId, x.PermutanteServidorId })
            .Concat(marcados)
            .Distinct()
            .ToList();
        if (servidorIds.Count == 0)
        {
            return;
        }

        var linhas = await db.EscalaServidores
            .AsNoTracking()
            .Where(x => servidorIds.Contains(x.ServidorId)
                        && x.Escala.Ano == data.Year
                        && x.Escala.Mes == data.Month)
            .Select(x => new { x.Id, x.ServidorId, x.Escala.Status, EscalaCriadaEm = x.Escala.CreatedAt })
            .ToListAsync(cancellationToken);

        var linhaIds = linhas.Select(x => x.Id).ToList();
        var celulas = (await db.EscalaOcorrencias
                .Where(x => x.Data == data && linhaIds.Contains(x.EscalaServidorId))
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.EscalaServidorId);

        var estado = new Dictionary<Guid, CelulaEscala?>();
        foreach (var servidorId in servidorIds)
        {
            var preferida = linhas
                .Where(x => x.ServidorId == servidorId)
                .OrderByDescending(x => PrioridadeStatus(x.Status))
                .ThenByDescending(x => x.EscalaCriadaEm)
                .FirstOrDefault();
            estado[servidorId] = preferida is not null && celulas.TryGetValue(preferida.Id, out var celula)
                ? celula.ValorBase
                : null;
        }

        var ultimaPermuta = new Dictionary<Guid, Guid>();
        var recebeu = new Dictionary<Guid, bool>();
        foreach (var permuta in permutas)
        {
            var solicitante = estado[permuta.SolicitanteServidorId];
            var permutante = estado[permuta.PermutanteServidorId];
            if (EhAfastamento(solicitante, categorias) || EhAfastamento(permutante, categorias))
            {
                continue;
            }

            estado[permuta.SolicitanteServidorId] = permutante;
            estado[permuta.PermutanteServidorId] = solicitante;
            ultimaPermuta[permuta.SolicitanteServidorId] = permuta.Id;
            ultimaPermuta[permuta.PermutanteServidorId] = permuta.Id;
            // Na data do solicitante quem recebe o dia é o permutante; na do permutante, o solicitante.
            var permutanteRecebe = permuta.DataIda == data;
            recebeu[permuta.PermutanteServidorId] = permutanteRecebe;
            recebeu[permuta.SolicitanteServidorId] = !permutanteRecebe;
        }

        foreach (var linha in linhas)
        {
            celulas.TryGetValue(linha.Id, out var celula);

            if (ultimaPermuta.TryGetValue(linha.ServidorId, out var permutaId))
            {
                var final = estado[linha.ServidorId] ?? Descanso;
                if (!final.MesmoConteudo(celula?.ValorBase))
                {
                    if (celula is null)
                    {
                        db.EscalaOcorrencias.Add(
                            EscalaOcorrencia.CriarPorPermuta(
                                linha.Id, data, final, permutaId, recebeu[linha.ServidorId], actorLogin));
                    }
                    else
                    {
                        celula.AplicarPermuta(final, permutaId, recebeu[linha.ServidorId], actorLogin);
                    }

                    continue;
                }
            }

            // Sem troca efetiva no dia: a célula volta ao valor base.
            if (celula is { Permutada: true })
            {
                if (celula.CodigoOriginal is null)
                {
                    db.EscalaOcorrencias.Remove(celula);
                    celula.EscalaServidor?.Ocorrencias.Remove(celula);
                }
                else
                {
                    celula.RestaurarOriginal(actorLogin);
                }
            }
        }
    }

    private static int PrioridadeStatus(StatusEscala status) => status switch
    {
        StatusEscala.Publicada or StatusEscala.DevolucaoSolicitada => 3,
        StatusEscala.Finalizada => 2,
        _ => 1,
    };

    private static bool EhAfastamento(CelulaEscala? celula, IReadOnlyDictionary<string, CategoriaOcorrencia> categorias) =>
        celula is not null
        && categorias.TryGetValue(celula.Codigo, out var categoria)
        && categoria == CategoriaOcorrencia.Afastamento;
}
