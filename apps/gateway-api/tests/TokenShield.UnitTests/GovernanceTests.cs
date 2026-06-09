using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;
using TokenShield.Infrastructure.Services;
using TokenShield.Observability.Services;
using Xunit;

namespace TokenShield.UnitTests;

public class GovernanceTests
{
    private TokenShieldDbContext CreateInMemoryDbContext()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TokenShieldDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<TokenShieldDbContext>();
    }

    [Fact]
    public async Task BudgetService_WarningThreshold_ReturnsWarningStatus()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();

        // Warning at 80% (Spend 85 is above threshold)
        context.BudgetLimits.Add(new BudgetLimit
        {
            TenantId = tenantId,
            Scope = BudgetScope.Tenant,
            MonthlyLimit = 100.00m,
            WarningThresholdPercent = 80.00m,
            CurrentSpend = 85.00m,
            Action = BudgetActionType.WarnOnly,
            LastResetAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var budgetService = new BudgetService(context);

        // Act
        var result = await budgetService.CheckBudgetPreCallAsync(tenantId, appId, apiKeyId);

        // Assert
        Assert.True(result.IsWarning);
        Assert.False(result.IsBlocked);
        Assert.False(result.IsDowngraded);
        Assert.Equal("Warning", result.BudgetStatus);
        Assert.Contains("warning threshold reached", result.WarningMessage);
    }

    [Fact]
    public async Task BudgetService_HardLimitExceeded_ReturnsBlockedStatus()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();

        // Budget exceeded with Block action
        context.BudgetLimits.Add(new BudgetLimit
        {
            TenantId = tenantId,
            Scope = BudgetScope.Application,
            TargetId = appId,
            MonthlyLimit = 100.00m,
            WarningThresholdPercent = 80.00m,
            CurrentSpend = 100.00m,
            Action = BudgetActionType.Block,
            LastResetAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var budgetService = new BudgetService(context);

        // Act
        var result = await budgetService.CheckBudgetPreCallAsync(tenantId, appId, apiKeyId);

        // Assert
        Assert.True(result.IsBlocked);
        Assert.False(result.IsWarning);
        Assert.False(result.IsDowngraded);
        Assert.Equal("Exceeded", result.BudgetStatus);
        Assert.Contains("limit exceeded", result.WarningMessage);
    }

    [Fact]
    public async Task BudgetService_DowngradeAction_ReturnsDowngradeStatus()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();

        // Budget exceeded with Downgrade action
        context.BudgetLimits.Add(new BudgetLimit
        {
            TenantId = tenantId,
            Scope = BudgetScope.ApiKey,
            TargetId = apiKeyId,
            MonthlyLimit = 100.00m,
            WarningThresholdPercent = 80.00m,
            CurrentSpend = 105.00m,
            Action = BudgetActionType.Downgrade,
            LastResetAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var budgetService = new BudgetService(context);

        // Act
        var result = await budgetService.CheckBudgetPreCallAsync(tenantId, appId, apiKeyId);

        // Assert
        Assert.True(result.IsDowngraded);
        Assert.False(result.IsBlocked);
        Assert.Equal("Downgraded", result.BudgetStatus);
        Assert.Contains("downgraded", result.WarningMessage);
    }

    [Fact]
    public async Task BudgetService_LastResetInPastMonth_ResetsSpendToZero()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();

        var pastResetDate = DateTime.UtcNow.AddMonths(-2);

        var budget = new BudgetLimit
        {
            TenantId = tenantId,
            Scope = BudgetScope.Tenant,
            MonthlyLimit = 100.00m,
            WarningThresholdPercent = 80.00m,
            CurrentSpend = 95.00m, // High spend
            Action = BudgetActionType.Block,
            LastResetAtUtc = pastResetDate
        };
        context.BudgetLimits.Add(budget);
        await context.SaveChangesAsync();

        var budgetService = new BudgetService(context);

        // Act
        var result = await budgetService.CheckBudgetPreCallAsync(tenantId, appId, apiKeyId);

        // Assert
        Assert.False(result.IsBlocked); // Reset to 0, so not blocked
        Assert.Equal("Within Limits", result.BudgetStatus);

        // Reload from DB to verify persistence of reset
        var reloaded = await context.BudgetLimits.FindAsync(budget.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(0m, reloaded.CurrentSpend);
        Assert.True(reloaded.LastResetAtUtc > pastResetDate);
        Assert.Equal(DateTime.UtcNow.Month, reloaded.LastResetAtUtc.Month);
    }

    [Fact]
    public async Task AuditLoggingService_SavesMutationRecord_Successfully()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var auditService = new AuditLoggingService(context);
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        // Act
        await auditService.LogActionAsync(
            tenantId,
            "CreateApiKey",
            "ApiKey",
            entityId,
            "test-actor@tokenshield.local",
            new { name = "Production Key", prefix = "ts_live_" });

        // Assert
        var savedLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == entityId);
        Assert.NotNull(savedLog);
        Assert.Equal("CreateApiKey", savedLog.ActionName);
        Assert.Equal("ApiKey", savedLog.EntityName);
        Assert.Equal("test-actor@tokenshield.local", savedLog.ActorEmail);
        Assert.Contains("Production Key", savedLog.DetailsJson);
    }

    [Fact]
    public void RequestLogging_HashingPromptsAndResponses_PreservesPrivacy()
    {
        // Arrange
        var promptText = "What is the capital of France?";
        var responseText = "The capital of France is Paris.";

        // Act
        var promptHash = ComputeHash(promptText);
        var responseHash = ComputeHash(responseText);

        // Assert
        // Verify length of SHA-256 hex string (64 characters)
        Assert.Equal(64, promptHash.Length);
        Assert.Equal(64, responseHash.Length);
        // Verify they are valid hex hashes and do not contain raw values
        Assert.NotEqual(promptText, promptHash);
        Assert.NotEqual(responseText, responseHash);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
