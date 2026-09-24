using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Setores;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// O mesmo servidor pode chefiar vários setores e, ao mesmo tempo, um núcleo. Antes, salvar um
/// setor apagava as chefias daquele servidor em qualquer outro setor.
/// </summary>
public class ChefiaEmVariasUnidadesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Login = "chefe.multiplo";

    private sealed record Contexto(Guid ServidorId, Guid SetorAId, Guid SetorBId, Guid NucleoId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE");
            var setorA = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV", nucleo);
            var setorB = b.AdicionarSetor("Setor de Engenharia Legal", "SELMA", nucleo);

            var servidor = b.AdicionarServidor(setorA, "Chefe de Tudo");
            b.AdicionarChefia(setorA, servidor, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(servidor, Login, CatalogSeed.PerfilChefeSetorId);

            return new Contexto(servidor.Id, setorA.Id, setorB.Id, nucleo.Id);
        });

    private static UpdateSetorRequest PedidoComChefia(string nome, string sigla, Guid nucleoId, Guid servidorId) =>
        new(nome, sigla, null, nucleoId, [new SetorChefiaInput(TipoChefia.ChefiaImediata, servidorId)]);

    [Fact]
    public async Task Assumir_chefia_de_outro_setor_mantem_a_chefia_anterior()
    {
        var ctx = await PrepararAsync();

        await using (var db = NewContext())
        {
            var result = await new SetorService(db).UpdateAsync(
                ctx.SetorBId,
                PedidoComChefia("Setor de Engenharia Legal", "SELMA", ctx.NucleoId, ctx.ServidorId),
                Login);
            result.Error.ShouldBeNull();
        }

        await using var assert = NewContext();
        var setores = await assert.SetorChefias
            .Where(x => x.ServidorId == ctx.ServidorId)
            .Select(x => x.SetorId)
            .ToListAsync();
        setores.OrderBy(x => x).ShouldBe(new[] { ctx.SetorAId, ctx.SetorBId }.OrderBy(x => x));
    }

    [Fact]
    public async Task Chefia_de_setor_e_de_nucleo_convivem_no_contexto_do_ator()
    {
        var ctx = await PrepararAsync();

        // Mesmo servidor vira também chefe do núcleo que engloba os dois setores.
        await using (var db = NewContext())
        {
            var nucleo = await db.Nucleos.FirstAsync(x => x.Id == ctx.NucleoId);
            nucleo.Atualizar(nucleo.Nome, nucleo.Sigla, ctx.ServidorId, "teste");
            await db.SaveChangesAsync();

            var result = await new SetorService(db).UpdateAsync(
                ctx.SetorBId,
                PedidoComChefia("Setor de Engenharia Legal", "SELMA", ctx.NucleoId, ctx.ServidorId),
                Login);
            result.Error.ShouldBeNull();
        }

        await using var assert = NewContext();
        var actor = await ActorContextLoader.LoadAsync(assert, Login);

        actor.SetoresGerenciadosIds.OrderBy(x => x)
            .ShouldBe(new[] { ctx.SetorAId, ctx.SetorBId }.OrderBy(x => x));
        actor.GerenciaNucleo(ctx.NucleoId).ShouldBeTrue();
        actor.GerenciaSetorViaNucleo(ctx.SetorAId).ShouldBeTrue();
    }

    [Fact]
    public async Task Mesmo_servidor_em_dois_papeis_do_mesmo_setor_continua_proibido()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var result = await new SetorService(db).UpdateAsync(
            ctx.SetorBId,
            new UpdateSetorRequest(
                "Setor de Engenharia Legal",
                "SELMA",
                null,
                ctx.NucleoId,
                [
                    new SetorChefiaInput(TipoChefia.ChefiaImediata, ctx.ServidorId),
                    new SetorChefiaInput(TipoChefia.ChefiaSubstituta, ctx.ServidorId),
                ]),
            Login);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("O mesmo servidor não pode ocupar mais de um papel de chefia no setor.");
    }
}
