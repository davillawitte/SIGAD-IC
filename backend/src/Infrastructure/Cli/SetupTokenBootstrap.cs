using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Cli;

/// <summary>
/// No primeiro start sem superadministrador ativo, gera o token de <c>/setup</c> automaticamente
/// (se ainda não existir um válido) e loga o valor uma vez. Rodar de novo depois que o TTL
/// expirar exige o comando <c>new-setup-token</c>.
/// </summary>
public static class SetupTokenBootstrap
{
    public static async Task EnsureTokenAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existeSuperAdmin = await db.Usuarios
            .Where(u => u.Ativo && !u.Bloqueado)
            .AnyAsync(
                u => u.UsuarioPerfis.Any(up => up.Perfil.Ativo && up.Perfil.Codigo == PerfilCodes.SuperAdministrador),
                cancellationToken);
        if (existeSuperAdmin)
        {
            return;
        }

        var existeTokenValido = await db.SetupTokens
            .AnyAsync(x => x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);
        if (existeTokenValido)
        {
            return;
        }

        var (entity, raw) = SetupTokenGenerator.Gerar("startup");
        db.SetupTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "SETUP: nenhum superadministrador ativo. Token de provisionamento gerado, válido por " +
            "{Minutos} minutos. Acesse /setup e informe: {Token}",
            SetupTokenGenerator.TtlMinutes,
            raw);
    }
}
