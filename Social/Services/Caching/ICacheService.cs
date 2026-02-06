namespace Social.API.Services.Caching
{
    /// <summary>
    /// Service for distributed caching using Redis.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets a cached value by key.
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a cached value with expiration.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a cached value by key.
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes multiple cached values by pattern.
        /// </summary>
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in cache.
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}
