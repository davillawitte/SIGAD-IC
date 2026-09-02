using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Caracterização de <c>GerarEscalaAsync</c>: distribuição de âncoras, regeneração e
/// interação com afastamentos.
/// </summary>
public class GerarEscalaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 7;
    private const string Login = "superadmin";

    private static readonly DateOnly PrimeiroDia = new(Ano, Mes, 1);

    private sealed record Contexto(Guid SetorId, Guid EscalaId, Guid AnaId, Guid BrunoId);

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
            return new Contexto(setor.Id, escala.Id, ana.Id, bruno.Id);
        });

    private static GerarEscalaItemRequest Item(Guid servidorId, Guid padraoId, DateOnly? ancora = null) =>
        new(servidorId, padraoId, ancora ?? PrimeiroDia, null, null);

    [Fact]
    public async Task Distribuicao_automatica_escalona_a_ancora_por_indice_do_item()
    {
        var ctx = await PrepararAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest(
                [Item(ctx.AnaId, padrao12x36), Item(ctx.BrunoId, padrao12x36)],
                DistribuirAutomaticamente: true,
                DataBaseDistribuicao: null),
            Login);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);

        var ana = await OcorrenciasAsync(ctx.AnaId);
        var bruno = await OcorrenciasAsync(ctx.BrunoId);

        // Ana é o índice 0 (âncora = dia 1) e Bruno o índice 1 (âncora = dia 2),
        // então os plantões ficam intercalados.
        ana.Single(x => x.Data == PrimeiroDia).TipoOcorrenciaCodigo.ShouldBe("PD");
        ana.Single(x => x.Data == PrimeiroDia.AddDays(1)).TipoOcorrenciaCodigo.ShouldBe("D");
        bruno.Single(x => x.Data == PrimeiroDia).TipoOcorrenciaCodigo.ShouldBe("D");
        bruno.Single(x => x.Data == PrimeiroDia.AddDays(1)).TipoOcorrenciaCodigo.ShouldBe("PD");
    }

    [Fact]
    public async Task Distribuicao_automatica_usa_a_data_base_informada_como_ancora()
    {
        var ctx = await PrepararAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");
        var dataBase = PrimeiroDia.AddDays(1);

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest(
                [Item(ctx.AnaId, padrao12x36)],
                DistribuirAutomaticamente: true,
                DataBaseDistribuicao: dataBase),
            Login);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);

        var ana = await OcorrenciasAsync(ctx.AnaId);
        ana.Single(x => x.Data == dataBase).TipoOcorrenciaCodigo.ShouldBe("PD");
        ana.Single(x => x.Data == PrimeiroDia).TipoOcorrenciaCodigo.ShouldBe("D");
    }

    [Fact]
    public async Task Sem_distribuicao_automatica_usa_a_ancora_de_cada_item()
    {
        var ctx = await PrepararAsync();
        var padrao24x72 = await PadraoIdAsync("24X72");
        var ancora = PrimeiroDia.AddDays(4);

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest(
                [Item(ctx.AnaId, padrao24x72, ancora)],
                DistribuirAutomaticamente: false,
                DataBaseDistribuicao: null),
            Login);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);

        var trabalho = (await OcorrenciasAsync(ctx.AnaId))
            .Where(x => x.TipoOcorrenciaCodigo == "PT")
            .Select(x => x.Data.Day)
            .ToList();

        // Ciclo de 4 dias ancorado no dia 5. O dia 1 também é plantão porque a fase
        // negativa do módulo mantém a continuidade do ciclo.
        trabalho.ShouldBe([1, 5, 9, 13, 17, 21, 25, 29]);
    }

    [Fact]
    public async Task Regeneracao_preserva_ocorrencias_manuais_e_substitui_as_de_regra()
    {
        var ctx = await PrepararAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");
        var padrao24x72 = await PadraoIdAsync("24X72");
        var diaManual = PrimeiroDia.AddDays(1);

        await using (var db = NewContext())
        {
            var service = new EscalaService(db);
            (await service.GerarEscalaAsync(
                ctx.EscalaId,
                new GerarEscalaRequest([Item(ctx.AnaId, padrao12x36)], false, null),
                Login)).Succeeded.ShouldBeTrue();

            // Sobrescreve um dia manualmente: passa a Origem = Manual.
            (await service.UpsertOcorrenciaAsync(
                ctx.EscalaId,
                ctx.AnaId,
                new UpsertOcorrenciaRequest(diaManual, "T", null, null, 6m, "ajuste manual"),
                Login)).Succeeded.ShouldBeTrue();
        }

        await using (var db = NewContext())
        {
            (await new EscalaService(db).GerarEscalaAsync(
                ctx.EscalaId,
                new GerarEscalaRequest([Item(ctx.AnaId, padrao24x72)], false, null),
                Login)).Succeeded.ShouldBeTrue();
        }

        var ocorrencias = await OcorrenciasAsync(ctx.AnaId);

        // O dia manual sobrevive à regeneração, inclusive com o padrão trocado.
        var manual = ocorrencias.Single(x => x.Data == diaManual);
        manual.TipoOcorrenciaCodigo.ShouldBe("T");
        manual.Origem.ShouldBe(OrigemOcorrencia.Manual);

        // Os demais dias passaram a seguir o novo padrão 24x72.
        ocorrencias.Single(x => x.Data == PrimeiroDia).TipoOcorrenciaCodigo.ShouldBe("PT");
        ocorrencias.Where(x => x.Data != diaManual)
            .ShouldAllBe(x => x.Origem == OrigemOcorrencia.Regra);
    }

    [Fact]
    public async Task Afastamento_sobrescreve_o_dia_gerado_pela_regra()
    {
        var ctx = await PrepararAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");
        var inicioFerias = PrimeiroDia.AddDays(2);
        var fimFerias = PrimeiroDia.AddDays(5);

        await using (var seed = NewContext())
        {
            var ana = await seed.Servidores.FindAsync(ctx.AnaId);
            new CenarioBuilder(seed, await CargoDoCatalogoAsync(seed))
                .AdicionarAfastamento(ana!, inicioFerias, fimFerias, "FR", sei: "12345");
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest([Item(ctx.AnaId, padrao12x36)], false, null),
            Login);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);

        var ocorrencias = await OcorrenciasAsync(ctx.AnaId);
        var noAfastamento = ocorrencias
            .Where(x => x.Data >= inicioFerias && x.Data <= fimFerias)
            .ToList();

        noAfastamento.Count.ShouldBe(4);
        noAfastamento.ShouldAllBe(x => x.TipoOcorrenciaCodigo == "FR");
        // O afastamento entra como Manual e guarda o SEI na observação.
        noAfastamento.ShouldAllBe(x => x.Origem == OrigemOcorrencia.Manual);
        noAfastamento.ShouldAllBe(x => x.Observacao == "12345");

        // Fora do afastamento a regra continua valendo.
        ocorrencias.Single(x => x.Data == PrimeiroDia).Origem.ShouldBe(OrigemOcorrencia.Regra);
    }

    [Fact]
    public async Task Ciclo_customizado_PT24_TL12_usa_a_duracao_de_cada_fase_pelo_proprio_codigo()
    {
        // PT24_TL12 (sequência "PT,D,D,D,TL12,D") mistura uma fase de 24h ("PT") com uma de
        // 12h ("TL12") sob o mesmo padrão — a duração de cada dia tem que vir do catálogo de
        // TipoOcorrencia por código, não do valor único do padrão (que é 24h), senão TL12
        // herda 24h e a carga horária remota conta em dobro.
        var ctx = await PrepararAsync();
        var padraoPT24TL12 = await PadraoIdAsync("PT24_TL12");

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest([Item(ctx.AnaId, padraoPT24TL12)], false, null),
            Login);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);

        var ocorrencias = await OcorrenciasAsync(ctx.AnaId);

        var diasPT = ocorrencias.Where(x => x.TipoOcorrenciaCodigo == "PT").ToList();
        diasPT.ShouldNotBeEmpty();
        diasPT.ShouldAllBe(x => x.Horas == 24m);

        var diasTL12 = ocorrencias.Where(x => x.TipoOcorrenciaCodigo == "TL12").ToList();
        diasTL12.ShouldNotBeEmpty();
        diasTL12.ShouldAllBe(x => x.Horas == 12m, "TL12 é 12h — não pode herdar as 24h do padrão PT24_TL12");
    }

    [Fact]
    public async Task Gerar_sem_itens_falha()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest([], false, null),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Informe ao menos um servidor para gerar a escala.");
    }

    [Fact]
    public async Task Gerar_com_padrao_inexistente_falha()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest([Item(ctx.AnaId, Guid.NewGuid())], false, null),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Há padrões de escala inválidos ou inativos.");
    }

    [Fact]
    public async Task Gerar_com_servidor_de_outro_setor_falha()
    {
        var ctx = await PrepararAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");

        var forasteiroId = await SemearAsync(b =>
        {
            var outroSetor = b.AdicionarSetor("Núcleo de Papiloscopia", "NP");
            return b.AdicionarServidor(outroSetor, "Forasteiro").Id;
        });

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest([Item(forasteiroId, padrao12x36)], false, null),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Há servidores inválidos ou que não pertencem a este setor/núcleo.");
    }

    private sealed record ContextoNucleo(Guid EscalaId, string Login);

    private Task<ContextoNucleo> PrepararNucleoAsync() =>
        SemearAsync(b =>
        {
            const string login = "chefe-nucleo-teste";
            var setor = b.AdicionarSetor("Setor do Núcleo Central", "SNC");
            var admin = b.AdicionarServidor(setor, "Administrador Núcleo");
            var nucleo = b.AdicionarNucleo("Núcleo Central", "NC", admin.Id);
            b.AdicionarSuperAdmin(admin, login, CatalogSeed.PerfilChefeSetorId);
            var escala = b.AdicionarEscalaDeNucleo(nucleo, Ano, Mes);
            return new ContextoNucleo(escala.Id, login);
        });

    [Fact]
    public async Task Gerar_com_servidor_de_outro_nucleo_falha()
    {
        var ctx = await PrepararNucleoAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");

        var forasteiroId = await SemearAsync(b =>
        {
            var outroSetor = b.AdicionarSetor("Núcleo de Papiloscopia", "NP2");
            return b.AdicionarServidor(outroSetor, "Forasteiro").Id;
        });

        await using var db = NewContext();
        var resultado = await new EscalaService(db).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest([Item(forasteiroId, padrao12x36)], false, null),
            ctx.Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Há servidores inválidos ou que não pertencem a este setor/núcleo.");
    }

    [Fact]
    public async Task Gerar_em_escala_publicada_falha()
    {
        var ctx = await PrepararAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");

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
        var resultado = await new EscalaService(ctxDb).GerarEscalaAsync(
            ctx.EscalaId,
            new GerarEscalaRequest([Item(ctx.AnaId, padrao12x36)], false, null),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("A escala só pode ser alterada enquanto estiver em rascunho ou finalizada.");
    }
}
