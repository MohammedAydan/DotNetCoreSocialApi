using Microsoft.Extensions.Logging;
using Social.Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Social.Infrastructure.Caching
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
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection failed while getting key: {Key}. Consider checking Redis connectivity.", key);
                return default;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Redis timeout while getting key: {Key}. Operation took too long.", key);
                return default;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize cached value for key: {Key}. Cache data may be corrupted.", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting cached value for key: {Key}", key);
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
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection failed while setting key: {Key}. Consider checking Redis connectivity.", key);
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Redis timeout while setting key: {Key}. Operation took too long.", key);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize value for key: {Key}. Check if the object is serializable.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error setting cached value for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = await _db.KeyDeleteAsync(key);
                _logger.LogDebug("Cache key {Key} deleted: {Deleted}", key, deleted);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection failed while removing key: {Key}. Consider checking Redis connectivity.", key);
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Redis timeout while removing key: {Key}. Operation took too long.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error removing cached value for key: {Key}", key);
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
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection failed while removing keys by pattern: {Pattern}. Consider checking Redis connectivity.", pattern);
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Redis timeout while removing keys by pattern: {Pattern}. Operation took too long.", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error removing cached values by pattern: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.KeyExistsAsync(key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection failed while checking if key exists: {Key}. Consider checking Redis connectivity.", key);
                return false;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Redis timeout while checking if key exists: {Key}. Operation took too long.", key);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking if key exists: {Key}", key);
                return false;
            }
        }
    }
}
