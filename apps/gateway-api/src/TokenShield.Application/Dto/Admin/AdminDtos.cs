using System;
using System.Collections.Generic;
using TokenShield.Domain.Enums;

namespace TokenShield.Application.Dto.Admin;

// --- Model Provider DTOs ---
public class CreateProviderRequest
{
    public string Name { get; set; } = null!;
    public string ApiUrl { get; set; } = null!;
    public string ApiKeySecretRef { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class UpdateProviderRequest
{
    public string Name { get; set; } = null!;
    public string ApiUrl { get; set; } = null!;
    public string ApiKeySecretRef { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class ProviderResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ApiUrl { get; set; } = null!;
    public string ApiKeySecretRef { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// --- AI Model DTOs ---
public class CreateModelRequest
{
    public Guid ProviderId { get; set; }
    public string Name { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;
    public ModelTier Tier { get; set; }
    public decimal InputTokenPricePerMillion { get; set; }
    public decimal OutputTokenPricePerMillion { get; set; }
    public int ContextWindow { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateModelRequest
{
    public string Name { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;
    public ModelTier Tier { get; set; }
    public decimal InputTokenPricePerMillion { get; set; }
    public decimal OutputTokenPricePerMillion { get; set; }
    public int ContextWindow { get; set; }
    public bool IsActive { get; set; }
}

public class ModelResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;
    public ModelTier Tier { get; set; }
    public decimal InputTokenPricePerMillion { get; set; }
    public decimal OutputTokenPricePerMillion { get; set; }
    public int ContextWindow { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// --- Routing Rule DTOs ---
public class CreateRoutingRuleRequest
{
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public string ConditionsJson { get; set; } = null!;
    public RoutingActionType Action { get; set; }
    public ModelTier? TargetTier { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateRoutingRuleRequest
{
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public string ConditionsJson { get; set; } = null!;
    public RoutingActionType Action { get; set; }
    public ModelTier? TargetTier { get; set; }
    public bool IsActive { get; set; }
}

public class RoutingRuleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public string ConditionsJson { get; set; } = null!;
    public RoutingActionType Action { get; set; }
    public ModelTier? TargetTier { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// --- Budget Limit DTOs ---
public class CreateBudgetRequest
{
    public BudgetScope Scope { get; set; }
    public Guid? TargetId { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal WarningThresholdPercent { get; set; }
    public BudgetActionType Action { get; set; }
}

public class UpdateBudgetRequest
{
    public decimal MonthlyLimit { get; set; }
    public decimal WarningThresholdPercent { get; set; }
    public BudgetActionType Action { get; set; }
}

public class BudgetResponse
{
    public Guid Id { get; set; }
    public BudgetScope Scope { get; set; }
    public Guid? TargetId { get; set; }
    public string? TargetName { get; set; } // Friendly name (e.g. application name, model name)
    public decimal MonthlyLimit { get; set; }
    public decimal WarningThresholdPercent { get; set; }
    public decimal CurrentSpend { get; set; }
    public BudgetActionType Action { get; set; }
    public DateTime LastResetAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// --- API Key DTOs ---
public class CreateApiKeyRequest
{
    public Guid ClientApplicationId { get; set; }
    public string Name { get; set; } = null!;
    public string? Prefix { get; set; } // e.g. "ts_live_"
    public DateTime? ExpiresAtUtc { get; set; }
}

public class ApiKeyResponse
{
    public Guid Id { get; set; }
    public Guid ClientApplicationId { get; set; }
    public string ClientApplicationName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Prefix { get; set; } = null!;
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ApiKeyCreatedResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Prefix { get; set; } = null!;
    public string RawKey { get; set; } = null!;
    public DateTime? ExpiresAtUtc { get; set; }
}

// --- Client Application DTOs ---
public class CreateApplicationRequest
{
    public string Name { get; set; } = null!;
}

public class ApplicationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}

// --- Usage Analytics DTOs ---
public class UsageLogResponse
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string RequestId { get; set; } = null!;
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = null!;
    public string PromptHash { get; set; } = null!;
    public string ResponseHash { get; set; } = null!;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public string SelectedProvider { get; set; } = null!;
    public string SelectedModel { get; set; } = null!;
    public string SelectedTier { get; set; } = null!;
    public string? MatchedRuleName { get; set; }
    public bool FallbackUsed { get; set; }
    public string BudgetStatus { get; set; } = null!;
    public string RequestStatus { get; set; } = null!;
    public int LatencyMs { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DashboardSummaryResponse
{
    public decimal TotalCost { get; set; }
    public int TotalRequests { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public double AverageLatencyMs { get; set; }
    
    public List<MetricStats> CostByProvider { get; set; } = new();
    public List<MetricStats> CostByModel { get; set; } = new();
    public List<MetricStats> CostByTier { get; set; } = new();
    public List<CountStat> RequestByStatus { get; set; } = new();
    public List<CountStat> RequestByBudgetState { get; set; } = new();
}

public class MetricStats
{
    public string GroupKey { get; set; } = null!;
    public decimal Cost { get; set; }
    public int RequestCount { get; set; }
}

public class CountStat
{
    public string GroupKey { get; set; } = null!;
    public int Count { get; set; }
}

// --- Audit Log DTOs ---
public class AuditLogResponse
{
    public Guid Id { get; set; }
    public string ActionName { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string ActorEmail { get; set; } = null!;
    public string DetailsJson { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
