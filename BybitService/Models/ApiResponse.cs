using Newtonsoft.Json;

namespace BybitService.Models
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

    public class BybitTickerResponse
    {
        [JsonProperty("retCode")]
        public int RetCode { get; set; }

        [JsonProperty("retMsg")]
        public string RetMsg { get; set; } = string.Empty;

        [JsonProperty("result")]
        public BybitTickerResult Result { get; set; } = new();
    }

    public class BybitTickerResult
    {
        [JsonProperty("list")]
        public List<BybitTickerItem> List { get; set; } = new();
    }

    public class BybitTickerItem
    {
        [JsonProperty("lastPrice")]
        public string LastPrice { get; set; } = string.Empty;

        [JsonProperty("symbol")]
        public string Symbol { get; set; } = string.Empty;
    }

    public class BybitOrderBookResponse
    {
        [JsonProperty("retCode")]
        public int RetCode { get; set; }

        [JsonProperty("retMsg")]
        public string RetMsg { get; set; } = string.Empty;

        [JsonProperty("result")]
        public BybitOrderBookResult Result { get; set; } = new();
    }

    public class BybitOrderBookResult
    {
        [JsonProperty("a")]
        public List<List<string>> Asks { get; set; } = new();

        [JsonProperty("b")]
        public List<List<string>> Bids { get; set; } = new();
    }
}