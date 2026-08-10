using Microsoft.EntityFrameworkCore;

namespace StudentWorkforceManagement.Application.Common.Pagination;

public static class PaginationExtensions
{
    public static async System.Threading.Tasks.Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResult<T>(items, page, pageSize, totalCount, totalPages, page < totalPages, page > 1);
    }
}
