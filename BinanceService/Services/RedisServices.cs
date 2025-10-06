using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace BinanceService.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisService> _logger;

        public RedisService(IDistributedCache cache, ILogger<RedisService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedData))
                    return default;

                return JsonConvert.DeserializeObject<T>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from Redis for key {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var serializedData = JsonConvert.SerializeObject(value);
                var options = new DistributedCacheEntryOptions();

                if (expiry.HasValue)
                {
                    options.SetAbsoluteExpiration(expiry.Value);
                }

                await _cache.SetStringAsync(key, serializedData, options);
                _logger.LogDebug("Data cached in Redis with key {Key}, expiry: {Expiry}", key, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting data in Redis for key {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Data removed from Redis with key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing data from Redis for key {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var data = await _cache.GetStringAsync(key);
                return data != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence in Redis for key {Key}", key);
                return false;
            }
        }
    }
}
