using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Cargos;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class CargoService(ApplicationDbContext db) : ICargoService
{
    public async Task<IReadOnlyList<CargoListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Cargos
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new CargoListItemDto(x.Id, x.Nome, x.Codigo, x.Ativo))
            .ToListAsync(cancellationToken);
    }
}
