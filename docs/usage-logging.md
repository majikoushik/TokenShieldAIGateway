# Privacy-Preserving Usage Logging

TokenShield AI Gateway logs every AI transaction through the gateway to support cost analysis and auditing. However, to maintain absolute user privacy and data security, raw prompt and response contents are never stored in the database.

---

## Logging Invariants

1. **No Raw Content Storage**: Prompts, messages, reasoning chains, and completion responses are excluded from request database records.
2. **Deterministic Hashing**: The content of prompts and responses is hashed using SHA-256 and stored as a hexadecimal hash string.
3. **No Key Storage**: Hashed keys or metadata parameters are kept, but raw keys are never written to any logs.

---

## Log Fields

Each entry in the `AiRequestLogs` table captures:

- **CorrelationId**: Unique transaction ID.
- **RequestId**: Unique chat completion request ID.
- **TenantId**: ID of the organization making the request.
- **ApplicationId**: ID of the client application.
- **ApiKeyId**: ID of the API key used.
- **PromptHash**: SHA-256 hash of the combined request messages.
- **ResponseHash**: SHA-256 hash of the completion response text.
- **InputTokens**: Simulated or actual count of prompt tokens.
- **OutputTokens**: Simulated or actual count of response completion tokens.
- **EstimatedCost**: Cost calculated using decimal model pricing.
- **SelectedProvider / Model / Tier**: Provider (e.g. OpenAI), Model (e.g. gpt-4o-mini), and resolved tier (e.g. cheap).
- **MatchedRuleName**: Name of the matched routing policy rule (if any).
- **FallbackUsed**: Boolean indicating if a provider fallback occurred.
- **BudgetStatus**: The status of the budgets during checking (e.g. `"Within Limits"`, `"Warning"`, `"Exceeded"`, `"Downgraded"`).
- **RequestStatus**: Outcome of the request (`"Success"`, `"Failed"`, `"Blocked"`).
- **LatencyMs**: Processing time in milliseconds.
- **CreatedAtUtc**: Timestamp of the transaction.
