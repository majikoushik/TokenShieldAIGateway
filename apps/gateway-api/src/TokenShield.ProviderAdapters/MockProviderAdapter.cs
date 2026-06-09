using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TokenShield.ProviderAdapters;

public class MockProviderAdapter : IProviderAdapter
{
    private readonly IConfiguration _configuration;
    private static int _failureCount = 0;

    public MockProviderAdapter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ProviderName => "Mock Provider";

    public virtual async Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var simulateFailures = _configuration.GetValue<bool>("ProviderSettings:SimulateMockFailures", false);
        if (simulateFailures && _failureCount < 1)
        {
            _failureCount++;
            throw new HttpRequestException("Simulated transient mock provider HTTP connection failure (503 Service Unavailable).");
        }

        await Task.Delay(20, cancellationToken); // simulate minor latency

        var text = $"Hello! I am a simulated response from {request.ModelName} via the Mock Provider.";

        return new ModelResponse
        {
            Id = $"mock-response-{Guid.NewGuid():N}",
            Content = text,
            PromptTokens = Math.Max(5, request.Messages.Count * 8),
            CompletionTokens = 15,
            ModelName = request.ModelName,
            ProviderName = ProviderName
        };
    }

    public static void ResetFailureCount()
    {
        _failureCount = 0;
    }
}
