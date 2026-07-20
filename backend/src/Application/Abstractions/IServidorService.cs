using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;

namespace TemplateSistema.Application.Abstractions;

public interface IServidorService
{
    Task<IReadOnlyList<ServidorListItemDto>> ListAsync(bool? semUsuario = null, CancellationToken cancellationToken = default);
    Task<Result<ServidorListItemDto>> CreateAsync(CreateServidorRequest request, string actorLogin, CancellationToken cancellationToken = default);
}
