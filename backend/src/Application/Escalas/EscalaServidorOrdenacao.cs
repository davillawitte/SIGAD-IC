using System.Text;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Escalas;

/// <summary>
/// Ordem de exibição dos servidores na escala (tela e PDF): agrupados por regime — plantão 24h
/// primeiro (inclui 24h + laudo 12h), depois plantão 12h, depois expediente — e em ordem
/// alfabética dentro de cada grupo. Espelhado no frontend em <c>escala-servidor-ordem.ts</c>.
/// </summary>
public static class EscalaServidorOrdenacao
{
    public static IReadOnlyList<EscalaServidorDto> Ordenar(IEnumerable<EscalaServidorDto> servidores) =>
        servidores
            .OrderBy(GrupoRegime)
            .ThenBy(x => ChaveNome(x.ServidorNome), StringComparer.Ordinal)
            .ThenBy(x => x.Matricula, StringComparer.Ordinal)
            .ToList();

    /// <summary>Grupo pelo regime escolhido pro servidor no passo de regimes (gravado na
    /// jornada): 0 = plantão 24h (24x72, 24x48, 24h + laudo 12h, rodízio da escala resumida),
    /// 1 = plantão 12h, 2 = expediente, 3 = sem regime. As ocorrências do mês não contam — trocar
    /// um código na grade não muda a pessoa de grupo.</summary>
    public static int GrupoRegime(EscalaServidorDto servidor) =>
        servidor.Jornadas.Count == 0 ? 3 : servidor.Jornadas.Min(GrupoDaJornada);

    private static int GrupoDaJornada(EscalaJornadaDto jornada) =>
        jornada.TipoJornada == TipoJornada.Plantao
            ? (jornada.Horas ?? 0m) >= 24m ? 0 : 1
            : 2;

    /// <summary>Chave alfabética sem diferenciar maiúsculas nem acentos ("ÁLVARO" junto de
    /// "ALVARO"). Feita à mão porque a API roda com InvariantGlobalization (sem culturas, então
    /// nada de comparador pt-BR).</summary>
    public static string ChaveNome(string? nome)
    {
        var sb = new StringBuilder(nome?.Length ?? 0);
        foreach (var c in (nome ?? string.Empty).Trim().ToUpperInvariant())
        {
            sb.Append(c switch
            {
                'Á' or 'À' or 'Â' or 'Ã' or 'Ä' => 'A',
                'É' or 'È' or 'Ê' or 'Ë' => 'E',
                'Í' or 'Ì' or 'Î' or 'Ï' => 'I',
                'Ó' or 'Ò' or 'Ô' or 'Õ' or 'Ö' => 'O',
                'Ú' or 'Ù' or 'Û' or 'Ü' => 'U',
                'Ç' => 'C',
                'Ñ' => 'N',
                _ => c,
            });
        }

        return sb.ToString();
    }
}
