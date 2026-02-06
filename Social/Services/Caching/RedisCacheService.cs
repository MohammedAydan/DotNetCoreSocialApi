using StackExchange.Redis;
using System.Text.Json;

namespace Social.API.Services.Caching
{
    /// <summary>
    /// Redis implementation of distributed caching.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(
            IConnectionMultiplexer redis,
            ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _db.StringGetAsync(key);

                if (!value.HasValue)
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var success = await _db.StringSetAsync(key, json, expiration);

                if (success)
                {
                    _logger.LogDebug("Cached value set for key: {Key} with expiration: {Expiration}", key, expiration);
                }
                else
                {
                    _logger.LogWarning("Failed to set cache for key: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = await _db.KeyDeleteAsync(key);
                _logger.LogDebug("Cache key {Key} deleted: {Deleted}", key, deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                var server = _redis.GetServer(endpoints.First());

                var keys = server.Keys(pattern: pattern).ToArray();
                if (keys.Length > 0)
                {
                    await _db.KeyDeleteAsync(keys);
                    _logger.LogDebug("Deleted {Count} keys matching pattern: {Pattern}", keys.Length, pattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists: {Key}", key);
                return false;
            }
        }
    }
}
