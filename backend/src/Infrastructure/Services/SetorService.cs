using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Setores;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class SetorService(ApplicationDbContext db) : ISetorService
{
    public async Task<IReadOnlyList<SetorListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Setores
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new SetorListItemDto(x.Id, x.Nome, x.Sigla, x.Ativo))
            .ToListAsync(cancellationToken);
    }
}
