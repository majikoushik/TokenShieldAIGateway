using System.Text.Json.Serialization;

namespace TokenShield.Application.Dto;

public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
}

public class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("choices")]
    public List<ChatCompletionChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public UsageInfo Usage { get; set; } = null!;

    [JsonPropertyName("routing")]
    public RoutingInfo Routing { get; set; } = null!;

    [JsonPropertyName("profile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProfileInfo? Profile { get; set; }
}

public class ChatCompletionChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; } = null!;

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = "stop";
}

public class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class RoutingInfo
{
    [JsonPropertyName("selectedTier")]
    public string SelectedTier { get; set; } = null!;

    [JsonPropertyName("selectedProvider")]
    public string SelectedProvider { get; set; } = null!;

    [JsonPropertyName("selectedModel")]
    public string SelectedModel { get; set; } = null!;

    [JsonPropertyName("matchedRule")]
    public string MatchedRule { get; set; } = null!;

    [JsonPropertyName("estimatedCost")]
    public decimal EstimatedCost { get; set; }

    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; }

    [JsonPropertyName("cacheHit")]
    public bool CacheHit { get; set; }

    [JsonPropertyName("budgetStatus")]
    public string BudgetStatus { get; set; } = "Within Limits";

    [JsonPropertyName("warning")]
    public string? Warning { get; set; }
}

public class ProfileInfo
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = null!;

    [JsonPropertyName("taskType")]
    public string TaskType { get; set; } = null!;

    [JsonPropertyName("taskTypeConfidence")]
    public double TaskTypeConfidence { get; set; }

    [JsonPropertyName("riskLevel")]
    public string RiskLevel { get; set; } = null!;

    [JsonPropertyName("riskConfidence")]
    public double RiskConfidence { get; set; }

    [JsonPropertyName("dataSensitivity")]
    public string DataSensitivity { get; set; } = null!;

    [JsonPropertyName("complexityScore")]
    public int ComplexityScore { get; set; }

    [JsonPropertyName("complexityBand")]
    public string ComplexityBand { get; set; } = null!;

    [JsonPropertyName("classificationMethod")]
    public string ClassificationMethod { get; set; } = null!;

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
}
