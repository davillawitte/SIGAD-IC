using TemplateSistema.Application.Afastamentos;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Application.Abstractions;

public interface IAfastamentoService
{
    Task<IReadOnlyList<AfastamentoDto>> ListAsync(
        AfastamentoListQuery query,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<AfastamentoDto>> GetByIdAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);

    Task<Result<AfastamentoDto>> CreateAsync(
        CreateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<AfastamentoDto>> UpdateAsync(
        Guid id,
        UpdateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
}
