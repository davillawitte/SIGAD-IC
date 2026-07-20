using TemplateSistema.Application.Setores;

namespace TemplateSistema.Application.Abstractions;

public interface ISetorService
{
    Task<IReadOnlyList<SetorListItemDto>> ListAsync(CancellationToken cancellationToken = default);
}
