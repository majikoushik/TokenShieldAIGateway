using Microsoft.AspNetCore.Http;
using TokenShield.Application.Common.Interfaces;

namespace TokenShield.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "x-correlation-id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationIdStr) &&
            Guid.TryParse(correlationIdStr, out var correlationId))
        {
            requestContext.CorrelationId = correlationId;
        }
        else
        {
            requestContext.CorrelationId = Guid.NewGuid();
        }

        context.Response.Headers[CorrelationIdHeader] = requestContext.CorrelationId.ToString();

        await _next(context);
    }
}
