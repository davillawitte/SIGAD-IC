using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Servidores;

public record ServidorListItemDto(
    Guid Id,
    string Nome,
    string Matricula,
    string Cpf,
    Guid CargoId,
    string Cargo,
    string Email,
    string? Telefone,
    DateOnly DataNascimento,
    Guid SetorId,
    string SetorNome,
    bool PossuiUsuario,
    StatusServidor Status);

public record CreateServidorRequest(
    string Nome,
    string Matricula,
    string Cpf,
    Guid CargoId,
    string Email,
    Guid SetorId,
    DateOnly DataNascimento,
    string? Telefone,
    StatusServidor? Status);

public record UpdateServidorRequest(
    string Nome,
    string Matricula,
    string Cpf,
    Guid CargoId,
    string Email,
    Guid SetorId,
    DateOnly DataNascimento,
    string? Telefone,
    StatusServidor Status);
