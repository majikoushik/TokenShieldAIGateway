using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TokenShield.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred during the request execution. Path: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            var unauthResponse = new
            {
                error = new
                {
                    message = "Unauthorized access. " + exception.Message,
                    type = "unauthorized",
                    code = "401"
                }
            };
            return context.Response.WriteAsync(JsonSerializer.Serialize(unauthResponse));
        }

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            error = new
            {
                message = "An internal server error occurred. Please contact system support.",
                type = "internal_server_error",
                code = "500"
            }
        };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}
