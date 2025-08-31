using System.Net;
using System.Text.Json;

namespace Social.API.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception has occurred");

            context.Response.ContentType = "application/json";
            var response = context.Response;

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case ArgumentNullException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = "البيانات المرسلة غير صالحة";
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "غير مخول للوصول";
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = "العنصر غير موجود";
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = "عملية غير صالحة";
                    errorResponse.StatusCode = response.StatusCode;
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "حدث خطأ داخلي في الخادم";
                    errorResponse.StatusCode = response.StatusCode;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}