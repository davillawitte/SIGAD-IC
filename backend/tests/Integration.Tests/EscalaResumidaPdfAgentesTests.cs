using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// PDF da escala resumida: o grupo "Agentes" sai em folha própria, separado dos setores, e cada
/// folha repete cabeçalho, assinatura e rodapé.
/// </summary>
public class EscalaResumidaPdfAgentesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string ChefeNucleo = "chefe.pdf";

    private sealed record Contexto(Guid NucleoId, Guid SetorId, Guid ServidorSetorId, Guid AgenteId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE", chefe.Id);
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var setor = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV", nucleo);
            var servidorSetor = b.AdicionarServidor(setor, "Servidor do Setor");
            var agente = b.AdicionarServidorNoNucleo(nucleo, "Agente do Núcleo");

            return new Contexto(nucleo.Id, setor.Id, servidorSetor.Id, agente.Id);
        });

    /// <summary>Escala resumida do núcleo com o setor e, opcionalmente, o grupo de Agentes.</summary>
    private async Task<EscalaResumidaDetailDto> MontarAsync(Contexto ctx, bool comAgentes)
    {
        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var escala = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(ctx.NucleoId, Ano, Mes, null), ChefeNucleo)).Value!;

        List<ConfigurarSetorItem> grupos = comAgentes
            ? [new ConfigurarSetorItem(ctx.SetorId, 1), new ConfigurarSetorItem(null, 2)]
            : [new ConfigurarSetorItem(ctx.SetorId, 1)];
        escala = (await service.ConfigurarSetoresAsync(
            escala.Id, new ConfigurarSetoresRequest(grupos), ChefeNucleo)).Value!;

        foreach (var grupo in escala.Setores)
        {
            var comEquipe = (await service.ConfigurarEquipeAsync(
                escala.Id, new ConfigurarEquipeRequest(grupo.Id), ChefeNucleo)).Value!;
            var equipeId = comEquipe.Setores.First(x => x.Id == grupo.Id).Equipes[0].Id;
            var servidorId = grupo.SetorId is null ? ctx.AgenteId : ctx.ServidorSetorId;

            escala = (await service.ConfigurarRotacaoAsync(
                escala.Id,
                equipeId,
                new ConfigurarRotacaoRequest(new DateOnly(Ano, Mes, 1), [new RotacaoMembroItem(0, servidorId)]),
                ChefeNucleo)).Value!;
        }

        return escala;
    }

    [Fact]
    public async Task Agentes_ficam_numa_folha_separada_dos_setores()
    {
        var ctx = await PrepararAsync();
        var escala = await MontarAsync(ctx, comAgentes: true);

        var paginas = EscalaResumidaPdfService.DividirPaginas(escala);

        paginas.Count.ShouldBe(2);
        paginas[0].ShouldAllBe(x => x.SetorId != null);
        paginas[1].Select(x => x.SetorId).ShouldBe([(Guid?)null]);
    }

    [Fact]
    public async Task Sem_agentes_continua_em_uma_folha_so()
    {
        var ctx = await PrepararAsync();
        var escala = await MontarAsync(ctx, comAgentes: false);

        var paginas = EscalaResumidaPdfService.DividirPaginas(escala);

        paginas.Count.ShouldBe(1);
        paginas[0].Select(x => x.SetorId).ShouldBe([(Guid?)ctx.SetorId]);
    }

    [Fact]
    public async Task Pdf_com_agentes_e_gerado_com_as_duas_folhas()
    {
        var ctx = await PrepararAsync();
        var escala = await MontarAsync(ctx, comAgentes: true);

        await using var db = NewContext();
        var pdfService = new EscalaResumidaPdfService(
            new EscalaResumidaService(db), db, new AmbienteDeTeste());
        var gerado = await pdfService.GenerateAsync(escala.Id, ChefeNucleo);

        gerado.Error.ShouldBeNull();
        gerado.Value.Content.Length.ShouldBeGreaterThan(0);
        // "/Count 2" na árvore de páginas do PDF: uma folha para os setores, outra para Agentes.
        System.Text.Encoding.Latin1.GetString(gerado.Value.Content).ShouldContain("/Count 2");
    }

    private sealed class AmbienteDeTeste : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "TemplateSistema.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
