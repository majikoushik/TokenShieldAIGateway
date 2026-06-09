using System;
using System.Collections.Generic;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;
using TokenShield.Domain.Models;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public class DefaultComplexityScorer : IComplexityScorer
{
    public ComplexityScoreResult Calculate(
        RequestClassificationInput input,
        TaskClassificationResult task,
        RiskClassificationResult risk,
        SensitivityDetectionResult sensitivity)
    {
        var score = 10;
        var signals = new List<ProfileSignal>();

        void AddScore(int amount, string reason)
        {
            score += amount;
            signals.Add(new ProfileSignal { Name = "complexity_modifier", Value = $"+{amount}", Source = reason });
        }

        // Token signals
        if (input.InputTokens > 12000) AddScore(25, "input_tokens_>_12000");
        else if (input.InputTokens > 4000) AddScore(20, "input_tokens_>_4000");
        else if (input.InputTokens > 1000) AddScore(10, "input_tokens_>_1000");

        if (input.EstimatedOutputTokens > 3000) AddScore(20, "output_tokens_>_3000");
        else if (input.EstimatedOutputTokens > 1000) AddScore(10, "output_tokens_>_1000");

        // Task type signals
        switch (task.TaskType.ToLowerInvariant())
        {
            case "summarization": AddScore(5, "task_summarization"); break;
            case "classification": AddScore(10, "task_classification"); break;
            case "extraction": AddScore(15, "task_extraction"); break;
            case "rag_answer": AddScore(20, "task_rag_answer"); break;
            case "policy_check": AddScore(20, "task_policy_check"); break;
            case "fraud_analysis": AddScore(25, "task_fraud_analysis"); break;
            case "code_debugging": AddScore(25, "task_code_debugging"); break;
            case "architecture_review": AddScore(30, "task_architecture_review"); break;
            case "incident_analysis": AddScore(30, "task_incident_analysis"); break;
            case "complex_reasoning": AddScore(35, "task_complex_reasoning"); break;
        }

        // Reasoning / Structured
        if (input.Metadata.TryGetValue("requiresReasoning", out var reqRes) && bool.TryParse(reqRes, out var isReqRes) && isReqRes)
        {
            AddScore(25, "requires_reasoning_metadata");
        }
        if (input.Metadata.TryGetValue("requiresStructuredOutput", out var reqStruct) && bool.TryParse(reqStruct, out var isReqStruct) && isReqStruct)
        {
            AddScore(10, "requires_structured_output_metadata");
        }

        // Sensitivity
        if (sensitivity.ContainsPii) AddScore(10, "contains_pii");
        if (sensitivity.ContainsFinancialData) AddScore(15, "contains_financial_data");
        if (sensitivity.ContainsHealthData) AddScore(20, "contains_health_data");
        if (sensitivity.ContainsLegalData) AddScore(20, "contains_legal_data");
        if (sensitivity.DataSensitivity == "restricted") AddScore(20, "sensitivity_restricted");

        // Risk
        switch (risk.RiskLevel.ToLowerInvariant())
        {
            case "medium": AddScore(10, "risk_medium"); break;
            case "high": AddScore(25, "risk_high"); break;
            case "critical": AddScore(35, "risk_critical"); break;
        }

        score = Math.Min(Math.Max(score, 0), 100);

        string band = score <= 30 ? "low" : score <= 65 ? "medium" : "high";

        return new ComplexityScoreResult
        {
            Score = score,
            Band = band,
            Signals = signals
        };
    }
}
