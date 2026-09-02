using Shouldly;
using TemplateSistema.Domain.Common;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// NIST SP 800-63B: só comprimento mínimo e bloqueio de senhas comuns — sem exigir
/// maiúscula/símbolo/número.
/// </summary>
public class PasswordPolicyTests
{
    [Fact]
    public void Senha_curta_e_invalida()
    {
        var senha = new string('a', PasswordPolicy.MinLength - 1);
        var valido = PasswordPolicy.IsValid(senha, out var erro);

        valido.ShouldBeFalse();
        erro.ShouldNotBeNull();
    }

    [Fact]
    public void Senha_comum_e_invalida_mesmo_tendo_comprimento_minimo_ou_mais()
    {
        // "administrador" tem 13 caracteres — passaria no comprimento mínimo se não
        // estivesse na lista de senhas comuns.
        var valido = PasswordPolicy.IsValid("administrador", out var erro);

        valido.ShouldBeFalse();
        erro.ShouldBe("Essa senha está numa lista de senhas muito comuns. Escolha outra.");
    }

    [Fact]
    public void Senha_da_lista_de_comuns_e_invalida()
    {
        var valido = PasswordPolicy.IsValid("administrador1", out var erro);

        valido.ShouldBeFalse();
        erro.ShouldBe("Essa senha está numa lista de senhas muito comuns. Escolha outra.");
    }

    [Fact]
    public void Senha_longa_sem_maiuscula_ou_simbolo_e_valida()
    {
        var valido = PasswordPolicy.IsValid("uma frase longa e facil de lembrar", out var erro);

        valido.ShouldBeTrue(erro);
    }

    [Fact]
    public void Senha_vazia_e_invalida()
    {
        var valido = PasswordPolicy.IsValid(string.Empty, out var erro);

        valido.ShouldBeFalse();
        erro.ShouldNotBeNull();
    }

    [Fact]
    public void Senha_no_limite_minimo_e_valida()
    {
        var senha = new string('a', PasswordPolicy.MinLength);
        var valido = PasswordPolicy.IsValid(senha, out var erro);

        valido.ShouldBeTrue(erro);
    }
}
