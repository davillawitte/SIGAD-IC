using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Cli;

/// <summary>
/// Comandos operacionais invocados via <c>docker compose run --rm api &lt;comando&gt;</c> —
/// nunca pela API HTTP. Cada um abre seu próprio escopo de DI e sai com um código de saída.
/// </summary>
public static class SetupCliCommands
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SetupCli");

        return args[0] switch
        {
            "new-setup-token" => await NewSetupTokenAsync(scope.ServiceProvider, logger),
            "reset-admin-password" => await ResetAdminPasswordAsync(scope.ServiceProvider, logger),
            var unknown => Unknown(unknown, logger),
        };
    }

    private static int Unknown(string command, ILogger logger)
    {
        logger.LogError(
            "Comando desconhecido: {Command}. Use new-setup-token ou reset-admin-password.", command);
        return 1;
    }

    /// <summary>Invalida qualquer token pendente e gera um novo — só um token válido por vez.</summary>
    private static async Task<int> NewSetupTokenAsync(IServiceProvider services, ILogger logger)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        var pendentes = await db.SetupTokens.Where(x => x.UsedAtUtc == null).ToListAsync();
        foreach (var pendente in pendentes)
        {
            pendente.MarcarUsado();
        }

        var (entity, raw) = SetupTokenGenerator.Gerar("cli:new-setup-token");
        db.SetupTokens.Add(entity);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "SETUP: novo token gerado, válido por {Minutos} minutos. Acesse /setup e informe: {Token}",
            SetupTokenGenerator.TtlMinutes,
            raw);
        return 0;
    }

    private static async Task<int> ResetAdminPasswordAsync(IServiceProvider services, ILogger logger)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasherService>();

        Console.Write("Login do superadministrador alvo (CPF): ");
        var login = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();

        var usuario = await db.Usuarios
            .Include(x => x.UsuarioPerfis).ThenInclude(x => x.Perfil)
            .FirstOrDefaultAsync(x => x.Login == login);
        if (usuario is null || !usuario.UsuarioPerfis.Any(up => up.Perfil.Codigo == PerfilCodes.SuperAdministrador))
        {
            logger.LogError("Nenhum superadministrador com esse login foi encontrado.");
            return 1;
        }

        Console.Write($"Confirma reset de senha para \"{login}\"? Digite o login de novo para confirmar: ");
        var confirmacao = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
        if (confirmacao != login)
        {
            logger.LogError("Confirmação não confere. Operação cancelada.");
            return 1;
        }

        var novaSenha = LerSenhaMascarada($"Nova senha (mínimo {PasswordPolicy.MinLength} caracteres): ");
        if (!PasswordPolicy.IsValid(novaSenha, out var erro))
        {
            logger.LogError("Senha inválida: {Erro}", erro);
            return 1;
        }

        usuario.AlterarSenha(hasher.Hash(novaSenha), exigirTrocaNoProximoLogin: true, "cli:reset-admin-password");
        await db.SaveChangesAsync();

        // Log de auditoria via Serilog (não há tabela de auditoria imutável neste projeto).
        logger.LogWarning(
            "SECURITY: senha do superadministrador {Login} redefinida via reset-admin-password às {HoraUtc:u}.",
            login,
            DateTime.UtcNow);
        return 0;
    }

    private static string LerSenhaMascarada(string prompt)
    {
        Console.Write(prompt);
        var senha = new StringBuilder();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace && senha.Length > 0)
            {
                senha.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                senha.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return senha.ToString();
    }
}
