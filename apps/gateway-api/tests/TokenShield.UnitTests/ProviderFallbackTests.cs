using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;
using TokenShield.Infrastructure.Services;
using TokenShield.ProviderAdapters;
using TokenShield.ProviderAdapters.Services;
using Xunit;

namespace TokenShield.UnitTests;

public class ProviderFallbackTests
{
    private TokenShieldDbContext CreateInMemoryDbContext()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TokenShieldDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<TokenShieldDbContext>();
    }

    [Fact]
    public void ProviderAdapterFactory_EnableRealCallsFalse_AlwaysReturnsMockProvider()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ProviderSettings:EnableRealCalls", "false" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddTransient<MockProviderAdapter>();
        services.AddTransient<OpenAiProviderAdapter>();
        services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(new HttpClient()));

        var serviceProvider = services.BuildServiceProvider();
        var factory = new ProviderAdapterFactory(serviceProvider, config);

        // Act & Assert
        var adapter1 = factory.GetAdapter("openai");
        Assert.IsType<MockProviderAdapter>(adapter1);

        var adapter2 = factory.GetAdapter("anthropic");
        Assert.IsType<MockProviderAdapter>(adapter2);
    }

    [Fact]
    public void ProviderAdapterFactory_EnableRealCallsTrue_ResolvesExpectedRealAdapters()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ProviderSettings:EnableRealCalls", "true" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddTransient<MockProviderAdapter>();
        services.AddTransient<OpenAiProviderAdapter>();
        services.AddTransient<AzureOpenAiProviderAdapter>();
        services.AddTransient<AnthropicProviderAdapter>();
        services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(new HttpClient()));

        var serviceProvider = services.BuildServiceProvider();
        var factory = new ProviderAdapterFactory(serviceProvider, config);

        // Act & Assert
        var openaiAdapter = factory.GetAdapter("openai");
        Assert.IsType<OpenAiProviderAdapter>(openaiAdapter);

        var azureAdapter = factory.GetAdapter("azure openai");
        Assert.IsType<AzureOpenAiProviderAdapter>(azureAdapter);

        var anthropicAdapter = factory.GetAdapter("anthropic");
        Assert.IsType<AnthropicProviderAdapter>(anthropicAdapter);
    }

    [Fact]
    public async Task ProviderExecutionService_MissingCredentials_ThrowsCredentialsException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();

        // Seed a provider that requires credentials (OpenAI)
        var provider = new ModelProvider
        {
            TenantId = tenantId,
            Name = "OpenAI",
            ApiUrl = "https://api.openai.com/v1",
            ApiKeySecretRef = "kv-secret-openai",
            IsActive = true
        };
        context.ModelProviders.Add(provider);

        var model = new AiModel
        {
            Provider = provider,
            Name = "gpt-4o",
            DeploymentName = "gpt-4o",
            Tier = ModelTier.Standard,
            IsActive = true
        };
        context.AiModels.Add(model);
        await context.SaveChangesAsync();

        // Config has real calls enabled, but NO secret seeded under "ProviderSettings:Secrets:kv-secret-openai"
        var settings = new Dictionary<string, string?>
        {
            { "ProviderSettings:EnableRealCalls", "true" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddTransient<MockProviderAdapter>();
        services.AddTransient<OpenAiProviderAdapter>();
        services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(new HttpClient()));
        services.AddScoped<IProviderAdapterFactory, ProviderAdapterFactory>();
        services.AddScoped<IBudgetService>(sp => new BudgetService(context));

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IProviderAdapterFactory>();
        var budgetService = serviceProvider.GetRequiredService<IBudgetService>();

        var execService = new ProviderExecutionService(context, factory, config, NullLogger<ProviderExecutionService>.Instance, budgetService);

        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new() { new() { Role = "user", Content = "test" } }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            execService.ExecuteWithFallbackAsync(tenantId, ModelTier.Standard, request, CancellationToken.None));
        
        Assert.Contains("Provider credentials missing", exception.InnerException?.Message);
    }

    [Fact]
    public async Task ProviderExecutionService_PollyRetry_SucceedsOnSecondAttempt()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();

        var provider = new ModelProvider
        {
            TenantId = tenantId,
            Name = "Mock Provider",
            ApiUrl = "http://localhost",
            ApiKeySecretRef = "secret-ref",
            IsActive = true
        };
        context.ModelProviders.Add(provider);

        var model = new AiModel
        {
            Provider = provider,
            Name = "mock-standard",
            DeploymentName = "mock-standard-deploy",
            Tier = ModelTier.Standard,
            IsActive = true
        };
        context.AiModels.Add(model);
        await context.SaveChangesAsync();

        // Configure configuration to simulate mock transient failures
        var settings = new Dictionary<string, string?>
        {
            { "ProviderSettings:EnableRealCalls", "false" },
            { "ProviderSettings:SimulateMockFailures", "true" } // throws once, succeeds on retry
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddTransient<MockProviderAdapter>();
        var serviceProvider = services.BuildServiceProvider();

        var factory = new ProviderAdapterFactory(serviceProvider, config);
        var budgetService = new BudgetService(context);
        var execService = new ProviderExecutionService(context, factory, config, NullLogger<ProviderExecutionService>.Instance, budgetService);

        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new() { new() { Role = "user", Content = "hello" } }
        };

        MockProviderAdapter.ResetFailureCount();

        // Act
        var result = await execService.ExecuteWithFallbackAsync(tenantId, ModelTier.Standard, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("mock-standard", result.ModelName);
        Assert.False(result.FallbackUsed); // Succeeded on retry for primary model, so fallback wasn't triggered
    }

    [Fact]
    public async Task ProviderExecutionService_SameTierFallback_TriesSecondaryModel()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();

        var provider = new ModelProvider
        {
            TenantId = tenantId,
            Name = "Mock Provider",
            ApiUrl = "http://localhost",
            ApiKeySecretRef = "secret",
            IsActive = true
        };
        context.ModelProviders.Add(provider);

        // Seed 2 active models in Standard tier:
        // Model A: configured to always fail (we will simulate it by throwing inside MockProvider if model name is model-a)
        // Model B: succeeds
        var modelA = new AiModel { Provider = provider, Name = "model-a", DeploymentName = "deploy-a", Tier = ModelTier.Standard, IsActive = true };
        var modelB = new AiModel { Provider = provider, Name = "model-b", DeploymentName = "deploy-b", Tier = ModelTier.Standard, IsActive = true };
        context.AiModels.AddRange(modelA, modelB);
        await context.SaveChangesAsync();

        var settings = new Dictionary<string, string?> { { "ProviderSettings:EnableRealCalls", "false" } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        // Register a customized MockProviderAdapter that fails for model-a
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddTransient<MockProviderAdapter>(sp => new FailingMockProviderAdapter(config));
        var serviceProvider = services.BuildServiceProvider();

        var factory = new ProviderAdapterFactory(serviceProvider, config);
        var budgetService = new BudgetService(context);
        var execService = new ProviderExecutionService(context, factory, config, NullLogger<ProviderExecutionService>.Instance, budgetService);

        var request = new ChatCompletionRequest { Model = "auto", Messages = new() { new() { Role = "user", Content = "test" } } };

        // Act
        var result = await execService.ExecuteWithFallbackAsync(tenantId, ModelTier.Standard, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("model-b", result.ModelName); // Succeeded on model-b
        Assert.True(result.FallbackUsed); // Secondary model in tier was used
    }

    [Fact]
    public async Task OpenAiProviderAdapter_MapsPayloads_Correctly()
    {
        // Arrange
        var openaiResponseJson = @"
        {
            ""id"": ""chatcmpl-test"",
            ""choices"": [
                { ""message"": { ""role"": ""assistant"", ""content"": ""Paris"" } }
            ],
            ""usage"": { ""prompt_tokens"": 8, ""completion_tokens"": 2 }
        }";

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Contains("chat/completions", req.RequestUri?.AbsoluteUri ?? "");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(openaiResponseJson, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var factory = new TestHttpClientFactory(new HttpClient(mockHandler));
        var adapter = new OpenAiProviderAdapter(factory);

        var request = new ModelRequest
        {
            ModelName = "gpt-4o",
            DeploymentName = "gpt-4o",
            Messages = new() { new() { Role = "user", Content = "capital of France?" } }
        };

        // Act
        var response = await adapter.CompleteChatAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("chatcmpl-test", response.Id);
        Assert.Equal("Paris", response.Content);
        Assert.Equal(8, response.PromptTokens);
        Assert.Equal(2, response.CompletionTokens);
    }

    [Fact]
    public async Task AzureOpenAiProviderAdapter_MapsPayloads_Correctly()
    {
        // Arrange
        var azureResponseJson = @"
        {
            ""id"": ""azure-test"",
            ""choices"": [
                { ""message"": { ""role"": ""assistant"", ""content"": ""Rome"" } }
            ],
            ""usage"": { ""prompt_tokens"": 5, ""completion_tokens"": 3 }
        }";

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Contains("/openai/deployments/deploy-gpt/chat/completions", req.RequestUri?.AbsoluteUri ?? "");
            Assert.Equal("azure-key", req.Headers.GetValues("api-key").FirstOrDefault());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(azureResponseJson, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var factory = new TestHttpClientFactory(new HttpClient(mockHandler));
        var adapter = new AzureOpenAiProviderAdapter(factory);

        var request = new ModelRequest
        {
            ModelName = "gpt-4",
            DeploymentName = "deploy-gpt",
            Messages = new() { new() { Role = "user", Content = "capital of Italy?" } },
            ApiKey = "azure-key"
        };

        // Act
        var response = await adapter.CompleteChatAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("azure-test", response.Id);
        Assert.Equal("Rome", response.Content);
        Assert.Equal(5, response.PromptTokens);
    }

    [Fact]
    public async Task AnthropicProviderAdapter_MapsPayloads_Correctly()
    {
        // Arrange
        var anthropicResponseJson = @"
        {
            ""id"": ""msg-test"",
            ""content"": [
                { ""type"": ""text"", ""text"": ""London"" }
            ],
            ""usage"": { ""input_tokens"": 7, ""output_tokens"": 4 }
        }";

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Contains("/messages", req.RequestUri?.AbsoluteUri ?? "");
            Assert.Equal("anthropic-key", req.Headers.GetValues("x-api-key").FirstOrDefault());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(anthropicResponseJson, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var factory = new TestHttpClientFactory(new HttpClient(mockHandler));
        var adapter = new AnthropicProviderAdapter(factory);

        var request = new ModelRequest
        {
            ModelName = "claude-3",
            DeploymentName = "claude-3",
            Messages = new()
            {
                new() { Role = "system", Content = "You are helpful." },
                new() { Role = "user", Content = "capital of UK?" }
            },
            ApiKey = "anthropic-key"
        };

        // Act
        var response = await adapter.CompleteChatAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("msg-test", response.Id);
        Assert.Equal("London", response.Content);
        Assert.Equal(7, response.PromptTokens);
        Assert.Equal(4, response.CompletionTokens);
    }

    // Helper implementations for Mock HTTP Client
    private class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public TestHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _sender;
        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> sender) => _sender = sender;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_sender(request));
        }
    }

    private class FailingMockProviderAdapter : MockProviderAdapter
    {
        public FailingMockProviderAdapter(IConfiguration config) : base(config) { }

        public override async Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken)
        {
            if (request.ModelName == "model-a")
            {
                throw new HttpRequestException("Simulated provider outage for Model A.");
            }
            return await base.CompleteChatAsync(request, cancellationToken);
        }
    }
}
