namespace TemplateSistema.Application.Setup;

public record SetupStatusDto(bool NeedsSetup);

public record CompleteSetupRequest(
    string Token,
    string Cpf,
    string Nome,
    string Matricula,
    string? Email,
    DateOnly DataNascimento,
    string Senha);
