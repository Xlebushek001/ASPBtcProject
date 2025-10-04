using Newtonsoft.Json;

namespace BybitService.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResponse(T data) => new ApiResponse<T>
        {
            Success = true,
            Data = data
        };

        public static ApiResponse<T> ErrorResponse(string error) => new ApiResponse<T>
        {
            Success = false,
            Error = error
        };
    }
    public class BybitTickerResponse
    {
        [JsonProperty("retCode")]
        public int RetCode { get; set; }

        [JsonProperty("retMsg")]
        public string RetMsg { get; set; }

        [JsonProperty("result")]
        public BybitTickerResult Result { get; set; }
    }
    public class BybitTickerResult
    {
        [JsonProperty("list")]
        public List<BybitTickerItem> List { get; set; }
    }

    public class BybitTickerItem
    {
        [JsonProperty("lastPrice")]
        public string LastPrice { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
    public class BybitOrderBookResponse
    {
        [JsonProperty("retCode")]
        public int RetCode { get; set; }

        [JsonProperty("retMsg")]
        public string RetMsg { get; set; }

        [JsonProperty("result")]
        public BybitOrderBookResult Result { get; set; }
    }

    public class BybitOrderBookResult
    {
        [JsonProperty("a")]
        public List<List<string>> Asks { get; set; }

        [JsonProperty("b")]
        public List<List<string>> Bids { get; set; }
    }
}
