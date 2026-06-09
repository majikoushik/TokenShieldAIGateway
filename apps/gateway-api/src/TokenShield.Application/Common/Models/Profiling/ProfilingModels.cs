using System;
using System.Collections.Generic;
using TokenShield.Domain.Models;

namespace TokenShield.Application.Common.Models.Profiling;

public sealed class RequestClassificationInput
{
    public Guid? TenantId { get; init; }
    public Guid? ClientApplicationId { get; init; }
    public Guid? ApiKeyId { get; init; }

    public string NormalizedText { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();

    public int InputTokens { get; init; }
    public int EstimatedOutputTokens { get; set; }

    public string? ModelRequested { get; init; }
    public string? Environment { get; init; }
    public string? Department { get; init; }
    
    // Original messages if classifiers need them (e.g. for structured data, roles)
    public IReadOnlyList<TokenShield.Application.Dto.ChatMessage> Messages { get; init; } = [];
}

public sealed class MetadataProfileResult
{
    public string? TaskType { get; init; }
    public string? RiskLevel { get; init; }
    public string? DataSensitivity { get; init; }
    public string? Department { get; init; }
    public string? Environment { get; init; }
    public bool RequiresReasoning { get; init; }
    public bool RequiresStructuredOutput { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class TaskClassificationResult
{
    public string TaskType { get; init; } = "general";
    public double Confidence { get; init; }
    public string ClassificationMethod { get; init; } = "unknown";
    public IReadOnlyList<ProfileSignal> Signals { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class RiskClassificationResult
{
    public string RiskLevel { get; init; } = "medium";
    public double Confidence { get; init; }
    public IReadOnlyList<ProfileSignal> Signals { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class SensitivityDetectionResult
{
    public bool ContainsPii { get; set; }
    public bool ContainsFinancialData { get; set; }
    public bool ContainsHealthData { get; set; }
    public bool ContainsLegalData { get; set; }
    public string DataSensitivity { get; set; } = "unknown";
    public double Confidence { get; set; }
    public IReadOnlyList<ProfileSignal> Signals { get; set; } = [];
}

public sealed class ComplexityScoreResult
{
    public int Score { get; init; }
    public string Band { get; init; } = "medium";
    public IReadOnlyList<ProfileSignal> Signals { get; init; } = [];
}
