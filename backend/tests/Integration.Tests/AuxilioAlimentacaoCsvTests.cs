using ClosedXML.Excel;
using Shouldly;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Planilha de auxílio-alimentação: plantão de 24h vale 3 unidades, jornada de 12h vale 2, e o
/// teletrabalho de 12h (TL12) conta como jornada de 12h. Cada unidade vale R$ 20,00.
/// </summary>
public class AuxilioAlimentacaoCsvTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string Login = "chefe.aux";

    private sealed record Contexto(Guid EscalaId, Guid ServidorId);

    /// <summary>Escala com um servidor e as ocorrências informadas (dia → código).</summary>
    private Task<Contexto> PrepararAsync(params (int Dia, string Codigo)[] ocorrencias) =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Núcleo de Balística", "NB");
            var servidor = b.AdicionarServidor(setor, "Servidor do Plantão");
            var chefe = b.AdicionarServidor(setor, "Chefe do Setor");
            b.AdicionarChefia(setor, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, Login, CatalogSeed.PerfilChefeSetorId);

            var escala = b.AdicionarEscala(setor, Ano, Mes, TipoFuncionamento.VinteQuatroHoras);
            var escalaServidor = b.AdicionarEscalaServidor(escala, servidor);

            foreach (var (dia, codigo) in ocorrencias)
            {
                b.Db.EscalaOcorrencias.Add(EscalaOcorrencia.Create(
                    escalaServidor.Id,
                    new DateOnly(Ano, Mes, dia),
                    codigo,
                    OrigemOcorrencia.Manual,
                    horaInicio: null,
                    horaFim: null,
                    horas: null,
                    createdBy: "teste"));
            }

            return new Contexto(escala.Id, servidor.Id);
        });

    private async Task<IXLWorksheet> GerarPlanilhaAsync(Guid escalaId)
    {
        await using var db = NewContext();
        var service = new EscalaCsvService(new EscalaService(db), db);
        var gerado = await service.GenerateResumidoAsync(escalaId, Login);
        gerado.Error.ShouldBeNull();

        var stream = new MemoryStream(gerado.Value.Content);
        return new XLWorkbook(stream).Worksheets.First();
    }

    [Fact]
    public async Task Teletrabalho_de_12h_conta_como_jornada_de_12h()
    {
        var ctx = await PrepararAsync((1, "TL12"), (3, "TL12"));

        var ws = await GerarPlanilhaAsync(ctx.EscalaId);

        // 2 unidades por jornada de 12h × 2 dias = 4 unidades = R$ 80,00.
        ws.Cell(3, 6).GetValue<int>().ShouldBe(4);
        ws.Cell(3, 7).GetValue<decimal>().ShouldBe(80m);
    }

    [Fact]
    public async Task Soma_plantoes_de_24h_12h_e_teletrabalho_de_12h()
    {
        var ctx = await PrepararAsync((1, "PT"), (5, "PD"), (9, "TL12"));

        var ws = await GerarPlanilhaAsync(ctx.EscalaId);

        // 3 (PT) + 2 (PD) + 2 (TL12) = 7 unidades = R$ 140,00.
        ws.Cell(3, 6).GetValue<int>().ShouldBe(7);
        ws.Cell(3, 7).GetValue<decimal>().ShouldBe(140m);
    }

    [Fact]
    public async Task Teletrabalho_de_6h_nao_gera_auxilio()
    {
        var ctx = await PrepararAsync((2, "TL6"));

        var ws = await GerarPlanilhaAsync(ctx.EscalaId);

        ws.Cell(3, 6).GetValue<int>().ShouldBe(0);
    }
}
