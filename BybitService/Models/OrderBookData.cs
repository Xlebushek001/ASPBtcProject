namespace BybitService.Models
{
    public class OrderBookData
    {
        public List<List<string>> Bids { get; set; } = new List<List<string>>();
        public List<List<string>> Asks { get; set; } = new List<List<string>>();
    }

    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
