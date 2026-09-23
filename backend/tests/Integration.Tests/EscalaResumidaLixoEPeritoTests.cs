using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Duas regras da escala resumida: ela não sobrevive à escala que a criou (senão vira lixo que
/// outra escala do mesmo núcleo/mês readota depois) e o grupo "Agentes" distingue perito dos
/// demais cargos, já que só quem não é perito entra nele.
/// </summary>
public class EscalaResumidaLixoEPeritoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string ChefeNucleo = "chefe.npe";

    private sealed record Contexto(Guid NucleoId, Guid SetorId, Guid EscalaId, Guid PeritoId, Guid AgenteId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE", chefe.Id);
            var setor = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV", nucleo);
            var agente = b.AdicionarServidorNoNucleo(nucleo, "Agente do Núcleo");

            // O banco de teste vem da migration, que semeia o código legado "PERITO_CRIMINAL";
            // o "PC" atual vem do seed da aplicação. Os dois valem como perito.
            var cargoPerito = b.Db.Cargos.AsEnumerable().First(x => CargoCodes.EhPeritoCriminal(x.Codigo));
            var perito = Servidor.Create(
                "Perito do Núcleo",
                matricula: "999.999-9",
                cpf: "99999999999",
                cargoId: cargoPerito.Id,
                email: null,
                setorId: null,
                nucleoId: nucleo.Id,
                dataNascimento: new DateOnly(1990, 1, 1),
                status: StatusServidor.Ativo,
                createdBy: "teste");
            b.Db.Servidores.Add(perito);

            var escala = b.AdicionarEscala(setor, Ano, Mes);

            return new Contexto(nucleo.Id, setor.Id, escala.Id, perito.Id, agente.Id);
        });

    /// <summary>Escala resumida do núcleo, vinculada à escala do setor.</summary>
    private async Task<Guid> CriarResumidaVinculadaAsync(Guid nucleoId, Guid escalaId)
    {
        await using var db = NewContext();
        var service = new EscalaResumidaService(db);
        var criada = await service.CreateAsync(
            new Application.EscalasResumidas.CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), ChefeNucleo);
        criada.Error.ShouldBeNull();
        (await service.VincularEscalaAsync(criada.Value!.Id, escalaId, ChefeNucleo)).Error.ShouldBeNull();
        return criada.Value!.Id;
    }

    [Fact]
    public async Task Excluir_a_escala_remove_a_escala_resumida_vinculada()
    {
        var ctx = await PrepararAsync();
        var resumidaId = await CriarResumidaVinculadaAsync(ctx.NucleoId, ctx.EscalaId);

        await using (var db = NewContext())
        {
            (await new EscalaService(db).DeleteAsync(ctx.EscalaId, ChefeNucleo)).Error.ShouldBeNull();
        }

        await using var assert = NewContext();
        (await assert.EscalasResumidas.AnyAsync(x => x.Id == resumidaId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Resumida_compartilhada_com_outro_setor_do_nucleo_sobrevive()
    {
        var ctx = await PrepararAsync();
        var resumidaId = await CriarResumidaVinculadaAsync(ctx.NucleoId, ctx.EscalaId);
        var outroSetorId = await SemearAsync(b =>
        {
            var nucleo = b.Db.Nucleos.First(x => x.Id == ctx.NucleoId);
            var outro = b.AdicionarSetor("Setor de Engenharia Legal", "SELMA", nucleo);
            b.AdicionarEscala(outro, Ano, Mes);
            return outro.Id;
        });

        await using (var db = NewContext())
        {
            (await new EscalaService(db).DeleteAsync(ctx.EscalaId, ChefeNucleo)).Error.ShouldBeNull();
        }

        await using var assert = NewContext();
        outroSetorId.ShouldNotBe(Guid.Empty);
        (await assert.EscalasResumidas.AnyAsync(x => x.Id == resumidaId)).ShouldBeTrue();
    }

    [Fact]
    public async Task Elegiveis_marcam_quem_e_perito()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var elegiveis = await new EscalaResumidaService(db)
            .ListServidoresElegiveisAsync(ctx.NucleoId, null, ChefeNucleo);

        elegiveis.First(x => x.Id == ctx.PeritoId).EhPerito.ShouldBeTrue();
        CargoCodes.EhPeritoCriminal(elegiveis.First(x => x.Id == ctx.PeritoId).CargoCodigo).ShouldBeTrue();
        elegiveis.First(x => x.Id == ctx.AgenteId).EhPerito.ShouldBeFalse();
    }
}
