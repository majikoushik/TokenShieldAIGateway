# Observability

## Overview

TokenShield uses a layered observability stack:

| Layer | Tool | Purpose |
|---|---|---|
| Structured logging | Serilog | Human-readable + machine-parseable logs |
| Distributed tracing | OpenTelemetry | End-to-end request tracing |
| Metrics | OpenTelemetry | Request counts, latency, token usage |
| Cloud monitoring | Azure Monitor / Application Insights | Dashboard, alerts, retention |

---

## Correlation IDs

Every request is assigned a `CorrelationId` by the `CorrelationIdMiddleware`.

- If `x-correlation-id` is present in the request and is a valid GUID, it is used.
- Otherwise, a new GUID is generated.
- The correlation ID is returned in the `x-correlation-id` response header.
- All structured log entries include `CorrelationId` as a property.

---

## Structured Log Fields

Every gateway AI request produces log entries enriched with:

| Field | Type | Description |
|---|---|---|
| `CorrelationId` | GUID | Ties all logs for one HTTP request together |
| `TenantId` | GUID | Tenant that owns the API key |
| `ApplicationId` | GUID | Client application that sent the request |
| `SelectedProvider` | string | Provider used (e.g. `Azure OpenAI`) |
| `SelectedModel` | string | Model name (e.g. `gpt-4o-mini`) |
| `SelectedTier` | string | `cheap`, `standard`, or `premium` |
| `MatchedRule` | string | Name of the routing rule that matched |
| `InputTokens` | int | Estimated prompt tokens |
| `OutputTokens` | int | Actual completion tokens |
| `EstimatedCost` | decimal | Estimated cost in USD |
| `LatencyMs` | long | End-to-end gateway latency |
| `FallbackUsed` | bool | Whether a fallback model was used |
| `BudgetStatus` | string | `Within Limits`, `Warning`, `Downgraded`, `Exceeded` |
| `RequestStatus` | string | `Success`, `Blocked`, `Failed`, `HumanReviewRequired` |

**Privacy rule**: raw prompt content and raw response content are **never** logged.
Prompt and response are stored only as SHA-256 hashes in the `AiRequestLog` table.

---

## Custom Telemetry Events

`ITelemetryService` emits 6 structured events at key lifecycle points:

| Event | When Emitted |
|---|---|
| `AiRequestReceived` | Request parsed and token estimation complete |
| `AiRoutingDecisionMade` | Routing rule matched and tier selected |
| `AiModelCalled` | About to call the provider adapter |
| `AiFallbackTriggered` | Fallback to alternative model/tier triggered |
| `AiBudgetExceeded` | Pre-call budget check determined budget is exceeded |
| `AiResponseReturned` | Final response returned to client |

These are emitted via `ILogger` with structured properties. They flow to:
- Console in local development
- Application Insights when a connection string is configured

---

## OpenTelemetry

`ObservabilityExtensions.AddTokenShieldObservability()` wires:

- **Traces**: ASP.NET Core request tracing + outbound HTTP calls (provider requests)
- **Metrics**: Request count, latency, error rates from ASP.NET Core and HTTP client
- **Logs bridge**: Forwards ILogger entries to OTel log pipeline

Health and version endpoints (`/health`, `/health/ready`, `/api/version`) are excluded from traces to reduce noise.

---

## Application Insights Setup

To enable Application Insights:

1. Deploy the Azure infrastructure (see `docs/deployment.md`).
2. Copy the Application Insights connection string from the Azure portal (or Bicep output).
3. Set the `ApplicationInsights:ConnectionString` environment variable on the gateway Container App.

```bash
az containerapp update \
  --name <gateway-app-name> \
  --resource-group <resource-group> \
  --set-env-vars "ApplicationInsights__ConnectionString=InstrumentationKey=xxx;..."
```

When the connection string is set, traces, metrics, and logs automatically flow to Application Insights. No code change is required.

---

## Health Endpoints

| Endpoint | Auth | Purpose |
|---|---|---|
| `GET /health` | Public | Liveness probe - confirms process is alive |
| `GET /health/ready` | Public | Readiness probe - confirms database connectivity |

The readiness endpoint returns HTTP 200 when the database check passes, HTTP 503 otherwise.

---

## Log Verbosity

Configured in `appsettings.json`:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

In production, consider setting `Default` to `Warning` and rely on Application Insights for `Information` event capture.
