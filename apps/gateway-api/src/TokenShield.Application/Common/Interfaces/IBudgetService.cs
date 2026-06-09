using TokenShield.Application.Dto;

namespace TokenShield.Application.Common.Interfaces;

public interface IBudgetService
{
    Task<BudgetCheckResult> CheckBudgetPreCallAsync(Guid tenantId, Guid applicationId, Guid apiKeyId);
    Task<BudgetCheckResult> CheckModelBudgetAsync(Guid tenantId, Guid modelId);
    Task UpdateBudgetSpendAsync(Guid tenantId, Guid applicationId, Guid apiKeyId, Guid? modelId, decimal cost);
}
