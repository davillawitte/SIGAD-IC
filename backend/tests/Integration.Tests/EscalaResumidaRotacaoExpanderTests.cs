using Shouldly;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Infrastructure.Services;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Motor de rodízio da escala resumida (por núcleo). Função pura, sem banco — mesma
/// mecânica de fase (módulo com correção de negativo) do <see cref="EscalaJornadaExpanderTests"/>,
/// só que aqui o que gira é a identidade de quem ocupa a vaga, não um código de ocorrência.
/// </summary>
public class EscalaResumidaRotacaoExpanderTests
{
    private static EscalaResumidaEquipe Equipe(string nome, int ordem, DateOnly? ancora, params Guid?[] pool)
    {
        var equipe = EscalaResumidaEquipe.Create(Guid.NewGuid(), nome, ordem);
        if (ancora is DateOnly a)
        {
            equipe.DefinirAncora(a);
        }

        for (var i = 0; i < pool.Length; i++)
        {
            equipe.Rotacao.Add(EscalaResumidaRotacaoMembro.Create(equipe.Id, i, pool[i]));
        }

        return equipe;
    }

    private static EscalaResumidaEquipe Equipe(DateOnly? ancora, params Guid?[] pool) =>
        Equipe("Equipe 01", 1, ancora, pool);

    private static List<Guid?> ExpandUma(EscalaResumidaEquipe equipe, DateOnly inicio, DateOnly fim) =>
        EscalaResumidaRotacaoExpander.ExpandSetor([equipe], inicio, fim).Select(x => x.ServidorId).ToList();

    [Fact]
    public void Avanca_uma_posicao_do_pool_por_dia_a_partir_da_ancora()
    {
        var servidorA = Guid.NewGuid();
        var servidorB = Guid.NewGuid();
        var servidorC = Guid.NewGuid();
        var ancora = new DateOnly(2026, 8, 1);
        var equipe = Equipe(ancora, servidorA, servidorB, servidorC);

        var resultado = ExpandUma(equipe, ancora, ancora.AddDays(5));

        resultado.ShouldBe([servidorA, servidorB, servidorC, servidorA, servidorB, servidorC]);
    }

    [Fact]
    public void Vaga_sem_servidor_representa_DO_no_ciclo()
    {
        var servidorA = Guid.NewGuid();
        var ancora = new DateOnly(2026, 8, 1);
        var equipe = Equipe(ancora, servidorA, null);

        var resultado = ExpandUma(equipe, ancora, ancora.AddDays(3));

        resultado.ShouldBe([servidorA, null, servidorA, null]);
    }

    [Fact]
    public void Datas_antes_da_ancora_mantem_a_fase_correta_sem_indice_negativo()
    {
        var servidorA = Guid.NewGuid();
        var servidorB = Guid.NewGuid();
        var ancora = new DateOnly(2026, 8, 15);
        var equipe = Equipe(ancora, servidorA, servidorB);

        // Um dia antes da âncora deve ser a posição anterior no ciclo (fase -1 -> pos 1).
        var resultado = ExpandUma(equipe, ancora.AddDays(-2), ancora);

        resultado.ShouldBe([servidorA, servidorB, servidorA]);
    }

    [Fact]
    public void Ancora_de_mes_anterior_continua_o_ciclo_em_fase_no_mes_seguinte()
    {
        var servidorA = Guid.NewGuid();
        var servidorB = Guid.NewGuid();
        var servidorC = Guid.NewGuid();
        // Âncora em julho; consulta expande sobre agosto — o ciclo de 3 deve continuar
        // sem "resetar" no início do novo mês.
        var ancora = new DateOnly(2026, 7, 30);
        var equipe = Equipe(ancora, servidorA, servidorB, servidorC);

        var resultado = ExpandUma(equipe, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3));

