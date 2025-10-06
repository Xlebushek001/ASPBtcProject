using BinanceService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Globalization;

namespace BinanceService.Services
{
    public class BinanceServiceOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.binance.com/";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; set; } = 3;
        public int CacheDurationSeconds { get; set; } = 2;
    }

    public class BinanceServices : IBinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BinanceServices> _logger;
        private readonly IRedisService _redisService;
        private readonly BinanceServiceOptions _options;

        // Константы для кэширования
        private const string PriceCacheKeyPrefix = "binance:price:";
        private const string OrderBookCacheKeyPrefix = "binance:orderbook:";
        private const string MarketStatsCacheKeyPrefix = "binance:marketstats:";

        public BinanceServices(
            HttpClient httpClient,
            ILogger<BinanceServices> logger,
            IRedisService redisService,
            IOptions<BinanceServiceOptions> options)
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
            _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _options.ApiKey);
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
                _logger.LogInformation("Getting price for {Symbol} from Binance API", symbol);

                using var response = await _httpClient.GetAsync($"/api/v3/ticker/price?symbol={symbol}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content);

                if (data == null)
                {
                    _logger.LogWarning("Null response data for {Symbol}", symbol);
                    return 0;
                }

                if (!decimal.TryParse(data.price.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal price))
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
                _logger.LogError(ex, "Error getting price for {Symbol} from Binance", symbol);
                throw new BinanceApiException($"Failed to get price for {symbol}", ex);
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
                if (limit is < 5 or > 1000)
                    throw new ArgumentException("Limit must be between 5 and 1000", nameof(limit));

                using var response = await _httpClient.GetAsync($"/api/v3/depth?symbol={symbol}&limit={limit}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var orderBook = JsonConvert.DeserializeObject<OrderBookData>(content);

                if (orderBook == null || !orderBook.IsValid())
                {
                    _logger.LogWarning("Invalid order book data received for {Symbol}", symbol);
                    throw new BinanceApiException($"Invalid order book data for {symbol}");
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
                throw new BinanceApiException($"Failed to get order book for {symbol}", ex);
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
                throw new BinanceApiException($"Failed to get market statistics for {symbol}", ex);
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
                BidVolume = bids.Sum(b => b.Price * b.Quantity),
                AskVolume = asks.Sum(a => a.Price * a.Quantity),
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
            return new MarketStats();
        }

        public class BinanceApiException : Exception
        {
            public BinanceApiException(string message) : base(message) { }
            public BinanceApiException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
