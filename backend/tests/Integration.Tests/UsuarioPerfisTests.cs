using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Usuarios;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Atualização dos perfis de um usuário. Acrescentar um perfil que ele ainda não tinha estourava
/// depois do commit (a navegação do perfil novo vinha nula no mapeamento da resposta): o primeiro
/// clique gravava e devolvia erro 500, e o segundo "funcionava".
/// </summary>
public class UsuarioPerfisTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Admin = "admin.sistema";

    private sealed record Contexto(Guid UsuarioId, Guid ServidorId);

    /// <summary>Usuário com o perfil Chefe de Setor, pronto para receber outro perfil.</summary>
    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV");
            var servidor = b.AdicionarServidor(setor, "Servidor Promovido");
            var usuario = b.AdicionarUsuario(servidor, "servidor.promovido", CatalogSeed.PerfilChefeSetorId);

            var direcao = b.AdicionarDirecaoIc();
            var admin = b.AdicionarServidor(direcao, "Administrador do Sistema");
            b.AdicionarSuperAdmin(admin, Admin);

            return new Contexto(usuario.Id, servidor.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<UsuarioService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new UsuarioService(db, new PasswordHasherService()));
    }

    private async Task<List<string>> PerfisAsync(Guid usuarioId)
    {
        await using var db = NewContext();
        return await db.UsuarioPerfis
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => x.Perfil.Codigo)
            .OrderBy(x => x)
            .ToListAsync();
    }

    [Fact]
    public async Task Acrescentar_um_perfil_novo_funciona_na_primeira_tentativa()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.UpdateAsync(
            ctx.UsuarioId,
            new UpdateUsuarioRequest([CatalogSeed.PerfilChefeSetorId, CatalogSeed.PerfilSuperAdminId], null),
            Admin));

        result.Error.ShouldBeNull();
        result.Value!.Perfis.Count.ShouldBe(2);
        (await PerfisAsync(ctx.UsuarioId)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Trocar_o_conjunto_de_perfis_substitui_os_vinculos()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.UpdateAsync(
            ctx.UsuarioId,
            new UpdateUsuarioRequest([CatalogSeed.PerfilDirecaoIcId], null),
            Admin));

        result.Error.ShouldBeNull();
        result.Value!.PerfilIds.ShouldBe([CatalogSeed.PerfilDirecaoIcId]);
        (await PerfisAsync(ctx.UsuarioId)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Lista_de_perfis_vazia_e_recusada()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.UpdateAsync(
            ctx.UsuarioId, new UpdateUsuarioRequest([], null), Admin));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Informe ao menos um perfil para o usuário.");
        // O usuário continua com o perfil que tinha.
        (await PerfisAsync(ctx.UsuarioId)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Perfil_inexistente_e_recusado()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.UpdateAsync(
            ctx.UsuarioId,
            new UpdateUsuarioRequest([CatalogSeed.PerfilChefeSetorId, Guid.NewGuid()], null),
            Admin));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Um ou mais perfis são inválidos.");
    }
}
