using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.Application.Validators;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers;

[ApiController]
[Route("v1/chat/completions")]
public class ChatCompletionsController : ControllerBase
{
    private readonly TokenShieldDbContext _dbContext;
    private readonly IRequestContext _requestContext;
    private readonly IRequestProfiler _requestProfiler;
    private readonly ICostEngineService _costEngineService;
    private readonly IRoutingRuleEngine _routingRuleEngine;
    private readonly IBudgetService _budgetService;
    private readonly IProviderExecutionService _providerExecutionService;

    public ChatCompletionsController(
        TokenShieldDbContext dbContext,
        IRequestContext requestContext,
        IRequestProfiler requestProfiler,
        ICostEngineService costEngineService,
        IRoutingRuleEngine routingRuleEngine,
        IBudgetService budgetService,
        IProviderExecutionService providerExecutionService)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
        _requestProfiler = requestProfiler;
        _costEngineService = costEngineService;
        _routingRuleEngine = routingRuleEngine;
        _budgetService = budgetService;
        _providerExecutionService = providerExecutionService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatCompletionRequest request)
    {
        // 1. Validate incoming JSON payload parameters
        var validator = new ChatCompletionRequestValidator();
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            return BadRequest(new
            {
                error = new
                {
                    message = "Request validation failed.",
                    type = "validation_error",
                    code = "400",
                    details = errors
                }
            });
        }

        // 2. Estimate input tokens
        var inputTokens = _costEngineService.EstimateRequestTokens(request);
        if (inputTokens == 0) inputTokens = 5; // Fallback minimum limit

        // 3. Create request profile (PII + taskType inference + complexity)
        var profile = _requestProfiler.ProfileRequest(request, inputTokens);

        // 4. Run pre-call budget checks (Tenant, Client Application, API Key)
        var preCallBudgetResult = await _budgetService.CheckBudgetPreCallAsync(
            _requestContext.TenantId,
            _requestContext.ClientApplicationId,
            _requestContext.ApiKeyId);

        if (preCallBudgetResult.IsBlocked)
        {
            await LogRequestAsync(request, null, "Blocked", "Budget Exceeded", inputTokens, 0, 0m, budgetStatus: preCallBudgetResult.BudgetStatus);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    message = preCallBudgetResult.WarningMessage ?? "Monthly budget limit exceeded.",
                    type = "budget_exceeded",
                    code = "403"
                }
            });
        }

        // 5. Run rule-based matching policies
        var (action, selectedTier, matchedRuleName) = await _routingRuleEngine.MatchRuleAsync(_requestContext.TenantId, profile);

        // 6. Handle action outcomes (Block or Human Review)
        if (action == RoutingActionType.Block)
        {
            await LogRequestAsync(request, null, "Blocked", matchedRuleName, inputTokens, 0, 0m, budgetStatus: preCallBudgetResult.BudgetStatus);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    message = "Request blocked by enterprise security routing policy.",
                    type = "policy_blocked",
                    code = "403"
                }
            });
        }

        if (action == RoutingActionType.HumanReview)
        {
            await LogRequestAsync(request, null, "HumanReviewRequired", matchedRuleName, inputTokens, 0, 0m, budgetStatus: preCallBudgetResult.BudgetStatus);
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new
            {
                error = new
                {
                    message = "Request requires human review before it can be processed.",
                    type = "human_review_required",
                    code = "422"
                }
            });
        }

        // 7. Apply budget downgrade if pre-call check triggered it
        var targetTier = selectedTier ?? ModelTier.Standard;
        if (preCallBudgetResult.IsDowngraded)
        {
            targetTier = targetTier switch
            {
                ModelTier.Premium => ModelTier.Standard,
                ModelTier.Standard => ModelTier.Cheap,
                ModelTier.Cheap => ModelTier.Cheap,
                _ => ModelTier.Cheap
            };
        }

        // 8. Execute via ProviderExecutionService (handling fallbacks & Polly retries)
        ProviderExecutionResult executionResult;
        try
        {
            executionResult = await _providerExecutionService.ExecuteWithFallbackAsync(
                _requestContext.TenantId,
                targetTier,
                request,
                HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // Log failure to database request logs
            await LogRequestAsync(request, null, "Failed", matchedRuleName, inputTokens, 0, 0m, budgetStatus: preCallBudgetResult.BudgetStatus);

            // Normalize provider exceptions into safe generic error payload
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = new
                {
                    message = "Provider error occurred while processing chat completion request.",
                    type = "provider_error",
                    code = "502"
                }
            });
        }

        // Determine aggregated budget status and warnings
        var finalBudgetStatus = "Within Limits";
        string? warningMessage = null;

        if (executionResult.ModelBudgetStatus == "Exceeded" || preCallBudgetResult.IsBlocked)
        {
            finalBudgetStatus = "Exceeded";
            warningMessage = executionResult.ModelBudgetWarningMessage ?? preCallBudgetResult.WarningMessage;
        }
        else if (executionResult.ModelBudgetStatus == "Downgraded" || preCallBudgetResult.IsDowngraded)
        {
            finalBudgetStatus = "Downgraded";
            warningMessage = executionResult.ModelBudgetWarningMessage ?? preCallBudgetResult.WarningMessage;
        }
        else if (executionResult.ModelBudgetIsWarning || preCallBudgetResult.IsWarning)
        {
            finalBudgetStatus = "Warning";
            warningMessage = executionResult.ModelBudgetWarningMessage ?? preCallBudgetResult.WarningMessage;
        }

        var completionTokens = executionResult.CompletionTokens;
        var promptTokens = executionResult.PromptTokens;
        var totalTokens = promptTokens + completionTokens;

        // 9. Cost calculations using decimal arithmetic for the model actually used
        var dbModel = await _dbContext.AiModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == executionResult.ModelId);

        if (dbModel == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = new
                {
                    message = "Successful model failed to load from database for cost mapping.",
                    type = "model_resolution_error",
                    code = "500"
                }
            });
        }

        var inputCost = _costEngineService.CalculateCost(promptTokens, dbModel.InputTokenPricePerMillion);
        var outputCost = _costEngineService.CalculateCost(completionTokens, dbModel.OutputTokenPricePerMillion);
        var estimatedCost = inputCost + outputCost;

        // 10. Update active budget spends
        await _budgetService.UpdateBudgetSpendAsync(_requestContext.TenantId, _requestContext.ClientApplicationId, _requestContext.ApiKeyId, dbModel.Id, estimatedCost);

        var response = new ChatCompletionResponse
        {
            Id = $"chatcmpl_{Guid.NewGuid():N}",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = $"routed:{dbModel.Name}",
            Choices = new()
            {
                new()
                {
                    Index = 0,
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = executionResult.ResponseText
                    },
                    FinishReason = "stop"
                }
            },
            Usage = new UsageInfo
            {
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens
            },
            Routing = new RoutingInfo
            {
                SelectedTier = targetTier.ToString().ToLowerInvariant(),
                SelectedProvider = dbModel.Provider.Name,
                SelectedModel = dbModel.Name,
                MatchedRule = matchedRuleName,
                EstimatedCost = estimatedCost,
                FallbackUsed = executionResult.FallbackUsed,
                CacheHit = false,
                BudgetStatus = finalBudgetStatus,
                Warning = warningMessage
            }
        };

        // 11. Write log to request logs history
        await LogRequestAsync(request, dbModel, "Success", matchedRuleName, promptTokens, completionTokens, estimatedCost, executionResult.ResponseText, finalBudgetStatus, executionResult.FallbackUsed);

        return Ok(response);
    }

    private async Task LogRequestAsync(
        ChatCompletionRequest request,
        AiModel? model,
        string requestStatus,
        string matchedRuleName,
        int inputTokens,
        int outputTokens,
        decimal cost,
        string? responseText = null,
        string budgetStatus = "Within Limits",
        bool fallbackUsed = false)
    {
        var promptHash = ComputeHash(string.Join("|", request.Messages.Select(m => $"{m.Role}:{m.Content}")));
        var responseHash = responseText != null ? ComputeHash(responseText) : ComputeHash("");

        var requestLog = new AiRequestLog
        {
            CorrelationId = _requestContext.CorrelationId,
            RequestId = $"chatcmpl_mock_{Guid.NewGuid():N}",
            TenantId = _requestContext.TenantId,
            ApplicationId = _requestContext.ClientApplicationId,
            ApiKeyId = _requestContext.ApiKeyId,
            PromptHash = promptHash,
            ResponseHash = responseHash,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            EstimatedCost = cost,
            SelectedProvider = model?.Provider?.Name ?? "None",
            SelectedModel = model?.Name ?? "None",
            SelectedTier = model?.Tier.ToString() ?? "None",
            MatchedRuleName = matchedRuleName,
            FallbackUsed = fallbackUsed,
            BudgetStatus = budgetStatus,
            RequestStatus = requestStatus,
            LatencyMs = 45
        };

        _dbContext.AiRequestLogs.Add(requestLog);
        await _dbContext.SaveChangesAsync();
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
