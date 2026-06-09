using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenShield.Guardrails.Profiling.Options;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public interface IDatabaseProfilerRuleProvider
{
    Task<List<TaskClassificationRule>> GetActiveTaskRulesAsync(Guid tenantId, CancellationToken cancellationToken);
}
