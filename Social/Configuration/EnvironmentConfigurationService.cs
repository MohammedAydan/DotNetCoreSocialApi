using System.Text.RegularExpressions;

namespace Social.API.Configuration
{
    /// <summary>
    /// Service for loading and validating environment variables with security masking.
    /// </summary>
    public class EnvironmentConfigurationService
    {
        private readonly ILogger<EnvironmentConfigurationService> _logger;

        public EnvironmentConfigurationService(ILogger<EnvironmentConfigurationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets an environment variable with proper validation and security masking.
        /// </summary>
        public string? GetEnvironmentVariable(string key, bool required = false)
        {
            var value = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrWhiteSpace(value) && required)
            {
                var errorMessage = $"Required environment variable '{key}' is not set. " +
                    $"Please add it to your .env file or system environment variables.";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                var masked = MaskSensitiveData(key, value);
                _logger.LogInformation("[✓] {Key} = {MaskedValue}", key, masked);
            }
            else
            {
                _logger.LogInformation("[ℹ] {Key} is not set (will use default if available)", key);
            }

            return value;
        }

        /// <summary>
        /// Loads and configures all environment variables into the IConfiguration.
        /// </summary>
        public Dictionary<string, string?> LoadAllConfigurations()
        {
            return new Dictionary<string, string?>
            {
                // Database (Required)
                ["ConnectionStrings:DefaultConnection"] = GetEnvironmentVariable("CONNECTION_STRING", required: true),

                // JWT (Required)
                ["Jwt:Key"] = GetEnvironmentVariable("JWT_KEY", required: true),
                ["Jwt:Issuer"] = GetEnvironmentVariable("JWT_ISSUER", required: true),
                ["Jwt:Audience"] = GetEnvironmentVariable("JWT_AUDIENCE", required: true),
                ["Jwt:ExpireTime"] = GetEnvironmentVariable("JWT_EXPIRE_TIME") ?? "30",

                // API Settings (Required)
                ["ApiSettings:ApiKey"] = GetEnvironmentVariable("API_KEY", required: true),

                // General Config (Required)
                ["GenralConfig:FrontendUrl"] = GetEnvironmentVariable("FRONTEND_URL", required: true),

                // Email Settings (Optional)
                ["EmailSettings:SmtpServer"] = GetEnvironmentVariable("EMAIL_SMTP_SERVER"),
                ["EmailSettings:SmtpPort"] = GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? "587",
                ["EmailSettings:SenderName"] = GetEnvironmentVariable("EMAIL_SENDER_NAME"),
                ["EmailSettings:SenderEmail"] = GetEnvironmentVariable("EMAIL_SENDER_EMAIL"),
                ["EmailSettings:Username"] = GetEnvironmentVariable("EMAIL_USERNAME"),
                ["EmailSettings:Password"] = GetEnvironmentVariable("EMAIL_PASSWORD"),
                ["EmailSettings:EnableSSL"] = GetEnvironmentVariable("EMAIL_ENABLE_SSL") ?? "true",
            };
        }

        /// <summary>
        /// Masks sensitive data in logs to prevent exposure of secrets.
        /// </summary>
        private static string MaskSensitiveData(string key, string value)
        {
            // Mask keys containing PASSWORD, KEY, or SECRET
            if (key.Contains("PASSWORD") || key.Contains("KEY") || key.Contains("SECRET"))
            {
                return "***";
            }

            // Mask password in connection string (Pwd=xxx;)
            if (key == "CONNECTION_STRING")
            {
                return Regex.Replace(
                    value,
                    @"(Pwd=)[^;]*",
                    "$1***",
                    RegexOptions.IgnoreCase
                );
            }

            return value;
        }
    }
}
