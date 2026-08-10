namespace StudentWorkforceManagement.Application.Common.Pagination;

public abstract record PagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
