using Newtonsoft.Json;

namespace BinanceService.Models
{
    public class OrderBookData
    {
        [JsonProperty("lastUpdateId")]
        public long LastUpdateId { get; set; }

        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; } = new List<List<string>>();

        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; } = new List<List<string>>();
    }

    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