        // 30/jul=pos0(A) 31/jul=pos1(B) 01/ago=pos2(C) 02/ago=pos0(A) 03/ago=pos1(B)
        resultado.ShouldBe([servidorC, servidorA, servidorB]);
    }

    [Fact]
    public void Sem_ancora_ou_pool_vazio_nao_gera_nada()
    {
        var semAncora = Equipe(null, Guid.NewGuid());
        var semPool = Equipe(new DateOnly(2026, 8, 1));

        ExpandUma(semAncora, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)).ShouldBeEmpty();
        ExpandUma(semPool, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5)).ShouldBeEmpty();
    }

    [Fact]
    public void Posicao_0_aparece_no_dia_ancora_cenario_relatado_pelo_usuario()
    {
        // Reproduz o relato: equipe de 4 posições (3 servidores + 1 DO), âncora 01/09/2026 —
        // posição 0 deveria aparecer no próprio dia 1, e novamente nos dias 5, 9 e 13.
        var sarah = Guid.NewGuid();
        var ricardo = Guid.NewGuid();
        var alyson = Guid.NewGuid();
        var ancora = new DateOnly(2026, 9, 1);
        var equipe = Equipe(ancora, sarah, ricardo, alyson, null);

        var resultado = ExpandUma(equipe, ancora, ancora.AddDays(12));

        resultado[0].ShouldBe(sarah, "dia 1 (a própria âncora) deveria mostrar a posição 0, não ficar em branco");
        resultado.ShouldBe(
            [sarah, ricardo, alyson, null, sarah, ricardo, alyson, null, sarah, ricardo, alyson, null, sarah]);
    }

    [Fact]
    public void RotacaoMembroId_aponta_para_o_membro_do_pool_que_gerou_o_dia()
    {
        var servidorA = Guid.NewGuid();
        var ancora = new DateOnly(2026, 8, 1);
        var equipe = Equipe(ancora, servidorA);
        var membroEsperado = equipe.Rotacao.Single();

        var resultado = EscalaResumidaRotacaoExpander.ExpandSetor([equipe], ancora, ancora).Single();

        resultado.RotacaoMembroId.ShouldBe(membroEsperado.Id);
    }

    [Fact]
    public void Duas_equipes_com_mesmo_tamanho_e_ancora_trocam_de_pool_a_cada_ciclo_completo()
    {
        // Cenário do usuário (SCCV): Equipe 01 = [Lucas..Luís Paulo] (6), Equipe 02 =
        // [Jethe..Matheus Fé] (6), mesma âncora — depois de 6 dias (1 ciclo), as equipes
        // trocam de pool inteiro entre si; depois de outro ciclo (N=2), volta ao original.
        var lucas = Guid.NewGuid();
        var guilherme = Guid.NewGuid();
        var jethe = Guid.NewGuid();
        var natalia = Guid.NewGuid();
        var ancora = new DateOnly(2026, 9, 1);

        var eq1 = Equipe("Equipe 01", 1, ancora, lucas, guilherme, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var eq2 = Equipe("Equipe 02", 2, ancora, jethe, natalia, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var resultado = EscalaResumidaRotacaoExpander.ExpandSetor([eq1, eq2], ancora, ancora.AddDays(13)).ToList();

        var eq1PorDia = resultado.Where(x => x.EquipeId == eq1.Id).OrderBy(x => x.Data).Select(x => x.ServidorId).ToList();
        var eq2PorDia = resultado.Where(x => x.EquipeId == eq2.Id).OrderBy(x => x.Data).Select(x => x.ServidorId).ToList();

        // Ciclo 1 (dias 0-5): cada equipe com seu próprio pool.
        eq1PorDia[0].ShouldBe(lucas);
        eq2PorDia[0].ShouldBe(jethe);

        // Ciclo 2 (dias 6-11): pools trocados entre as equipes.
        eq1PorDia[6].ShouldBe(jethe);
        eq2PorDia[6].ShouldBe(lucas);
        eq1PorDia[7].ShouldBe(natalia);
        eq2PorDia[7].ShouldBe(guilherme);

        // Ciclo 3 (dia 12, N=2 equipes): volta ao pool original.
        eq1PorDia[12].ShouldBe(lucas);
        eq2PorDia[12].ShouldBe(jethe);
    }

    [Fact]
    public void Tres_equipes_caminham_para_frente_ate_voltar_a_equipe_original_apos_n_ciclos()
    {
        var a0 = Guid.NewGuid();
        var b0 = Guid.NewGuid();
        var c0 = Guid.NewGuid();
        var ancora = new DateOnly(2026, 9, 1);

        var eq1 = Equipe("Equipe 01", 1, ancora, a0, Guid.NewGuid());
        var eq2 = Equipe("Equipe 02", 2, ancora, b0, Guid.NewGuid());
        var eq3 = Equipe("Equipe 03", 3, ancora, c0, Guid.NewGuid());

        var resultado = EscalaResumidaRotacaoExpander.ExpandSetor([eq1, eq2, eq3], ancora, ancora.AddDays(5)).ToList();
        var eq1PorDia = resultado.Where(x => x.EquipeId == eq1.Id).OrderBy(x => x.Data).Select(x => x.ServidorId).ToList();

        // Tamanho do pool = 2 -> ciclo 0 = dias 0-1, ciclo 1 = dias 2-3, ciclo 2 = dias 4-5.
        eq1PorDia[0].ShouldBe(a0, "ciclo 0: equipe 1 com o próprio pool");
        eq1PorDia[2].ShouldBe(c0, "ciclo 1: equipe 1 recebe o pool de quem estava na equipe 3 (caminha pra frente)");
        eq1PorDia[4].ShouldBe(b0, "ciclo 2: equipe 1 recebe o pool de quem estava na equipe 2");
    }

    [Fact]
    public void Equipes_com_tamanhos_diferentes_nao_trocam_entre_si()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var ancora = new DateOnly(2026, 9, 1);

        var eq1 = Equipe("Equipe 01", 1, ancora, a, Guid.NewGuid());
        var eq2 = Equipe("Equipe 02", 2, ancora, b, Guid.NewGuid(), Guid.NewGuid());

        var resultado = EscalaResumidaRotacaoExpander.ExpandSetor([eq1, eq2], ancora, ancora.AddDays(5)).ToList();
        var eq1PorDia = resultado.Where(x => x.EquipeId == eq1.Id).OrderBy(x => x.Data).Select(x => x.ServidorId).ToList();

        // Tamanhos diferentes (2 vs 3): cada equipe forma seu próprio grupo de 1, sem troca —
        // equipe 1 sempre alterna entre o próprio pool.
        eq1PorDia.ShouldAllBe(x => x == a || x == eq1.Rotacao.ElementAt(1).ServidorId);
    }
}
