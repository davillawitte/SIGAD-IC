using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;

namespace TemplateSistema.Application.Abstractions;

public interface IServidorService
{
    Task<IReadOnlyList<ServidorListItemDto>> ListAsync(bool? semUsuario = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServidorListItemDto>> ListMeusAsync(
        string actorLogin,
        bool? semUsuario = null,
        CancellationToken cancellationToken = default);
    Task<Result<ServidorDetalheDto>> GetByIdAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<ServidorDetalheDto>> CreateAsync(CreateServidorRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<ServidorDetalheDto>> UpdateAsync(Guid id, UpdateServidorRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<ServidorExclusaoImpactoDto>> GetExclusaoImpactoAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
}
