using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Afastamento é por data, não por dia da semana: a cópia não herda férias/licença da matriz —
/// ela reaplica o que está CADASTRADO, recortado no mês de destino. Assim a licença encerrada
/// não reaparece e as férias que atravessam a virada continuam nos primeiros dias do mês novo.
/// </summary>
public class CopiarEscalaComAfastamentoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 10;
    private const string Login = "chefe.afast";

    private sealed record Contexto(Guid EscalaId, Guid ServidorId);

    /// <summary>Escala administrativa de outubro com um servidor; afastamentos vêm por parâmetro.</summary>
    private Task<Contexto> PrepararAsync(params (DateOnly Inicio, DateOnly Fim, string Codigo)[] afastamentos) =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Setor Administrativo", "ADM");
            var servidor = b.AdicionarServidor(setor, "Servidor Administrativo");
            var chefe = b.AdicionarServidor(setor, "Chefe do Setor");
            b.AdicionarChefia(setor, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, Login, CatalogSeed.PerfilChefeSetorId);

            foreach (var (inicio, fim, codigo) in afastamentos)
            {
                b.AdicionarAfastamento(servidor, inicio, fim, codigo, sei: "SEI-123");
            }

            var escala = b.AdicionarEscala(setor, Ano, Mes, TipoFuncionamento.Expediente);
            b.AdicionarEscalaServidor(escala, servidor);

            return new Contexto(escala.Id, servidor.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    /// <summary>Expediente de seg a sex no mês, como numa escala administrativa real.</summary>
    private async Task GerarExpedienteAsync(Contexto ctx)
    {
        var padrao = await PadraoIdAsync("EXP_ADM");
        var result = await ExecutarAsync(s => s.GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest(
                [new GerarEscalaItemRequest(ctx.ServidorId, padrao, new DateOnly(Ano, Mes, 1), null, null)],
                DistribuirAutomaticamente: false,
                DataBaseDistribuicao: null),
            Login));
        result.Error.ShouldBeNull();
    }

    private async Task<List<DateOnly>> DatasAsync(Guid escalaId, string codigo)
    {
        await using var db = NewContext();
        return await db.EscalaOcorrencias
            .Where(x => x.EscalaServidor.EscalaId == escalaId && x.TipoOcorrenciaCodigo == codigo)
            .Select(x => x.Data)
            .OrderBy(x => x)
            .ToListAsync();
    }

    private async Task<Guid> CopiarParaNovembroAsync(Guid escalaId)
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
    public async Task Licenca_encerrada_no_mes_de_origem_nao_vai_para_o_mes_seguinte()
    {
        // Licença médica de 05 a 09 de outubro, já encerrada quando novembro começa.
        var ctx = await PrepararAsync((new DateOnly(Ano, Mes, 5), new DateOnly(Ano, Mes, 9), "LM"));
        await GerarExpedienteAsync(ctx);
        (await DatasAsync(ctx.EscalaId, "LM")).Count.ShouldBe(5);

        var copiaId = await CopiarParaNovembroAsync(ctx.EscalaId);

        (await DatasAsync(copiaId, "LM")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Ferias_que_atravessam_a_virada_continuam_no_mes_seguinte()
    {
        // 30 dias a partir de 15/10 — termina em 13/11.
        var ctx = await PrepararAsync((new DateOnly(Ano, Mes, 15), new DateOnly(Ano, Mes + 1, 13), "FR"));
        await GerarExpedienteAsync(ctx);

        var copiaId = await CopiarParaNovembroAsync(ctx.EscalaId);

        var ferias = await DatasAsync(copiaId, "FR");
        ferias.First().ShouldBe(new DateOnly(Ano, Mes + 1, 1));
        ferias.Last().ShouldBe(new DateOnly(Ano, Mes + 1, 13));
        ferias.Count.ShouldBe(13);
    }

    [Fact]
    public async Task Ferias_lancadas_so_na_matriz_nao_sao_herdadas()
    {
        var ctx = await PrepararAsync();
        await GerarExpedienteAsync(ctx);
        // Lançamento direto na grade, sem cadastro no módulo de Afastamentos.
        (await ExecutarAsync(s => s.UpsertOcorrenciaAsync(
            ctx.EscalaId,
            ctx.ServidorId,
            new UpsertOcorrenciaRequest(new DateOnly(Ano, Mes, 8), "FR", null, null, null, null),
            Login))).Error.ShouldBeNull();

        var copiaId = await CopiarParaNovembroAsync(ctx.EscalaId);

        (await DatasAsync(copiaId, "FR")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Afastamento_do_mes_de_destino_entra_mesmo_sem_existir_na_origem()
    {
        // Licença que só começa em novembro: não existe na escala de outubro.
        var ctx = await PrepararAsync((new DateOnly(Ano, Mes + 1, 4), new DateOnly(Ano, Mes + 1, 6), "LM"));
        await GerarExpedienteAsync(ctx);
        (await DatasAsync(ctx.EscalaId, "LM")).ShouldBeEmpty();

        var copiaId = await CopiarParaNovembroAsync(ctx.EscalaId);

        (await DatasAsync(copiaId, "LM")).ShouldBe([
            new DateOnly(Ano, Mes + 1, 4),
            new DateOnly(Ano, Mes + 1, 5),
            new DateOnly(Ano, Mes + 1, 6),
        ]);
    }
}
