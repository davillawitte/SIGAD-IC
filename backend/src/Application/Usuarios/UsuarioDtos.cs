namespace TemplateSistema.Application.Usuarios;

public record UsuarioListItemDto(
    Guid Id,
    string Login,
    string NomeServidor,
    string Matricula,
    string Cpf,
    bool Ativo,
    DateTime? UltimoLogin,
    IReadOnlyList<string> Perfis);

public record UsuarioDetailDto(
    Guid Id,
    Guid ServidorId,
    string Login,
    string NomeServidor,
    string Matricula,
    string Email,
    bool Ativo,
    DateTime? UltimoLogin,
    IReadOnlyList<Guid> PerfilIds,
    IReadOnlyList<string> Perfis);

public record UsuarioComSenhaDto(
    Guid Id,
    Guid ServidorId,
    string Login,
    string NomeServidor,
    string Matricula,
    string Email,
    bool Ativo,
    DateTime? UltimoLogin,
    IReadOnlyList<Guid> PerfilIds,
    IReadOnlyList<string> Perfis,
    string SenhaTemporaria);

public record CreateUsuarioRequest(
    Guid ServidorId,
    IReadOnlyList<Guid> PerfilIds);

public record UpdateUsuarioRequest(
    IReadOnlyList<Guid>? PerfilIds,
    bool? Ativo);

public record ResetSenhaResultDto(
    Guid Id,
    string Login,
    string NomeServidor,
    string SenhaTemporaria);
