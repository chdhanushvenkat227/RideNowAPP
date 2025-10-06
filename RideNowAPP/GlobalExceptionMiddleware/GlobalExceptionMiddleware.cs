using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RideNowAPP.GlobalExceptionMiddleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.ContentType = "application/json";
            
            var (statusCode, message) = GetErrorResponse(ex);
            context.Response.StatusCode = statusCode;

            var response = new
            {
                error = new
                {
                    message,
                    statusCode,
                    details = _env.IsDevelopment() ? ex.StackTrace : null
                }
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static (int statusCode, string message) GetErrorResponse(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => (401, "Unauthorized access"),
                //InvalidOperationException => (400, ex.Message),
                ArgumentException => (400, "Invalid request data"),
                //KeyNotFoundException => (404, "Resource not found"),
                _ => (500, "An internal server error occurred")
            };
        }
    }
}