namespace ComparisonService.Models
{
    public class ComparisonResult
    {
        public decimal BinancePrice { get; set; }
        public decimal BybitPrice { get; set; }
        public decimal DifferenceAbsolute { get; set; }
        public decimal DifferencePercentage { get; set; }
        public string RecommendedExchange { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public MarketStats BinanceStats { get; set; } = new MarketStats();
        public MarketStats BybitStats { get; set; } = new MarketStats();
    }

    public class MarketStats
    {
        public decimal CurrentPrice { get; set; }
        public decimal BestBidPrice { get; set; }
        public decimal BestAskPrice { get; set; }
        public decimal Spread { get; set; }
        public List<OrderBookEntry> TopBids { get; set; } = new List<OrderBookEntry>();
        public List<OrderBookEntry> TopAsks { get; set; } = new List<OrderBookEntry>();
    }

    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }

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
}
