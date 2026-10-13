using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Application.Permutas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Permuta troca as células do solicitante e do permutante na data da ida e na da volta, por cima
/// da escala (qualquer status). A troca é presa à data: copiar a escala para o mês seguinte não
/// pode levá-la junto (o mesmo problema que as férias já tiveram), e salvar/regenerar a escala não
/// pode trocar duas vezes.
/// </summary>
public class PermutaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string ChefeNb = "chefe.nb.permuta";
    private const string ChefeNp = "chefe.np.permuta";
    private const string ChefeNucleo = "chefe.nucleo.permuta";

    // 12x36 a partir do dia 1: PD nos dias ímpares; a partir do dia 2: PD nos pares.
    private static readonly DateOnly Dia1 = new(Ano, Mes, 1);
    private static readonly DateOnly Dia2 = new(Ano, Mes, 2);
    private static readonly DateOnly Dia4 = new(Ano, Mes, 4);

    private sealed record Contexto(
        Guid SetorNbId,
        Guid SetorNpId,
        Guid EscalaNbId,
        Guid A,
        Guid B,
        Guid D,
        Guid C);

    /// <summary>Núcleo NPE engloba NB e NP. NB: A (PD nos ímpares), B e D (PD nos pares), na
    /// mesma escala de setembro. NP: C, de outro setor do núcleo, sem escala ainda.</summary>
    private async Task<Contexto> PrepararAsync(bool gerar = true)
    {
        var ctx = await SemearAsync(b =>
        {
            var administrativo = b.AdicionarSetor("Setor Administrativo", "SA");
            var chefeNucleo = b.AdicionarServidor(administrativo, "Chefe do Núcleo");
            b.AdicionarUsuario(chefeNucleo, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);
            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE", chefeNucleo.Id);

            var nb = b.AdicionarSetor("Setor de Balística", "NB", nucleo);
            var np = b.AdicionarSetor("Setor de Papiloscopia", "NP", nucleo);

            var chefeNb = b.AdicionarServidor(nb, "Chefe NB");
            b.AdicionarChefia(nb, chefeNb, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefeNb, ChefeNb, CatalogSeed.PerfilChefeSetorId);

            var chefeNp = b.AdicionarServidor(np, "Chefe NP");
            b.AdicionarChefia(np, chefeNp, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefeNp, ChefeNp, CatalogSeed.PerfilChefeSetorId);

            var a = b.AdicionarServidor(nb, "Perito A");
            var bb = b.AdicionarServidor(nb, "Perito B");
            var d = b.AdicionarServidor(nb, "Perito D");
            var c = b.AdicionarServidor(np, "Perito C");

            var escala = b.AdicionarEscala(nb, Ano, Mes, TipoFuncionamento.VinteQuatroHoras);
            b.AdicionarEscalaServidor(escala, a, 1);
            b.AdicionarEscalaServidor(escala, bb, 2);
            b.AdicionarEscalaServidor(escala, d, 3);

            return new Contexto(nb.Id, np.Id, escala.Id, a.Id, bb.Id, d.Id, c.Id);
        });

        if (gerar)
        {
            await GerarAsync(ctx.EscalaNbId, ChefeNb, (ctx.A, Dia1), (ctx.B, Dia2), (ctx.D, Dia2));
        }

        return ctx;
    }

    private async Task<T> EscalaAsync<T>(Func<EscalaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    private async Task<T> PermutaAsync<T>(Func<PermutaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new PermutaService(db));
    }

    private async Task GerarAsync(Guid escalaId, string login, params (Guid Servidor, DateOnly Ancora)[] itens)
    {
        var padrao = await PadraoIdAsync("12X36");
        var result = await EscalaAsync(s => s.GerarEscalaAsync(
            escalaId,
            new GerarEscalaRequest(
                itens.Select(x => new GerarEscalaItemRequest(x.Servidor, padrao, x.Ancora, null, null)).ToList(),
                DistribuirAutomaticamente: false,
                DataBaseDistribuicao: null),
            login));
        result.Error.ShouldBeNull();
    }

    private async Task PublicarAsync(Guid escalaId, string login)
    {
        (await EscalaAsync(s => s.FinalizarAsync(escalaId, login))).Error.ShouldBeNull();
        (await EscalaAsync(s => s.PublicarAsync(
            escalaId, new PublicarEscalaRequest(ConfirmarConflitos: true), login))).Error.ShouldBeNull();
    }

    private async Task<Guid> PermutarAsync(Guid solicitante, Guid permutante, DateOnly ida, DateOnly volta, string login = ChefeNb)
    {
        var result = await PermutaAsync(s => s.CreateAsync(
            new CreatePermutaRequest(solicitante, permutante, ida, volta), login));
        result.Error.ShouldBeNull();
        return result.Value!.Id;
    }

    /// <summary>Células da escala: (servidor, data) → (código, permutada).</summary>
    private async Task<Dictionary<(Guid Servidor, DateOnly Data), (string Codigo, bool Permutada)>> GradeAsync(Guid escalaId)
    {
        await using var db = NewContext();
        var itens = await db.EscalaOcorrencias
            .Where(x => x.EscalaServidor.EscalaId == escalaId)
            .Select(x => new { x.EscalaServidor.ServidorId, x.Data, x.TipoOcorrenciaCodigo, x.Permutada })
            .ToListAsync();
        return itens.ToDictionary(x => (x.ServidorId, x.Data), x => (x.TipoOcorrenciaCodigo, x.Permutada));
    }

    private static Dictionary<(Guid, DateOnly), string> SoCodigos(
        Dictionary<(Guid Servidor, DateOnly Data), (string Codigo, bool Permutada)> grade) =>
        grade.ToDictionary(x => x.Key, x => x.Value.Codigo);

    [Fact]
    public async Task Troca_as_celulas_na_ida_e_na_volta_em_escala_publicada()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaNbId, ChefeNb);
        var antes = await GradeAsync(ctx.EscalaNbId);

        // A passa o plantão do dia 1 para B; B devolve o plantão do dia 2 para A.
        await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);

        var depois = await GradeAsync(ctx.EscalaNbId);
        depois[(ctx.A, Dia1)].ShouldBe(("D", true));
        depois[(ctx.B, Dia1)].ShouldBe(("PD", true));
        depois[(ctx.A, Dia2)].ShouldBe(("PD", true));
        depois[(ctx.B, Dia2)].ShouldBe(("D", true));

        // Só essas quatro células mudam.
        depois.Count(x => x.Value.Permutada).ShouldBe(4);
        foreach (var (chave, valor) in antes.Where(x => x.Key.Data != Dia1 && x.Key.Data != Dia2))
        {
            depois[chave].ShouldBe((valor.Codigo, false));
        }

        // O "*" fica só no dia final de cada um: B no dia 1 (recebeu o plantão de A) e A no
        // dia 2 (recebeu o de B). Quem cedeu o dia não tem a marca.
        (await MarcasAsync(ctx.EscalaNbId)).ShouldBe(
            [(ctx.A, Dia2), (ctx.B, Dia1)], ignoreOrder: true);
    }

    [Fact]
    public async Task Na_permuta_da_permuta_o_asterisco_fica_com_quem_recebeu_por_ultimo()
    {
        var ctx = await PrepararAsync();

        // A passa o dia 1 para B; B repassa esse dia 1 para D.
        await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);
        await PermutarAsync(ctx.B, ctx.D, Dia1, Dia4);

        var marcas = await MarcasAsync(ctx.EscalaNbId);
        marcas.ShouldContain((ctx.D, Dia1));
        marcas.ShouldNotContain((ctx.A, Dia1));
        marcas.ShouldNotContain((ctx.B, Dia1));
        marcas.ShouldContain((ctx.A, Dia2));
        marcas.ShouldNotContain((ctx.B, Dia2));

        // Na tela de cadastro a lista de dias também só marca o dia recebido.
        var diasB = await PermutaAsync(s => s.GetEscalaServidorAsync(ctx.B, Ano, Mes));
        diasB.Dias.Where(x => x.Permuta).ShouldBeEmpty();
        var diasD = await PermutaAsync(s => s.GetEscalaServidorAsync(ctx.D, Ano, Mes));
        diasD.Dias.Where(x => x.Permuta).Select(x => x.Data).ShouldBe([Dia1]);
    }

    /// <summary>Células exibidas com "*" na visualização da escala.</summary>
    private async Task<List<(Guid Servidor, DateOnly Data)>> MarcasAsync(Guid escalaId)
    {
        var detalhe = (await EscalaAsync(s => s.GetByIdAsync(escalaId, ChefeNb))).Value!;
        return detalhe.Servidores
            .SelectMany(s => s.Ocorrencias.Where(o => o.Permuta).Select(o => (s.ServidorId, o.Data)))
            .ToList();
    }

    [Fact]
    public async Task Troca_entre_escalas_de_setores_diferentes_e_meses_diferentes()
    {
        var ctx = await PrepararAsync();
        var outubro = new DateOnly(Ano, Mes + 1, 1);

        // C (NP) tem escala em setembro e outubro; A (NB) tem escala de outubro também.
        var escalaNpSet = await CriarEscalaAsync(ctx.SetorNpId, Mes, ChefeNp, ctx.C);
        await GerarAsync(escalaNpSet, ChefeNp, (ctx.C, Dia2));
        var escalaNpOut = await CriarEscalaAsync(ctx.SetorNpId, Mes + 1, ChefeNp, ctx.C);
        await GerarAsync(escalaNpOut, ChefeNp, (ctx.C, outubro));
        var escalaNbOut = await CriarEscalaAsync(ctx.SetorNbId, Mes + 1, ChefeNb, ctx.A);
        await GerarAsync(escalaNbOut, ChefeNb, (ctx.A, outubro.AddDays(1)));

        // Setores diferentes: quem registra é o chefe do núcleo que engloba os dois.
        // Ida: plantão de A em 01/09. Volta: plantão de C em 01/10.
        await PermutarAsync(ctx.A, ctx.C, Dia1, outubro, ChefeNucleo);

        (await GradeAsync(ctx.EscalaNbId))[(ctx.A, Dia1)].ShouldBe(("D", true));
        (await GradeAsync(escalaNpSet))[(ctx.C, Dia1)].ShouldBe(("PD", true));
        (await GradeAsync(escalaNbOut))[(ctx.A, outubro)].ShouldBe(("PD", true));
        (await GradeAsync(escalaNpOut))[(ctx.C, outubro)].ShouldBe(("D", true));
    }

    [Fact]
    public async Task Permuta_da_permuta_e_exclusao_no_meio_da_cadeia()
    {
        var ctx = await PrepararAsync();
        var original = await GradeAsync(ctx.EscalaNbId);

        // A passa o dia 1 para B; depois B repassa esse mesmo dia 1 para D.
        var primeira = await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);
        await PermutarAsync(ctx.B, ctx.D, Dia1, Dia4);

        var grade = await GradeAsync(ctx.EscalaNbId);
        grade[(ctx.A, Dia1)].ShouldBe(("D", true));
        grade[(ctx.B, Dia1)].ShouldBe(("D", false)); // recebeu e repassou: igual ao original
        grade[(ctx.D, Dia1)].ShouldBe(("PD", true));
        // B e D trabalham nos mesmos dias: na volta (dia 4) a troca não muda nada.
        grade[(ctx.B, Dia4)].ShouldBe(("PD", false));
        grade[(ctx.D, Dia4)].ShouldBe(("PD", false));

        // Excluir a primeira: o dia 1 volta a ser de A, e a segunda (B↔D) deixa de mexer nele
        // porque B e D estão de folga nesse dia.
        (await PermutaAsync(s => s.DeleteAsync(primeira, ChefeNb))).Error.ShouldBeNull();

        grade = await GradeAsync(ctx.EscalaNbId);
        grade[(ctx.A, Dia1)].ShouldBe(("PD", false));
        grade[(ctx.B, Dia1)].ShouldBe(("D", false));
        grade[(ctx.D, Dia1)].ShouldBe(("D", false));
        grade[(ctx.A, Dia2)].ShouldBe(("D", false));
        grade[(ctx.B, Dia2)].ShouldBe(("PD", false));
        grade.Count(x => x.Value.Permutada).ShouldBe(0);

        // Excluindo também a segunda, tudo volta ao original.
        var segunda = (await PermutaAsync(s => s.ListAsync(new PermutaListQuery(), ChefeNb))).Single().Id;
        (await PermutaAsync(s => s.DeleteAsync(segunda, ChefeNb))).Error.ShouldBeNull();
        (await GradeAsync(ctx.EscalaNbId)).ShouldBe(original);
    }

    [Fact]
    public async Task Permuta_cadastrada_antes_da_escala_existir_aparece_ao_gerar()
    {
        var ctx = await PrepararAsync();
        var dia1Out = new DateOnly(Ano, Mes + 1, 1);
        var dia2Out = new DateOnly(Ano, Mes + 1, 2);

        // Outubro ainda não tem escala.
        await PermutarAsync(ctx.A, ctx.B, dia1Out, dia2Out);

        var escalaOut = await CriarEscalaAsync(ctx.SetorNbId, Mes + 1, ChefeNb, ctx.A, ctx.B);
        await GerarAsync(escalaOut, ChefeNb, (ctx.A, dia1Out), (ctx.B, dia2Out));

        var grade = await GradeAsync(escalaOut);
        grade[(ctx.A, dia1Out)].ShouldBe(("D", true));
        grade[(ctx.B, dia1Out)].ShouldBe(("PD", true));
        grade[(ctx.A, dia2Out)].ShouldBe(("PD", true));
        grade[(ctx.B, dia2Out)].ShouldBe(("D", true));
        grade.Count(x => x.Value.Permutada).ShouldBe(4);
    }

    [Fact]
    public async Task Copiar_escala_nao_leva_a_permuta_para_o_mes_seguinte()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaNbId, ChefeNb);

        // Referência: cópia feita antes de qualquer permuta.
        var referencia = await EscalaAsync(s => s.CopiarAsync(
            ctx.EscalaNbId, new CopiarEscalaRequest(Ano, Mes + 1), ChefeNb));
        referencia.Error.ShouldBeNull();
        var esperada = await GradeAsync(referencia.Value!.Id);

        await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);
        await PermutarAsync(ctx.B, ctx.D, Dia1, Dia4);

        var copia = await EscalaAsync(s => s.CopiarAsync(
            ctx.EscalaNbId, new CopiarEscalaRequest(Ano, Mes + 1), ChefeNb));
        copia.Error.ShouldBeNull();
        var grade = await GradeAsync(copia.Value!.Id);

        grade.Values.ShouldAllBe(x => !x.Permutada);
        SoCodigos(grade).ShouldBe(SoCodigos(esperada));
    }

    [Fact]
    public async Task Copiar_escala_aplica_permuta_ja_cadastrada_para_o_mes_de_destino()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaNbId, ChefeNb);

        var referencia = await EscalaAsync(s => s.CopiarAsync(
            ctx.EscalaNbId, new CopiarEscalaRequest(Ano, Mes + 1), ChefeNb));
        var esperada = await GradeAsync(referencia.Value!.Id);
        // Exclui a referência para a permuta abaixo ser aplicada só na cópia de verdade.
        (await EscalaAsync(s => s.DeleteAsync(referencia.Value!.Id, ChefeNb))).Error.ShouldBeNull();

        // Um dia em que A trabalha e B folga em outubro, e outro o contrário.
        var idaOut = esperada.Where(x => x.Key.Servidor == ctx.A && x.Value.Codigo == "PD")
            .Select(x => x.Key.Data)
            .First(d => esperada[(ctx.B, d)].Codigo == "D");
        var voltaOut = esperada.Where(x => x.Key.Servidor == ctx.B && x.Value.Codigo == "PD")
            .Select(x => x.Key.Data)
            .First(d => esperada[(ctx.A, d)].Codigo == "D");
        await PermutarAsync(ctx.A, ctx.B, idaOut, voltaOut);

        var copia = await EscalaAsync(s => s.CopiarAsync(
            ctx.EscalaNbId, new CopiarEscalaRequest(Ano, Mes + 1), ChefeNb));
        copia.Error.ShouldBeNull();
        var grade = await GradeAsync(copia.Value!.Id);

        grade[(ctx.A, idaOut)].ShouldBe(("D", true));
        grade[(ctx.B, idaOut)].ShouldBe(("PD", true));
        grade[(ctx.A, voltaOut)].ShouldBe(("PD", true));
        grade[(ctx.B, voltaOut)].ShouldBe(("D", true));
        grade.Count(x => x.Value.Permutada).ShouldBe(4);
    }

    [Fact]
    public async Task Salvar_a_escala_no_wizard_e_regenerar_nao_trocam_duas_vezes()
    {
        var ctx = await PrepararAsync();
        await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);
        var comPermuta = await GradeAsync(ctx.EscalaNbId);

        // O wizard devolve a matriz exatamente como recebeu (já com a permuta aplicada).
        var detalhe = (await EscalaAsync(s => s.GetByIdAsync(ctx.EscalaNbId, ChefeNb))).Value!;
        var itens = detalhe.Servidores
            .SelectMany(s => s.Ocorrencias.Select(o => new SyncOcorrenciaItemRequest(
                s.ServidorId, o.Data, o.TipoOcorrenciaCodigo, o.HoraInicio, o.HoraFim, o.Horas, o.Observacao)))
            .ToList();
        (await EscalaAsync(s => s.SyncOcorrenciasAsync(
            ctx.EscalaNbId, new SyncOcorrenciasRequest(itens), ChefeNb))).Error.ShouldBeNull();
        (await EscalaAsync(s => s.SyncOcorrenciasAsync(
            ctx.EscalaNbId, new SyncOcorrenciasRequest(itens), ChefeNb))).Error.ShouldBeNull();

        (await GradeAsync(ctx.EscalaNbId)).ShouldBe(comPermuta);

        // Regenerar o regime também mantém uma única troca.
        await GerarAsync(ctx.EscalaNbId, ChefeNb, (ctx.A, Dia1), (ctx.B, Dia2), (ctx.D, Dia2));
        SoCodigos(await GradeAsync(ctx.EscalaNbId)).ShouldBe(SoCodigos(comPermuta));
        (await GradeAsync(ctx.EscalaNbId)).Count(x => x.Value.Permutada).ShouldBe(4);
    }

    [Fact]
    public async Task Excluir_restaura_as_celulas_inclusive_a_criada_pela_permuta()
    {
        // Só A tem regime; B está na escala sem nenhuma célula.
        var ctx = await PrepararAsync(gerar: false);
        await GerarAsync(ctx.EscalaNbId, ChefeNb, (ctx.A, Dia1));
        var original = await GradeAsync(ctx.EscalaNbId);
        original.Keys.ShouldNotContain((ctx.B, Dia1));

        var id = await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);
        var grade = await GradeAsync(ctx.EscalaNbId);
        grade[(ctx.A, Dia1)].ShouldBe(("D", true)); // recebeu de quem não tinha nada: descanso
        grade[(ctx.B, Dia1)].ShouldBe(("PD", true)); // célula criada pela permuta

        (await PermutaAsync(s => s.DeleteAsync(id, ChefeNb))).Error.ShouldBeNull();
        (await GradeAsync(ctx.EscalaNbId)).ShouldBe(original);
    }

    [Fact]
    public async Task Permuta_com_servidor_afastado_na_data_e_recusada()
    {
        var ctx = await PrepararAsync();
        await AfastarAsync(ctx.B, Dia1);

        var result = await PermutaAsync(s => s.CreateAsync(
            new CreatePermutaRequest(ctx.A, ctx.B, Dia1, Dia2), ChefeNb));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("PERITO B está afastado (férias/licença) em 01/09/2026.");
    }

    [Fact]
    public async Task Ferias_lancadas_depois_nao_sao_movidas_para_o_outro_servidor()
    {
        var ctx = await PrepararAsync();
        await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);

        await AfastarAsync(ctx.A, Dia1);
        (await EscalaAsync(s => s.AplicarAfastamentosAsync(ctx.EscalaNbId, ChefeNb))).Error.ShouldBeNull();

        var grade = await GradeAsync(ctx.EscalaNbId);
        grade[(ctx.A, Dia1)].ShouldBe(("FR", false));
        grade[(ctx.B, Dia1)].ShouldBe(("D", false)); // não recebe as férias de A
        grade[(ctx.A, Dia2)].ShouldBe(("PD", true)); // a volta continua valendo
        grade[(ctx.B, Dia2)].ShouldBe(("D", true));
    }

    [Fact]
    public async Task Chefe_de_outro_setor_nao_registra_nem_ve_a_permuta()
    {
        var ctx = await PrepararAsync();

        const string mensagem = "Só é possível registrar permuta entre servidores do setor ou núcleo em que você é chefe.";

        var negado = await PermutaAsync(s => s.CreateAsync(
            new CreatePermutaRequest(ctx.A, ctx.B, Dia1, Dia2), ChefeNp));
        negado.Succeeded.ShouldBeFalse();
        negado.Error.ShouldBe(mensagem);

        // Chefe de setor não troca com servidor de outro setor, nem do mesmo núcleo.
        var outroSetor = await PermutaAsync(s => s.CreateAsync(
            new CreatePermutaRequest(ctx.A, ctx.C, Dia1, Dia2), ChefeNb));
        outroSetor.Succeeded.ShouldBeFalse();
        outroSetor.Error.ShouldBe(mensagem);

        await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);
        (await PermutaAsync(s => s.ListAsync(new PermutaListQuery(), ChefeNb))).Count.ShouldBe(1);
        (await PermutaAsync(s => s.ListAsync(new PermutaListQuery(), ChefeNp))).ShouldBeEmpty();

        // Envolvendo um servidor de NP, a permuta passa a aparecer para o chefe de NP também.
        await PermutarAsync(ctx.A, ctx.C, Dia1.AddDays(2), Dia2.AddDays(2), ChefeNucleo);
        (await PermutaAsync(s => s.ListAsync(new PermutaListQuery(), ChefeNp))).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Servidores_da_permuta_seguem_a_chefia_e_o_nucleo()
    {
        var ctx = await PrepararAsync();

        var doChefeNb = await PermutaAsync(s => s.ListServidoresAsync(ChefeNb));
        doChefeNb.Permutantes.Select(x => x.Id).ShouldContain(ctx.B);
        doChefeNb.Permutantes.Select(x => x.Id).ShouldNotContain(ctx.C);
        doChefeNb.Solicitantes.Select(x => x.Id).ShouldBe(doChefeNb.Permutantes.Select(x => x.Id));

        // Chefe do núcleo vê os setores do núcleo inteiro.
        var doNucleo = (await PermutaAsync(s => s.ListServidoresAsync(ChefeNucleo))).Permutantes
            .Select(x => x.Id)
            .ToList();
        doNucleo.ShouldContain(ctx.A);
        doNucleo.ShouldContain(ctx.C);
    }

    [Fact]
    public async Task Listagem_traz_o_nome_de_quem_registrou()
    {
        var ctx = await PrepararAsync();
        await PermutarAsync(ctx.A, ctx.B, Dia1, Dia2);

        var item = (await PermutaAsync(s => s.ListAsync(new PermutaListQuery(), ChefeNb))).Single();
        item.CreatedBy.ShouldBe(ChefeNb);
        item.RegistradoPorNome.ShouldBe("CHEFE NB");
    }

    private async Task AfastarAsync(Guid servidorId, DateOnly dia)
    {
        await using var db = NewContext();
        db.Afastamentos.Add(Afastamento.Create(servidorId, dia, dia, "FR", sei: "SEI-1", createdBy: "teste"));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> CriarEscalaAsync(Guid setorId, int mes, string login, params Guid[] servidores)
    {
        var criada = await EscalaAsync(s => s.CreateAsync(
            new CreateEscalaRequest(setorId, null, Ano, mes, TipoFuncionamento.VinteQuatroHoras, null), login));
        criada.Error.ShouldBeNull();
        (await EscalaAsync(s => s.AddServidoresAsync(
            criada.Value!.Id, new AddEscalaServidoresRequest(servidores), login))).Error.ShouldBeNull();
        return criada.Value!.Id;
    }
}
