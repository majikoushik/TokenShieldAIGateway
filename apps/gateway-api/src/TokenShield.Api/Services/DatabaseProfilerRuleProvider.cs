using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TokenShield.Guardrails.Profiling.Classifiers;
using TokenShield.Guardrails.Profiling.Options;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Services;

public class DatabaseProfilerRuleProvider : IDatabaseProfilerRuleProvider
{
    private readonly TokenShieldDbContext _context;

    public DatabaseProfilerRuleProvider(TokenShieldDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaskClassificationRule>> GetActiveTaskRulesAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var dbRules = await _context.ProfilerRules
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

        var rules = new List<TaskClassificationRule>();

        foreach (var r in dbRules)
        {
            var phrases = ParseJsonArray(r.PhrasesJson);
            var regexes = ParseJsonArray(r.RegexPatternsJson);

            rules.Add(new TaskClassificationRule
            {
                TaskType = r.TargetTaskType,
                Phrases = phrases,
                RegexPatterns = regexes,
                Confidence = r.Confidence,
                Priority = r.Priority,
                IsEnabled = r.IsActive
            });
        }

        return rules;
    }

    private List<string> ParseJsonArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
