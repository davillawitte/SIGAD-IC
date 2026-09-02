using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Setup;
using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Infrastructure.Security;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;
using TemplateSistema.Persistence;
using TemplateSistema.Persistence.Seed;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Provisionamento do superadministrador via wizard /setup: token de uso único, cadeia completa
/// (Servidor + Usuario + perfil + chefia da Direção IC) e autodesligamento assim que existe
/// superadministrador ativo.
/// </summary>
public class SetupWorkflowTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static CompleteSetupRequest BuildRequest(string token, string cpf = "11111111111") =>
        new(token, cpf, "Admin Teste", "000.001-0", "admin@pci.rn.gov.br", new DateOnly(1990, 1, 1), "SenhaForteDe12Caracteres!");

    private async Task PrepararCatalogoAsync()
    {
        await using var db = NewContext();
        if (!await db.Cargos.AnyAsync(x => x.Codigo == CargoCodes.PeritoCriminal))
        {
            db.Cargos.Add(Cargo.Create("Perito Criminal", CargoCodes.PeritoCriminal, createdBy: "teste"));
        }

        if (!await db.Setores.AnyAsync(x => x.Id == AuthSeed.SetorDiretoriaId))
        {
            db.Setores.Add(Setor.Create(
                SetorSiglas.DirecaoIcNome,
                SetorSiglas.DirecaoIc,
                nucleoId: null,
                createdBy: "teste",
                id: AuthSeed.SetorDiretoriaId));
        }

        await db.SaveChangesAsync();
    }

    private async Task<string> SeedTokenAsync(TimeSpan ttl, bool usado = false)
    {
        await using var db = NewContext();
        var raw = $"raw-{Guid.NewGuid():N}";
        var entity = SetupToken.Create(SetupTokenGenerator.HashToken(raw), ttl, "teste");
        if (usado)
        {
            entity.MarcarUsado();
        }

        db.SetupTokens.Add(entity);
        await db.SaveChangesAsync();
        return raw;
    }

    private static SetupService BuildService(ApplicationDbContext db)
    {
        var hasher = new PasswordHasherService();
        var jwtOptions = Options.Create(new JwtOptions { Key = "teste-chave-secreta-com-mais-de-32-caracteres!" });
        var authService = new AuthService(db, hasher, new JwtTokenService(jwtOptions));
        return new SetupService(db, hasher, authService);
    }

    [Fact]
    public async Task Status_indica_needs_setup_true_quando_nao_ha_superadmin()
    {
        await using var db = NewContext();
        var status = await BuildService(db).GetStatusAsync();

        status.NeedsSetup.ShouldBeTrue();
    }

    [Fact]
    public async Task Complete_com_token_valido_provisiona_superadmin_e_retorna_login()
    {
        await PrepararCatalogoAsync();
        var token = await SeedTokenAsync(TimeSpan.FromMinutes(60));

        await using var db = NewContext();
        var resultado = await BuildService(db).CompleteAsync(BuildRequest(token));

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        resultado.Value!.Usuario.Login.ShouldBe("11111111111");
        resultado.Value!.Usuario.Perfis.ShouldContain(PerfilCodes.SuperAdministrador);
        resultado.Value!.Usuario.DeveAlterarSenha.ShouldBeTrue();

        await using var check = NewContext();
        var usuario = await check.Usuarios.FirstAsync(x => x.Login == "11111111111");
        usuario.DeveAlterarSenha.ShouldBeTrue();

        var chefia = await check.SetorChefias.FirstAsync(x => x.ServidorId == usuario.ServidorId);
        chefia.SetorId.ShouldBe(AuthSeed.SetorDiretoriaId);

        var tokenUsado = await check.SetupTokens.FirstAsync();
        tokenUsado.UsedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Status_indica_needs_setup_false_apos_provisionar()
    {
        await PrepararCatalogoAsync();
        var token = await SeedTokenAsync(TimeSpan.FromMinutes(60));

        await using (var db = NewContext())
        {
            (await BuildService(db).CompleteAsync(BuildRequest(token))).Succeeded.ShouldBeTrue();
        }

        await using var check = NewContext();
        var status = await BuildService(check).GetStatusAsync();
        status.NeedsSetup.ShouldBeFalse();
    }

    [Fact]
    public async Task Complete_com_token_expirado_falha()
    {
        await PrepararCatalogoAsync();
        var token = await SeedTokenAsync(TimeSpan.FromMinutes(-5));

        await using var db = NewContext();
        var resultado = await BuildService(db).CompleteAsync(BuildRequest(token));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Token inválido ou expirado.");
    }

    [Fact]
    public async Task Complete_com_token_ja_usado_falha()
    {
        await PrepararCatalogoAsync();
        var token = await SeedTokenAsync(TimeSpan.FromMinutes(60), usado: true);

        await using var db = NewContext();
        var resultado = await BuildService(db).CompleteAsync(BuildRequest(token));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Token inválido ou expirado.");
    }

    [Fact]
    public async Task Complete_com_token_incorreto_falha()
    {
        await PrepararCatalogoAsync();
        await SeedTokenAsync(TimeSpan.FromMinutes(60));

        await using var db = NewContext();
        var resultado = await BuildService(db).CompleteAsync(BuildRequest("token-errado"));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Token inválido ou expirado.");
    }

    [Fact]
    public async Task Complete_quando_ja_existe_superadmin_ativo_falha()
    {
        await PrepararCatalogoAsync();
        var primeiroToken = await SeedTokenAsync(TimeSpan.FromMinutes(60));

        await using (var db = NewContext())
        {
            (await BuildService(db).CompleteAsync(BuildRequest(primeiroToken))).Succeeded.ShouldBeTrue();
        }

        var segundoToken = await SeedTokenAsync(TimeSpan.FromMinutes(60));

        await using var check = NewContext();
        var resultado = await BuildService(check).CompleteAsync(BuildRequest(segundoToken, "22222222222"));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Setup já foi concluído.");
    }
}
