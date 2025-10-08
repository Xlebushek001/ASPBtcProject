using Newtonsoft.Json;

namespace BybitService.Models
{
    public class OrderBookData
    {
        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; } = new();

        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; } = new();

        // Метод для валидации данных
        public bool IsValid()
        {
            return Bids?.Count > 0 && Asks?.Count > 0;
        }
    }

    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Volume => Price * Quantity;
    }
}