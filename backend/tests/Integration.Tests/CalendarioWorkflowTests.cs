using Shouldly;
using TemplateSistema.Application.Calendario;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Caracterização do fluxo do calendário institucional: geração de ano com feriados
/// fixos/móveis, publicação (sem travar edição), marcações manuais e a consulta de
/// dias não úteis que escalas/prazos vão consumir no futuro.
/// </summary>
public class CalendarioWorkflowTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Login = "direcao.ic";

    private async Task<CalendarioAnoDto> GerarAnoAsync(int ano)
    {
        await using var db = NewContext();
        var service = new CalendarioService(db);
        var resultado = await service.GerarAnoAsync(new GerarAnoRequest(ano), Login);
        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        return resultado.Value!;
    }

    [Fact]
    public async Task Gerar_ano_cria_feriados_fixos_e_moveis_em_rascunho()
    {
        var calendario = await GerarAnoAsync(2027);

        calendario.Status.ShouldBe("Rascunho");
        calendario.Marcacoes.Count.ShouldBe(9 + 2 + 2 + 1); // nacionais + estaduais RN + municipais Natal + móvel

        var sextaSanta = calendario.Marcacoes.Single(m => m.Nome == "Sexta-feira Santa");
        sextaSanta.Tipo.ShouldBe("FeriadoNacional");
        sextaSanta.Origem.ShouldBe("Calculado");
        sextaSanta.Data.ShouldBe(FeriadoMovelCalculator.Pascoa(2027).AddDays(-2));

        var natal = calendario.Marcacoes.Single(m => m.Nome == "Natal");
        natal.Origem.ShouldBe("Fixo");
        natal.Data.ShouldBe(new DateOnly(2027, 12, 25));

        var martires = calendario.Marcacoes.Single(m => m.Nome == "Santos Mártires de Cunhaú e Uruaçu");
        martires.Tipo.ShouldBe("FeriadoEstadual");
        martires.Data.ShouldBe(new DateOnly(2027, 10, 3));

        var santosReis = calendario.Marcacoes.Single(m => m.Nome == "Dia de Santos Reis");
        santosReis.Tipo.ShouldBe("FeriadoMunicipal");
        santosReis.Data.ShouldBe(new DateOnly(2027, 1, 6));

        var apresentacao = calendario.Marcacoes.Single(m => m.Nome == "Nossa Senhora da Apresentação");
        apresentacao.Tipo.ShouldBe("FeriadoMunicipal");
        apresentacao.Data.ShouldBe(new DateOnly(2027, 11, 21));

        calendario.Marcacoes.ShouldNotContain(m => m.Nome.Contains("Carnaval"));
        calendario.Marcacoes.ShouldNotContain(m => m.Nome.Contains("Corpus Christi"));
    }

    [Fact]
    public async Task Gerar_ano_ja_existente_falha()
    {
        await GerarAnoAsync(2028);

        await using var db = NewContext();
        var service = new CalendarioService(db);
        var resultado = await service.GerarAnoAsync(new GerarAnoRequest(2028), Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("O calendário de 2028 já foi gerado.");
    }

    [Fact]
    public async Task Publicar_muda_status_e_permite_editar_marcacao_depois_sem_despublicar()
    {
        var calendario = await GerarAnoAsync(2029);

        await using (var db = NewContext())
        {
            var service = new CalendarioService(db);
            var publicar = await service.PublicarAsync(calendario.Id, Login);
            publicar.Succeeded.ShouldBeTrue(publicar.Error);
            publicar.Value!.Status.ShouldBe("Publicado");
        }

        var natal = calendario.Marcacoes.Single(m => m.Nome == "Natal");

        await using (var db = NewContext())
        {
            var service = new CalendarioService(db);
            var editar = await service.AtualizarMarcacaoAsync(
                natal.Id,
                new AtualizarMarcacaoRequest("FeriadoNacional", natal.Data, null, "Natal (editado)", null, null, null),
                Login);

            editar.Succeeded.ShouldBeTrue(editar.Error);
            editar.Value!.Nome.ShouldBe("Natal (editado)");
            editar.Value!.Origem.ShouldBe("Fixo");
        }

        await using var check = NewContext();
        var reconsulta = await new CalendarioService(check).ObterAnoAsync(2029);
        reconsulta.Value!.Status.ShouldBe("Publicado");
    }

    [Fact]
    public async Task Adicionar_marcacao_manual_evento_institucional_preenche_campos_extras()
    {
        var calendario = await GerarAnoAsync(2030);

        await using var db = NewContext();
        var service = new CalendarioService(db);
        var resultado = await service.AdicionarMarcacaoAsync(
            new CriarMarcacaoRequest(
                calendario.Id,
                "EventoInstitucional",
                new DateOnly(2030, 3, 10),
                new DateOnly(2030, 3, 12),
                "Semana de Perícia",
                "Encontro anual de peritos do IC.",
                "Auditório Central",
                "Servidores do IC"),
            Login);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        resultado.Value!.Origem.ShouldBe("Manual");
        resultado.Value!.Descricao.ShouldBe("Encontro anual de peritos do IC.");
        resultado.Value!.Local.ShouldBe("Auditório Central");
        resultado.Value!.PublicoAlvo.ShouldBe("Servidores do IC");
    }

    [Fact]
    public async Task Adicionar_marcacao_feriado_ignora_campos_de_evento()
    {
        var calendario = await GerarAnoAsync(2031);

        await using var db = NewContext();
        var service = new CalendarioService(db);
        var resultado = await service.AdicionarMarcacaoAsync(
            new CriarMarcacaoRequest(
                calendario.Id,
                "PontoFacultativo",
                new DateOnly(2031, 5, 20),
                null,
                "Ponto facultativo local",
                "Não deveria aparecer",
                "Não deveria aparecer",
                "Não deveria aparecer"),
            Login);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        resultado.Value!.Descricao.ShouldBeNull();
        resultado.Value!.Local.ShouldBeNull();
        resultado.Value!.PublicoAlvo.ShouldBeNull();
    }

    [Fact]
    public async Task Excluir_marcacao_remove_do_ano()
    {
        var calendario = await GerarAnoAsync(2032);
        var tiradentes = calendario.Marcacoes.First(m => m.Nome == "Tiradentes");

        await using (var db = NewContext())
        {
            var resultado = await new CalendarioService(db).ExcluirMarcacaoAsync(tiradentes.Id, Login);
            resultado.Succeeded.ShouldBeTrue(resultado.Error);
        }

        await using var check = NewContext();
        var reconsulta = await new CalendarioService(check).ObterAnoAsync(2032);
        reconsulta.Value!.Marcacoes.ShouldNotContain(m => m.Id == tiradentes.Id);
    }

    [Fact]
    public async Task Adicionar_marcacao_com_data_fora_do_ano_do_calendario_falha()
    {
        var calendario = await GerarAnoAsync(2035);

        await using var db = NewContext();
        var resultado = await new CalendarioService(db).AdicionarMarcacaoAsync(
            new CriarMarcacaoRequest(
                calendario.Id, "PontoFacultativo", new DateOnly(2036, 1, 2), null, "Data errada",
                null, null, null),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("A data da marcação deve estar dentro do ano do calendário.");
    }

    [Fact]
    public async Task Atualizar_marcacao_com_data_fim_fora_do_ano_do_calendario_falha()
    {
        var calendario = await GerarAnoAsync(2036);
        var natal = calendario.Marcacoes.Single(m => m.Nome == "Natal");

        await using var db = NewContext();
        var resultado = await new CalendarioService(db).AtualizarMarcacaoAsync(
            natal.Id,
            new AtualizarMarcacaoRequest(
                "EventoInstitucional", natal.Data, new DateOnly(2037, 1, 2), "Virada de ano", null, null, null),
            Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("A data da marcação deve estar dentro do ano do calendário.");
    }

    [Fact]
    public async Task Excluir_ano_em_rascunho_remove_calendario_e_marcacoes()
    {
        var calendario = await GerarAnoAsync(2038);

        await using (var db = NewContext())
        {
            var resultado = await new CalendarioService(db).ExcluirAnoAsync(calendario.Id, Login);
            resultado.Succeeded.ShouldBeTrue(resultado.Error);
        }

        await using var check = NewContext();
        var reconsulta = await new CalendarioService(check).ObterAnoAsync(2038);
        reconsulta.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Excluir_ano_publicado_falha()
    {
        var calendario = await GerarAnoAsync(2039);

        await using (var db = NewContext())
        {
            (await new CalendarioService(db).PublicarAsync(calendario.Id, Login)).Succeeded.ShouldBeTrue();
        }

        await using var db2 = NewContext();
        var resultado = await new CalendarioService(db2).ExcluirAnoAsync(calendario.Id, Login);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Somente calendários em rascunho podem ser excluídos.");
    }

    [Fact]
    public async Task Dias_nao_uteis_so_considera_ano_publicado_e_ignora_evento_institucional()
    {
        var rascunho = await GerarAnoAsync(2033);
        var publicado = await GerarAnoAsync(2034);

        await using (var db = NewContext())
        {
            var service = new CalendarioService(db);
            (await service.PublicarAsync(publicado.Id, Login)).Succeeded.ShouldBeTrue();
            await service.AdicionarMarcacaoAsync(
                new CriarMarcacaoRequest(
                    publicado.Id, "EventoInstitucional", new DateOnly(2034, 1, 1), null, "Evento no feriado",
                    null, null, null),
                Login);
        }

        await using var check = NewContext();
        var service2 = new CalendarioService(check);

        var diasDoRascunho = await service2.ConsultarDiasNaoUteisAsync(
            new DateOnly(2033, 1, 1), new DateOnly(2033, 1, 31));
        diasDoRascunho.ShouldBeEmpty();

        var diasDoPublicado = await service2.ConsultarDiasNaoUteisAsync(
            new DateOnly(2034, 1, 1), new DateOnly(2034, 1, 31));
        diasDoPublicado.ShouldContain(d => d.Data == new DateOnly(2034, 1, 1) && d.Tipo == "FeriadoNacional");
        diasDoPublicado.Count(d => d.Data == new DateOnly(2034, 1, 1)).ShouldBe(1); // evento não conta
    }
}
