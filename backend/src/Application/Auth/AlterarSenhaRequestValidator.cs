using FluentValidation;
using TemplateSistema.Domain.Common;

namespace TemplateSistema.Application.Auth;

public class AlterarSenhaRequestValidator : AbstractValidator<AlterarSenhaRequest>
{
    public AlterarSenhaRequestValidator()
    {
        RuleFor(x => x.SenhaAtual).NotEmpty();
        RuleFor(x => x.NovaSenha).Custom((senha, context) =>
        {
            if (!PasswordPolicy.IsValid(senha, out var erro))
            {
                context.AddFailure(erro!);
            }
        });
    }
}
