namespace BinanceService.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? RequestId { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string? requestId = null) => new()
        {
            Success = true,
            Data = data,
            RequestId = requestId
        };

        public static ApiResponse<T> ErrorResponse(string error, string? requestId = null) => new()
        {
            Success = false,
            Error = error,
            RequestId = requestId
        };
    }
}