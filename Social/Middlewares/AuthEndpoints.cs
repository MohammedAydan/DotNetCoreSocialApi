
namespace Social.API.Middlewares
{
    public class AuthEndpoints : IMiddleware
    {
        private readonly string _apiKey;

        public AuthEndpoints(IConfiguration configuration)
        {
            _apiKey = configuration["ApiSettings:ApiKey"] ?? throw new ArgumentNullException("ApiSettings:ApiKey", "API Key is missing from configuration");
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.Request.Headers.TryGetValue("x-api-key", out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing!");
                return;
            }

            if (!_apiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid API Key!");
                return;
            }

            await next(context);
        }
    }
}
