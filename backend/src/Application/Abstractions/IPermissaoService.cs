using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permissoes;

namespace TemplateSistema.Application.Abstractions;

public interface IPermissaoService
{
    Task<PagedResult<PermissaoDto>> ListAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<PermissaoDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PermissaoDto>> CreateAsync(CreatePermissaoRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<PermissaoDto>> UpdateAsync(Guid id, UpdatePermissaoRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
}
