# Budget Enforcement

TokenShield AI Gateway enforces enterprise spending controls and prevents unchecked LLM provider costs by evaluating budget limits in real time.

---

## Budget Scopes

Monthly budgets can be configured at multiple levels:

1. **Tenant**: A top-level budget representing the total spend limit across the entire organization.
2. **Client Application**: Budgets assigned to specific client applications (e.g. Fraud Detection App, Support Bot).
3. **API Key**: Budgets assigned to individual API credentials.
4. **Model**: Budgets assigned to restrict spending on specific concrete AI models (e.g., GPT-4o, Claude 3.5 Sonnet).

---

## Enforcement Actions

When a budget limit is reached or exceeded, one of the following actions is executed:

| Action | Behavior |
| :--- | :--- |
| `WarnOnly` | The request is allowed to proceed, but warning metadata is returned in the API response and written to telemetry logs. |
| `Block` | The request is rejected immediately with HTTP `403 Forbidden` and a clear error payload. No provider calls are made. |
| `Downgrade` | The target model tier is dynamically downgraded to a cheaper tier (e.g. `Premium` -> `Standard` -> `Cheap`), avoiding expensive model costs. |

---

## Warning Thresholds

Each budget limit defines a `WarningThresholdPercent` (e.g. `80.00%`). Once the `CurrentSpend` exceeds this percentage of the `MonthlyLimit`, any API response processing through the gateway will include:
- `budgetStatus = "Warning"` or `"Downgraded"` in the `routing` metadata block.
- A descriptive warning message in the `warning` response property.

---

## Dynamic Month Reset

To avoid complex cron job schedulers, budgets are reset dynamically. Every time a budget limit is loaded, the gateway checks if `LastResetAtUtc` belongs to a calendar month prior to the current system date. If so:
1. `CurrentSpend` is reset to `0.00`.
2. `LastResetAtUtc` is updated to the start of the current calendar month.
3. The reset state is saved back to the database.
