using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Observability.Services;

namespace TokenShield.Observability.Extensions;

/// <summary>
/// Extension methods to register TokenShield observability stack:
/// - Structured telemetry service (ITelemetryService)
/// - OpenTelemetry traces, metrics, logs
/// - Azure Monitor / Application Insights exporter (when ConnectionString is configured)
///
/// Usage in Program.cs:
///   builder.Services.AddTokenShieldObservability(builder.Configuration);
///   builder.Logging.AddTokenShieldLogging(builder.Configuration);
/// </summary>
public static class ObservabilityExtensions
{
    public static IServiceCollection AddTokenShieldObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the structured telemetry service
        services.AddSingleton<ITelemetryService, TelemetryService>();

        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "tokenshield-gateway";
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
        var hasAppInsights = !string.IsNullOrWhiteSpace(appInsightsConnectionString);

        if (hasAppInsights)
        {
            // Azure Monitor distro: single call wires traces, metrics, and logs
            // to Application Insights — no manual exporter registration needed.
            services.AddOpenTelemetry()
                .UseAzureMonitor(opts =>
                {
                    opts.ConnectionString = appInsightsConnectionString;
                })
                .ConfigureResource(r => r.AddService(serviceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation(opts =>
                        {
                            opts.Filter = ctx =>
                                !ctx.Request.Path.StartsWithSegments("/health") &&
                                !ctx.Request.Path.StartsWithSegments("/api/version");
                        })
                        .AddHttpClientInstrumentation();
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                });
        }
        else
        {
            // Local development: use console exporters (no Azure subscription required)
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName)
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector();

            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation(opts =>
                        {
                            opts.Filter = ctx =>
                                !ctx.Request.Path.StartsWithSegments("/health") &&
                                !ctx.Request.Path.StartsWithSegments("/api/version");
                        })
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter();
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter();
                });
        }

        return services;
    }

    /// <summary>
    /// Wire OpenTelemetry log bridge so Serilog/ILogger entries flow to OTel exporters.
    /// Call on builder.Logging before Build().
    /// </summary>
    public static ILoggingBuilder AddTokenShieldLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration)
    {
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
        var hasAppInsights = !string.IsNullOrWhiteSpace(appInsightsConnectionString);

        // Only add OTel log exporter in development (no AppInsights).
        // When AppInsights is configured, UseAzureMonitor() already handles logs.
        if (!hasAppInsights)
        {
            logging.AddOpenTelemetry(opts =>
            {
                opts.IncludeFormattedMessage = true;
                opts.IncludeScopes = true;
                opts.ParseStateValues = true;
                opts.AddConsoleExporter();
            });
        }

        return logging;
    }
}
