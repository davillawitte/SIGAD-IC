using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Application.Perfis;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;
using TemplateSistema.Persistence.Seed;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Testes da matriz: anti-escalonamento por perfil, Admin não delegável e seed não-destrutivo.
/// </summary>
public class MatrizAutorizacaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Avaliacao_por_perfil_nao_une_abrangencia_de_um_com_permissao_de_outro()
    {
        // Perfil A: escalas.listar + TodosOsSetores.
        // Perfil B: escalas.criar/editar + MeusSetores (só NB).
        // União errada daria criar em qualquer setor; a matriz exige o mesmo perfil.
        await SemearAsync(b =>
        {
            var nb = b.AdicionarSetor("NB", "NB");
            var np = b.AdicionarSetor("NP", "NP");

            var perfilLeitura = Perfil.Create("Leitura Global", "LEITURA_GLOBAL", createdBy: "teste");
            var perfilEscrita = Perfil.Create("Escrita Local", "ESCRITA_LOCAL", createdBy: "teste");
            b.Db.Perfis.AddRange(perfilLeitura, perfilEscrita);

            var servidor = b.AdicionarServidor(nb, "Gestor Híbrido");
            b.AdicionarChefia(nb, servidor, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(servidor, "hibrido", perfilLeitura.Id, perfilEscrita.Id);
            return 0;
        });

        await using (var db = NewContext())
        {
            var perfilLeitura = await db.Perfis.FirstAsync(x => x.Codigo == "LEITURA_GLOBAL");
            var perfilEscrita = await db.Perfis.FirstAsync(x => x.Codigo == "ESCRITA_LOCAL");

            var listar = await db.Permissoes.FirstAsync(x => x.Codigo == PermissionCodes.EscalasListar);
            var criar = await db.Permissoes.FirstAsync(x => x.Codigo == PermissionCodes.EscalasCriar);
            var editar = await db.Permissoes.FirstAsync(x => x.Codigo == PermissionCodes.EscalasEditar);
            var finalizar = await db.Permissoes.FirstAsync(x => x.Codigo == PermissionCodes.EscalasFinalizar);

            db.PerfilPermissoes.Add(PerfilPermissao.Create(perfilLeitura.Id, listar.Id, Abrangencia.TodosOsSetores));

            db.PerfilPermissoes.AddRange(
                PerfilPermissao.Create(perfilEscrita.Id, criar.Id),
                PerfilPermissao.Create(perfilEscrita.Id, editar.Id),
                PerfilPermissao.Create(perfilEscrita.Id, finalizar.Id),
                PerfilPermissao.Create(perfilEscrita.Id, listar.Id));

            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var actor = await ActorContextLoader.LoadAsync(db, "hibrido");
            var npId = await db.Setores.Where(x => x.Sigla == "NP").Select(x => x.Id).FirstAsync();
            var nbId = await db.Setores.Where(x => x.Sigla == "NB").Select(x => x.Id).FirstAsync();

            actor.PodeAcessar(PermissionCodes.EscalasListar, npId).ShouldBeTrue();
            actor.PodeAcessar(PermissionCodes.EscalasCriar, nbId).ShouldBeTrue();
            actor.PodeAcessar(PermissionCodes.EscalasCriar, npId).ShouldBeFalse();

            var service = new EscalaService(db);
            var criadoNp = await service.CreateAsync(
                new CreateEscalaRequest(npId, null, 2026, 8, TipoFuncionamento.Expediente, null),
                "hibrido");
            criadoNp.Succeeded.ShouldBeFalse();

            var criadoNb = await service.CreateAsync(
                new CreateEscalaRequest(nbId, null, 2026, 8, TipoFuncionamento.Expediente, null),
                "hibrido");
            criadoNb.Succeeded.ShouldBeTrue(criadoNb.Error);
        }
    }

    [Fact]
    public async Task PerfilService_rejeita_permissoes_de_administracao_do_sistema()
    {
        await SemearAsync(_ => 0);

        await using var db = NewContext();
        var service = new PerfilService(db);

        var criado = await service.CreateAsync(
            new CreatePerfilRequest("Perfil Ops", null, null, null),
            "teste");
        criado.Succeeded.ShouldBeTrue(criado.Error);

        var adminPerm = await db.Permissoes
            .Where(x => x.Codigo == PermissionCodes.UsuariosCriar)
            .Select(x => x.Id)
            .FirstAsync();

        var result = await service.SetPermissoesAsync(
            criado.Value!.Id,
            new SetPerfilPermissoesRequest([adminPerm]),
            "teste");

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("Administração do Sistema");
    }

    [Fact]
    public async Task AuthSeed_nao_apaga_permissoes_custom_do_chefe_apos_reinicio()
    {
        await SemearAsync(_ => 0);

        Guid extraPermissaoId;
        await using (var db = NewContext())
        {
            if (!await db.Perfis.AnyAsync(x => x.Id == AuthSeed.PerfilChefeSetorId))
            {
                db.Perfis.Add(Perfil.Create(
                    "Chefe de Setor Seed",
                    "CHEFE_SETOR_SEED_TEST",
                    sistema: true,
                    createdBy: "teste",
                    id: AuthSeed.PerfilChefeSetorId));
                await db.SaveChangesAsync();
            }

            var nucleosListar = await db.Permissoes
                .FirstAsync(x => x.Codigo == PermissionCodes.NucleosListar);
            extraPermissaoId = nucleosListar.Id;

            if (!await db.PerfilPermissoes.AnyAsync(x =>
                    x.PerfilId == AuthSeed.PerfilChefeSetorId && x.PermissaoId == extraPermissaoId))
            {
                db.PerfilPermissoes.Add(PerfilPermissao.Create(AuthSeed.PerfilChefeSetorId, extraPermissaoId));
                await db.SaveChangesAsync();
            }
        }

        await using (var db = NewContext())
        {
            await AuthSeed.SeedAsync(db, NullLogger.Instance);
        }

        await using (var db = NewContext())
        {
            var aindaTem = await db.PerfilPermissoes.AnyAsync(x =>
                x.PerfilId == AuthSeed.PerfilChefeSetorId && x.PermissaoId == extraPermissaoId);
            aindaTem.ShouldBeTrue();
        }
    }
}
