namespace TemplateSistema.Application.Common;

public record PaginationQuery
{
    public const int DefaultPageSize = 50;
    public static readonly int[] AllowedPageSizes = [30, 50, 100];

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Search { get; init; }

    public PaginationQuery Normalize()
    {
        var page = Page < 1 ? 1 : Page;
        var pageSize = AllowedPageSizes.Contains(PageSize) ? PageSize : DefaultPageSize;
        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

        return new PaginationQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
        };
    }

    public int Skip => (Normalize().Page - 1) * Normalize().PageSize;
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages =>
        TotalItems <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new([], page, pageSize, 0);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems) =>
        new(items, page, pageSize, totalItems);
}
