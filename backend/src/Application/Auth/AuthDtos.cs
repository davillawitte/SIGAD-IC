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
    IReadOnlyList<string> Permissoes,
    Guid ServidorId,
    Guid? SetorLotacaoId,
    string? SetorLotacaoNome,
    IReadOnlyList<Guid> SetoresGerenciadosIds,
    bool DeveAlterarSenha);

public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
