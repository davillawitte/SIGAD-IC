using Shouldly;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Escala resumida de núcleo montada por quem chefia só um setor dele: pode criar e mexer no
/// grupo do próprio setor e no de Agentes, mas não escolhe os setores participantes — isso
/// continua sendo do chefe de núcleo.
/// </summary>
public class EscalaResumidaChefeDeSetorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string ChefeSccv = "chefe.sccv";
    private const string ChefeNucleo = "chefe.npe";

    private sealed record Contexto(Guid NucleoId, Guid SetorId, Guid OutroSetorId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefeNucleo = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE", chefeNucleo.Id);
            b.AdicionarUsuario(chefeNucleo, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var sccv = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV", nucleo);
            var selma = b.AdicionarSetor("Setor de Engenharia Legal", "SELMA", nucleo);

            var chefeSetor = b.AdicionarServidor(sccv, "Chefe do SCCV");
            b.AdicionarChefia(sccv, chefeSetor, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefeSetor, ChefeSccv, CatalogSeed.PerfilChefeSetorId);

            return new Contexto(nucleo.Id, sccv.Id, selma.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaResumidaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaResumidaService(db));
    }

    private Task<Application.Common.Result<EscalaResumidaDetailDto>> CriarAsync(Guid nucleoId, string login) =>
        ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), login));

    [Fact]
    public async Task Chefe_de_setor_cria_a_resumida_do_nucleo_ja_com_o_proprio_setor()
    {
        var ctx = await PrepararAsync();

        var criada = await CriarAsync(ctx.NucleoId, ChefeSccv);

        criada.Error.ShouldBeNull();
        criada.Value!.Setores.Select(x => x.SetorId).ShouldBe([ctx.SetorId]);
    }

    [Fact]
    public async Task Chefe_de_setor_inclui_o_grupo_de_agentes()
    {
        var ctx = await PrepararAsync();
        var criada = await CriarAsync(ctx.NucleoId, ChefeSccv);
        criada.Error.ShouldBeNull();

        var configurado = await ExecutarAsync(s => s.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest([
                new ConfigurarSetorItem(ctx.SetorId, 1),
                new ConfigurarSetorItem(null, 2),
            ]),
            ChefeSccv));

        configurado.Error.ShouldBeNull();
        configurado.Value!.Setores.Count.ShouldBe(2);
        configurado.Value!.Setores.Any(x => x.SetorId is null).ShouldBeTrue();
    }

    [Fact]
    public async Task Chefe_de_setor_nao_inclui_outro_setor_do_nucleo()
    {
        var ctx = await PrepararAsync();
        var criada = await CriarAsync(ctx.NucleoId, ChefeSccv);
        criada.Error.ShouldBeNull();

        var configurado = await ExecutarAsync(s => s.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest([
                new ConfigurarSetorItem(ctx.SetorId, 1),
                new ConfigurarSetorItem(ctx.OutroSetorId, 2),
            ]),
            ChefeSccv));

        configurado.Succeeded.ShouldBeFalse();
        configurado.Error.ShouldBe(
            "Só o chefe do núcleo pode incluir outros setores nesta escala resumida.");
    }

    [Fact]
    public async Task Chefe_de_setor_nao_remove_o_setor_de_outro_chefe()
    {
        var ctx = await PrepararAsync();
        var criada = await CriarAsync(ctx.NucleoId, ChefeNucleo);
        criada.Error.ShouldBeNull();

        // O chefe do núcleo põe os dois setores...
        (await ExecutarAsync(s => s.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest([
                new ConfigurarSetorItem(ctx.SetorId, 1),
                new ConfigurarSetorItem(ctx.OutroSetorId, 2),
            ]),
            ChefeNucleo))).Error.ShouldBeNull();

        // ...e o chefe do SCCV manda só o dele: o do outro setor continua na escala resumida.
        var configurado = await ExecutarAsync(s => s.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest([new ConfigurarSetorItem(ctx.SetorId, 1)]),
            ChefeSccv));

        configurado.Error.ShouldBeNull();
        configurado.Value!.Setores
            .Select(x => x.SetorId)
            .OrderBy(x => x)
            .ToList()
            .ShouldBe(new List<Guid?> { ctx.SetorId, ctx.OutroSetorId }.OrderBy(x => x).ToList());
    }
}
