using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Application.Abstractions;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> RefreshSessionAsync(string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> AlterarSenhaAsync(string actorLogin, AlterarSenhaRequest request, CancellationToken cancellationToken = default);
}
