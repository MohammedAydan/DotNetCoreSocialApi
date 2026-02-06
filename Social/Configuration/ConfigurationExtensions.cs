using DotNetEnv;
using Social.API.Configuration;

namespace Social.API.Extensions
{
    /// <summary>
    /// Extension methods for configuration setup.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds environment-based configuration to the application.
        /// Loads .env file and maps all environment variables to IConfiguration.
        /// </summary>
        public static WebApplicationBuilder AddEnvironmentConfiguration(this WebApplicationBuilder builder)
        {
            // Load .env file
            try
            {
                Env.Load();
                Console.WriteLine("[✓] .env file loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[⚠] Warning: Could not load .env file: {ex.Message}. Will use system environment variables.");
            }

            // Register the configuration service
            builder.Services.AddScoped<EnvironmentConfigurationService>();

            // Get the service from the service provider
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configService = serviceProvider.GetRequiredService<EnvironmentConfigurationService>();

            // Load all configurations
            var configurations = configService.LoadAllConfigurations();

            // Add configurations to IConfiguration
            builder.Configuration.AddInMemoryCollection(configurations);

            return builder;
        }
    }
}
