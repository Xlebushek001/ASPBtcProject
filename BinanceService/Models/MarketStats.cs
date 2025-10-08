namespace BinanceService.Models
{
    public class MarketStats
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal BestBidPrice { get; set; }
        public decimal BestAskPrice { get; set; }
        public decimal BidVolume { get; set; }
        public decimal AskVolume { get; set; }
        public decimal Spread => BestAskPrice > 0 && BestBidPrice > 0 ? BestAskPrice - BestBidPrice : 0;
        public decimal SpreadPercentage => CurrentPrice > 0 ? (Spread / CurrentPrice) * 100 : 0;
        public List<OrderBookEntry> TopBids { get; set; } = new();
        public List<OrderBookEntry> TopAsks { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}