using TemplateSistema.Application.Common;
using TemplateSistema.Application.Nucleos;

namespace TemplateSistema.Application.Abstractions;

public interface INucleoService
{
    Task<IReadOnlyList<NucleoListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<Result<NucleoDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<NucleoDetailDto>> CreateAsync(CreateNucleoRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<NucleoDetailDto>> UpdateAsync(Guid id, UpdateNucleoRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
