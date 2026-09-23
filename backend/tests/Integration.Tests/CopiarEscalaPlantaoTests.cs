using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Cópia de escala de plantão: o ciclo (12x36) continua em fase no mês de destino, vindo da
/// jornada e da âncora. A reprodução da semana vale só para escala administrativa — plantão não
/// tem "dia fixo da semana", então nada ali pode mexer nesta grade.
/// </summary>
public class CopiarEscalaPlantaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string Login = "chefe.plantao";
    private static readonly DateOnly Ancora = new(Ano, Mes, 2);

    private sealed record Contexto(Guid SetorId, Guid EscalaId, Guid PlantonistaId, Guid ComparacaoId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Núcleo de Balística", "NB");
            var plantonista = b.AdicionarServidor(setor, "Plantonista");
            var comparacao = b.AdicionarServidor(setor, "Plantonista de Comparação");
            var chefe = b.AdicionarServidor(setor, "Chefe do Setor");
            b.AdicionarChefia(setor, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, Login, CatalogSeed.PerfilChefeSetorId);

            var escala = b.AdicionarEscala(setor, Ano, Mes, TipoFuncionamento.VinteQuatroHoras);
            b.AdicionarEscalaServidor(escala, plantonista);

            return new Contexto(setor.Id, escala.Id, plantonista.Id, comparacao.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    private async Task GerarAsync(Guid escalaId, Guid servidorId, DateOnly ancora)
    {
        var padrao = await PadraoIdAsync("12X36");
        var result = await ExecutarAsync(s => s.GerarEscalaAsync(
            escalaId,
            new GerarEscalaRequest(
                [new GerarEscalaItemRequest(servidorId, padrao, ancora, null, null)],
                DistribuirAutomaticamente: false,
                DataBaseDistribuicao: null),
            Login));
        result.Error.ShouldBeNull();
    }

    private async Task<List<(DateOnly Data, string Codigo)>> GradeAsync(Guid escalaId, Guid servidorId)
    {
        await using var db = NewContext();
        var itens = await db.EscalaOcorrencias
            .Where(x => x.EscalaServidor.EscalaId == escalaId && x.EscalaServidor.ServidorId == servidorId)
            .OrderBy(x => x.Data)
            .Select(x => new { x.Data, x.TipoOcorrenciaCodigo })
            .ToListAsync();
        return itens.Select(x => (x.Data, x.TipoOcorrenciaCodigo)).ToList();
    }

    private async Task<Guid> CopiarParaOutubroAsync(Guid escalaId)
    {
        (await ExecutarAsync(s => s.FinalizarAsync(escalaId, Login))).Error.ShouldBeNull();
        (await ExecutarAsync(s => s.PublicarAsync(
            escalaId, new PublicarEscalaRequest(ConfirmarConflitos: true), Login))).Error.ShouldBeNull();

        var copia = await ExecutarAsync(s => s.CopiarAsync(
            escalaId, new CopiarEscalaRequest(Ano, Mes + 1), Login));
        copia.Error.ShouldBeNull();
        return copia.Value!.Id;
    }

    [Fact]
    public async Task Ciclo_12x36_continua_em_fase_no_mes_copiado()
    {
        var ctx = await PrepararAsync();
        await GerarAsync(ctx.EscalaId, ctx.PlantonistaId, Ancora);

        var copiaId = await CopiarParaOutubroAsync(ctx.EscalaId);

        // Referência: outra escala de outubro gerada do zero com a MESMA âncora de setembro —
        // a cópia tem que produzir exatamente a mesma grade.
        var referencia = await ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaRequest(ctx.SetorId, null, Ano, Mes + 1, TipoFuncionamento.VinteQuatroHoras, null),
            Login));
        referencia.Error.ShouldBeNull();
        (await ExecutarAsync(s => s.AddServidoresAsync(
            referencia.Value!.Id, new AddEscalaServidoresRequest([ctx.ComparacaoId]), Login))).Error.ShouldBeNull();
        await GerarAsync(referencia.Value!.Id, ctx.ComparacaoId, Ancora);

        var daCopia = await GradeAsync(copiaId, ctx.PlantonistaId);
        var esperada = await GradeAsync(referencia.Value!.Id, ctx.ComparacaoId);

        daCopia.ShouldNotBeEmpty();
        daCopia.ShouldBe(esperada);
    }

    [Fact]
    public async Task Marcacao_manual_de_plantao_continua_indo_pela_data()
    {
        var ctx = await PrepararAsync();
        await GerarAsync(ctx.EscalaId, ctx.PlantonistaId, Ancora);
        // Teletrabalho num dia específico do ciclo — em plantão não há "dia fixo da semana".
        (await ExecutarAsync(s => s.UpsertOcorrenciaAsync(
            ctx.EscalaId,
            ctx.PlantonistaId,
            new UpsertOcorrenciaRequest(new DateOnly(Ano, Mes, 8), "TL12", null, null, null, null),
            Login))).Error.ShouldBeNull();

        var copiaId = await CopiarParaOutubroAsync(ctx.EscalaId);

        var tl12 = (await GradeAsync(copiaId, ctx.PlantonistaId))
            .Where(x => x.Codigo == "TL12")
            .Select(x => x.Data)
            .ToList();
        tl12.ShouldBe([new DateOnly(Ano, Mes + 1, 8)]);
    }
}
