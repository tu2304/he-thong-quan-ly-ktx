using System.Net;
using System.Text.Json;
using KtxAI.Api.Common;

namespace KtxAI.Api.Middleware
{
    // Middleware bắt toàn bộ exception chưa xử lý, tránh ứng dụng crash
    // và luôn trả về JSON theo đúng định dạng ApiResponse (tiêu chí KT2 #8).
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (AppException ex)
            {
                _logger.LogWarning(ex, "Lỗi nghiệp vụ: {Message}", ex.Message);
                await WriteResponse(context, ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định");
                await WriteResponse(context, (int)HttpStatusCode.InternalServerError,
                    "Đã có lỗi xảy ra ở hệ thống. Vui lòng thử lại sau.");
            }
        }

        private static async Task WriteResponse(HttpContext context, int statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var payload = ApiResponse<object>.Fail(message);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
