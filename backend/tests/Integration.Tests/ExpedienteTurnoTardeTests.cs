using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Turno do expediente administrativo é regime próprio (EXP_ADM manhã / EXP_ADM_TARDE tarde),
/// e não marcação solta na grade: assim a escolha fica na jornada, sobrevive à regeneração da
/// matriz e acompanha a cópia para o mês seguinte.
/// </summary>
public class ExpedienteTurnoTardeTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string Login = "chefe.turno";

    private sealed record Contexto(Guid EscalaId, Guid ManhaId, Guid TardeId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Setor Administrativo", "ADM");
            var manha = b.AdicionarServidor(setor, "Servidor da Manhã");
            var tarde = b.AdicionarServidor(setor, "Estagiário da Tarde");
            var chefe = b.AdicionarServidor(setor, "Chefe do Setor");
            b.AdicionarChefia(setor, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, Login, CatalogSeed.PerfilChefeSetorId);

            var escala = b.AdicionarEscala(setor, Ano, Mes, TipoFuncionamento.Expediente);
            b.AdicionarEscalaServidor(escala, manha, ordem: 1);
            b.AdicionarEscalaServidor(escala, tarde, ordem: 2);

            return new Contexto(escala.Id, manha.Id, tarde.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    private async Task GerarAsync(Contexto ctx)
    {
        var padraoManha = await PadraoIdAsync("EXP_ADM");
        var padraoTarde = await PadraoIdAsync("EXP_ADM_TARDE");
        var primeiroDia = new DateOnly(Ano, Mes, 1);

        var result = await ExecutarAsync(s => s.GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest(
                [
                    new GerarEscalaItemRequest(ctx.ManhaId, padraoManha, primeiroDia, null, null),
                    new GerarEscalaItemRequest(ctx.TardeId, padraoTarde, primeiroDia, null, null),
                ],
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

    [Fact]
    public async Task Regime_de_tarde_gera_codigo_T_e_manha_gera_M()
    {
        var ctx = await PrepararAsync();

        await GerarAsync(ctx);

        var tarde = await GradeAsync(ctx.EscalaId, ctx.TardeId);
        var manha = await GradeAsync(ctx.EscalaId, ctx.ManhaId);
        tarde.Where(x => x.Data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ShouldAllBe(x => x.Codigo == "T");
        manha.Where(x => x.Data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ShouldAllBe(x => x.Codigo == "M");
    }

    [Fact]
    public async Task Jornada_guarda_o_turno_escolhido()
    {
        var ctx = await PrepararAsync();
        await GerarAsync(ctx);

        await using var db = NewContext();
        var jornada = await db.EscalaJornadas
            .Include(x => x.PadraoEscala)
            .FirstAsync(x => x.EscalaServidor.ServidorId == ctx.TardeId);

        jornada.PadraoEscala!.Codigo.ShouldBe("EXP_ADM_TARDE");
        jornada.TipoOcorrenciaCodigo.ShouldBe("T");
        jornada.HoraInicio.ShouldBe(new TimeOnly(13, 0));
        jornada.HoraFim.ShouldBe(new TimeOnly(19, 0));
    }

    [Fact]
    public async Task Copia_para_o_mes_seguinte_mantem_cada_servidor_no_seu_turno()
    {
        var ctx = await PrepararAsync();
        await GerarAsync(ctx);
        (await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, Login))).Error.ShouldBeNull();
        (await ExecutarAsync(s => s.PublicarAsync(
            ctx.EscalaId, new PublicarEscalaRequest(ConfirmarConflitos: true), Login))).Error.ShouldBeNull();

        var copia = await ExecutarAsync(s => s.CopiarAsync(
            ctx.EscalaId, new CopiarEscalaRequest(Ano, Mes + 1), Login));
        copia.Error.ShouldBeNull();

        var tarde = await GradeAsync(copia.Value!.Id, ctx.TardeId);
        var manha = await GradeAsync(copia.Value!.Id, ctx.ManhaId);
        tarde.Where(x => x.Data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ShouldAllBe(x => x.Codigo == "T");
        manha.Where(x => x.Data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ShouldAllBe(x => x.Codigo == "M");

        // E o turno continua estruturado na jornada da cópia, não só nas células.
        await using var db = NewContext();
        var jornadaCopiada = await db.EscalaJornadas
            .Include(x => x.PadraoEscala)
            .FirstAsync(x => x.EscalaServidor.EscalaId == copia.Value!.Id
                             && x.EscalaServidor.ServidorId == ctx.TardeId);
        jornadaCopiada.PadraoEscala!.Codigo.ShouldBe("EXP_ADM_TARDE");
    }
}
