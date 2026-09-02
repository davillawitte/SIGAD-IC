using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Calcula a data da Páscoa (algoritmo de Meeus/Jones/Butcher, calendário gregoriano) e os
/// feriados/pontos facultativos móveis derivados dela — sem dependência externa.
/// </summary>
public static class FeriadoMovelCalculator
{
    public static DateOnly Pascoa(int ano)
    {
        var a = ano % 19;
        var b = ano / 100;
        var c = ano % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var mes = (h + l - 7 * m + 114) / 31;
        var dia = (h + l - 7 * m + 114) % 31 + 1;

        return new DateOnly(ano, mes, dia);
    }

    /// <summary>
    /// Só a Sexta-feira Santa é feriado nacional obrigatório. Carnaval e Corpus Christi
    /// são ponto facultativo — não são pré-gerados por <c>GerarAno</c>, só cadastro manual.
    /// </summary>
    public static IReadOnlyList<(DateOnly Data, string Nome, TipoMarcacaoCalendario Tipo)> Moveis(int ano)
    {
        var pascoa = Pascoa(ano);

        return
        [
            (pascoa.AddDays(-2), "Sexta-feira Santa", TipoMarcacaoCalendario.FeriadoNacional),
        ];
    }
}

/// <summary>
/// Feriados fixos usados para pré-gerar um novo ano do calendário institucional.
/// Pontos facultativos e eventos institucionais ficam a cargo de cadastro manual
/// da Direção IC.
/// </summary>
public static class FeriadoFixoCatalogo
{
    public static readonly IReadOnlyList<(int Mes, int Dia, string Nome)> Nacionais =
    [
        (1, 1, "Confraternização Universal"),
        (4, 21, "Tiradentes"),
        (5, 1, "Dia do Trabalho"),
        (9, 7, "Independência do Brasil"),
        (10, 12, "Nossa Senhora Aparecida"),
        (11, 2, "Finados"),
        (11, 15, "Proclamação da República"),
        (11, 20, "Dia Nacional de Zumbi e da Consciência Negra"),
        (12, 25, "Natal"),
    ];

    public static readonly IReadOnlyList<(int Mes, int Dia, string Nome)> EstaduaisRn =
    [
        (6, 29, "Emancipação Política do Rio Grande do Norte"),
        (10, 3, "Santos Mártires de Cunhaú e Uruaçu"),
    ];

    /// <summary>Feriados municipais de Natal/RN, sede do Instituto de Criminalística.</summary>
    public static readonly IReadOnlyList<(int Mes, int Dia, string Nome)> MunicipaisNatal =
    [
        (1, 6, "Dia de Santos Reis"),
        (11, 21, "Nossa Senhora da Apresentação"),
    ];
}
