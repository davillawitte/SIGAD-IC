using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Setup;

namespace TemplateSistema.Application.Abstractions;

public interface ISetupService
{
    Task<SetupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<Result<LoginResponse>> CompleteAsync(
        CompleteSetupRequest request,
        CancellationToken cancellationToken = default);
}
