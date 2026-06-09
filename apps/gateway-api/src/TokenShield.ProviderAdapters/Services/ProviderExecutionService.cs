using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.ProviderAdapters.Services;

public class ProviderExecutionService : IProviderExecutionService
{
    private readonly TokenShieldDbContext _dbContext;
    private readonly IProviderAdapterFactory _adapterFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProviderExecutionService> _logger;
    private readonly IBudgetService _budgetService;

    public ProviderExecutionService(
        TokenShieldDbContext dbContext,
        IProviderAdapterFactory adapterFactory,
        IConfiguration configuration,
        ILogger<ProviderExecutionService> logger,
        IBudgetService budgetService)
    {
        _dbContext = dbContext;
        _adapterFactory = adapterFactory;
        _configuration = configuration;
        _logger = logger;
        _budgetService = budgetService;
    }

    public async Task<ProviderExecutionResult> ExecuteWithFallbackAsync(
        Guid tenantId,
        ModelTier selectedTier,
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Load active candidate models in the selected tier
        var targetModels = await _dbContext.AiModels
            .Include(m => m.Provider)
            .Where(m => m.Provider.TenantId == tenantId && m.Tier == selectedTier && m.IsActive && !m.IsDeleted)
            .ToListAsync(cancellationToken);

        // 2. Load active candidate models in fallback tiers (Premium -> Standard -> Cheap)
        var fallbackTiers = new List<ModelTier>();
        if (selectedTier == ModelTier.Premium)
        {
            fallbackTiers.Add(ModelTier.Standard);
            fallbackTiers.Add(ModelTier.Cheap);
        }
        else if (selectedTier == ModelTier.Standard)
        {
            fallbackTiers.Add(ModelTier.Cheap);
        }

        var fallbackModels = new List<AiModel>();
        foreach (var tier in fallbackTiers)
        {
            var tierModels = await _dbContext.AiModels
                .Include(m => m.Provider)
                .Where(m => m.Provider.TenantId == tenantId && m.Tier == tier && m.IsActive && !m.IsDeleted)
                .ToListAsync(cancellationToken);
            fallbackModels.AddRange(tierModels);
        }

        // Combine into prioritized candidates list
        var candidates = new List<AiModel>();
        candidates.AddRange(targetModels);
        candidates.AddRange(fallbackModels);

        // Fallback to any active model if candidates list is empty
        if (candidates.Count == 0)
        {
            var anyActiveModels = await _dbContext.AiModels
                .Include(m => m.Provider)
                .Where(m => m.Provider.TenantId == tenantId && m.IsActive && !m.IsDeleted)
                .ToListAsync(cancellationToken);
            candidates.AddRange(anyActiveModels);
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No active AI models resolved for this request routing tier.");
        }

        // 3. Define Polly retry policy (retries transient provider errors once)
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .RetryAsync(1, (exception, retryCount) =>
            {
                _logger.LogWarning(exception, "Transient error occurred. Retrying call once (retry count: {Count})", retryCount);
            });

        Exception? lastException = null;
        AiModel? successfulModel = null;
        ModelResponse? successfulResponse = null;
        BudgetCheckResult? successfulModelBudget = null;

        var primaryModelId = candidates[0].Id;

        // 4. Try candidate models in order
        foreach (var model in candidates)
        {
            try
            {
                _logger.LogInformation("Checking model budget for '{ModelName}'", model.Name);
                
                // Pre-evaluate model-scoped budget
                var budgetResult = await _budgetService.CheckModelBudgetAsync(tenantId, model.Id);
                if (budgetResult.IsBlocked)
                {
                    _logger.LogWarning("Model '{ModelName}' budget is exceeded and action is Block. Skipping this model.", model.Name);
                    continue;
                }

                _logger.LogInformation("Attempting completion call with model '{ModelName}' via provider '{ProviderName}'", model.Name, model.Provider.Name);

                var adapter = _adapterFactory.GetAdapter(model.Provider.Name);
                
                // Load credentials if real calls are enabled
                string? apiKey = null;
                var enableRealCalls = _configuration.GetValue<bool>("ProviderSettings:EnableRealCalls", false);
                if (enableRealCalls && model.Provider.Name.ToLowerInvariant() != "mock provider")
                {
                    var secretRef = model.Provider.ApiKeySecretRef;
                    apiKey = _configuration[$"ProviderSettings:Secrets:{secretRef}"];
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        throw new InvalidOperationException($"Provider credentials missing: API Key reference '{secretRef}' was not found in ProviderSettings:Secrets configuration.");
                    }
                }

                var modelRequest = new ModelRequest
                {
                    ModelName = model.Name,
                    DeploymentName = model.DeploymentName,
                    Messages = request.Messages.Select(m => new ModelMessage { Role = m.Role, Content = m.Content }).ToList(),
                    Temperature = request.Temperature,
                    MaxTokens = request.MaxTokens,
                    ApiKey = apiKey,
                    ApiUrl = model.Provider.ApiUrl
                };

                // Execute adapter with Polly retry wrapper
                successfulResponse = await retryPolicy.ExecuteAsync(() => adapter.CompleteChatAsync(modelRequest, cancellationToken));
                successfulModel = model;
                successfulModelBudget = budgetResult;
                break; // Succeeded! Exit iteration.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call model '{ModelName}'", model.Name);
                lastException = ex;
            }
        }

        if (successfulResponse == null || successfulModel == null || successfulModelBudget == null)
        {
            throw new InvalidOperationException("All resolved AI models and fallback endpoints failed to execute.", lastException);
        }

        var fallbackUsed = successfulModel.Id != primaryModelId;

        return new ProviderExecutionResult
        {
            ResponseText = successfulResponse.Content,
            PromptTokens = successfulResponse.PromptTokens,
            CompletionTokens = successfulResponse.CompletionTokens,
            ModelName = successfulModel.Name,
            ProviderName = successfulModel.Provider.Name,
            Tier = successfulModel.Tier,
            FallbackUsed = fallbackUsed,
            ModelId = successfulModel.Id,
            ModelBudgetIsWarning = successfulModelBudget.IsWarning,
            ModelBudgetStatus = successfulModelBudget.BudgetStatus,
            ModelBudgetWarningMessage = successfulModelBudget.WarningMessage
        };
    }
}
