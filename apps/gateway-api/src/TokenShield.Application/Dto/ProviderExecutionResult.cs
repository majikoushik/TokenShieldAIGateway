using TokenShield.Domain.Enums;

namespace TokenShield.Application.Dto;

public class ProviderExecutionResult
{
    public string ResponseText { get; set; } = null!;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public string ModelName { get; set; } = null!;
    public string ProviderName { get; set; } = null!;
    public ModelTier Tier { get; set; }
    public bool FallbackUsed { get; set; }
    public Guid ModelId { get; set; }

    // Model budget evaluation result parameters
    public bool ModelBudgetIsWarning { get; set; }
    public string ModelBudgetStatus { get; set; } = "Within Limits";
    public string? ModelBudgetWarningMessage { get; set; }
}
