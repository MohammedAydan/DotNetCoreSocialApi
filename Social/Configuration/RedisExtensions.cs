using Social.API.Services.Caching;
using StackExchange.Redis;

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
                builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
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
                builder.Services.AddSingleton<ICacheService, RedisCacheService>();

                var maskedEndpoint = redisEndpoint.Split(':')[0] + ":***";
                Console.WriteLine($"[✓] Redis connected successfully to: {maskedEndpoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[⚠] Failed to connect to Redis: {ex.Message}");
                Console.WriteLine("[ℹ] Using in-memory cache fallback.");
                builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
            }

            return builder;
        }
    }
}
