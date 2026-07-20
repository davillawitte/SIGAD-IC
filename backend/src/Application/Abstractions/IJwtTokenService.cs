using TemplateSistema.Application.Auth;

namespace TemplateSistema.Application.Abstractions;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(UsuarioAuthDto usuario);
}
