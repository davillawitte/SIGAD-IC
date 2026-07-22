using TemplateSistema.Application.Cargos;

namespace TemplateSistema.Application.Abstractions;

public interface ICargoService
{
    Task<IReadOnlyList<CargoListItemDto>> ListAsync(CancellationToken cancellationToken = default);
}
