using TemplateSistema.Application.Common;
using TemplateSistema.Application.Usuarios;

namespace TemplateSistema.Application.Abstractions;

public interface IUsuarioService
{
    Task<PagedResult<UsuarioListItemDto>> ListAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<UsuarioDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UsuarioComSenhaDto>> CreateAsync(CreateUsuarioRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<UsuarioDetailDto>> UpdateAsync(Guid id, UpdateUsuarioRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<ResetSenhaResultDto>> ResetPasswordAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
}
