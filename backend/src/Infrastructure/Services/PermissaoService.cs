using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permissoes;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Infrastructure.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class PermissaoService(ApplicationDbContext db) : IPermissaoService
{
    public async Task<PagedResult<PermissaoDto>> ListAsync(
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var normalized = pagination.Normalize();
        var query = db.Permissoes.AsNoTracking().Where(x => x.Ativo);

        if (normalized.Search is not null)
        {
            var term = normalized.Search.ToLowerInvariant();
            query = query.Where(x =>
                x.Codigo.ToLower().Contains(term) ||
                x.Nome.ToLower().Contains(term) ||
                x.Modulo.ToLower().Contains(term) ||
                (x.Descricao != null && x.Descricao.ToLower().Contains(term)));
        }

        var paged = await query
            .OrderBy(x => x.Modulo)
            .ThenBy(x => x.Codigo)
            .ToPagedResultAsync(normalized, cancellationToken);

        var items = paged.Items.Select(Map).ToList();
        return PagedResult<PermissaoDto>.Create(items, paged.Page, paged.PageSize, paged.TotalItems);
    }

    public async Task<Result<PermissaoDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permissao = await db.Permissoes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return permissao is null
            ? Result<PermissaoDto>.Failure("Permissão não encontrada.")
            : Result<PermissaoDto>.Success(Map(permissao));
    }

    private static PermissaoDto Map(Permissao permissao) =>
        new(
            permissao.Id,
            permissao.Codigo,
            permissao.Nome,
            permissao.Descricao,
            permissao.Modulo,
            permissao.Sistema,
            permissao.Ativo);
}
