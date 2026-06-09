using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;
using TokenShield.Domain.Models;
using TokenShield.Guardrails.Profiling.Options;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public class RuleBasedRiskClassifier : IRiskClassifier
{
    private readonly RequestProfilerOptions _options;

    // Hardcoded patterns as per requirements
    private static readonly string[] HighRiskPhrases = {
        "approve transaction", "reject claim", "block account", "close account",
        "make final decision", "legal decision", "medical decision", "financial decision",
        "compliance violation", "terminate employee", "report to regulator", "override policy"
    };

    private static readonly string[] MediumRiskPhrases = {
        "fraud", "risk", "compliance", "audit", "policy", 
        "customer complaint", "investigation", "suspicious activity"
    };

    public RuleBasedRiskClassifier(IOptions<RequestProfilerOptions> options)
    {
        _options = options.Value;
    }

    public Task<RiskClassificationResult> ClassifyAsync(RequestClassificationInput input, CancellationToken cancellationToken)
    {
        var normalized = input.NormalizedText;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult(new RiskClassificationResult
            {
                RiskLevel = _options.DefaultRiskLevel,
                Confidence = 0.1
            });
        }

        var signals = new List<ProfileSignal>();

        // 1. High risk check
        if (HighRiskPhrases.Any(p => normalized.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            signals.Add(new ProfileSignal
            {
                Name = "high_risk_phrase_detected",
                Value = "high",
                Confidence = 0.90,
                Source = "rule_based_risk_classifier"
            });
            return Task.FromResult(new RiskClassificationResult
            {
                RiskLevel = "high",
                Confidence = 0.90,
                Signals = signals
            });
        }

        // 2. Medium risk check
        if (MediumRiskPhrases.Any(p => normalized.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            signals.Add(new ProfileSignal
            {
                Name = "medium_risk_phrase_detected",
                Value = "medium",
                Confidence = 0.70,
                Source = "rule_based_risk_classifier"
            });
            return Task.FromResult(new RiskClassificationResult
            {
                RiskLevel = "medium",
                Confidence = 0.70,
                Signals = signals
            });
        }

        // 3. Fallback
        return Task.FromResult(new RiskClassificationResult
        {
            RiskLevel = _options.DefaultRiskLevel,
            Confidence = 0.2
        });
    }
}
