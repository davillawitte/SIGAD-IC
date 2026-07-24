namespace TemplateSistema.Domain.Common;

/// <summary>
/// Senha temporária padrão: primeiroNome + ultimoNome + 3 primeiros dígitos do CPF.
/// </summary>
public static class SenhaTemporaria
{
    public static string Gerar(string nomeCompleto, string cpf)
    {
        var parts = (nomeCompleto ?? string.Empty)
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var primeiro = parts.Length > 0 ? parts[0] : "Usuario";
        var ultimo = parts.Length > 1 ? parts[^1] : primeiro;
        var cpfDigits = new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
        var prefixoCpf = cpfDigits.Length >= 3
            ? cpfDigits[..3]
            : cpfDigits.PadRight(3, '0');

        var senha = $"{primeiro}{ultimo}{prefixoCpf}";

        // Garante mínimo de 8 caracteres (requisito de senha do sistema).
        var i = 3;
        while (senha.Length < 8 && i < cpfDigits.Length)
        {
            senha += cpfDigits[i++];
        }

        while (senha.Length < 8)
        {
            senha += '0';
        }

        return senha;
    }

    public static string NormalizeLoginCpf(string loginOuCpf)
    {
        var trimmed = (loginOuCpf ?? string.Empty).Trim().ToLowerInvariant();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length is 11 ? digits : trimmed;
    }
}
