using System;
using System.Collections.Generic;
using System.Linq;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public class MetadataProfileResolver : IMetadataProfileResolver
{
    private static readonly HashSet<string> ValidTaskTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "summarization", "classification", "extraction", "rag_answer",
        "architecture_review", "code_debugging", "incident_analysis",
        "complex_reasoning", "policy_check", "fraud_analysis",
        "customer_support", "general"
    };

    private static readonly HashSet<string> ValidRiskLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "low", "medium", "high", "critical"
    };

    private static readonly HashSet<string> ValidDataSensitivities = new(StringComparer.OrdinalIgnoreCase)
    {
        "public", "internal", "confidential", "restricted", "unknown"
    };

    public MetadataProfileResult Resolve(RequestClassificationInput input)
    {
        var warnings = new List<string>();

        var taskType = GetMetadataValue(input.Metadata, "taskType");
        if (taskType != null && !ValidTaskTypes.Contains(taskType))
        {
            warnings.Add($"Unsupported taskType metadata value: {taskType}");
            taskType = null;
        }

        var riskLevel = GetMetadataValue(input.Metadata, "riskLevel");
        if (riskLevel != null && !ValidRiskLevels.Contains(riskLevel))
        {
            warnings.Add($"Unsupported riskLevel metadata value: {riskLevel}");
            riskLevel = null;
        }

        var dataSensitivity = GetMetadataValue(input.Metadata, "dataSensitivity");
        if (dataSensitivity != null && !ValidDataSensitivities.Contains(dataSensitivity))
        {
            warnings.Add($"Unsupported dataSensitivity metadata value: {dataSensitivity}");
            dataSensitivity = null;
        }

        return new MetadataProfileResult
        {
            TaskType = taskType?.ToLowerInvariant(),
            RiskLevel = riskLevel?.ToLowerInvariant(),
            DataSensitivity = dataSensitivity?.ToLowerInvariant(),
            Department = GetMetadataValue(input.Metadata, "department"),
            Environment = GetMetadataValue(input.Metadata, "environment"),
            RequiresReasoning = GetMetadataBool(input.Metadata, "requiresReasoning", false),
            RequiresStructuredOutput = GetMetadataBool(input.Metadata, "requiresStructuredOutput", false),
            Warnings = warnings
        };
    }

    private static string? GetMetadataValue(IReadOnlyDictionary<string, string> meta, string key)
    {
        var actualKey = meta.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (actualKey != null && meta.TryGetValue(actualKey, out var val))
        {
            return val;
        }
        return null;
    }

    private static bool GetMetadataBool(IReadOnlyDictionary<string, string> meta, string key, bool defaultValue)
    {
        var val = GetMetadataValue(meta, key);
        if (val != null && bool.TryParse(val, out var result))
        {
            return result;
        }
        return defaultValue;
    }
}
