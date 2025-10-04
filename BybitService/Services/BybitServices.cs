using BybitService.Models;
using Newtonsoft.Json;
using System.Globalization;

namespace BybitService.Services
{
    public class BybitServices : IBybitService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BybitServices> _logger;
        private const string BaseUrl = "https://api.bybit.com";

        public BybitServices(HttpClient httpClient, ILogger<BybitServices> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<decimal> GetPriceAsync(string symbol = "BTCUSDT")
        {
            try
            {
                _logger.LogInformation($"Получение цены для {symbol} с Bybit");

                var response = await _httpClient.GetAsync($"/v5/market/tickers?category=spot&symbol={symbol}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<BybitTickerResponse>(content);

                if (data?.Result?.List?.Count > 0 &&
                    decimal.TryParse(data.Result.List[0].LastPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price))
                {
                    _logger.LogInformation($"Цена для {symbol} успешно получена: {price}");
                    return price;
                }

                _logger.LogWarning($"Не удалось распарсить цену для {symbol}");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении цены для {symbol} с Bybit");
                return 0;
            }
        }

        public async Task<OrderBookData> GetOrderBookAsync(string symbol = "BTCUSDT", int limit = 30)
        {
            try
            {
                _logger.LogInformation($"Получение стакана заявок для {symbol} с лимитом {limit}");

                var response = await _httpClient.GetAsync($"/v5/market/orderbook?category=spot&symbol={symbol}&limit={limit}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<BybitOrderBookResponse>(content);

                if (data?.Result != null)
                {
                    var orderBook = new OrderBookData
                    {
                        Bids = data.Result.Bids ?? new List<List<string>>(),
                        Asks = data.Result.Asks ?? new List<List<string>>()
                    };

                    _logger.LogInformation($"Стакан заявок для {symbol} успешно получен");
                    return orderBook;
                }

                return new OrderBookData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении стакана заявок для {symbol}");
                return new OrderBookData();
            }
        }

        public async Task<MarketStats> GetMarketStatsAsync(string symbol = "BTCUSDT")
        {
            try
            {
                _logger.LogInformation($"Получение рыночной статистики для {symbol}");

                var priceTask = GetPriceAsync(symbol);
                var orderBookTask = GetOrderBookAsync(symbol);

                await Task.WhenAll(priceTask, orderBookTask);

                var price = await priceTask;
                var orderBook = await orderBookTask;

                var bids = orderBook.Bids
                    .Select(b => new OrderBookEntry
                    {
                        Price = decimal.Parse(b[0], CultureInfo.InvariantCulture),
                        Quantity = decimal.Parse(b[1], CultureInfo.InvariantCulture)
                    })
                    .OrderByDescending(b => b.Price)
                    .Take(5)
                    .ToList();

                var asks = orderBook.Asks
                    .Select(a => new OrderBookEntry
                    {
                        Price = decimal.Parse(a[0], CultureInfo.InvariantCulture),
                        Quantity = decimal.Parse(a[1], CultureInfo.InvariantCulture)
                    })
                    .OrderBy(a => a.Price)
                    .Take(5)
                    .ToList();

                var stats = new MarketStats
                {
                    CurrentPrice = price,
                    BestBidPrice = bids.Count > 0 ? bids[0].Price : 0,
                    BestAskPrice = asks.Count > 0 ? asks[0].Price : 0,
                    BidVolume = bids.Sum(b => b.Quantity * b.Price),
                    AskVolume = asks.Sum(a => a.Quantity * a.Price),
                    Spread = asks.Count > 0 && bids.Count > 0 ? asks[0].Price - bids[0].Price : 0,
                    TopBids = bids,
                    TopAsks = asks
                };

                _logger.LogInformation($"Рыночная статистика для {symbol} успешно рассчитана");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении рыночной статистики для {symbol}");
                return new MarketStats();
            }
        }

        public async Task<MarketStats> GetMarketStatsWithRetryAsync(string symbol = "BTCUSDT", int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await GetMarketStatsAsync(symbol);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Попытка {attempt} не удалась для {symbol}");
                    if (attempt == maxRetries) throw;

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
            return new MarketStats();
        }
    }
}