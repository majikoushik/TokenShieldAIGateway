using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;
using Xunit;

namespace TokenShield.UnitTests;

public class PersistenceTests
{
    private static DbContextOptions<TokenShieldDbContext> CreateNewInMemoryDatabaseOptions()
    {
        // Use a unique database name per test run to prevent test state cross-contamination
        return new DbContextOptionsBuilder<TokenShieldDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task DbContext_CanBeConstructedAndCanSaveEntities()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var context = new TokenShieldDbContext(options);

        var tenant = new Tenant { Name = "Acme Testing" };

        // Act
        context.Tenants.Add(tenant);
        var affected = await context.SaveChangesAsync();

        // Assert
        Assert.Equal(1, affected);
        var savedTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == "Acme Testing");
        Assert.NotNull(savedTenant);
        Assert.Equal(tenant.Id, savedTenant.Id);
    }

    [Fact]
    public async Task GlobalQueryFilters_SoftDeleteFilterIsAppliedByDefaults()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var context = new TokenShieldDbContext(options);

        var tenant = new Tenant { Name = "Test Tenant Soft Delete", IsDeleted = true };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Act & Assert (Should be filtered out)
        var fetchedTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);
        Assert.Null(fetchedTenant);

        // Act & Assert (Should be queryable when ignoring filters)
        var fetchedTenantIgnored = await context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenant.Id);
        Assert.NotNull(fetchedTenantIgnored);
        Assert.True(fetchedTenantIgnored.IsDeleted);
    }

    [Fact]
    public async Task Timestamps_PopulatedAutomaticallyOnSave()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var context = new TokenShieldDbContext(options);

        var tenant = new Tenant { Name = "Timestamp Check" };

        // Act
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Tenants.FirstAsync(t => t.Id == tenant.Id);
        Assert.True(saved.CreatedAtUtc > DateTime.MinValue);
        Assert.True(saved.UpdatedAtUtc > DateTime.MinValue);
        
        var originalCreated = saved.CreatedAtUtc;
        
        // Modify
        saved.Name = "Timestamp Check Modified";
        await context.SaveChangesAsync();

        Assert.Equal(originalCreated, saved.CreatedAtUtc);
        Assert.True(saved.UpdatedAtUtc >= originalCreated);
    }

    [Fact]
    public async Task DbInitializer_SeedingIsFullyIdempotent()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        
        var dbName = Guid.NewGuid().ToString();
        
        // Setup Dependency Injection Container mock for scope services
        var services = new ServiceCollection();
        services.AddDbContext<TokenShieldDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        // Act - Call Seeding First Time
        await DbInitializer.InitializeAsync(serviceProvider);

        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TokenShieldDbContext>();
            
            // Assert Initial Seed
            var tenants = await dbContext.Tenants.ToListAsync();
            Assert.Single(tenants);
            Assert.Equal("Acme Enterprise", tenants[0].Name);

            var models = await dbContext.AiModels.ToListAsync();
            Assert.True(models.Count > 0);

            var providers = await dbContext.ModelProviders.ToListAsync();
            Assert.Equal(4, providers.Count); // Mock, Azure, OpenAI, Anthropic
        }

        // Act - Call Seeding Second Time (Check Idempotency)
        await DbInitializer.InitializeAsync(serviceProvider);

        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TokenShieldDbContext>();
            
            // Assert Duplicate records are NOT created
            var tenants = await dbContext.Tenants.ToListAsync();
            Assert.Single(tenants);

            var providers = await dbContext.ModelProviders.ToListAsync();
            Assert.Equal(4, providers.Count);

            var budgets = await dbContext.BudgetLimits.ToListAsync();
            Assert.Equal(2, budgets.Count); // Tenant scope & Application scope
        }
    }
}
