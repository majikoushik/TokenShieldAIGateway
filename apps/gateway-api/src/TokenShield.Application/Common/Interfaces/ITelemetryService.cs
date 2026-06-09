namespace TokenShield.Application.Common.Interfaces;

/// <summary>
/// Emits structured telemetry events for gateway request lifecycle.
/// Wraps ILogger and is ready for Application Insights custom events via OpenTelemetry.
/// </summary>
public interface ITelemetryService
{
    /// <summary>Emitted when a gateway AI request is first received and parsed.</summary>
    void TrackAiRequestReceived(AiRequestTelemetry telemetry);

    /// <summary>Emitted after the routing engine makes a routing decision.</summary>
    void TrackRoutingDecisionMade(AiRequestTelemetry telemetry);

    /// <summary>Emitted just before the provider adapter is called.</summary>
    void TrackAiModelCalled(AiRequestTelemetry telemetry);

    /// <summary>Emitted when fallback to an alternative model/tier is triggered.</summary>
    void TrackFallbackTriggered(AiRequestTelemetry telemetry);

    /// <summary>Emitted when a budget limit is exceeded for a request.</summary>
    void TrackBudgetExceeded(AiRequestTelemetry telemetry);

    /// <summary>Emitted when the final response is returned to the client.</summary>
    void TrackAiResponseReturned(AiRequestTelemetry telemetry);
}

/// <summary>
/// Structured telemetry payload for gateway AI request events.
/// All sensitive fields (prompt, response content) are excluded by design.
/// </summary>
public record AiRequestTelemetry
{
    public Guid CorrelationId { get; init; }
    public Guid RequestId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ApplicationId { get; init; }
    public string SelectedProvider { get; init; } = string.Empty;
    public string SelectedModel { get; init; } = string.Empty;
    public string SelectedTier { get; init; } = string.Empty;
    public string MatchedRule { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public decimal EstimatedCost { get; init; }
    public long LatencyMs { get; init; }
    public bool FallbackUsed { get; init; }
    public string BudgetStatus { get; init; } = "Within Limits";
    public string RequestStatus { get; init; } = "Pending";
}
