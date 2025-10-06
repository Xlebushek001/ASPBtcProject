using Newtonsoft.Json;

namespace BinanceService.Models
{
    public class OrderBookData
    {
        [JsonProperty("lastUpdateId")]
        public long LastUpdateId { get; set; }

        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; } = new();

        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; } = new();

        // Метод для валидации данных
        public bool IsValid()
        {
            return LastUpdateId > 0 && Bids?.Count > 0 && Asks?.Count > 0;
        }
    }

    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Volume => Price * Quantity;
    }
}