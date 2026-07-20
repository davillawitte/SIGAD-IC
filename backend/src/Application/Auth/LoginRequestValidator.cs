using FluentValidation;

namespace TemplateSistema.Application.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Senha).NotEmpty().MaximumLength(200);
    }
}
