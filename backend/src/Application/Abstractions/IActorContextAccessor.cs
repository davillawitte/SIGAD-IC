using TemplateSistema.Application.Auth;

namespace TemplateSistema.Application.Abstractions;

public interface IActorContextAccessor
{
    Task<ActorContext> GetAsync(CancellationToken cancellationToken = default);
    Task<ActorContext> GetByLoginAsync(string login, CancellationToken cancellationToken = default);
}
