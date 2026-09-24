using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Super Administrador enxerga os cadastros do Instituto inteiro (é quem cria os usuários), mas
/// ver tudo não dá direito de mutar nada: alterar escala continua exigindo chefia do setor.
/// </summary>
public class SuperAdminVisibilidadeTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string SuperAdmin = "super.puro";

    private sealed record Contexto(Guid SetorAlheioId, Guid EscalaAlheiaId, Guid ServidorAlheioId);

    /// <summary>Super Administrador "puro": só o perfil de administração, sem chefia nenhuma.</summary>
    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var admin = b.AdicionarServidor(direcao, "Administrador do Sistema");
            b.AdicionarUsuario(admin, SuperAdmin, CatalogSeed.PerfilSuperAdminId);

            var setor = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV");
            var servidor = b.AdicionarServidor(setor, "Servidor de Outro Setor");
            var chefe = b.AdicionarServidor(setor, "Chefe do SCCV");
            b.AdicionarChefia(setor, chefe, TipoChefia.ChefiaImediata);

            var escala = b.AdicionarEscala(setor, Ano, Mes);

            return new Contexto(setor.Id, escala.Id, servidor.Id);
        });

    [Fact]
    public async Task Enxerga_todos_os_servidores_mesmo_sem_chefiar_nada()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var servidores = await new ServidorService(db).ListMeusAsync(SuperAdmin);

        servidores.Select(x => x.Id).ShouldContain(ctx.ServidorAlheioId);
    }

    [Fact]
    public async Task Tem_visao_global_dos_cadastros_e_nao_das_escalas()
    {
        await PrepararAsync();

        await using var db = NewContext();
        var actor = await ActorContextLoader.LoadAsync(db, SuperAdmin);

        actor.IsSuperAdmin.ShouldBeTrue();
        actor.TemVisaoGlobal(PermissionModules.Servidores).ShouldBeTrue();
        actor.TemVisaoGlobal(PermissionModules.Setores).ShouldBeTrue();
        actor.TemVisaoGlobal(PermissionModules.Nucleos).ShouldBeTrue();
        // Escalas e afastamentos institucionais continuam fora do alcance dele.
        actor.TemVisaoGlobal(PermissionModules.Escalas).ShouldBeFalse();
        actor.TemVisaoGlobal(PermissionModules.Afastamentos).ShouldBeFalse();
    }

    [Fact]
    public async Task Continua_sem_poder_alterar_escala_de_setor_que_nao_chefia()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var result = await new EscalaService(db).UpdateAsync(
            ctx.EscalaAlheiaId,
            new UpdateEscalaRequest(Ano, Mes, TipoFuncionamento.Expediente, "tentativa"),
            SuperAdmin);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Sem permissão para esta escala.");
    }
}
