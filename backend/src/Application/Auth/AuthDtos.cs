namespace TemplateSistema.Application.Auth;

public record LoginRequest(string Login, string Senha);

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UsuarioAuthDto Usuario);

public record UsuarioAuthDto(
    Guid Id,
    string Login,
    string Nome,
    string? Email,
    IReadOnlyList<string> Perfis,
    IReadOnlyList<string> Permissoes);
