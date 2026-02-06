namespace Social.API.Services.Caching
{
    /// <summary>
    /// In-memory fallback cache service when Redis is unavailable.
    /// Thread-safe implementation with automatic expiration cleanup.
    /// </summary>
    public class InMemoryCacheService : ICacheService
    {
        private readonly Dictionary<string, (object Value, DateTime? Expiration)> _cache = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ILogger<InMemoryCacheService> _logger;

        public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
        {
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(key, out var cached))
                {
                    // Check if expired
                    if (!cached.Expiration.HasValue || cached.Expiration.Value > DateTime.UtcNow)
                    {
                        _logger.LogDebug("In-memory cache hit for key: {Key}", key);
                        return (T)cached.Value;
                    }
                    else
                    {
                        // Remove expired entry
                        _cache.Remove(key);
                        _logger.LogDebug("In-memory cache expired for key: {Key}", key);
                    }
                }

                _logger.LogDebug("In-memory cache miss for key: {Key}", key);
                return default;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var expirationTime = expiration.HasValue 
                    ? DateTime.UtcNow.Add(expiration.Value) 
                    : (DateTime?)null;
                    
                _cache[key] = (value!, expirationTime);
                _logger.LogDebug("In-memory cache set for key: {Key} with expiration: {Expiration}", key, expiration);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var removed = _cache.Remove(key);
                _logger.LogDebug("In-memory cache removed key: {Key}, Success: {Removed}", key, removed);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var searchPattern = pattern.Replace("*", "");
                var keysToRemove = _cache.Keys
                    .Where(k => k.Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                    
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
                
                _logger.LogDebug("In-memory cache removed {Count} keys matching pattern: {Pattern}", 
                    keysToRemove.Count, pattern);
            }
            finally
            {
                _lock.Release();
            }
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            var exists = _cache.ContainsKey(key);
            
            // Check if expired
            if (exists && _cache[key].Expiration.HasValue && _cache[key].Expiration.Value <= DateTime.UtcNow)
            {
                _cache.Remove(key);
                return Task.FromResult(false);
            }
            
            return Task.FromResult(exists);
        }
    }
}
