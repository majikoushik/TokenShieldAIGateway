using Microsoft.EntityFrameworkCore;
using Serilog;
using TokenShield.Api.Middleware;
using TokenShield.Api.Services;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Services;
using TokenShield.Infrastructure.Persistence;
using TokenShield.Infrastructure.Services;
using TokenShield.Guardrails.Profiling;
using TokenShield.CostEngine.Services;
using TokenShield.PolicyEngine.Engine;
using TokenShield.Observability.Services;
using TokenShield.Observability.Extensions;
using TokenShield.ProviderAdapters;
using TokenShield.ProviderAdapters.Services;
using TokenShield.Guardrails.Profiling.Options;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Guardrails.Profiling.Classifiers;
var builder = WebApplication.CreateBuilder(args);

// ─── Structured Logging with Serilog ───────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", builder.Configuration["OpenTelemetry:ServiceName"] ?? "tokenshield-gateway")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ─── OpenTelemetry + Application Insights Observability ────────────────────
builder.Services.AddTokenShieldObservability(builder.Configuration);
builder.Logging.AddTokenShieldLogging(builder.Configuration);

// ─── Database ──────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TokenShieldDbContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("TokenShield.Infrastructure")));

// ─── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TokenShieldDbContext>("database", tags: ["ready"]);

// ─── Application Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IRequestContext, RequestContext>();
builder.Services.AddSingleton<ApiKeyService>();
// ─── Profiling subsystem ───────────────────────────────────────────────────
builder.Services.Configure<RequestProfilerOptions>(
    builder.Configuration.GetSection(RequestProfilerOptions.SectionName));

builder.Services.AddSingleton<IMetadataProfileResolver, MetadataProfileResolver>();
builder.Services.AddSingleton<ITaskClassifier, ConfigurableRuleBasedTaskClassifier>();
builder.Services.AddSingleton<ISemanticTaskClassifier, DisabledSemanticTaskClassifier>();
builder.Services.AddSingleton<ILlmRequestClassifier, DisabledLlmRequestClassifier>();
builder.Services.AddSingleton<IRiskClassifier, RuleBasedRiskClassifier>();
builder.Services.AddSingleton<ISensitivityDetector, RegexSensitivityDetector>();
builder.Services.AddSingleton<IComplexityScorer, DefaultComplexityScorer>();
builder.Services.AddSingleton<IProfileResultMerger, ProfileResultMerger>();

builder.Services.AddSingleton<MvpRequestProfiler>();
builder.Services.AddSingleton<ProductionRequestProfiler>();
builder.Services.AddSingleton<HybridRequestProfiler>();
builder.Services.AddSingleton<IRequestProfilerFactory, RequestProfilerFactory>();

// Register the actual profiler interface by delegating to the factory
builder.Services.AddTransient<IRequestProfiler>(sp => sp.GetRequiredService<IRequestProfilerFactory>().Create());

builder.Services.AddSingleton<ICostEngineService, CostEngineService>();
builder.Services.AddScoped<IRoutingRuleEngine, RoutingRuleEngine>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IAuditLoggingService, AuditLoggingService>();
builder.Services.AddScoped<IProfilerRuleService, ProfilerRuleService>();
builder.Services.AddScoped<IDatabaseProfilerRuleProvider, DatabaseProfilerRuleProvider>();

// ─── HttpClient & Provider Adapters ───────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddTransient<MockProviderAdapter>();
builder.Services.AddTransient<OpenAiProviderAdapter>();
builder.Services.AddTransient<AzureOpenAiProviderAdapter>();
builder.Services.AddTransient<AnthropicProviderAdapter>();
builder.Services.AddScoped<IProviderAdapterFactory, ProviderAdapterFactory>();
builder.Services.AddScoped<IProviderExecutionService, ProviderExecutionService>();

// ─── Controllers & Swagger ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "TokenShield AI Gateway API",
        Version = "v1",
        Description = "Production-grade AI FinOps and model-routing gateway for enterprises."
    });
    options.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "x-api-key",
        Description = "Gateway API key (ts_live_xxx or ts_dev_xxx)"
    });
    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── CORS ──────────────────────────────────────────────────────────────────
// In Development: allow any origin for easy local development
// In Production: restrict to configured AllowedOrigins only
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddCors(options =>
{
    if (isDevelopment)
    {
        options.AddPolicy("GatewayPolicy", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    }
    else
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        options.AddPolicy("GatewayPolicy", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials());
    }
});

var app = builder.Build();

// ─── Middleware Pipeline ────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

// ─── Database Initialization ───────────────────────────────────────────────
// Migrations run in ALL environments (Development + Production).
// Seeding only runs when SeedDatabase=true (default true in Development, false in Production).
var seedEnabled = builder.Configuration.GetValue<bool>("SeedDatabase",
    defaultValue: app.Environment.IsDevelopment()); // true by default in Dev, false in Prod

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TokenShield API v1");
        c.DocumentTitle = "TokenShield AI Gateway";
    });
}

// Apply pending EF Core migrations and optionally seed data
await DbInitializer.InitializeAsync(app.Services, seedEnabled);

app.UseHttpsRedirection();
app.UseCors("GatewayPolicy");
app.MapControllers();

// ─── Public Endpoints ──────────────────────────────────────────────────────

// GET /health — lightweight liveness probe
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "tokenshield-gateway",
    timestamp = DateTime.UtcNow
}))
.WithName("HealthLiveness")
.WithTags("Health")
.WithOpenApi()
.AllowAnonymous();

// GET /health/ready — readiness probe (includes database check)
app.MapGet("/health/ready", async (Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService) =>
{
    var report = await healthCheckService.CheckHealthAsync(reg => reg.Tags.Contains("ready"));
    return report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
        ? Results.Ok(new { status = "Ready", checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }) })
        : Results.Json(new { status = "Degraded", checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description }) }, statusCode: 503);
})
.WithName("HealthReadiness")
.WithTags("Health")
.WithOpenApi()
.AllowAnonymous();

// GET /api/version — public version metadata
app.MapGet("/api/version", () => Results.Ok(new
{
    productName = "TokenShield AI Gateway",
    version = "1.0.0-mvp",
    environment = app.Environment.EnvironmentName,
    serverTime = DateTime.UtcNow
}))
.WithName("GetVersion")
.WithTags("System")
.WithOpenApi();

try
{
    Log.Information("Starting TokenShield AI Gateway (env={Environment})", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
