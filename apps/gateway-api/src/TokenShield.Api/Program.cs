using Microsoft.EntityFrameworkCore;
using Serilog;
using TokenShield.Api.Middleware;
using TokenShield.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configure structured logging with Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext with Npgsql PostgreSQL configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TokenShieldDbContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("TokenShield.Infrastructure")));

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "TokenShield AI Gateway API", Version = "v1" });
});

// Configure CORS Policy (Placeholder for MVP, restrict in production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllLocal", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TokenShield API v1");
    });

    // Run database initializer / idempotent seed
    var seedEnabled = builder.Configuration.GetValue<bool>("SeedDatabase", true);
    if (seedEnabled)
    {
        await DbInitializer.InitializeAsync(app.Services);
    }
}

app.UseHttpsRedirection();

app.UseCors("AllowAllLocal");

// GET /health - Public Endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

// GET /api/version - Public Endpoint
app.MapGet("/api/version", () => Results.Ok(new
{
    productName = "TokenShield AI Gateway",
    version = "1.0.0-mvp",
    environment = app.Environment.EnvironmentName,
    serverTime = DateTime.UtcNow
}))
.WithName("GetVersion")
.WithOpenApi();

try
{
    Log.Information("Starting TokenShield AI Gateway API...");
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
