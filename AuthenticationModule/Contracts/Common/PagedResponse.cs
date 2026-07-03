namespace AuthenticationModule.Contracts.Common;

public class PagedResponse<T> : ApiResponse<List<T>>
{
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasNextPage { get; set; }

    public bool HasPreviousPage { get; set; }

    public static PagedResponse<T> SuccessPage(
        IEnumerable<T> items,
        int pageIndex,
        int pageSize,
        int totalItems,
        string message = "",
        string traceId = "")
    {
        var totalPages = pageSize <= 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResponse<T>
        {
            Success = true,
            Message = message,
            Data = items.ToList(),
            Errors = null,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNextPage = pageIndex < totalPages,
            HasPreviousPage = pageIndex > 1
        };
    }

    public static PagedResponse<T> FailPage(string message, string traceId = "")
        => new()
        {
            Success = false,
            Message = message,
            Data = [],
            Errors = [],
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
}

