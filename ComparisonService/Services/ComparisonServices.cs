using ComparisonService.Models;
using Newtonsoft.Json;

namespace ComparisonService.Services
{
    public interface IComparisonService
    {
        Task<ComparisonResult> ComparePricesAsync(string symbol = "BTCUSDT");
        Task<ComparisonResult> CompareMarketStatsAsync(string symbol = "BTCUSDT");
    }

    public class ComparisonServices : IComparisonService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ComparisonServices> _logger;

        public ComparisonServices(IHttpClientFactory httpClientFactory, ILogger<ComparisonServices> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ComparisonResult> ComparePricesAsync(string symbol = "BTCUSDT")
        {
            try
            {
                _logger.LogInformation($"Сравнение цен для {symbol}");

                var binanceClient = _httpClientFactory.CreateClient("BinanceService");
                var bybitClient = _httpClientFactory.CreateClient("BybitService");

                // Параллельные запросы для увеличения скорости
                var binanceTask = binanceClient.GetAsync($"/api/binance/price/{symbol}");
                var bybitTask = bybitClient.GetAsync($"/api/bybit/price/{symbol}");

                await Task.WhenAll(binanceTask, bybitTask);

                var binanceResponse = await binanceTask.Result.Content.ReadAsStringAsync();
                var bybitResponse = await bybitTask.Result.Content.ReadAsStringAsync();

                var binanceData = JsonConvert.DeserializeObject<ApiResponse<decimal>>(binanceResponse);
                var bybitData = JsonConvert.DeserializeObject<ApiResponse<decimal>>(bybitResponse);

                if (!binanceData.Success || !bybitData.Success)
                {
                    throw new Exception("Не удалось получить цены с бирж");
                }

                var binancePrice = binanceData.Data;
                var bybitPrice = bybitData.Data;

                var differenceAbsolute = bybitPrice - binancePrice;
                var differencePercentage = (differenceAbsolute / (binancePrice != 0 ? binancePrice : 1)) * 100;

                return new ComparisonResult
                {
                    BinancePrice = binancePrice,
                    BybitPrice = bybitPrice,
                    DifferenceAbsolute = differenceAbsolute,
                    DifferencePercentage = differencePercentage,
                    RecommendedExchange = differencePercentage > 0 ? "Bybit" : "Binance",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при сравнении цен для {symbol}");
                throw;
            }
        }

        public async Task<ComparisonResult> CompareMarketStatsAsync(string symbol = "BTCUSDT")
        {
            try
            {
                _logger.LogInformation($"Сравнение рыночной статистики для {symbol}");

                var binanceClient = _httpClientFactory.CreateClient("BinanceService");
                var bybitClient = _httpClientFactory.CreateClient("BybitService");

                // Параллельные запросы для увеличения скорости
                var binanceTask = binanceClient.GetAsync($"/api/binance/marketstats/{symbol}");
                var bybitTask = bybitClient.GetAsync($"/api/bybit/marketstats/{symbol}");

                await Task.WhenAll(binanceTask, bybitTask);

                var binanceResponse = await binanceTask.Result.Content.ReadAsStringAsync();
                var bybitResponse = await bybitTask.Result.Content.ReadAsStringAsync();

                var binanceData = JsonConvert.DeserializeObject<ApiResponse<MarketStats>>(binanceResponse);
                var bybitData = JsonConvert.DeserializeObject<ApiResponse<MarketStats>>(bybitResponse);

                if (!binanceData.Success || !bybitData.Success)
                {
                    throw new Exception("Не удалось получить рыночную статистику с бирж");
                }

                var binanceStats = binanceData.Data;
                var bybitStats = bybitData.Data;

                var differenceAbsolute = bybitStats.CurrentPrice - binanceStats.CurrentPrice;
                var differencePercentage = (differenceAbsolute / (binanceStats.CurrentPrice != 0 ? binanceStats.CurrentPrice : 1)) * 100;

                return new ComparisonResult
                {
                    BinancePrice = binanceStats.CurrentPrice,
                    BybitPrice = bybitStats.CurrentPrice,
                    DifferenceAbsolute = differenceAbsolute,
                    DifferencePercentage = differencePercentage,
                    RecommendedExchange = differencePercentage > 0 ? "Bybit" : "Binance",
                    BinanceStats = binanceStats,
                    BybitStats = bybitStats,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при сравнении рыночной статистики для {symbol}");
                throw;
            }
        }
    }
}