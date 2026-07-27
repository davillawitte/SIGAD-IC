using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Caracterização de <c>SyncOcorrenciasAsync</c>. O ponto central é a assimetria do
/// escopo de exclusão: lista vazia limpa a escala inteira, lista preenchida só mexe
/// nos servidores citados no payload.
/// </summary>
public class SyncOcorrenciasTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 7;
    private const string Login = "superadmin";

    private static readonly DateOnly Dia1 = new(Ano, Mes, 1);
    private static readonly DateOnly Dia2 = new(Ano, Mes, 2);

    private sealed record Contexto(Guid EscalaId, Guid AnaId, Guid BrunoId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Núcleo de Balística", "NB");
            var ana = b.AdicionarServidor(setor, "Ana");
            var bruno = b.AdicionarServidor(setor, "Bruno");
            var admin = b.AdicionarServidor(setor, "Administrador");
            b.AdicionarChefia(setor, admin, TipoChefia.ChefiaImediata);
            b.AdicionarSuperAdmin(admin, Login, CatalogSeed.PerfilChefeSetorId);

            var escala = b.AdicionarEscala(setor, Ano, Mes);
            b.AdicionarEscalaServidor(escala, ana, ordem: 1);
            b.AdicionarEscalaServidor(escala, bruno, ordem: 2);

            return new Contexto(escala.Id, ana.Id, bruno.Id);
        });

    private static SyncOcorrenciaItemRequest Item(Guid servidorId, DateOnly data, string codigo = "M") =>
        new(servidorId, data, codigo, null, null, 6m, null);

    private async Task SincronizarAsync(Guid escalaId, params SyncOcorrenciaItemRequest[] itens)
    {
        await using var db = NewContext();
        var resultado = await new EscalaService(db).SyncOcorrenciasAsync(
            escalaId,
            new SyncOcorrenciasRequest(itens),
            Login);
        resultado.Succeeded.ShouldBeTrue(resultado.Error);
    }

    [Fact]
    public async Task Cria_ocorrencias_manuais_a_partir_do_payload()
    {
        var ctx = await PrepararAsync();

        await SincronizarAsync(ctx.EscalaId, Item(ctx.AnaId, Dia1), Item(ctx.AnaId, Dia2, "T"));

        var ana = await OcorrenciasAsync(ctx.AnaId);
        ana.Count.ShouldBe(2);
        ana.ShouldAllBe(x => x.Origem == OrigemOcorrencia.Manual);
        ana.Select(x => x.TipoOcorrenciaCodigo).ShouldBe(["M", "T"]);
    }

    [Fact]
    public async Task Lista_vazia_apaga_ocorrencias_de_todos_os_servidores_da_escala()
    {
        var ctx = await PrepararAsync();
        await SincronizarAsync(ctx.EscalaId, Item(ctx.AnaId, Dia1), Item(ctx.BrunoId, Dia1));

        await SincronizarAsync(ctx.EscalaId);

        // Comportamento relevante: o payload vazio limpa o período da escala inteira,
        // não apenas de um servidor.
        (await OcorrenciasAsync(ctx.AnaId)).ShouldBeEmpty();
        (await OcorrenciasAsync(ctx.BrunoId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Lista_preenchida_apaga_apenas_dos_servidores_citados_no_payload()
    {
        var ctx = await PrepararAsync();
        await SincronizarAsync(ctx.EscalaId, Item(ctx.AnaId, Dia1), Item(ctx.BrunoId, Dia1, "T"));

        // Só Ana aparece no novo payload.
        await SincronizarAsync(ctx.EscalaId, Item(ctx.AnaId, Dia2, "TL6"));

        var ana = await OcorrenciasAsync(ctx.AnaId);
        var bruno = await OcorrenciasAsync(ctx.BrunoId);

        // Ana foi substituída por completo; Bruno ficou intacto.
        ana.Count.ShouldBe(1);
        ana[0].Data.ShouldBe(Dia2);
        ana[0].TipoOcorrenciaCodigo.ShouldBe("TL6");

        bruno.Count.ShouldBe(1);
        bruno[0].Data.ShouldBe(Dia1);
        bruno[0].TipoOcorrenciaCodigo.ShouldBe("T");
    }

    [Fact]
    public async Task Itens_duplicados_por_servidor_e_data_mantem_o_ultimo()
    {
        var ctx = await PrepararAsync();

        await SincronizarAsync(
            ctx.EscalaId,
            Item(ctx.AnaId, Dia1, "M"),
            Item(ctx.AnaId, Dia1, "T"),
            Item(ctx.AnaId, Dia1, "TL6"));

        var ana = await OcorrenciasAsync(ctx.AnaId);
        ana.Count.ShouldBe(1);
        ana[0].TipoOcorrenciaCodigo.ShouldBe("TL6");
    }

    [Fact]
    public async Task Codigo_de_tipo_e_normalizado_para_maiusculas()
    {
        var ctx = await PrepararAsync();

        await SincronizarAsync(ctx.EscalaId, Item(ctx.AnaId, Dia1, "tl6"));

        (await OcorrenciasAsync(ctx.AnaId))[0].TipoOcorrenciaCodigo.ShouldBe("TL6");
    }

    [Fact]
    public async Task Servidor_fora_da_escala_falha()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SyncOcorrenciasAsync(
            ctx.EscalaId,
            new SyncOcorrenciasRequest([Item(Guid.NewGuid(), Dia1)]),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Há servidores que não estão na escala.");
    }

    [Fact]
    public async Task Data_fora_do_periodo_falha_informando_a_data()
    {
        var ctx = await PrepararAsync();
        var foraDoPeriodo = new DateOnly(Ano, 8, 3);

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SyncOcorrenciasAsync(
            ctx.EscalaId,
            new SyncOcorrenciasRequest([Item(ctx.AnaId, foraDoPeriodo)]),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Data 03/08/2026 fora do período da escala.");
    }

    [Fact]
    public async Task Tipo_de_ocorrencia_inexistente_falha()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SyncOcorrenciasAsync(
            ctx.EscalaId,
            new SyncOcorrenciasRequest([Item(ctx.AnaId, Dia1, "XPTO")]),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Tipo de ocorrência inválido: XPTO.");
    }

    [Fact]
    public async Task Escala_publicada_nao_aceita_sincronizacao()
    {
        var ctx = await PrepararAsync();

        await using (var db = NewContext())
        {
            var service = new EscalaService(db);
            (await service.FinalizarAsync(ctx.EscalaId, Login)).Succeeded.ShouldBeTrue();
            (await service.PublicarAsync(
                ctx.EscalaId,
                new PublicarEscalaRequest(ConfirmarConflitos: true),
                Login)).Succeeded.ShouldBeTrue();
        }

        await using var ctxDb = NewContext();
        var resultado = await new EscalaService(ctxDb).SyncOcorrenciasAsync(
            ctx.EscalaId,
            new SyncOcorrenciasRequest([Item(ctx.AnaId, Dia1)]),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("A escala só pode ser alterada enquanto estiver em rascunho ou finalizada.");
    }

    [Fact]
    public async Task Escala_finalizada_ainda_aceita_sincronizacao()
    {
        var ctx = await PrepararAsync();

        await using (var db = NewContext())
        {
            (await new EscalaService(db).FinalizarAsync(ctx.EscalaId, Login)).Succeeded.ShouldBeTrue();
        }

        await SincronizarAsync(ctx.EscalaId, Item(ctx.AnaId, Dia1));

        (await OcorrenciasAsync(ctx.AnaId)).Count.ShouldBe(1);
    }
}
