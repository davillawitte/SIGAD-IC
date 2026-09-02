using System.Security.Cryptography;

namespace TemplateSistema.Domain.Common;

/// <summary>
/// Senha temporária de novos usuários: aleatória, gerada com <see cref="RandomNumberGenerator"/>
/// — nada previsível a partir de nome/CPF. Sempre acima do mínimo de <see cref="PasswordPolicy"/>.
/// O usuário troca no primeiro login (<c>DeveAlterarSenha = true</c>).
/// </summary>
public static class SenhaTemporaria
{
    // Sem 0/O, 1/l/I — evita ambiguidade quando a senha é lida/digitada por alguém.
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
    private const int Tamanho = 16;

    public static string Gerar()
    {
        Span<char> chars = stackalloc char[Tamanho];
        for (var i = 0; i < Tamanho; i++)
        {
            chars[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        }

        return new string(chars);
    }

    public static string NormalizeLoginCpf(string loginOuCpf)
    {
        var trimmed = (loginOuCpf ?? string.Empty).Trim().ToLowerInvariant();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length is 11 ? digits : trimmed;
    }
}
