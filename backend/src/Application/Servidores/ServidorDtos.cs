namespace TemplateSistema.Application.Servidores;

public record ServidorListItemDto(
    Guid Id,
    string Nome,
    string Matricula,
    string Cpf,
    string Cargo,
    string Email,
    string? Telefone,
    Guid SetorId,
    string SetorNome,
    bool PossuiUsuario,
    bool Ativo);

public record CreateServidorRequest(
    string Nome,
    string Matricula,
    string Cpf,
    string Cargo,
    string Email,
    Guid SetorId,
    string? Telefone);
