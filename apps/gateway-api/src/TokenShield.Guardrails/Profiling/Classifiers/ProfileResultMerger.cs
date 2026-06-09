using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;
using TokenShield.Domain.Models;
using TokenShield.Guardrails.Profiling.Options;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public class ProfileResultMerger : IProfileResultMerger
{
    private readonly RequestProfilerOptions _options;

    public ProfileResultMerger(IOptions<RequestProfilerOptions> options)
    {
        _options = options.Value;
    }

    public RequestProfile Merge(
        RequestClassificationInput input,
        MetadataProfileResult metadata,
        TaskClassificationResult task,
        RiskClassificationResult risk,
        SensitivityDetectionResult sensitivity,
        ComplexityScoreResult complexity)
    {
        var warnings = new List<string>(metadata.Warnings);
        var signals = new List<ProfileSignal>();

        signals.AddRange(task.Signals);
        signals.AddRange(risk.Signals);
        signals.AddRange(sensitivity.Signals);
        signals.AddRange(complexity.Signals);

        // Task Type Merge
        var finalTaskType = task.TaskType;
        var finalTaskConfidence = task.Confidence;
        var classificationMethod = task.ClassificationMethod;

        if (_options.UseMetadataAsPrimarySignal && !string.IsNullOrEmpty(metadata.TaskType))
        {
            finalTaskType = metadata.TaskType;
            finalTaskConfidence = 0.95;
            classificationMethod = "metadata";
        }
        else if (finalTaskConfidence < _options.LowConfidenceThreshold)
        {
            finalTaskType = _options.DefaultTaskType;
            warnings.Add("task_type_confidence_too_low_used_default");
            classificationMethod += "_low_confidence";
        }

        // Risk Merge
        var finalRiskLevel = risk.RiskLevel;
        var finalRiskConfidence = risk.Confidence;

        // If metadata risk is provided, but classifier says high, we don't downgrade
        if (!string.IsNullOrEmpty(metadata.RiskLevel))
        {
            if (metadata.RiskLevel == "high" || metadata.RiskLevel == "critical")
            {
                finalRiskLevel = metadata.RiskLevel;
                finalRiskConfidence = 0.90;
            }
            else if (risk.RiskLevel == "high" || risk.RiskLevel == "critical")
            {
                warnings.Add("metadata_low_risk_overridden_by_high_risk_classifier");
            }
            else
            {
                finalRiskLevel = metadata.RiskLevel;
                finalRiskConfidence = 0.90;
            }
        }
        else if (finalRiskConfidence < _options.LowConfidenceThreshold)
        {
            warnings.Add("risk_confidence_too_low_used_medium");
            finalRiskLevel = "medium";
        }

        // Sensitivity Merge
        var finalSensitivity = sensitivity.DataSensitivity;
        var finalSensitivityConfidence = sensitivity.Confidence;

        if (!string.IsNullOrEmpty(metadata.DataSensitivity))
        {
            // Similar logic: don't let metadata override actual detected restricted
            if ((metadata.DataSensitivity == "public" || metadata.DataSensitivity == "internal") && 
                (sensitivity.DataSensitivity == "restricted" || sensitivity.DataSensitivity == "confidential"))
            {
                warnings.Add("metadata_low_sensitivity_overridden_by_detector");
            }
            else
            {
                finalSensitivity = metadata.DataSensitivity;
                finalSensitivityConfidence = 0.90;
            }
        }
        else if (finalSensitivityConfidence < _options.LowConfidenceThreshold)
        {
            finalSensitivity = "unknown";
        }

        return new RequestProfile
        {
            TaskType = finalTaskType,
            TaskTypeConfidence = finalTaskConfidence,
            RiskLevel = finalRiskLevel,
            RiskConfidence = finalRiskConfidence,
            InputTokens = input.InputTokens,
            EstimatedOutputTokens = input.EstimatedOutputTokens,
            RequiresReasoning = metadata.RequiresReasoning,
            RequiresStructuredOutput = metadata.RequiresStructuredOutput,
            ContainsPii = sensitivity.ContainsPii,
            DataSensitivity = finalSensitivity,
            SensitivityConfidence = finalSensitivityConfidence,
            ComplexityScore = complexity.Score,
            ComplexityBand = complexity.Band,
            Department = metadata.Department,
            Environment = metadata.Environment,
            ClassificationMethod = classificationMethod,
            Signals = _options.EnableProfileSignals ? signals : new List<ProfileSignal>(),
            Warnings = warnings
        };
    }
}
