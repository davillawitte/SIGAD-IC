using System.IO.Compression;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;
using TemplateSistema.Persistence;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// "Baixar todas" nas Escalas Institucionais: o .zip traz todas as escalas da lista com os mesmos
/// filtros, cada arquivo como "SIGLA-Mês-Ano".
/// </summary>
public class EscalaLoteExportTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string Diretor = "diretor.lote";

    private Task PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var diretor = b.AdicionarServidor(direcao, "Diretor");
            b.AdicionarChefia(direcao, diretor, TipoChefia.Diretor);
            b.AdicionarUsuario(diretor, Diretor, CatalogSeed.PerfilChefeSetorId, CatalogSeed.PerfilDirecaoIcId);

            var sccv = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV");
            var nb = b.AdicionarSetor("Núcleo de Balística", "NB");
            foreach (var setor in new[] { sccv, nb })
            {
                var escala = b.AdicionarEscala(setor, Ano, Mes);
                b.AdicionarEscalaServidor(escala, b.AdicionarServidor(setor, $"Perito {setor.Sigla}"));
            }

            // Duas versões do mesmo setor no mês e uma escala de outubro, fora do filtro.
            b.AdicionarEscala(nb, Ano, Mes);
            b.AdicionarEscala(sccv, Ano, Mes + 1);
            return true;
        });

    private static EscalaLoteExportService Servico(ApplicationDbContext db)
    {
        var escalas = new EscalaService(db);
        var resumidas = new EscalaResumidaService(db);
        var ambiente = new AmbienteDeTeste();
        return new EscalaLoteExportService(
            escalas,
            new EscalaPdfService(db, escalas, ambiente),
            new EscalaCsvService(escalas, db),
            resumidas,
            new EscalaResumidaPdfService(resumidas, db, ambiente));
    }

    [Fact]
    public async Task Baixa_todas_as_escalas_do_filtro_com_o_nome_sigla_mes_ano()
    {
        await PrepararAsync();

        await using var db = NewContext();
        var gerado = await Servico(db).GerarAsync(
            new EscalaListQuery { Escopo = "institucional", Ano = Ano, Mes = Mes },
            "pdf",
            "definitiva",
            Diretor);

        gerado.Error.ShouldBeNull();
        gerado.Value.FileName.ShouldBe("Escalas-Setembro-2026.zip");

        using var zip = new ZipArchive(new MemoryStream(gerado.Value.Content));
        zip.Entries.Select(x => x.FullName).ShouldBe(
            ["NB-Setembro-2026.pdf", "NB-Setembro-2026 (2).pdf", "SCCV-Setembro-2026.pdf"],
            ignoreOrder: true);
        zip.Entries.ShouldAllBe(x => x.Length > 0);
    }

    [Fact]
    public async Task Excel_sai_em_xlsx_e_formato_invalido_e_recusado()
    {
        await PrepararAsync();

        await using var db = NewContext();
        var service = Servico(db);
        var query = new EscalaListQuery { Escopo = "institucional", Ano = Ano, Mes = Mes, SetorId = null };

        var excel = await service.GerarAsync(query, "excel", "resumida", Diretor);
        excel.Error.ShouldBeNull();
        using (var zip = new ZipArchive(new MemoryStream(excel.Value.Content)))
        {
            zip.Entries.ShouldAllBe(x => x.FullName.EndsWith(".xlsx"));
            zip.Entries.Count.ShouldBe(3);
        }

        var invalido = await service.GerarAsync(query, "doc", "definitiva", Diretor);
        invalido.Succeeded.ShouldBeFalse();
        invalido.Error.ShouldBe("Formato inválido. Use PDF ou Excel.");
    }

    [Fact]
    public async Task Pdf_resumida_sem_nenhuma_resumida_avisa()
    {
        await PrepararAsync();

        await using var db = NewContext();
        var gerado = await Servico(db).GerarAsync(
            new EscalaListQuery { Escopo = "institucional", Ano = Ano, Mes = Mes }, "pdf", "resumida", Diretor);

        gerado.Succeeded.ShouldBeFalse();
        gerado.Error.ShouldBe("Nenhuma das escalas listadas tem escala resumida.");
    }

    private sealed class AmbienteDeTeste : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "TemplateSistema.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
