using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TokenShield.ProviderAdapters;

public class ProviderAdapterFactory : IProviderAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ProviderAdapterFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public IProviderAdapter GetAdapter(string providerName)
    {
        var enableRealCalls = _configuration.GetValue<bool>("ProviderSettings:EnableRealCalls", false);

        if (!enableRealCalls)
        {
            return _serviceProvider.GetRequiredService<MockProviderAdapter>();
        }

        var providerKey = providerName.ToLowerInvariant();

        if (providerKey == "openai")
        {
            return _serviceProvider.GetRequiredService<OpenAiProviderAdapter>();
        }
        else if (providerKey == "azure openai" || providerKey == "azureopenai")
        {
            return _serviceProvider.GetRequiredService<AzureOpenAiProviderAdapter>();
        }
        else if (providerKey == "anthropic")
        {
            return _serviceProvider.GetRequiredService<AnthropicProviderAdapter>();
        }

        return _serviceProvider.GetRequiredService<MockProviderAdapter>();
    }
}
