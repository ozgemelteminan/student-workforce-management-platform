namespace StudentWorkforceManagement.Application.Common.Pagination;

public sealed record PaginatedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);
