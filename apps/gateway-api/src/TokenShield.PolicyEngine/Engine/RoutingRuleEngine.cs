using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Models;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.PolicyEngine.Engine;

public class RoutingRuleEngine : IRoutingRuleEngine
{
    private readonly TokenShieldDbContext _dbContext;

    public RoutingRuleEngine(TokenShieldDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(RoutingActionType Action, ModelTier? SelectedTier, string MatchedRuleName)> MatchRuleAsync(Guid tenantId, RequestProfile profile)
    {
        // Load active routing rules for the tenant, ordered by Priority ascending
        var rules = await _dbContext.RoutingRules
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        foreach (var rule in rules)
        {
            var conditions = ParseConditions(rule.ConditionsJson);
            if (conditions != null && conditions.Count > 0 && EvaluateConditions(conditions, profile))
            {
                // Rule matched!
                return (rule.Action, rule.TargetTier, rule.Name);
            }
        }

        // Default routing if no rule matches
        // If risk level is high, block or require human review by default
        if (profile.RiskLevel == "high")
        {
            return (RoutingActionType.HumanReview, null, "Default High Risk Policy");
        }

        return (RoutingActionType.RouteToTier, ModelTier.Standard, "Default Routing");
    }

    private static List<RuleCondition>? ParseConditions(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<RuleCondition>>(json, options);
        }
        catch
        {
            return null;
        }
    }

    private static bool EvaluateConditions(List<RuleCondition> conditions, RequestProfile profile)
    {
        foreach (var condition in conditions)
        {
            if (!EvaluateCondition(condition, profile))
            {
                return false;
            }
        }
        return true;
    }

    private static bool EvaluateCondition(RuleCondition condition, RequestProfile profile)
    {
        var field = condition.Field.ToLowerInvariant();
        var op = condition.Operator.ToLowerInvariant();
        var expectedVal = condition.Value;

        switch (field)
        {
            case "tasktype":
                return CompareString(profile.TaskType, op, expectedVal);
            case "risklevel":
                return CompareString(profile.RiskLevel, op, expectedVal);
            case "department":
                return CompareString(profile.Department ?? "", op, expectedVal);
            case "environment":
                return CompareString(profile.Environment ?? "", op, expectedVal);
            case "containspii":
                return bool.TryParse(expectedVal, out var expectedBool) && CompareBool(profile.ContainsPii, op, expectedBool);
            case "inputtokens":
                return int.TryParse(expectedVal, out var expectedIntTokens) && CompareNumeric(profile.InputTokens, op, expectedIntTokens);
            case "complexityscore":
                return int.TryParse(expectedVal, out var expectedIntComplexity) && CompareNumeric(profile.ComplexityScore, op, expectedIntComplexity);
            default:
                return false; // Unknown field
        }
    }

    private static bool CompareString(string actual, string op, string expected)
    {
        actual = actual.ToLowerInvariant();
        expected = expected.ToLowerInvariant();

        return op switch
        {
            "equals" => actual == expected,
            "notequals" => actual != expected,
            _ => false
        };
    }

    private static bool CompareBool(bool actual, string op, bool expected)
    {
        return op switch
        {
            "equals" => actual == expected,
            "notequals" => actual != expected,
            _ => false
        };
    }

    private static bool CompareNumeric(int actual, string op, int expected)
    {
        return op switch
        {
            "equals" => actual == expected,
            "notequals" => actual != expected,
            "greaterthan" => actual > expected,
            "lessthan" => actual < expected,
            "greaterthanorequals" => actual >= expected,
            "lessthanorequals" => actual <= expected,
            _ => false
        };
    }

    private class RuleCondition
    {
        public string Field { get; set; } = null!;
        public string Operator { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
