using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto.Admin;

namespace TokenShield.Application.Services;

public class ProfilerRuleService : IProfilerRuleService
{
    public Task<TestProfilerRuleResponse> TestRuleAsync(TestProfilerRuleRequest request)
    {
        var normalized = request.Prompt.ToLowerInvariant();
        var isMatch = false;
        var matchReason = string.Empty;

        // 1. Phrase matching
        if (request.Phrases != null && request.Phrases.Any())
        {
            var matchedPhrase = request.Phrases.FirstOrDefault(p => normalized.Contains(p.ToLowerInvariant()));
            if (matchedPhrase != null)
            {
                isMatch = true;
                matchReason = $"Matched phrase: '{matchedPhrase}'";
            }
        }

        // 2. Regex matching
        if (!isMatch && request.RegexPatterns != null && request.RegexPatterns.Any())
        {
            foreach (var pattern in request.RegexPatterns)
            {
                try
                {
                    if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                    {
                        isMatch = true;
                        matchReason = $"Matched regex pattern: '{pattern}'";
                        break;
                    }
                }
                catch
                {
                    // Ignore invalid regex for test purposes
                }
            }
        }

        return Task.FromResult(new TestProfilerRuleResponse
        {
            IsMatch = isMatch,
            MatchReason = isMatch ? matchReason : "No matching phrases or regex patterns found in the prompt.",
            TargetTaskType = request.TargetTaskType,
            Confidence = request.Confidence
        });
    }
}
