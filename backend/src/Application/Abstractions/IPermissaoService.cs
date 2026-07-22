using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permissoes;

namespace TemplateSistema.Application.Abstractions;

public interface IPermissaoService
{
    Task<PagedResult<PermissaoDto>> ListAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<PermissaoDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
