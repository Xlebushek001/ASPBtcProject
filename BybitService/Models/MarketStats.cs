namespace BybitService.Models
{
    public class MarketStats
    {
        public decimal CurrentPrice { get; set; }
        public decimal BestBidPrice { get; set; }
        public decimal BestAskPrice { get; set; }
        public decimal BidVolume { get; set; }
        public decimal AskVolume { get; set; }
        public decimal Spread { get; set; }
        public List<OrderBookEntry> TopBids { get; set; } = new List<OrderBookEntry>();
        public List<OrderBookEntry> TopAsks { get; set; } = new List<OrderBookEntry>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
