using ClosedXML.Excel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TemplateSistema.Application.Servidores;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Exportação da tela Servidores: sempre em ordem alfabética; listagem atual (ids enviados) ou
/// todos; separado por setor ou não; PDF e Excel.
/// </summary>
public class ServidorExportTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private sealed record Contexto(Guid Bruno, Guid Alvaro, Guid Carla, Guid Agente);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE");
            var sccv = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV", nucleo);
            var balistica = b.AdicionarSetor("Setor de Balística", "BAL", nucleo);

            var bruno = b.AdicionarServidor(sccv, "Bruno Souza");
            var alvaro = b.AdicionarServidor(sccv, "Álvaro Lima");
            var carla = b.AdicionarServidor(balistica, "Carla Dias");
            var agente = b.AdicionarServidorNoNucleo(nucleo, "Agente do Núcleo");
            return new Contexto(bruno.Id, alvaro.Id, carla.Id, agente.Id);
        });

    private ServidorExportService Servico(Persistence.ApplicationDbContext db) => new(db, new AmbienteDeTeste());

    [Fact]
    public async Task Todos_em_ordem_alfabetica_com_acento_no_lugar_certo()
    {
        await PrepararAsync();
        await using var db = NewContext();

        var linhas = await Servico(db).CarregarLinhasAsync(null, CancellationToken.None);

        linhas.Select(x => x.Nome).ShouldBe(["AGENTE DO NÚCLEO", "ÁLVARO LIMA", "BRUNO SOUZA", "CARLA DIAS"]);
    }

    [Fact]
    public async Task Listagem_atual_exporta_so_os_ids_enviados()
    {
        var ctx = await PrepararAsync();
        await using var db = NewContext();

        var linhas = await Servico(db).CarregarLinhasAsync([ctx.Carla, ctx.Bruno], CancellationToken.None);

        linhas.Select(x => x.Nome).ShouldBe(["BRUNO SOUZA", "CARLA DIAS"]);
    }

    [Fact]
    public async Task Separado_por_setor_grupos_em_ordem_alfabetica()
    {
        await PrepararAsync();
        await using var db = NewContext();

        var linhas = await Servico(db).CarregarLinhasAsync(null, CancellationToken.None);
        var grupos = ServidorExportService.Agrupar(linhas);

        grupos.Select(g => g.Lotacao).ShouldBe([
            "BAL — Setor de Balística",
            "NPE — Núcleo de Perícias Externas",
            "SCCV — Setor de Crimes Contra a Vida",
        ]);
        grupos.Single(g => g.Lotacao!.StartsWith("SCCV")).Linhas.Select(x => x.Nome)
            .ShouldBe(["ÁLVARO LIMA", "BRUNO SOUZA"]);
    }

    [Fact]
    public async Task Excel_separado_por_setor_tem_um_bloco_por_setor_em_ordem()
    {
        await PrepararAsync();
        await using var db = NewContext();

        var gerado = await Servico(db).GerarAsync(new ExportarServidoresRequest("excel", AgruparPorSetor: true));

        gerado.Error.ShouldBeNull();
        gerado.Value.FileName.ShouldEndWith(".xlsx");
        using var workbook = new XLWorkbook(new MemoryStream(gerado.Value.Content));
        var textos = workbook.Worksheet(1).CellsUsed().Select(c => c.GetString()).ToList();
        var bal = textos.FindIndex(t => t.StartsWith("BAL — "));
        var sccv = textos.FindIndex(t => t.StartsWith("SCCV — "));
        bal.ShouldBeGreaterThan(-1);
        sccv.ShouldBeGreaterThan(bal);
        textos.IndexOf("ÁLVARO LIMA").ShouldBeLessThan(textos.IndexOf("BRUNO SOUZA"));
    }

    [Fact]
    public async Task Pdf_e_gerado_e_formato_invalido_e_recusado()
    {
        await PrepararAsync();
        await using var db = NewContext();
        var service = Servico(db);

        var pdf = await service.GerarAsync(new ExportarServidoresRequest("pdf", AgruparPorSetor: false));
        pdf.Error.ShouldBeNull();
        pdf.Value.ContentType.ShouldBe("application/pdf");
        pdf.Value.Content.Length.ShouldBeGreaterThan(0);

        var invalido = await service.GerarAsync(new ExportarServidoresRequest("doc", AgruparPorSetor: false));
        invalido.Succeeded.ShouldBeFalse();
        invalido.Error.ShouldBe("Formato inválido. Use PDF ou Excel.");
    }

    private sealed class AmbienteDeTeste : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "TemplateSistema.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
