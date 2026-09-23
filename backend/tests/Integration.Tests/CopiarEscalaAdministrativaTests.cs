using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Cópia de escala administrativa: marcação manual de trabalho é combinado semanal ("toda
/// quinta"), então segue o dia da semana no mês de destino — e não o mesmo dia do mês, que
/// cairia em outro dia da semana. Férias/licenças continuam pela data.
/// </summary>
public class CopiarEscalaAdministrativaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;   // setembro/2026: quintas nos dias 3, 10, 17 e 24
    private const string Login = "chefe.adm";

    private sealed record Contexto(Guid EscalaId, Guid ServidorId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Setor Administrativo", "ADM");
            var servidor = b.AdicionarServidor(setor, "Servidor Administrativo");
            var chefe = b.AdicionarServidor(setor, "Chefe do Setor");
            b.AdicionarChefia(setor, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, Login, CatalogSeed.PerfilChefeSetorId);

            var escala = b.AdicionarEscala(setor, Ano, Mes, TipoFuncionamento.Expediente);
            b.AdicionarEscalaServidor(escala, servidor);

            return new Contexto(escala.Id, servidor.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    private async Task LancarAsync(Guid escalaId, Guid servidorId, int dia, string codigo)
    {
        var result = await ExecutarAsync(s => s.UpsertOcorrenciaAsync(
            escalaId,
            servidorId,
            new UpsertOcorrenciaRequest(new DateOnly(Ano, Mes, dia), codigo, null, null, null, null),
            Login));
        result.Error.ShouldBeNull();
    }

    /// <summary>Expediente de seg a sex no mês inteiro, como numa escala administrativa real.</summary>
    private async Task GerarExpedienteAsync(Guid escalaId, Guid servidorId)
    {
        var padrao = await PadraoIdAsync("EXP_ADM");
        var result = await ExecutarAsync(s => s.GerarEscalaAsync(
            escalaId,
            new GerarEscalaRequest(
                [new GerarEscalaItemRequest(servidorId, padrao, new DateOnly(Ano, Mes, 1), null, null)],
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
    public async Task Home_office_semanal_cai_no_mesmo_dia_da_semana_no_mes_seguinte()
    {
        var ctx = await PrepararAsync();
        await GerarExpedienteAsync(ctx.EscalaId, ctx.ServidorId);
        // Todas as quintas de setembro/2026.
        foreach (var dia in new[] { 3, 10, 17, 24 })
        {
            await LancarAsync(ctx.EscalaId, ctx.ServidorId, dia, "TL6");
        }

        var copiaId = await CopiarParaOutubroAsync(ctx.EscalaId);

        // Outubro/2026 tem cinco quintas: 1, 8, 15, 22 e 29 — todas viram home office.
        var datas = await DatasAsync(copiaId, "TL6");
        datas.ShouldAllBe(x => x.DayOfWeek == DayOfWeek.Thursday);
        datas.Select(x => x.Day).ShouldBe([1, 8, 15, 22, 29]);
    }

    /// <summary>
    /// Lançar ocorrência num dia que ainda não tem nenhuma (sábado, ou escala sem regime gerado)
    /// estourava DbUpdateConcurrencyException: a ocorrência nova entrava só na coleção do
    /// servidor e o EF, vendo o `Id` já preenchido, tentava UPDATE de uma linha inexistente.
    /// </summary>
    [Fact]
    public async Task Lancar_ocorrencia_em_dia_ainda_sem_nenhuma()
    {
        var ctx = await PrepararAsync();

        // Sem gerar expediente: todos os dias estão vazios.
        await LancarAsync(ctx.EscalaId, ctx.ServidorId, 5, "TL6");

        (await DatasAsync(ctx.EscalaId, "TL6")).ShouldBe([new DateOnly(Ano, Mes, 5)]);
    }

    [Fact]
    public async Task Ferias_lancadas_na_matriz_continuam_indo_pela_data()
    {
        var ctx = await PrepararAsync();
        await GerarExpedienteAsync(ctx.EscalaId, ctx.ServidorId);
        await LancarAsync(ctx.EscalaId, ctx.ServidorId, 8, "FR");

        var copiaId = await CopiarParaOutubroAsync(ctx.EscalaId);

        (await DatasAsync(copiaId, "FR")).ShouldBe([new DateOnly(Ano, Mes + 1, 8)]);
    }

    [Fact]
    public async Task Home_office_nao_marca_fim_de_semana_nem_dia_sem_expediente()
    {
        var ctx = await PrepararAsync();
        await GerarExpedienteAsync(ctx.EscalaId, ctx.ServidorId);
        // Sábado 5 de setembro: marcação avulsa, fora do expediente de seg a sex.
        await LancarAsync(ctx.EscalaId, ctx.ServidorId, 5, "TL6");

        var copiaId = await CopiarParaOutubroAsync(ctx.EscalaId);

        // Os sábados de outubro não têm expediente gerado, então não viram home office.
        (await DatasAsync(copiaId, "TL6")).ShouldBeEmpty();
    }
}
