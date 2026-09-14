using Social.Core.Interfaces;
using Social.Infrastructure.Caching;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace Social.API.Configuration
{
    /// <summary>
    /// Extension methods for Redis configuration.
    /// </summary>
    public static class RedisExtensions
    {
        /// <summary>
        /// Adds Redis distributed caching to the application.
        /// Falls back to in-memory cache if Redis is unavailable.
        /// </summary>
        public static WebApplicationBuilder AddRedisCache(this WebApplicationBuilder builder)
        {
            var redisEndpoint = builder.Configuration["Redis:Endpoint"];
            var redisUsername = builder.Configuration["Redis:Username"];
            var redisPassword = builder.Configuration["Redis:Password"];

            // Skip Redis configuration if not provided
            if (string.IsNullOrWhiteSpace(redisEndpoint))
            {
                builder.Services.AddSingleton<Social.Core.Interfaces.ICacheService, InMemoryCacheService>();
                Console.WriteLine("[ℹ] Redis is not configured. Using in-memory cache fallback.");
                return builder;
            }

            try
            {
                var configurationOptions = new ConfigurationOptions
                {
                    EndPoints = { redisEndpoint },
                    AbortOnConnectFail = false,
                    ConnectTimeout = 5000,
                    SyncTimeout = 5000,
                    AsyncTimeout = 5000,
                    ConnectRetry = 3,
                    KeepAlive = 60,
                    DefaultDatabase = 0,
                };

                // Add authentication if provided
                if (!string.IsNullOrWhiteSpace(redisUsername))
                {
                    configurationOptions.User = redisUsername;
                }

                if (!string.IsNullOrWhiteSpace(redisPassword))
                {
                    configurationOptions.Password = redisPassword;
                }

                // Create connection multiplexer
                var redis = ConnectionMultiplexer.Connect(configurationOptions);

                // Test connection
                var db = redis.GetDatabase();
                db.Ping();

                // Register as singleton
                builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
                builder.Services.AddSingleton<Social.Core.Interfaces.ICacheService, RedisCacheService>();

                var maskedEndpoint = redisEndpoint.Split(':')[0] + ":***";
                Console.WriteLine($"[✓] Redis connected successfully to: {maskedEndpoint}");
            }
            catch (Exception ex)
            {
                var productionWarning = builder.Environment.IsProduction()
                    ? "\n[❌] WARNING: Production environment detected but Redis connection failed. In-memory cache has limited capacity and will not persist across restarts."
                    : "";

                Console.WriteLine($"[⚠] Failed to connect to Redis: {ex.Message}");
                Console.WriteLine($"[⚠] Redis endpoint: {redisEndpoint}");
                Console.WriteLine($"[ℹ] Using in-memory cache fallback. This is NOT recommended for production environments.{productionWarning}");

                builder.Services.AddSingleton<Social.Core.Interfaces.ICacheService, InMemoryCacheService>();

                // In production, consider throwing an exception instead of silently falling back
                // Uncomment the line below to enforce Redis in production
                // if (builder.Environment.IsProduction())
                // {
                //     throw new InvalidOperationException(
                //         "Redis connection is required in production environment. " +
                //         $"Failed to connect to {redisEndpoint}. Details: {ex.Message}");
                // }
            }

            return builder;
        }
    }
}
