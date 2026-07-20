using FluentValidation;

namespace TemplateSistema.Application.Usuarios;

public class CreateUsuarioRequestValidator : AbstractValidator<CreateUsuarioRequest>
{
    public CreateUsuarioRequestValidator()
    {
        RuleFor(x => x.ServidorId).NotEmpty();
        RuleFor(x => x.Login).NotEmpty().MinimumLength(3).MaximumLength(100)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Login deve conter apenas letras, números, ponto, underscore ou hífen.");
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.PerfilIds).NotEmpty().WithMessage("Informe ao menos um perfil.");
    }
}
