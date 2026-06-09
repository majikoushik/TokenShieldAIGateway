using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeader = "x-api-key";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TokenShieldDbContext dbContext, IRequestContext requestContext)
    {
        var path = context.Request.Path;

        // Only authenticate /v1 routes
        if (!path.StartsWithSegments("/v1"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var rawApiKey) || string.IsNullOrWhiteSpace(rawApiKey))
        {
            await WriteUnauthorizedResponseAsync(context, "API Key is missing from headers.");
            return;
        }

        var keyHash = HashKey(rawApiKey!);

        var apiKeyRecord = await dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

        if (apiKeyRecord == null)
        {
            await WriteUnauthorizedResponseAsync(context, "Invalid API Key.");
            return;
        }

        if (apiKeyRecord.IsRevoked)
        {
            await WriteUnauthorizedResponseAsync(context, "API Key has been revoked.");
            return;
        }

        if (apiKeyRecord.ExpiresAtUtc.HasValue && apiKeyRecord.ExpiresAtUtc.Value < DateTime.UtcNow)
        {
            await WriteUnauthorizedResponseAsync(context, "API Key has expired.");
            return;
        }

        // Set request context values
        requestContext.TenantId = apiKeyRecord.TenantId;
        requestContext.ClientApplicationId = apiKeyRecord.ClientApplicationId;
        requestContext.ApiKeyId = apiKeyRecord.Id;

        // Update last used timestamp
        apiKeyRecord.LastUsedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        await _next(context);
    }

    private static async Task WriteUnauthorizedResponseAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = new
            {
                message = message,
                type = "authentication_error",
                code = "401"
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
