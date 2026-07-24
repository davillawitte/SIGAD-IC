using FluentValidation;

namespace TemplateSistema.Application.Auth;

public class AlterarSenhaRequestValidator : AbstractValidator<AlterarSenhaRequest>
{
    public AlterarSenhaRequestValidator()
    {
        RuleFor(x => x.SenhaAtual).NotEmpty();
        RuleFor(x => x.NovaSenha).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}
