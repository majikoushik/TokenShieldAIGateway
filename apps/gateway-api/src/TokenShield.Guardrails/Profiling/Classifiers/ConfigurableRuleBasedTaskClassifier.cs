using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;
using TokenShield.Domain.Models;
using TokenShield.Guardrails.Profiling.Options;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public class ConfigurableRuleBasedTaskClassifier : ITaskClassifier
{
    private readonly RequestProfilerOptions _options;

    public ConfigurableRuleBasedTaskClassifier(IOptions<RequestProfilerOptions> options)
    {
        _options = options.Value;
    }

    public Task<TaskClassificationResult> ClassifyAsync(RequestClassificationInput input, CancellationToken cancellationToken)
    {
        var normalized = input.NormalizedText;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult(new TaskClassificationResult
            {
                TaskType = _options.DefaultTaskType,
                Confidence = 0.1,
                ClassificationMethod = "default_fallback"
            });
        }

        var activeRules = _options.TaskClassificationRules
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ToList();

        var bestMatch = default(TaskClassificationRule);
        var bestConfidence = 0.0;
        var signalName = string.Empty;

        foreach (var rule in activeRules)
        {
            var matched = false;

            // 1. Phrase matching
            if (rule.Phrases.Any(p => normalized.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                matched = true;
                signalName = "phrase_match";
            }
            // 2. Regex matching
            else if (rule.RegexPatterns.Any(p => Regex.IsMatch(normalized, p, RegexOptions.IgnoreCase)))
            {
                matched = true;
                signalName = "regex_match";
            }

            if (matched)
            {
                if (rule.Confidence > bestConfidence)
                {
                    bestConfidence = rule.Confidence;
                    bestMatch = rule;
                }
            }
        }

        if (bestMatch != null)
        {
            return Task.FromResult(new TaskClassificationResult
            {
                TaskType = bestMatch.TaskType,
                Confidence = bestMatch.Confidence,
                ClassificationMethod = "configurable_rules",
                Signals = new List<ProfileSignal>
                {
                    new()
                    {
                        Name = "task_rule_match",
                        Value = bestMatch.TaskType,
                        Confidence = bestMatch.Confidence,
                        Source = "configurable_rule_classifier"
                    }
                }
            });
        }

        return Task.FromResult(new TaskClassificationResult
        {
            TaskType = _options.DefaultTaskType,
            Confidence = 0.2, // Low confidence for default
            ClassificationMethod = "default_fallback"
        });
    }
}
