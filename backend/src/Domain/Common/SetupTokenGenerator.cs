using System.Security.Cryptography;
using System.Text;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Domain.Common;

/// <summary>
/// Gera o token de uso único do wizard de provisionamento e o hash usado para comparação —
/// o valor bruto nunca é persistido, só o hash SHA-256.
/// </summary>
public static class SetupTokenGenerator
{
    public const int TtlMinutes = 60;

    public static (SetupToken Entity, string RawToken) Gerar(string? createdBy = null)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var entity = SetupToken.Create(HashToken(raw), TimeSpan.FromMinutes(TtlMinutes), createdBy);
        return (entity, raw);
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty)));
}
