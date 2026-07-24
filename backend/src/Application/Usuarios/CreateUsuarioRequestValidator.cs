using FluentValidation;

namespace TemplateSistema.Application.Usuarios;

public class CreateUsuarioRequestValidator : AbstractValidator<CreateUsuarioRequest>
{
    public CreateUsuarioRequestValidator()
    {
        RuleFor(x => x.ServidorId).NotEmpty();
        RuleFor(x => x.PerfilIds).NotEmpty().WithMessage("Informe ao menos um perfil.");
    }
}
