using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Infrastructure.Common;

public static class QueryablePagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var normalized = pagination.Normalize();
        var totalItems = await query.CountAsync(cancellationToken);

        if (totalItems == 0)
        {
            return PagedResult<T>.Empty(normalized.Page, normalized.PageSize);
        }

        var items = await query
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, normalized.Page, normalized.PageSize, totalItems);
    }
}
