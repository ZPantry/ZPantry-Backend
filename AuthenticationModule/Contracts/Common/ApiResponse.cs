namespace AuthenticationModule.Contracts.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    public List<ApiErrorDetail>? Errors { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T? data, string message = "", string traceId = "")
        => new()
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };

    public static ApiResponse<T> Fail(string message, IEnumerable<ApiErrorDetail>? errors = null, string traceId = "")
        => new()
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors?.ToList(),
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
}

