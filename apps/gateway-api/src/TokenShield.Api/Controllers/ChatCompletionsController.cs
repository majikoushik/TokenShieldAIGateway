using Microsoft.AspNetCore.Mvc;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.Application.Validators;
using TokenShield.Domain.Entities;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers;

[ApiController]
[Route("v1/chat/completions")]
public class ChatCompletionsController : ControllerBase
{
    private readonly TokenShieldDbContext _dbContext;
    private readonly IRequestContext _requestContext;

    public ChatCompletionsController(TokenShieldDbContext dbContext, IRequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatCompletionRequest request)
    {
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

        // Determine routed model name based on simple mock rules for now
        var requestedModel = request.Model;
        var selectedModel = requestedModel == "auto" ? "mock-cheap" : requestedModel;
        var selectedProvider = "Mock Provider";
        var selectedTier = "cheap";
        var matchedRule = "Default Routing";

        if (requestedModel.Contains("standard"))
        {
            selectedModel = "mock-standard";
            selectedProvider = "Mock Provider";
            selectedTier = "standard";
        }
        else if (requestedModel.Contains("premium") || requestedModel.Contains("o1") || requestedModel.Contains("opus"))
        {
            selectedModel = "mock-premium";
            selectedProvider = "Mock Provider";
            selectedTier = "premium";
        }

        // Estimate tokens
        var inputChars = request.Messages.Sum(m => (m.Content?.Length ?? 0) + (m.Role?.Length ?? 0));
        var promptTokens = (int)Math.Ceiling(inputChars / 4.0);
        if (promptTokens == 0) promptTokens = 5; // Minimum fallback tokens

        var responseText = "Hello! I am a simulated response from TokenShield AI Gateway core slice. Your request passed validation, authentication, and routing checks successfully.";
        var completionTokens = (int)Math.Ceiling(responseText.Length / 4.0);
        var totalTokens = promptTokens + completionTokens;

        // Estimated Cost (Cheap tier pricing: $0.10 input per M, $0.20 output per M)
        var inputCost = (promptTokens / 1000000.0m) * 0.10m;
        var outputCost = (completionTokens / 1000000.0m) * 0.20m;
        var estimatedCost = inputCost + outputCost;

        var response = new ChatCompletionResponse
        {
            Id = $"chatcmpl_mock_{Guid.NewGuid():N}",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = $"routed:{selectedModel}",
            Choices = new()
            {
                new()
                {
                    Index = 0,
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = responseText
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
                SelectedTier = selectedTier,
                SelectedProvider = selectedProvider,
                SelectedModel = selectedModel,
                MatchedRule = matchedRule,
                EstimatedCost = estimatedCost,
                FallbackUsed = false,
                CacheHit = false
            }
        };

        // Telemetry logging (NEVER save raw prompt/response content)
        var promptHash = ComputeHash(string.Join("|", request.Messages.Select(m => $"{m.Role}:{m.Content}")));
        var responseHash = ComputeHash(responseText);

        var requestLog = new AiRequestLog
        {
            CorrelationId = _requestContext.CorrelationId,
            RequestId = response.Id,
            TenantId = _requestContext.TenantId,
            ApplicationId = _requestContext.ClientApplicationId,
            ApiKeyId = _requestContext.ApiKeyId,
            PromptHash = promptHash,
            ResponseHash = responseHash,
            InputTokens = promptTokens,
            OutputTokens = completionTokens,
            EstimatedCost = estimatedCost,
            SelectedProvider = selectedProvider,
            SelectedModel = selectedModel,
            SelectedTier = selectedTier,
            MatchedRuleName = matchedRule,
            FallbackUsed = false,
            BudgetStatus = "Within Limits",
            RequestStatus = "Success",
            LatencyMs = 45 // Mock proxy latency
        };

        _dbContext.AiRequestLogs.Add(requestLog);
        await _dbContext.SaveChangesAsync();

        return Ok(response);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
