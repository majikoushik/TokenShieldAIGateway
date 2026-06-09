using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Dto;
using TokenShield.Domain.Models;
using TokenShield.Guardrails.Profiling.Options;

namespace TokenShield.Guardrails.Profiling;

public class HybridRequestProfiler : IRequestProfiler
{
    private readonly RequestProfilerOptions _options;
    private readonly ProductionRequestProfiler _productionProfiler;
    private readonly MvpRequestProfiler _mvpProfiler;

    public HybridRequestProfiler(
        IOptions<RequestProfilerOptions> options,
        ProductionRequestProfiler productionProfiler,
        MvpRequestProfiler mvpProfiler)
    {
        _options = options.Value;
        _productionProfiler = productionProfiler;
        _mvpProfiler = mvpProfiler;
    }

    public async Task<RequestProfile> ProfileRequestAsync(ChatCompletionRequest request, int inputTokens, CancellationToken cancellationToken = default)
    {
        var profile = await _productionProfiler.ProfileRequestAsync(request, inputTokens, cancellationToken);

        if (profile.TaskTypeConfidence < _options.LowConfidenceThreshold)
        {
            var mvpProfile = await _mvpProfiler.ProfileRequestAsync(request, inputTokens, cancellationToken);
            
            profile.TaskType = mvpProfile.TaskType;
            profile.ClassificationMethod = "hybrid";
            
            var warnings = profile.Warnings.ToList();
            warnings.Add("Production profiler confidence was low; MVP fallback was used.");
            profile.Warnings = warnings;
        }

        return profile;
    }
}
