using TokenShield.Domain.Entities;
using TokenShield.Domain.Models;
using TokenShield.Domain.Enums;

namespace TokenShield.Application.Common.Interfaces;

public interface IRoutingRuleEngine
{
    Task<(RoutingActionType Action, ModelTier? SelectedTier, string MatchedRuleName)> MatchRuleAsync(Guid tenantId, RequestProfile profile);
}
