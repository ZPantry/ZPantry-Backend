namespace AuthenticationModule.Contracts.Common;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }

    public List<ApiErrorDetail> Errors { get; set; } = [];

    public string TraceId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

