using FluentValidation;
using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Application.Setup;

public class CompleteSetupRequestValidator : AbstractValidator<CompleteSetupRequest>
{
    public CompleteSetupRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Matricula)
            .NotEmpty()
            .Must(Servidor.IsMatriculaValida)
            .WithMessage("Matrícula inválida. Use o formato xxx.xxx-x ou xx.xxx-x.");

        RuleFor(x => x.Cpf)
            .Must(cpf => Servidor.NormalizeCpf(cpf ?? string.Empty).Length == 11)
            .WithMessage("CPF inválido.");

        RuleFor(x => x.DataNascimento)
            .NotEqual(default(DateOnly))
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Data de nascimento inválida.");

        RuleFor(x => x.Senha).Custom((senha, context) =>
        {
            if (!PasswordPolicy.IsValid(senha, out var erro))
            {
                context.AddFailure(erro!);
            }
        });
    }
}
