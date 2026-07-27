using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Auth;

public record LoginRequest(string Login, string Senha);

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UsuarioAuthDto Usuario);

public record PerfilAuthDto(
    string Codigo,
    IReadOnlyList<string> Permissoes,
    IReadOnlyDictionary<string, Abrangencia> AbrangenciaPorPermissao);

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
    bool DeveAlterarSenha,
    IReadOnlyList<PerfilAuthDto> PerfisDetalhe);

public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
