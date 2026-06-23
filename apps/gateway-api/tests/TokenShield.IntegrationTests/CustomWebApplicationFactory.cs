using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TokenShield.Infrastructure.Persistence;
using System.Linq;
using System.Collections.Generic;

namespace TokenShield.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SkipDbInitializer"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(TokenShieldDbContext) ||
                d.ServiceType.Name.Contains("DbContextOptions") ||
                d.ServiceType == typeof(System.Data.Common.DbConnection) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Options.IConfigureOptions<>))
            ).ToList();

            foreach (var d in descriptors)
            {
                // Only remove if it's related to EF Core options or TokenShieldDbContext
                if (d.ServiceType.ToString().Contains("DbContext") || d.ServiceType == typeof(System.Data.Common.DbConnection))
                {
                    services.Remove(d);
                }
            }

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<TokenShieldDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TokenShieldDbContext>();
            db.Database.EnsureCreated();

            if (!db.Tenants.Any())
            {
                db.Tenants.Add(new TokenShield.Domain.Entities.Tenant { Id = System.Guid.NewGuid(), Name = "Integration Test Demo Tenant" });
                db.SaveChanges();
            }
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
