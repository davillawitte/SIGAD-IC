namespace TemplateSistema.Domain.Common;

/// <summary>
/// Política de senha conforme NIST SP 800-63B: comprimento mínimo, sem regra de composição
/// (não exige maiúscula/símbolo — o guia recomenda explicitamente contra isso) e bloqueio de
/// uma lista curta de senhas comuns embutida (sem depender de internet).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 200;

    private static readonly HashSet<string> SenhasComuns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Clássicos de listas de senhas vazadas (inglês).
        "123456", "123456789", "12345678", "1234567890", "1234567", "12345",
        "password", "password1", "passw0rd", "qwerty", "qwerty123", "qwertyuiop",
        "111111", "123123", "123321", "654321", "666666", "121212", "1q2w3e4r",
        "zaq12wsx", "1qaz2wsx", "abc123", "abcd1234", "admin", "admin123",
        "letmein", "letmein1", "welcome", "welcome1", "monkey", "dragon",
        "master", "login", "princess", "solo", "starwars", "freedom",
        "whatever", "trustno1", "iloveyou", "sunshine", "football", "baseball",
        "shadow", "michael", "jennifer", "hunter", "hunter2", "flower",
        "hottie", "loveme", "jordan23", "harley", "ranger", "buster",
        "soccer", "hockey", "killer", "george", "andrew", "charlie",
        "superman", "batman", "chelsea", "diamond", "computer", "asdfgh",
        "asdfghjkl", "qazwsx", "root", "toor", "changeme", "changeme123",
        "temp1234", "temporary", "guest", "guest123", "user1234", "test1234",
        "12345678900", "0123456789", "987654321", "11111111", "00000000",
        "aaaaaaaa", "abcdefgh", "abcdefg1", "passwrd1", "passw0rd1",

        // Comuns em português (Brasil).
        "senha123", "senha1234", "123mudar", "mudar123", "brasil123",
        "brasil1234", "vitoria123", "vitoria1234", "familia123", "amorlindo",
        "meuamor123", "deus1234", "jesus1234", "gestaoic123", "criminal123",
        "pericia123", "instituto123", "rn123456", "natal12345", "governo123",
        "trocarsenha", "senhaforte", "minhasenha", "senhanova1", "administrador",
        "administrador1", "supervisor1", "gestor12345", "sistema1234",

        // Variações institucionais óbvias (evitar repetir a credencial que estamos removendo).
        "vitor@123", "vitorlopes", "sigadic123", "sigad@1234", "pciic1234",
    };

    /// <summary>
    /// Sem exigência de maiúscula/número/símbolo — só comprimento mínimo e não estar
    /// numa lista curta de senhas muito comuns.
    /// </summary>
    public static bool IsValid(string? senha, out string? erro)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            erro = "Senha é obrigatória.";
            return false;
        }

        if (senha.Length < MinLength)
        {
            erro = $"A senha deve ter ao menos {MinLength} caracteres.";
            return false;
        }

        if (senha.Length > MaxLength)
        {
            erro = $"A senha deve ter no máximo {MaxLength} caracteres.";
            return false;
        }

        if (SenhasComuns.Contains(senha))
        {
            erro = "Essa senha está numa lista de senhas muito comuns. Escolha outra.";
            return false;
        }

        erro = null;
        return true;
    }
}
