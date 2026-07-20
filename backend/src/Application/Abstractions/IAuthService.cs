using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Application.Abstractions;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
