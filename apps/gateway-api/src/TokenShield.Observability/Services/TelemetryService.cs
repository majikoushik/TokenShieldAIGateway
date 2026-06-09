using Microsoft.Extensions.Logging;
using TokenShield.Application.Common.Interfaces;

namespace TokenShield.Observability.Services;

/// <summary>
/// Structured telemetry service for TokenShield gateway events.
///
/// Emits the 6 required custom events defined in AGENTS.md:
///   AiRequestReceived, AiRoutingDecisionMade, AiModelCalled,
///   AiFallbackTriggered, AiBudgetExceeded, AiResponseReturned
///
/// Uses ILogger with structured properties so events are captured by
/// Serilog, OpenTelemetry, and Application Insights exporters without
/// any code change. Raw prompt/response content is never logged.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
    }

    public void TrackAiRequestReceived(AiRequestTelemetry t)
    {
        _logger.LogInformation(
            "[AiRequestReceived] CorrelationId={CorrelationId} RequestId={RequestId} " +
            "TenantId={TenantId} ApplicationId={ApplicationId} " +
            "InputTokens={InputTokens} BudgetStatus={BudgetStatus}",
            t.CorrelationId, t.RequestId, t.TenantId, t.ApplicationId,
            t.InputTokens, t.BudgetStatus);
    }

    public void TrackRoutingDecisionMade(AiRequestTelemetry t)
    {
        _logger.LogInformation(
            "[AiRoutingDecisionMade] CorrelationId={CorrelationId} TenantId={TenantId} " +
            "SelectedTier={SelectedTier} MatchedRule={MatchedRule}",
            t.CorrelationId, t.TenantId, t.SelectedTier, t.MatchedRule);
    }

    public void TrackAiModelCalled(AiRequestTelemetry t)
    {
        _logger.LogInformation(
            "[AiModelCalled] CorrelationId={CorrelationId} TenantId={TenantId} " +
            "SelectedProvider={SelectedProvider} SelectedModel={SelectedModel} " +
            "SelectedTier={SelectedTier}",
            t.CorrelationId, t.TenantId, t.SelectedProvider, t.SelectedModel, t.SelectedTier);
    }

    public void TrackFallbackTriggered(AiRequestTelemetry t)
    {
        _logger.LogWarning(
            "[AiFallbackTriggered] CorrelationId={CorrelationId} TenantId={TenantId} " +
            "SelectedProvider={SelectedProvider} SelectedModel={SelectedModel} " +
            "SelectedTier={SelectedTier}",
            t.CorrelationId, t.TenantId, t.SelectedProvider, t.SelectedModel, t.SelectedTier);
    }

    public void TrackBudgetExceeded(AiRequestTelemetry t)
    {
        _logger.LogWarning(
            "[AiBudgetExceeded] CorrelationId={CorrelationId} TenantId={TenantId} " +
            "ApplicationId={ApplicationId} BudgetStatus={BudgetStatus} " +
            "EstimatedCost={EstimatedCost}",
            t.CorrelationId, t.TenantId, t.ApplicationId, t.BudgetStatus, t.EstimatedCost);
    }

    public void TrackAiResponseReturned(AiRequestTelemetry t)
    {
        _logger.LogInformation(
            "[AiResponseReturned] CorrelationId={CorrelationId} TenantId={TenantId} " +
            "SelectedProvider={SelectedProvider} SelectedModel={SelectedModel} " +
            "SelectedTier={SelectedTier} MatchedRule={MatchedRule} " +
            "InputTokens={InputTokens} OutputTokens={OutputTokens} " +
            "EstimatedCost={EstimatedCost} LatencyMs={LatencyMs} " +
            "FallbackUsed={FallbackUsed} BudgetStatus={BudgetStatus} " +
            "RequestStatus={RequestStatus}",
            t.CorrelationId, t.TenantId, t.SelectedProvider, t.SelectedModel, t.SelectedTier,
            t.MatchedRule, t.InputTokens, t.OutputTokens, t.EstimatedCost, t.LatencyMs,
            t.FallbackUsed, t.BudgetStatus, t.RequestStatus);
    }
}
