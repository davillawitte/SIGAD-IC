using Shouldly;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Caracterização do algoritmo de grade de dias. Função pura, sem banco.
/// Congela o comportamento atual — incluindo o que é surpreendente — para que a
/// divisão do EscalaService na Fase 6 seja verificável.
/// </summary>
public class EscalaJornadaExpanderTests
{
    // 01/07/2026 é uma quarta-feira; 06/07/2026 é a segunda-feira seguinte.
    private static readonly DateOnly Quarta = new(2026, 7, 1);
    private static readonly DateOnly SegundaFeira = new(2026, 7, 6);

    private static EscalaJornada Jornada(
        DateOnly dataInicio,
        DateOnly dataFim,
        RecorrenciaTipo recorrencia,
        string codigoTrabalho = "M",
        string? diasSemana = null,
        int? intervaloDias = null,
        int? diasTrabalho = null,
        int? diasFolga = null,
        string? codigoFolga = null,
        DateOnly? dataInicioCiclo = null) =>
        EscalaJornada.Create(
            escalaServidorId: Guid.NewGuid(),
            tipoJornada: TipoJornada.Plantao,
            dataInicio: dataInicio,
            dataFim: dataFim,
            tipoOcorrenciaCodigo: codigoTrabalho,
            recorrenciaTipo: recorrencia,
            diasSemana: diasSemana,
            intervaloDias: intervaloDias,
            diasTrabalho: diasTrabalho,
            diasFolga: diasFolga,
            tipoOcorrenciaFolgaCodigo: codigoFolga,
            dataInicioCiclo: dataInicioCiclo);

    [Fact]
    public void ExpAdm_emite_somente_dias_uteis_e_ignora_o_fim_de_semana()
    {
        // Semana fechada: segunda a domingo.
        var jornada = Jornada(
            SegundaFeira,
            SegundaFeira.AddDays(6),
            RecorrenciaTipo.DiasSemana,
            codigoTrabalho: "M",
            diasSemana: "1,2,3,4,5");

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        // Comportamento relevante: o fim de semana não gera ocorrência alguma,
        // nem mesmo folga "D". A grade simplesmente não tem aqueles dias.
        resultado.Count.ShouldBe(5);
        resultado.ShouldAllBe(x => x.Codigo == "M" && x.IsTrabalho);
        resultado.Select(x => x.Data.DayOfWeek).ShouldBe(
        [
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
        ]);
    }

    [Fact]
    public void DiasSemana_sem_configuracao_assume_segunda_a_sexta()
    {
        var jornada = Jornada(
            SegundaFeira,
            SegundaFeira.AddDays(6),
            RecorrenciaTipo.DiasSemana,
            diasSemana: null);

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Count.ShouldBe(5);
        resultado.Select(x => x.Data.DayOfWeek).ShouldNotContain(DayOfWeek.Saturday);
        resultado.Select(x => x.Data.DayOfWeek).ShouldNotContain(DayOfWeek.Sunday);
    }

    [Fact]
    public void DiasSemana_ignora_valores_fora_da_faixa_iso()
    {
        var jornada = Jornada(
            SegundaFeira,
            SegundaFeira.AddDays(6),
            RecorrenciaTipo.DiasSemana,
            diasSemana: "0,1,8,9,abc,7");

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        // Só 1 (segunda) e 7 (domingo) sobrevivem ao filtro.
        resultado.Select(x => x.Data.DayOfWeek).ShouldBe([DayOfWeek.Monday, DayOfWeek.Sunday]);
    }

