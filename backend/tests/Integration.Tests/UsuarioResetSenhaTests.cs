using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Usuarios;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;
using TemplateSistema.Persistence;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Reset de senha volta a senha para o CPF, igual ao primeiro acesso, com troca obrigatória no
/// próximo login. Caso real em produção: o reset gerava uma senha aleatória mostrada só na tela de
/// quem resetou, o servidor entrava com o CPF e recebia "Usuário ou senha inválidos".
/// </summary>
public class UsuarioResetSenhaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Admin = "admin.reset";

    private sealed record Contexto(Guid ServidorId, string Cpf);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Setor de Teste", "STR");
            var admin = b.AdicionarServidor(setor, "Administrador");
            b.AdicionarSuperAdmin(admin, Admin);
            var servidor = b.AdicionarServidor(setor, "Servidor Que Esqueceu");
            return new Contexto(servidor.Id, servidor.Cpf);
        });

    private static AuthService Auth(ApplicationDbContext db) =>
        new(db, new PasswordHasherService(), new JwtTokenService(
            Options.Create(new JwtOptions { Key = "teste-chave-secreta-com-mais-de-32-caracteres!" })));

    [Fact]
    public async Task Depois_do_reset_o_servidor_entra_com_o_cpf_e_precisa_trocar_a_senha()
    {
        var ctx = await PrepararAsync();

        Guid usuarioId;
        await using (var db = NewContext())
        {
            var criado = await new UsuarioService(db, new PasswordHasherService()).CreateAsync(
                new CreateUsuarioRequest(ctx.ServidorId, [CatalogSeed.PerfilServidorId]), Admin);
            criado.Error.ShouldBeNull();
            usuarioId = criado.Value!.Id;
        }

        // Primeiro acesso com o CPF e troca para uma senha própria.
        await using (var db = NewContext())
        {
            (await Auth(db).LoginAsync(new LoginRequest(ctx.Cpf, ctx.Cpf))).Error.ShouldBeNull();
            (await Auth(db).AlterarSenhaAsync(ctx.Cpf, new AlterarSenhaRequest(ctx.Cpf, "MinhaSenhaNova#2026")))
                .Error.ShouldBeNull();
        }

        // Esqueceu a senha: o admin reseta.
        await using (var db = NewContext())
        {
            var reset = await new UsuarioService(db, new PasswordHasherService()).ResetPasswordAsync(usuarioId, Admin);
            reset.Error.ShouldBeNull();
            reset.Value!.SenhaTemporaria.ShouldBe(ctx.Cpf);
        }

        await using (var db = NewContext())
        {
            var auth = Auth(db);
            (await auth.LoginAsync(new LoginRequest(ctx.Cpf, "MinhaSenhaNova#2026"))).Succeeded.ShouldBeFalse();

            var login = await auth.LoginAsync(new LoginRequest(ctx.Cpf, ctx.Cpf));
            login.Error.ShouldBeNull();
            login.Value!.Usuario.DeveAlterarSenha.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Cpf_corrigido_depois_de_criar_o_usuario_tambem_entra_apos_o_reset()
    {
        var ctx = await PrepararAsync();
        const string cpfCorrigido = "09678916479";

        Guid usuarioId;
        await using (var db = NewContext())
        {
            usuarioId = (await new UsuarioService(db, new PasswordHasherService()).CreateAsync(
                new CreateUsuarioRequest(ctx.ServidorId, [CatalogSeed.PerfilServidorId]), Admin)).Value!.Id;
        }

        // CPF do servidor corrigido no cadastro depois: o login continua o CPF antigo.
        await using (var db = NewContext())
        {
            await db.Servidores
                .Where(x => x.Id == ctx.ServidorId)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.Cpf, cpfCorrigido));
            (await new UsuarioService(db, new PasswordHasherService()).ResetPasswordAsync(usuarioId, Admin))
                .Error.ShouldBeNull();
        }

        await using (var db = NewContext())
        {
            var login = await Auth(db).LoginAsync(new LoginRequest(cpfCorrigido, cpfCorrigido));
            login.Error.ShouldBeNull();
            login.Value!.Usuario.Login.ShouldBe(ctx.Cpf);
        }
    }
}
