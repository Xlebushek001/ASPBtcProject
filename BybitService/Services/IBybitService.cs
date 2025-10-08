using BybitService.Models;

namespace BybitService.Services
{
    public interface IBybitService
    {
        Task<decimal> GetPriceAsync(string symbol = "BTCUSDT");
        Task<OrderBookData> GetOrderBookAsync(string symbol = "BTCUSDT", int limit = 30);
        Task<MarketStats> GetMarketStatsAsync(string symbol = "BTCUSDT");
        Task<MarketStats> GetMarketStatsWithRetryAsync(string symbol = "BTCUSDT", int maxRetries = 3);


        //Task InvalidateCacheAsync(string symbol);
    }
}