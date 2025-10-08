using BybitService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Globalization;

namespace BybitService.Services
{
    public class BybitServiceOptions
    {
        public string BaseUrl { get; set; } = "https://api.bybit.com/";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; set; } = 3;
        public int CacheDurationSeconds { get; set; } = 2;
    }

    public class BybitServices : IBybitService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BybitServices> _logger;
        private readonly IRedisService _redisService;
        private readonly BybitServiceOptions _options;

        // Константы для кэширования (единообразно с Binance)
        private const string PriceCacheKeyPrefix = "bybit:price:";
        private const string OrderBookCacheKeyPrefix = "bybit:orderbook:";
        private const string MarketStatsCacheKeyPrefix = "bybit:marketstats:";

        public BybitServices(
            HttpClient httpClient,
            ILogger<BybitServices> logger,
            IRedisService redisService,
            IOptions<BybitServiceOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _redisService = redisService;
            _options = options.Value;

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.Timeout = _options.Timeout;
        }

        public async Task<decimal> GetPriceAsync(string symbol = "BTCUSDT")
        {
            var cacheKey = $"{PriceCacheKeyPrefix}{symbol.ToUpper()}";

            // Пытаемся получить из кэша
            var cachedPrice = await _redisService.GetAsync<decimal?>(cacheKey);
            if (cachedPrice.HasValue)
            {
                _logger.LogDebug("Price for {Symbol} retrieved from Redis cache", symbol);
                return cachedPrice.Value;
            }

            try
            {
                _logger.LogInformation("Getting price for {Symbol} from Bybit API", symbol);

                using var response = await _httpClient.GetAsync($"/v5/market/tickers?category=spot&symbol={symbol}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<BybitTickerResponse>(content);

                // Проверка кода ответа Bybit
                if (data?.RetCode != 0)
                {
                    _logger.LogWarning("Bybit API returned error for {Symbol}: {Error}", symbol, data?.RetMsg);
                    return 0;
                }

                if (data?.Result?.List?.Count == 0)
                {
                    _logger.LogWarning("Empty response data for {Symbol}", symbol);
                    return 0;
                }

                var ticker = data.Result.List[0];
                if (!decimal.TryParse(ticker.LastPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price))
                {
                    _logger.LogWarning("Failed to parse price for {Symbol}", symbol);
                    return 0;
                }

                _logger.LogInformation("Successfully retrieved price for {Symbol}: {Price}", symbol, price);

                // Сохраняем в Redis с TTL
                var cacheExpiry = TimeSpan.FromSeconds(_options.CacheDurationSeconds);
                await _redisService.SetAsync(cacheKey, price, cacheExpiry);

                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting price for {Symbol} from Bybit", symbol);
                throw new BybitApiException($"Failed to get price for {symbol}", ex);
            }
        }

        public async Task<OrderBookData> GetOrderBookAsync(string symbol = "BTCUSDT", int limit = 30)
        {
            var cacheKey = $"{OrderBookCacheKeyPrefix}{symbol.ToUpper()}:{limit}";

            // Пытаемся получить из кэша
            var cachedOrderBook = await _redisService.GetAsync<OrderBookData>(cacheKey);
            if (cachedOrderBook != null)
            {
                _logger.LogDebug("Order book for {Symbol} retrieved from Redis cache", symbol);
                return cachedOrderBook;
            }

            try
            {
                _logger.LogInformation("Getting order book for {Symbol} with limit {Limit}", symbol, limit);

                // Валидация параметров
                if (limit is < 1 or > 200)
                    throw new ArgumentException("Limit must be between 1 and 200", nameof(limit));

                using var response = await _httpClient.GetAsync($"/v5/market/orderbook?category=spot&symbol={symbol}&limit={limit}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<BybitOrderBookResponse>(content);

                // Проверка кода ответа Bybit
                if (data?.RetCode != 0)
                {
                    _logger.LogWarning("Bybit API returned error for {Symbol}: {Error}", symbol, data?.RetMsg);
                    throw new BybitApiException($"Bybit API error: {data?.RetMsg}");
                }

                if (data?.Result == null)
                {
                    _logger.LogWarning("Invalid order book data received for {Symbol}", symbol);
                    throw new BybitApiException($"Invalid order book data for {symbol}");
                }
                 
                var orderBook = new OrderBookData
                {
                    Bids = data.Result.Bids ?? new List<List<string>>(),
                    Asks = data.Result.Asks ?? new List<List<string>>()
                };

                if (!orderBook.IsValid())
                {
                    _logger.LogWarning("Invalid order book data received for {Symbol}", symbol);
                    throw new BybitApiException($"Invalid order book data for {symbol}");
                }

                _logger.LogInformation("Successfully retrieved order book for {Symbol}", symbol);

                // Сохраняем в Redis с TTL
                var cacheExpiry = TimeSpan.FromSeconds(_options.CacheDurationSeconds);
                await _redisService.SetAsync(cacheKey, orderBook, cacheExpiry);

                return orderBook;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order book for {Symbol}", symbol);
                throw new BybitApiException($"Failed to get order book for {symbol}", ex);
            }
        }

        public async Task<MarketStats> GetMarketStatsAsync(string symbol = "BTCUSDT")
        {
            var cacheKey = $"{MarketStatsCacheKeyPrefix}{symbol.ToUpper()}";

            // Пытаемся получить из кэша
            var cachedStats = await _redisService.GetAsync<MarketStats>(cacheKey);
            if (cachedStats != null)
            {
                _logger.LogDebug("Market stats for {Symbol} retrieved from Redis cache", symbol);
                return cachedStats;
            }

            try
            {
                _logger.LogInformation("Getting market statistics for {Symbol}", symbol);

                var priceTask = GetPriceAsync(symbol);
                var orderBookTask = GetOrderBookAsync(symbol);

                await Task.WhenAll(priceTask, orderBookTask);

                var price = await priceTask;
                var orderBook = await orderBookTask;

                var stats = CalculateMarketStats(symbol, price, orderBook);
                _logger.LogInformation("Successfully calculated market statistics for {Symbol}", symbol);

                // Сохраняем в Redis с TTL
                var cacheExpiry = TimeSpan.FromSeconds(_options.CacheDurationSeconds);
                await _redisService.SetAsync(cacheKey, stats, cacheExpiry);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market statistics for {Symbol}", symbol);
                throw new BybitApiException($"Failed to get market statistics for {symbol}", ex);
            }
        }

        private static MarketStats CalculateMarketStats(string symbol, decimal price, OrderBookData orderBook)
        {
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

            var bestBid = bids.Count > 0 ? bids[0].Price : 0;
            var bestAsk = asks.Count > 0 ? asks[0].Price : 0;

            return new MarketStats
            {
                Symbol = symbol,
                CurrentPrice = price,
                BestBidPrice = bestBid,
                BestAskPrice = bestAsk,
                BidVolume = bids.Sum(b => b.Volume),
                AskVolume = asks.Sum(a => a.Volume),
                TopBids = bids,
                TopAsks = asks
            };
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
                    _logger.LogWarning(ex, "Attempt {Attempt} failed for {Symbol}", attempt, symbol);
                    if (attempt == maxRetries) throw;

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }

            throw new BybitApiException($"All {maxRetries} attempts failed for {symbol}");
        }

        // Метод для инвалидации кэша
        public async Task InvalidateCacheAsync(string symbol)
        {
            var symbolUpper = symbol.ToUpper();
            var priceKey = $"{PriceCacheKeyPrefix}{symbolUpper}";
            var orderBookKey = $"{OrderBookCacheKeyPrefix}{symbolUpper}:30";
            var marketStatsKey = $"{MarketStatsCacheKeyPrefix}{symbolUpper}";

            await Task.WhenAll(
                _redisService.RemoveAsync(priceKey),
                _redisService.RemoveAsync(orderBookKey),
                _redisService.RemoveAsync(marketStatsKey)
            );

            _logger.LogInformation("Cache invalidated for {Symbol}", symbol);
        }
    }

    public class BybitApiException : Exception
    {
        public BybitApiException(string message) : base(message) { }
        public BybitApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}