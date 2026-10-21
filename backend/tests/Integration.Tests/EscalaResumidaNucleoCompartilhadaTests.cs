using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// A resumida de núcleo é uma só por mês, compartilhada pelos setores do núcleo: cada setor monta
/// o grupo dele e o chefe do núcleo vê (e exporta) tudo. Caso real: o chefe do SELMA marcava
/// "usar escala resumida" depois do SCCV já ter criado a do mês, não a achava, tentava criar outra
/// e travava em "Já existe escala resumida para este núcleo no período".
/// </summary>
public class EscalaResumidaNucleoCompartilhadaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 11;
    private const string ChefeNucleo = "chefe.npe.compartilhada";
    private const string ChefeSccv = "chefe.sccv.compartilhada";
    private const string ChefeSelma = "chefe.selma.compartilhada";

    private sealed record Contexto(Guid NucleoId, Guid SccvId, Guid SelmaId, Guid PeritoSccvId, Guid MotoristaId, Guid FotografoId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefeNucleo = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            b.AdicionarUsuario(chefeNucleo, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);
            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE", chefeNucleo.Id);

            var sccv = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV", nucleo);
            var selma = b.AdicionarSetor("Setor de Engenharia Legal e Meio Ambiente", "SELMA", nucleo);

            var chefeSccv = b.AdicionarServidor(sccv, "Chefe do SCCV");
            b.AdicionarChefia(sccv, chefeSccv, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefeSccv, ChefeSccv, CatalogSeed.PerfilChefeSetorId);

            var chefeSelma = b.AdicionarServidor(selma, "Chefe do SELMA");
            b.AdicionarChefia(selma, chefeSelma, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefeSelma, ChefeSelma, CatalogSeed.PerfilChefeSetorId);

            var perito = b.AdicionarServidor(sccv, "Perito do SCCV");
            var motorista = b.AdicionarServidorNoNucleo(nucleo, "Motorista");
            var fotografo = b.AdicionarServidorNoNucleo(nucleo, "Fotógrafo");

            return new Contexto(nucleo.Id, sccv.Id, selma.Id, perito.Id, motorista.Id, fotografo.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaResumidaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaResumidaService(db));
    }

    /// <summary>SCCV cria a resumida do núcleo e monta uma equipe com rodízio.</summary>
    private async Task<EscalaResumidaDetailDto> SccvMontaAsync(Contexto ctx)
    {
        var criada = await ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaResumidaRequest(ctx.NucleoId, Ano, Mes, null), ChefeSccv));
        criada.Error.ShouldBeNull();
        var grupoSccv = criada.Value!.Setores.Single(x => x.SetorId == ctx.SccvId).Id;
        var comEquipe = await ExecutarAsync(s => s.ConfigurarEquipeAsync(
            criada.Value.Id, new ConfigurarEquipeRequest(grupoSccv), ChefeSccv));
        var equipeId = comEquipe.Value!.Setores.Single(x => x.SetorId == ctx.SccvId).Equipes[0].Id;
        var comRodizio = await ExecutarAsync(s => s.ConfigurarRotacaoAsync(
            criada.Value.Id, equipeId,
            new ConfigurarRotacaoRequest(new DateOnly(Ano, Mes, 1), [new RotacaoMembroItem(0, ctx.PeritoSccvId)]),
            ChefeSccv));
        comRodizio.Error.ShouldBeNull();
        return comRodizio.Value!;
    }

    [Fact]
    public async Task Chefe_de_outro_setor_do_nucleo_encontra_a_resumida_ja_criada()
    {
        var ctx = await PrepararAsync();
        var resumida = await SccvMontaAsync(ctx);

        var doSelma = await ExecutarAsync(s => s.ListAsync(
            new EscalaResumidaListQuery { NucleoId = ctx.NucleoId, Ano = Ano, Mes = Mes }, ChefeSelma));

        doSelma.Items.Select(x => x.Id).ShouldBe([resumida.Id]);
    }

    [Fact]
    public async Task Chefe_do_setor_inclui_o_proprio_grupo_na_resumida_do_nucleo()
    {
        var ctx = await PrepararAsync();
        var resumida = await SccvMontaAsync(ctx);
        (await ExecutarAsync(s => s.ConfigurarSetoresAsync(
            resumida.Id,
            new ConfigurarSetoresRequest([new ConfigurarSetorItem(ctx.SccvId, 1), new ConfigurarSetorItem(null, 2)]),
            ChefeSccv))).Error.ShouldBeNull();

        var incluida = await ExecutarAsync(s => s.IncluirSetorAsync(resumida.Id, ctx.SelmaId, ChefeSelma));

        incluida.Error.ShouldBeNull();
        var grupos = incluida.Value!.Setores.OrderBy(x => x.Ordem).Select(x => x.SetorId).ToList();
        // O SCCV continua com a equipe dele; Agentes ficam por último.
        grupos.ShouldBe(new List<Guid?> { ctx.SccvId, ctx.SelmaId, null });
        incluida.Value.Setores.Single(x => x.SetorId == ctx.SccvId).Equipes.ShouldHaveSingleItem();

        // Repetir não duplica.
        var denovo = await ExecutarAsync(s => s.IncluirSetorAsync(resumida.Id, ctx.SelmaId, ChefeSelma));
        denovo.Value!.Setores.Count(x => x.SetorId == ctx.SelmaId).ShouldBe(1);
    }

    [Fact]
    public async Task Chefe_de_setor_tambem_inclui_outro_setor_do_nucleo()
    {
        var ctx = await PrepararAsync();
        var resumida = await SccvMontaAsync(ctx);

        var incluido = await ExecutarAsync(s => s.IncluirSetorAsync(resumida.Id, ctx.SelmaId, ChefeSccv));

        incluido.Error.ShouldBeNull();
        incluido.Value!.Setores.Select(x => x.SetorId).ShouldContain(ctx.SelmaId);
    }

    [Fact]
    public async Task Chefe_de_setor_nao_exclui_a_resumida_com_equipes_de_outro_setor()
    {
        var ctx = await PrepararAsync();
        var resumida = await SccvMontaAsync(ctx);
        (await ExecutarAsync(s => s.IncluirSetorAsync(resumida.Id, ctx.SelmaId, ChefeSelma))).Error.ShouldBeNull();

        var negado = await ExecutarAsync(s => s.DeleteAsync(resumida.Id, ChefeSelma));
        negado.Succeeded.ShouldBeFalse();
        negado.Error.ShouldBe(
            "Esta escala resumida já tem equipes de outros setores do núcleo — só o chefe do núcleo pode excluí-la.");

        (await ExecutarAsync(s => s.DeleteAsync(resumida.Id, ChefeNucleo))).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Escala_de_cada_setor_e_o_chefe_do_nucleo_chegam_na_resumida_para_exportar()
    {
        var ctx = await PrepararAsync();
        var resumida = await SccvMontaAsync(ctx);

        // Escalas do SCCV e do SELMA no mês; nenhuma vinculada à resumida.
        await SemearAsync(b =>
        {
            b.AdicionarEscalaDoSetor(ctx.SccvId, Ano, Mes);
            b.AdicionarEscalaDoSetor(ctx.SelmaId, Ano, Mes);
            return true;
        });

        foreach (var login in new[] { ChefeSelma, ChefeNucleo })
        {
            await using var db = NewContext();
            var lista = await new EscalaService(db).ListAsync(new EscalaListQuery { Escopo = "setor" }, login);
            lista.Items.ShouldNotBeEmpty();
            lista.Items.ShouldAllBe(x => x.ResumidaId == resumida.Id);
        }
    }

    [Fact]
    public async Task Segunda_funcao_pode_ficar_em_aberto_como_DO_no_rodizio()
    {
        var ctx = await PrepararAsync();
        var criada = (await ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaResumidaRequest(ctx.NucleoId, Ano, Mes, null), ChefeNucleo))).Value!;
        var comAgentes = (await ExecutarAsync(s => s.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(null, 1)]), ChefeNucleo))).Value!;
        var comEquipe = (await ExecutarAsync(s => s.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(comAgentes.Setores[0].Id), ChefeNucleo))).Value!;
        var equipeId = comEquipe.Setores[0].Equipes[0].Id;
        var dia1 = new DateOnly(Ano, Mes, 1);

        // Dia 1: motorista escalado e fotógrafo em aberto. Dia 2: motorista em aberto e fotógrafo escalado.
        var final = await ExecutarAsync(s => s.ConfigurarRotacaoAsync(
            criada.Id, equipeId,
            new ConfigurarRotacaoRequest(dia1, [
                new RotacaoMembroItem(0, ctx.MotoristaId, null, IsFolga2: true),
                new RotacaoMembroItem(1, null, ctx.FotografoId),
            ]),
            ChefeNucleo));

        final.Error.ShouldBeNull();
        var equipe = final.Value!.Setores[0].Equipes[0];
        equipe.Rotacao.Single(x => x.Posicao == 0).IsFolga2.ShouldBeTrue();
        equipe.Dias.Single(x => x.Data == dia1).Rotulo.ShouldBe("MOTORISTA + DO");
        equipe.Dias.Single(x => x.Data == dia1.AddDays(1)).Rotulo.ShouldBe("DO + FOTÓGRAFO");

        // A cópia para o mês seguinte mantém a segunda função em aberto.
        var copia = await ExecutarAsync(s => s.CopiarAsync(
            criada.Id, new CopiarEscalaResumidaRequest(Ano, Mes + 1), ChefeNucleo));
        copia.Error.ShouldBeNull();
        copia.Value!.Setores[0].Equipes[0].Rotacao.Single(x => x.Posicao == 0).IsFolga2.ShouldBeTrue();
    }
}
