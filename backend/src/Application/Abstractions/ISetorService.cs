using TemplateSistema.Application.Common;
using TemplateSistema.Application.Setores;

namespace TemplateSistema.Application.Abstractions;

public interface ISetorService
{
    Task<IReadOnlyList<SetorListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SetorListItemDto>> ListMeusAsync(string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<SetorListItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<EstruturaOrganizacionalDto>> GetEstruturaAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChefiaConflitoDto>> PreviewChefiasConflitosAsync(
        PreviewChefiasConflitosRequest request,
        CancellationToken cancellationToken = default);
    Task<Result<SetorListItemDto>> CreateAsync(CreateSetorRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<SetorListItemDto>> UpdateAsync(Guid id, UpdateSetorRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
