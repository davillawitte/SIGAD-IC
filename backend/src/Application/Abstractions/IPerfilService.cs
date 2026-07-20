using TemplateSistema.Application.Common;
using TemplateSistema.Application.Perfis;

namespace TemplateSistema.Application.Abstractions;

public interface IPerfilService
{
    Task<PagedResult<PerfilListItemDto>> ListAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<PerfilDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PerfilDetailDto>> CreateAsync(CreatePerfilRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<PerfilDetailDto>> UpdateAsync(Guid id, UpdatePerfilRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<PerfilDetailDto>> SetPermissoesAsync(Guid id, SetPerfilPermissoesRequest request, string actorLogin, CancellationToken cancellationToken = default);
}
