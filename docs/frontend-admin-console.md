# TokenShield Admin Console Frontend Documentation

The TokenShield Admin Console is a modern SaaS portal designed for operators, administrators, and FinOps engineers. It provides centralized controls to trace LLM cost, configure routing rules, manage budgets, generate developer credentials, and audit all gateway transactions.

## Technology Stack

- **Framework**: Next.js (with React 19 and React Server Components vs. Client Components layout separation)
- **Styling**: Tailwind CSS v4 with curated custom variables defining a premium dark-themed color system
- **State Management & Server Queries**: TanStack React Query (`@tanstack/react-query`) for cached backend queries, mutations, and optimistic updates
- **Telemetry Visualizations**: Recharts for responsive, animated dashboards
- **Forms Validation**: Zod (`zod`) for client-side schemas validation

---

## Workspace Navigation & Pages

### 1. Dashboard (`/dashboard`)
- **KPI Metrics**: Monthly estimated spend, proxy requests count, gateway latency index, and estimated tokens distribution (input vs. output tokens).
- **Spending Charts**: Animated horizontal and vertical bar charts indicating costs by Provider, Cost Tier, and Model.
- **Proxy Status Breakdown**: Pie charts illustrating requests success vs. blocked/failure rates, and budget threshold status counts (Within Limits, Warning, Exceeded).
- **Live Logs Feed**: A table showing the 5 most recent requests routed through the proxy with instant metrics.

### 2. Model Providers (`/providers`)
- Cards grid displaying the integration URLs and Azure Key Vault secret names of external model providers (e.g., Azure OpenAI, Anthropic, OpenAI).
- Form inputs validate endpoints format and enforce credential secret name isolation.

### 3. Models Catalog (`/models`)
- Interactive list showcasing model tiers mapping (Cheap, Standard, Premium).
- Enforces price-per-million token entries checks and context windows boundaries before registering.

### 4. Routing Policy Rules (`/routing-rules`)
- Displays prioritized rule configurations (matching field operators like `taskType`, `riskLevel`, `complexityScore`, `containsPii`).
- Features a **Rule Simulator Panel** allowing administrators to submit sample JSON metadata signals to test routing behaviors.

### 5. Budget Limits (`/budgets`)
- Renders cards displaying monthly budgets mapped to scopes (Tenant overall, Application, API Key, Model).
- Draws custom progress bars with dual markers indicating both the warning threshold and 100% hard limit.
- Color codes progress based on spend limits: Green (< Warning), Amber (>= Warning but < 100%), Red (>= 100%).

### 6. API Access Keys (`/api-keys`)
- Lists active client developer credentials with inline revocation switches.
- **Secure Key Generation**: Shows the raw generated API key precisely **once** in an alert banner with warnings and clipboard copy utilities.

### 7. Proxy Request Logs (`/usage-logs`)
- Provides paginated transaction queries with granular filters for Client Application, Provider, Model, Tier, and request status.
- Inspecting a row displays a panel highlighting prompt/response hashes and gateway routing metadata.

### 8. System Audit Logs (`/audit-logs`)
- Logs config mutations. Rows expand to show formatted JSON before/after state diffs.

### 9. Settings (`/settings`)
- Mappings for tenant workspace credentials, registering developer applications, and checking Azure Key Vault connection health.

---

## API Layer & Mock Fallback

The API client helper is defined in `lib/api.ts`.

### Automatic Mock Failover
If the backend Web API is offline or returns connection errors:
1. `lib/api.ts` outputs a console warning: `API call to /api/admin/... failed, falling back to mock data`.
2. The UI falls back to high-fidelity mock seed data (`MOCK_DATA`) so the portal remains operational for reviews and local validations.
3. This allows seamless local development when the gateway engine is not running locally.
