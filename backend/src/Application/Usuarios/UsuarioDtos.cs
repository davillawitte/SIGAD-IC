namespace TemplateSistema.Application.Usuarios;

public record UsuarioListItemDto(
    Guid Id,
    string Login,
    string NomeServidor,
    string Matricula,
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

public record CreateUsuarioRequest(
    Guid ServidorId,
    string Login,
    string Senha,
    IReadOnlyList<Guid> PerfilIds);

public record UpdateUsuarioRequest(
    IReadOnlyList<Guid>? PerfilIds,
    bool? Ativo);

public record ChangePasswordRequest(string NovaSenha);
