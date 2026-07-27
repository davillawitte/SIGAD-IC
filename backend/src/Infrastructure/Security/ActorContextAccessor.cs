using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Security;

public sealed class ActorContextAccessor(
    ApplicationDbContext db,
    IHttpContextAccessor httpContextAccessor) : IActorContextAccessor
{
    private ActorContext? _cached;
    private string? _cachedLogin;

    public Task<ActorContext> GetAsync(CancellationToken cancellationToken = default)
    {
        var login = ResolveLoginFromHttpContext();
        return GetByLoginAsync(login, cancellationToken);
    }

    public async Task<ActorContext> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        var normalized = login.Trim().ToLowerInvariant();
        if (_cached is not null
            && string.Equals(_cachedLogin, normalized, StringComparison.Ordinal))
        {
            return _cached;
        }

        _cached = await ActorContextLoader.LoadAsync(db, normalized, cancellationToken);
        _cachedLogin = normalized;
        return _cached;
    }

    private string ResolveLoginFromHttpContext()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return string.Empty;
        }

        return user.FindFirstValue("login")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.Identity?.Name
            ?? string.Empty;
    }
}
