using TemplateSistema.Application.Common;
using TemplateSistema.Application.Usuarios;

namespace TemplateSistema.Application.Abstractions;

public interface IUsuarioService
{
    Task<PagedResult<UsuarioListItemDto>> ListAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<UsuarioDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UsuarioDetailDto>> CreateAsync(CreateUsuarioRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<UsuarioDetailDto>> UpdateAsync(Guid id, UpdateUsuarioRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid id, ChangePasswordRequest request, string actorLogin, CancellationToken cancellationToken = default);
}
