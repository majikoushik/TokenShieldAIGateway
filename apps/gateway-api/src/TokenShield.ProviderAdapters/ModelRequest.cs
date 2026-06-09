using System.Collections.Generic;

namespace TokenShield.ProviderAdapters;

public class ModelRequest
{
    public string ModelName { get; set; } = null!;
    public string DeploymentName { get; set; } = null!;
    public List<ModelMessage> Messages { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiUrl { get; set; }
}

public class ModelMessage
{
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
}

public class ModelResponse
{
    public string Id { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public string ModelName { get; set; } = null!;
    public string ProviderName { get; set; } = null!;
}
