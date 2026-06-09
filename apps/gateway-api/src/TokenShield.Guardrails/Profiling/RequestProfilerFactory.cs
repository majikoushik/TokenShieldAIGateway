using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Guardrails.Profiling.Options;

namespace TokenShield.Guardrails.Profiling;

public class RequestProfilerFactory : IRequestProfilerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RequestProfilerOptions _options;

    public RequestProfilerFactory(IServiceProvider serviceProvider, IOptions<RequestProfilerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public IRequestProfiler Create()
    {
        if (!_options.EnableProductionProfiler || _options.Mode.Equals("Mvp", StringComparison.OrdinalIgnoreCase))
        {
            return _serviceProvider.GetRequiredService<MvpRequestProfiler>();
        }

        if (_options.Mode.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            return _serviceProvider.GetRequiredService<ProductionRequestProfiler>();
        }

        if (_options.Mode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
        {
            return _serviceProvider.GetRequiredService<HybridRequestProfiler>();
        }

        // Default safe fallback
        return _serviceProvider.GetRequiredService<MvpRequestProfiler>();
    }
}
