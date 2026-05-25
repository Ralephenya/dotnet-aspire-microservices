namespace GACS.Shared.Pagination;

public sealed record PaginationParameters
{
    private const int MaxPageSize = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;

    public int ValidPage => Math.Max(1, Page);
    public int ValidPageSize => Math.Clamp(PageSize, 1, MaxPageSize);
    public int Offset => (ValidPage - 1) * ValidPageSize;
}