    [Fact]
    public void Ciclo_12x36_alterna_trabalho_e_folga_a_partir_da_ancora()
    {
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(7),
            RecorrenciaTipo.CicloPlantao,
            codigoTrabalho: "PD",
            diasTrabalho: 1,
            diasFolga: 1,
            dataInicioCiclo: Quarta);

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Count.ShouldBe(8);
        resultado.Select(x => x.Codigo).ShouldBe(["PD", "D", "PD", "D", "PD", "D", "PD", "D"]);
        resultado.Select(x => x.IsTrabalho).ShouldBe([true, false, true, false, true, false, true, false]);
    }

    [Fact]
    public void Ciclo_24x72_gera_um_plantao_seguido_de_tres_folgas()
    {
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(7),
            RecorrenciaTipo.CicloPlantao,
            codigoTrabalho: "PT",
            diasTrabalho: 1,
            diasFolga: 3,
            dataInicioCiclo: Quarta);

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Select(x => x.Codigo).ShouldBe(["PT", "D", "D", "D", "PT", "D", "D", "D"]);
    }

    [Fact]
    public void Ciclo_sem_dias_configurados_assume_um_de_trabalho_e_tres_de_folga()
    {
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(3),
            RecorrenciaTipo.CicloPlantao,
            codigoTrabalho: "PT",
            diasTrabalho: null,
            diasFolga: null,
            dataInicioCiclo: Quarta);

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Select(x => x.IsTrabalho).ShouldBe([true, false, false, false]);
    }

    [Fact]
    public void Ciclo_com_datas_anteriores_a_ancora_mantem_a_fase_do_ciclo()
    {
        // Âncora no dia 3, período começando no dia 1: o módulo negativo é
        // normalizado, então os dias 1 e 2 caem na fase de folga.
        var ancora = Quarta.AddDays(2);
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(4),
            RecorrenciaTipo.CicloPlantao,
            codigoTrabalho: "PT",
            diasTrabalho: 1,
            diasFolga: 3,
            dataInicioCiclo: ancora);

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Select(x => x.IsTrabalho).ShouldBe([false, false, true, false, false]);
        resultado.Single(x => x.IsTrabalho).Data.ShouldBe(ancora);
    }

    [Fact]
    public void Ciclo_sem_ancora_explicita_usa_a_data_de_inicio()
    {
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(3),
            RecorrenciaTipo.CicloPlantao,
            codigoTrabalho: "PT",
            diasTrabalho: 1,
            diasFolga: 1,
            dataInicioCiclo: null);

        // EscalaJornada.Create preenche DataInicioCiclo com DataInicio quando o tipo
        // é CicloPlantao e a âncora não é informada.
        jornada.DataInicioCiclo.ShouldBe(Quarta);
        EscalaJornadaExpander.Expand(jornada).First().IsTrabalho.ShouldBeTrue();
    }

    [Fact]
    public void Ciclo_usa_codigo_de_folga_customizado()
    {
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(1),
            RecorrenciaTipo.CicloPlantao,
            codigoTrabalho: "PT",
            diasTrabalho: 1,
            diasFolga: 1,
            codigoFolga: "F",
            dataInicioCiclo: Quarta);

        EscalaJornadaExpander.Expand(jornada).Select(x => x.Codigo).ShouldBe(["PT", "F"]);
    }

    [Fact]
    public void Recorrencia_nenhuma_emite_apenas_a_data_de_inicio()
    {
        var jornada = Jornada(Quarta, Quarta.AddDays(10), RecorrenciaTipo.Nenhuma, codigoTrabalho: "M");

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Count.ShouldBe(1);
        resultado[0].Data.ShouldBe(Quarta);
        resultado[0].IsTrabalho.ShouldBeTrue();
    }

    [Fact]
    public void Recorrencia_todos_os_dias_emite_trabalho_em_todo_o_periodo()
    {
        var jornada = Jornada(Quarta, Quarta.AddDays(4), RecorrenciaTipo.TodosOsDias, codigoTrabalho: "M");

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Count.ShouldBe(5);
        resultado.ShouldAllBe(x => x.IsTrabalho && x.Codigo == "M");
    }

    [Fact]
    public void Recorrencia_a_cada_x_dias_respeita_o_intervalo()
    {
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(6),
            RecorrenciaTipo.ACadaXDias,
            codigoTrabalho: "M",
            intervaloDias: 3);

        var resultado = EscalaJornadaExpander.Expand(jornada).ToList();

        resultado.Select(x => x.Data).ShouldBe([Quarta, Quarta.AddDays(3), Quarta.AddDays(6)]);
    }

    [Fact]
    public void Recorrencia_a_cada_x_dias_com_intervalo_invalido_cai_para_um_dia()
    {
        var jornada = Jornada(
            Quarta,
            Quarta.AddDays(2),
            RecorrenciaTipo.ACadaXDias,
            codigoTrabalho: "M",
            intervaloDias: 0);

        EscalaJornadaExpander.Expand(jornada).Count().ShouldBe(3);
    }
}
