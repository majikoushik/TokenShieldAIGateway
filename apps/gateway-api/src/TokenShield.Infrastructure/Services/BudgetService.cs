using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly TokenShieldDbContext _dbContext;

    public BudgetService(TokenShieldDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BudgetCheckResult> CheckBudgetPreCallAsync(Guid tenantId, Guid applicationId, Guid apiKeyId)
    {
        var result = new BudgetCheckResult
        {
            IsBlocked = false,
            IsWarning = false,
            IsDowngraded = false,
            BudgetStatus = "Within Limits",
            WarningMessage = null
        };

        // Query active budgets for Tenant, Application, and API Key
        var budgets = await _dbContext.BudgetLimits
            .Where(b => b.TenantId == tenantId && !b.IsDeleted)
            .Where(b => (b.Scope == BudgetScope.Tenant && b.TargetId == null) ||
                        (b.Scope == BudgetScope.Application && b.TargetId == applicationId) ||
                        (b.Scope == BudgetScope.ApiKey && b.TargetId == apiKeyId))
            .ToListAsync();

        bool hasChanges = false;

        foreach (var budget in budgets)
        {
            if (CheckAndApplyMonthlyReset(budget))
            {
                _dbContext.BudgetLimits.Update(budget);
                hasChanges = true;
            }

            EvaluateBudgetLimit(budget, result);
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync();
        }

        return result;
    }

    public async Task<BudgetCheckResult> CheckModelBudgetAsync(Guid tenantId, Guid modelId)
    {
        var result = new BudgetCheckResult
        {
            IsBlocked = false,
            IsWarning = false,
            IsDowngraded = false,
            BudgetStatus = "Within Limits",
            WarningMessage = null
        };

        // Query active budget for Model
        var budget = await _dbContext.BudgetLimits
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Scope == BudgetScope.Model && b.TargetId == modelId && !b.IsDeleted);

        if (budget != null)
        {
            if (CheckAndApplyMonthlyReset(budget))
            {
                _dbContext.BudgetLimits.Update(budget);
                await _dbContext.SaveChangesAsync();
            }

            EvaluateBudgetLimit(budget, result);
        }

        return result;
    }

    public async Task UpdateBudgetSpendAsync(Guid tenantId, Guid applicationId, Guid apiKeyId, Guid? modelId, decimal cost)
    {
        // Query active budgets for Tenant, Application, API Key, and Model
        var budgets = await _dbContext.BudgetLimits
            .Where(b => b.TenantId == tenantId && !b.IsDeleted)
            .Where(b => (b.Scope == BudgetScope.Tenant && b.TargetId == null) ||
                        (b.Scope == BudgetScope.Application && b.TargetId == applicationId) ||
                        (b.Scope == BudgetScope.ApiKey && b.TargetId == apiKeyId) ||
                        (modelId != null && b.Scope == BudgetScope.Model && b.TargetId == modelId))
            .ToListAsync();

        foreach (var budget in budgets)
        {
            CheckAndApplyMonthlyReset(budget);
            budget.CurrentSpend += cost;
            _dbContext.BudgetLimits.Update(budget);
        }

        await _dbContext.SaveChangesAsync();
    }

    private static bool CheckAndApplyMonthlyReset(BudgetLimit budget)
    {
        var now = DateTime.UtcNow;
        if (budget.LastResetAtUtc.Month != now.Month || budget.LastResetAtUtc.Year != now.Year)
        {
            budget.CurrentSpend = 0m;
            budget.LastResetAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return true;
        }
        return false;
    }

    private static void EvaluateBudgetLimit(BudgetLimit budget, BudgetCheckResult result)
    {
        var warningSpend = budget.MonthlyLimit * (budget.WarningThresholdPercent / 100m);
        bool isBlocked = budget.Action == BudgetActionType.Block && budget.CurrentSpend >= budget.MonthlyLimit;
        bool isDowngrade = budget.Action == BudgetActionType.Downgrade && budget.CurrentSpend >= budget.MonthlyLimit;
        bool isWarning = budget.CurrentSpend >= warningSpend;

        if (isBlocked)
        {
            result.IsBlocked = true;
            result.BudgetStatus = "Exceeded";
            result.WarningMessage = $"Monthly budget limit exceeded for the {budget.Scope} scope.";
        }
        else if (isDowngrade && !result.IsBlocked)
        {
            result.IsDowngraded = true;
            // Downgraded status takes priority unless we are already blocked by another budget scope
            if (result.BudgetStatus != "Exceeded")
            {
                result.BudgetStatus = "Downgraded";
                result.WarningMessage = $"Monthly budget limit exceeded for the {budget.Scope} scope. Request tier will be downgraded.";
            }
        }
        else if (isWarning && !result.IsBlocked && !result.IsDowngraded)
        {
            result.IsWarning = true;
            if (result.BudgetStatus != "Exceeded" && result.BudgetStatus != "Downgraded")
            {
                result.BudgetStatus = "Warning";
                result.WarningMessage = $"Monthly budget warning threshold reached for the {budget.Scope} scope.";
            }
        }
    }
}
