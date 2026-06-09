# TokenShield AI Gateway Architecture

## 1. System Topology
TokenShield AI Gateway acts as a reverse proxy sitting between client applications and downstream AI models.

```text
       ┌──────────────────────┐
       │  Client Application  │
       └──────────┬───────────┘
                  │ HTTPS POST /v1/chat/completions (x-api-key)
                  ▼
       ┌──────────────────────┐
       │TokenShield Gateway   │
       │ (ASP.NET Core API)   │
       └────┬────────────┬────┘
            │            │
            │ EF Core    │ HTTPS REST
            ▼            ▼
   ┌───────────┐    ┌───────────────────────────────────┐
   │PostgreSQL │    │ Downstream AI Providers           │
   │ Database  │    │ (Azure OpenAI / OpenAI / Anthropic)│
   └───────────┘    └───────────────────────────────────┘
```

---

## 2. Backend Clean Architecture
The backend is structured into domain-isolated layers, conforming to Domain-Driven Design (DDD) principles.

```text
   TokenShield.Api (HTTP endpoint handlers, middleware, swagger)
          │
          ▼
   TokenShield.Application (Use cases, orchestrators, interface definitions)
    ┌─────┴──────────────────────────────────────────────────────┐
    │                                                            │
    ▼                                                            ▼
   TokenShield.Domain (Entities, value objects, core exceptions)  TokenShield.ProviderAdapters
    ▲                                                            (SDK adapters for models)
    │
    ├─ TokenShield.Infrastructure (EF Core DbContext, Migrations)
    ├─ TokenShield.PolicyEngine (Rule-matching evaluations)
    ├─ TokenShield.CostEngine (Token estimators, cost engine helpers)
    ├─ TokenShield.Guardrails (PII and prompt checks)
    └─ TokenShield.Observability (Telemetry tracking, Serilog setup)
```

### Layer Roles and Responsibilities
- **TokenShield.Domain**: Defines entities like `Tenant`, `ClientApplication`, `ApiKey`, `ModelProvider`, `AiModel`, `RoutingRule`, `BudgetLimit`, and `AiRequestLog`.
- **TokenShield.Application**: Orchestrates request processing. Coordinates budget checking, profiling, rule evaluation, provider invocation, and logging.
- **TokenShield.Infrastructure**: Connects application layer logic with PostgreSQL database storage using EF Core. Contains configuration configurations and database seed utilities.
- **TokenShield.ProviderAdapters**: Encapsulates external vendor REST clients (OpenAI client, Azure OpenAI client, Anthropic client, MockProvider).
- **TokenShield.PolicyEngine**: Runs Boolean evaluation matrices against incoming requests. Matches inputs (e.g. `riskLevel`, `complexityScore`) to selected routing actions.
- **TokenShield.CostEngine**: Simple lightweight token estimations. Implements the character-to-token converter logic ($1 \text{ token} \approx 4 \text{ characters}$).
- **TokenShield.Guardrails**: Validates requests before executing third-party API calls. Implements simple regex-based PII masking.
- **TokenShield.Observability**: Standardizes correlation contexts across execution flows. Generates OpenTelemetry-compliant metrics and audit trail events.

---

## 3. Database Schema Concept
All database tables utilize unique `Guid` values as Primary Keys and track creation and update dates in UTC. 
Crucial entities:
1. **Tenants**: Enforces tenant-isolation (`TenantId`).
2. **ClientApplications**: Mapped to a specific tenant.
3. **ApiKeys**: Store SHA-256 hashed keys. Raw keys are shown only once at generation time.
4. **ModelProviders**: Registers Azure OpenAI, OpenAI, and Anthropic.
5. **AiModels**: Defines deploy name, tier (`cheap`/`standard`/`premium`), input and output token pricing (decimal per million tokens).
6. **RoutingRules**: Match rules using operators like `Equals`, `GreaterThan`, etc.
7. **BudgetLimits**: Tracks monthly allocation, warn/hard thresholds, and current usage.
8. **AiRequestLogs**: Privacy-preserving logs containing execution metadata, latency, rule matches, and cryptographic prompt/response hashes.

---

## 4. Frontend Design Concept
The frontend is a single-page administration app scaffolded using Next.js.
- **Path**: `apps/web-admin/`
- **Engine**: App Router
- **Styling**: Tailwind CSS
- **Features**:
  - Left navigation sidebar providing quick access to all CRUD views.
  - Interactive grid metrics cards showing current tenant monthly budgets.
  - Sleek dark theme palette with consistent modern typography (e.g. Inter).
  - Integrates React Hook Form + Zod for forms and validations.
