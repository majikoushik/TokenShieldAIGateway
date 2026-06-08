# AGENTS.md

## Project Name

**TokenShield AI Gateway**

## Product Vision

TokenShield AI Gateway is a production-grade AI FinOps and model-routing platform for enterprises.

The product helps companies control LLM cost, improve reliability, enforce AI usage policies, and govern model access by routing every AI request through a centralized gateway.

The gateway sits between enterprise applications and AI model providers.

```text
Client Application
        |
        v
TokenShield AI Gateway
        |
        |-- Cheap model
        |-- Standard model
        |-- Premium model
        |-- Human review
        |-- Blocked request
```

The MVP must focus on:

1. OpenAI-compatible AI gateway API
2. Model catalog
3. Three model tiers: cheap, standard, premium
4. Rule-based model routing
5. Budget enforcement
6. Token and cost calculation
7. Provider adapters for Azure OpenAI, OpenAI, and Anthropic
8. Admin console
9. Usage dashboard
10. API key management
11. Audit logs
12. Azure-ready deployment

---

## Technology Stack

Use the following stack unless explicitly instructed otherwise.

### Frontend

- Next.js
- TypeScript
- Tailwind CSS
- shadcn/ui
- TanStack Query
- TanStack Table
- React Hook Form
- Zod
- Recharts or Apache ECharts for charts

### Backend

- .NET 8 Web API
- C#
- ASP.NET Core Controllers
- Entity Framework Core
- PostgreSQL
- FluentValidation
- Serilog
- OpenTelemetry
- Application Insights
- Polly for retries and fallback
- Swagger / OpenAPI

### Database

- PostgreSQL
- Use EF Core migrations
- Use JSONB-compatible columns where needed
- Use `Guid` primary keys
- Use `CreatedAt` and `UpdatedAt`
- Use soft delete where appropriate

### Azure Infrastructure

Target infrastructure:

- Azure Container Apps
- Azure API Management
- Azure Database for PostgreSQL Flexible Server
- Azure Key Vault
- Azure Application Insights
- Azure Monitor
- Log Analytics Workspace
- Azure Container Registry
- Azure Cache for Redis later
- Azure Service Bus later

### Local Development

Use Docker Compose for local development.

Local services should include:

- PostgreSQL
- gateway-api
- web-admin

Redis and Service Bus can be added later.

---

## Repository Structure

Use this structure:

```text
/
├── apps/
│   ├── gateway-api/
│   └── web-admin/
│
├── docs/
│   ├── product-spec.md
│   ├── architecture.md
│   ├── database-schema.md
│   ├── routing-rules.md
│   ├── cost-engine.md
│   ├── provider-azure-openai.md
│   ├── provider-openai.md
│   ├── provider-anthropic.md
│   └── observability.md
│
├── infra/
│   ├── bicep/
│   ├── docker/
│   └── README.md
│
├── scripts/
│
├── AGENTS.md
└── README.md
```

---

## Backend Architecture

The backend should follow Clean Architecture principles.

Recommended backend structure:

```text
apps/gateway-api/
├── src/
│   ├── TokenShield.Api/
│   ├── TokenShield.Domain/
│   ├── TokenShield.Application/
│   ├── TokenShield.Infrastructure/
│   ├── TokenShield.ProviderAdapters/
│   ├── TokenShield.PolicyEngine/
│   ├── TokenShield.CostEngine/
│   ├── TokenShield.Guardrails/
│   └── TokenShield.Observability/
│
└── tests/
    ├── TokenShield.UnitTests/
    └── TokenShield.IntegrationTests/
```

### Layer Responsibilities

#### TokenShield.Api

Responsible for:

- Controllers
- Middleware
- API key authentication
- Request validation
- Swagger
- Health checks
- Dependency injection setup

#### TokenShield.Domain

Responsible for:

- Domain entities
- Value objects
- Domain constants
- Enums
- Core business rules

#### TokenShield.Application

Responsible for:

- Use cases
- DTOs
- Interfaces
- Request profiling
- Routing orchestration
- Budget orchestration
- Application services

#### TokenShield.Infrastructure

Responsible for:

- EF Core DbContext
- EF Core configurations
- Repository implementations if needed
- PostgreSQL integration
- Key Vault integration later
- Application settings

#### TokenShield.ProviderAdapters

Responsible for:

- Azure OpenAI adapter
- OpenAI adapter
- Anthropic adapter
- Mock provider adapter
- Provider adapter factory

#### TokenShield.PolicyEngine

Responsible for:

- Routing rule evaluation
- Rule matching
- Condition parsing
- Action selection

#### TokenShield.CostEngine

Responsible for:

- Token estimation
- Token usage calculation
- Cost calculation
- Budget checks

#### TokenShield.Guardrails

Responsible for:

- PII detection
- Secret detection
- Prompt safety checks
- Future prompt injection checks

#### TokenShield.Observability

Responsible for:

- Structured logging
- Audit logging
- Metrics
- Tracing
- Application Insights events

---

## Frontend Architecture

The frontend should be a modern SaaS admin console.

Recommended structure:

```text
apps/web-admin/
├── app/
├── components/
│   ├── layout/
│   ├── dashboard/
│   ├── providers/
│   ├── models/
│   ├── routing-rules/
│   ├── budgets/
│   ├── api-keys/
│   ├── usage-logs/
│   └── shared/
│
├── lib/
│   ├── api/
│   ├── auth/
│   ├── constants/
│   └── utils/
│
├── hooks/
├── types/
└── styles/
```

Frontend pages required for MVP:

1. Dashboard
2. Providers
3. Models
4. Routing Rules
5. Budgets
6. Usage Logs
7. API Keys
8. Audit Logs
9. Settings

---

## Core Product Concepts

### Tenant

A company or organization using TokenShield.

### Client Application

An application owned by a tenant that sends AI requests through the gateway.

Example:

```text
Fraud Investigation App
Customer Support Bot
Internal Developer Assistant
```

### Provider

An AI provider.

Examples:

```text
Azure OpenAI
OpenAI
Anthropic
Google Gemini
AWS Bedrock
Ollama
```

For the MVP, implement:

1. Azure OpenAI
2. OpenAI
3. Anthropic
4. Mock provider

### AI Model

A model registered under a provider.

Example:

```text
gpt-mini
gpt-standard
gpt-premium
claude-sonnet
claude-opus
```

### Model Tier

A logical cost/quality tier.

Required MVP tiers:

```text
cheap
standard
premium
```

### Routing Rule

A tenant-defined rule that decides which tier or action should be used.

Example:

```text
IF taskType = summarization
AND riskLevel = low
THEN routeToTier = cheap
```

### Budget Limit

A monthly spend limit for an application, API key, model, or tenant.

### AI Request Log

A record of every AI request processed through the gateway.

Do not store raw prompt or raw response by default.

Store:

- Prompt hash
- Response hash
- Token counts
- Estimated cost
- Selected model
- Selected provider
- Matched rule
- Fallback status
- Budget status
- Latency
- Timestamp

---

## API Design Requirements

### Gateway Endpoint

The main gateway endpoint must be OpenAI-compatible.

```http
POST /v1/chat/completions
```

The request should support:

```json
{
  "model": "auto",
  "messages": [
    {
      "role": "user",
      "content": "Summarize this case"
    }
  ],
  "temperature": 0.2,
  "max_tokens": 1000,
  "stream": false,
  "metadata": {
    "taskType": "summarization",
    "riskLevel": "low",
    "department": "fraud",
    "environment": "prod"
  }
}
```

For MVP:

- Support `stream: false` only.
- Return a clear validation error if `stream: true`.
- Support `model: "auto"` for routed mode.
- Support explicit model name later.

The response should follow OpenAI-compatible shape and include routing metadata.

```json
{
  "id": "chatcmpl_xxx",
  "object": "chat.completion",
  "created": 1730000000,
  "model": "routed:gpt-mini",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Response text"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 1200,
    "completion_tokens": 200,
    "total_tokens": 1400
  },
  "routing": {
    "selectedTier": "cheap",
    "selectedProvider": "Azure OpenAI",
    "selectedModel": "gpt-mini",
    "matchedRule": "Low-risk summarization",
    "estimatedCost": 0.0021,
    "fallbackUsed": false,
    "cacheHit": false
  }
}
```

---

## API Key Authentication

Client applications must authenticate using:

```http
x-api-key: ts_live_xxxxxx
```

Rules:

1. Never store raw API keys.
2. Store only hashed API keys.
3. Show raw API key only once during creation.
4. Resolve `TenantId` and `ApplicationId` from API key.
5. Reject missing or invalid API keys with `401 Unauthorized`.
6. Add `LastUsedAt` when a key is used.
7. Add API key status: active, revoked, expired.

---

## Admin API Requirements

Create admin APIs for:

1. Providers
2. Models
3. Model tiers
4. Routing rules
5. Budgets
6. API keys
7. Usage analytics
8. Audit logs

For MVP, admin authentication may be a clearly marked placeholder, but the code must be structured so Microsoft Entra ID can be added later.

All admin mutation endpoints must write audit logs.

Mutation means:

- Create
- Update
- Delete
- Enable
- Disable
- Revoke
- Publish

---

## Routing Engine Requirements

The routing engine must be rule-based for MVP.

Execution order:

1. Authenticate request.
2. Estimate input tokens.
3. Build request profile.
4. Check PII placeholder.
5. Check budget.
6. Match routing rules.
7. Select model tier.
8. Select model.
9. Call provider.
10. Calculate final cost.
11. Log request.
12. Return response.

### Supported Rule Conditions

Implement these condition fields:

```text
taskType
riskLevel
inputTokens
complexityScore
containsPii
department
environment
```

Supported operators:

```text
equals
notEquals
greaterThan
lessThan
greaterThanOrEquals
lessThanOrEquals
```

### Supported Rule Actions

```text
routeToTier
humanReview
block
```

### Default Behavior

If no rule matches:

```text
routeToTier = standard
```

If `riskLevel = high`:

```text
humanReview
```

unless an explicit approved rule allows routing.

---

## Request Profiling Requirements

Create a request profile for every AI request.

Fields:

```text
TaskType
RiskLevel
InputTokens
EstimatedOutputTokens
RequiresReasoning
RequiresStructuredOutput
ContainsPii
ComplexityScore
Department
Environment
```

Rules:

1. Prefer metadata values when provided.
2. If `taskType` is missing, infer using keyword rules.
3. If `riskLevel` is missing, default to `medium`.
4. Detect simple PII using regex:
   - email address
   - phone-like number
5. Complexity score:
   - base 20
   - +20 if input tokens > 4000
   - +30 if requiresReasoning = true
   - +20 if taskType = complex_reasoning
   - cap at 100

---

## Cost Engine Requirements

Create:

```text
ITokenEstimator
ICostCalculator
IBudgetService
```

### Token Estimation

For MVP, use approximation:

```text
1 token ≈ 4 characters
```

Count all message content.

### Cost Calculation

Model prices should be stored as:

```text
InputTokenPricePerMillion
OutputTokenPricePerMillion
```

Estimated cost:

```text
inputCost = inputTokens / 1,000,000 * inputTokenPrice
outputCost = outputTokens / 1,000,000 * outputTokenPrice
totalCost = inputCost + outputCost
```

Use decimal for money.

Never use floating point for cost storage.

---

## Budget Requirements

For MVP, support monthly budgets for:

1. Client application
2. API key

Budget actions:

```text
warn_only
block
downgrade
```

Threshold behavior:

1. If usage reaches 80%, include warning metadata.
2. If hard limit is exceeded and action is `block`, do not call provider.
3. If action is `downgrade`, try a cheaper tier if possible.
4. Log all budget decisions.

---

## Provider Adapter Requirements

All providers must implement:

```csharp
public interface IModelProviderAdapter
{
    Task<ModelResponse> CompleteChatAsync(
        ModelRequest request,
        CancellationToken cancellationToken);
}
```

Required adapters:

1. MockModelProviderAdapter
2. AzureOpenAiProviderAdapter
3. OpenAiProviderAdapter
4. AnthropicProviderAdapter

Rules:

1. Controllers must not call provider SDKs directly.
2. Use `ModelProviderAdapterFactory`.
3. Provider credentials must come from secure configuration or Key Vault later.
4. Do not store raw API keys in the database.
5. Real provider calls must be controlled by configuration:

```json
{
  "ProviderCalls": {
    "EnableRealProviderCalls": false
  }
}
```

When disabled, use the mock provider.

---

## Fallback Requirements

Use Polly for resilience.

Required behavior:

1. Retry transient provider errors once.
2. If model fails, try another model in the same tier if available.
3. If still failing, try configured fallback tier.
4. If fallback succeeds, mark `FallbackUsed = true`.
5. If fallback fails, return a controlled error.
6. Never leak provider exception details to the client.
7. Store fallback metadata in request logs.

---

## Security Rules

Follow these rules strictly:

1. Never store raw API keys.
2. Never log raw API keys.
3. Never log raw prompts by default.
4. Never log raw responses by default.
5. Store prompt hash and response hash instead.
6. Use safe error messages.
7. Use validation on all public endpoints.
8. Use tenant isolation in all queries.
9. Add indexes on tenant-scoped columns.
10. Store provider credentials only as secret references.
11. Prepare for Azure Key Vault integration.
12. Add CORS configuration explicitly. Do not allow all origins in production.
13. All admin mutations must produce audit logs.

---

## Observability Requirements

Use structured logs.

Every gateway request must have:

```text
CorrelationId
RequestId
TenantId
ApplicationId
SelectedProvider
SelectedModel
SelectedTier
MatchedRule
InputTokens
OutputTokens
EstimatedCost
LatencyMs
FallbackUsed
BudgetStatus
```

Do not log prompt or response content.

Required custom events:

```text
AiRequestReceived
AiRoutingDecisionMade
AiModelCalled
AiFallbackTriggered
AiBudgetExceeded
AiResponseReturned
```

Required metrics:

```text
Request count
Input tokens
Output tokens
Estimated cost
Latency
Fallback count
Budget exceeded count
Premium model usage
Human review count
Blocked request count
```

---

## Frontend UX Requirements

The frontend should look like a polished SaaS product.

Use:

- clean sidebar
- top navigation/header
- cards for metrics
- charts for usage
- tables for logs
- modals or side panels for create/edit forms
- toast notifications
- empty states
- loading states
- error states

### Required Pages

#### Dashboard

Show:

1. Total spend this month
2. Total requests
3. Total tokens
4. Average latency
5. Cost by model
6. Cost by application
7. Token usage trend
8. Premium model usage
9. Fallback rate
10. Recent requests

#### Providers

Allow:

1. List providers
2. Add provider
3. Edit provider
4. Enable/disable provider

#### Models

Allow:

1. List models
2. Add model
3. Edit model
4. Assign model to tier
5. Show pricing and context window

#### Routing Rules

Allow:

1. List rules by priority
2. Add rule
3. Edit rule
4. Enable/disable rule
5. Test rule against sample request profile
6. Show JSON preview

#### Budgets

Allow:

1. List budgets
2. Create budget
3. Edit budget
4. Delete budget
5. View budget status
6. See warning and hard-limit indicators

#### API Keys

Allow:

1. List API keys
2. Create API key
3. Show raw key only once
4. Revoke key
5. Show last used date

#### Usage Logs

Allow:

1. View recent requests
2. Filter by date, application, model, provider
3. See routing metadata
4. See estimated cost

#### Audit Logs

Allow:

1. View audit events
2. Filter by actor, action, entity, date
3. Expand before/after JSON

---

## Testing Requirements

Do not consider a feature complete unless tests are added or intentionally documented as not applicable.

### Backend Tests

Use xUnit.

Required tests:

1. API key validation
2. Token estimation
3. Cost calculation
4. Request profiling
5. Rule matching
6. Default routing
7. High-risk human review
8. Budget warning
9. Budget exceeded
10. Provider adapter factory
11. Fallback behavior
12. Usage log creation
13. Audit log creation

### Frontend Tests

At minimum:

1. TypeScript must compile.
2. Lint must pass.
3. Important forms should validate inputs.
4. API client errors should be handled.

---

## Coding Standards

### C# Standards

1. Use async/await correctly.
2. Pass `CancellationToken` where appropriate.
3. Use nullable reference types.
4. Use dependency injection.
5. Keep controllers thin.
6. Put business logic in services.
7. Use FluentValidation for request validation.
8. Use `decimal` for money.
9. Do not use magic strings when enums/constants are better.
10. Use clear DTOs for API requests and responses.
11. Do not expose EF entities directly from API responses.
12. Keep tenant filtering explicit.
13. Add XML comments only where they add real value.

### TypeScript Standards

1. Use strict TypeScript.
2. Avoid `any` unless necessary.
3. Use Zod for form validation schemas where useful.
4. Keep components small and reusable.
5. Put API calls in `lib/api`.
6. Use typed DTOs.
7. Handle loading, empty, and error states.
8. Avoid hardcoded mock data once API integration exists.

---

## Database Rules

1. Use migrations for schema changes.
2. Never manually edit generated migrations unless necessary.
3. Add indexes for frequently queried fields.
4. Add tenant-aware indexes.
5. Use JSONB-compatible fields for rule conditions/actions.
6. Store cost values as decimal/numeric.
7. Store timestamps in UTC.
8. Do not store provider secrets directly.
9. Do not store raw prompt/response by default.

Important indexes:

```text
TenantId
ApplicationId
ApiKeyHash
ProviderId
ModelId
CreatedAt
TenantId + CreatedAt
TenantId + ApplicationId + CreatedAt
TenantId + ModelId + CreatedAt
```

---

## Local Commands

Use these commands where applicable.

### Backend

From:

```text
apps/gateway-api
```

Restore:

```bash
dotnet restore
```

Build:

```bash
dotnet build
```

Test:

```bash
dotnet test
```

Run:

```bash
dotnet run --project src/TokenShield.Api
```

Create migration:

```bash
dotnet ef migrations add <MigrationName> --project src/TokenShield.Infrastructure --startup-project src/TokenShield.Api
```

Apply migration:

```bash
dotnet ef database update --project src/TokenShield.Infrastructure --startup-project src/TokenShield.Api
```

### Frontend

From:

```text
apps/web-admin
```

Install:

```bash
npm install
```

Run dev server:

```bash
npm run dev
```

Build:

```bash
npm run build
```

Lint:

```bash
npm run lint
```

### Docker Compose

From repository root:

```bash
docker compose up --build
```

---

## Definition of Done

A task is complete only when:

1. Code builds successfully.
2. Relevant tests are added or updated.
3. Existing tests pass.
4. No raw secrets are stored or logged.
5. No raw prompts/responses are logged by default.
6. Public endpoints validate inputs.
7. Admin mutations write audit logs where applicable.
8. Documentation is updated if behavior changes.
9. README or relevant docs include setup/configuration changes.
10. The implementation follows the MVP scope and does not add unnecessary complexity.

---

## MVP Build Order

Follow this sequence unless explicitly instructed otherwise:

1. Product specification
2. Repository setup
3. Backend database model
4. Development seed data
5. API key authentication
6. OpenAI-compatible endpoint with mock response
7. Token and cost engine
8. Request profiler
9. Rule-based router
10. Provider adapter abstraction
11. End-to-end gateway flow
12. Budget enforcement
13. Azure OpenAI adapter
14. OpenAI adapter
15. Anthropic adapter
16. Fallback logic
17. Admin API for providers and models
18. Admin API for routing rules
19. Admin API for budgets
20. Usage analytics APIs
21. Frontend layout
22. Frontend dashboard
23. Frontend model catalog
24. Frontend routing rule builder
25. Frontend budget page
26. Connect frontend to backend
27. API key management
28. Audit logs
29. Observability
30. Docker Compose
31. Azure Bicep infrastructure
32. GitHub Actions CI/CD
33. MVP hardening

---

## Out of Scope for MVP

Do not implement these unless explicitly requested:

1. Streaming responses
2. Semantic cache
3. Prompt compression
4. LLM-based router
5. ML-based cost optimizer
6. Full SAML/SSO
7. Stripe billing
8. Kubernetes / AKS deployment
9. Complex approval workflow
10. Fine-tuning
11. Prompt marketplace
12. Multi-region active-active deployment
13. Air-gapped deployment
14. Advanced prompt injection detection
15. Full data residency policy engine

---

## Important Product Principles

1. Cost control is the main product value.
2. Governance is as important as routing.
3. The gateway must be provider-agnostic.
4. The API must be easy for developers to adopt.
5. Prefer OpenAI-compatible APIs where possible.
6. Enterprise buyers care about audit, security, and control.
7. Do not over-engineer the MVP.
8. Rule-based routing must be explainable.
9. Never compromise tenant isolation.
10. Never leak secrets, prompts, or sensitive data through logs.

---

## Response Style for Codex

When completing a task, provide:

1. Summary of changes
2. Files changed
3. Tests added or updated
4. Commands run
5. Any known limitations
6. Recommended next step

Do not claim tests passed unless they were actually run.

---

## Safety and Data Handling

This product may process sensitive enterprise prompts.

Therefore:

1. Treat prompts as sensitive data.
2. Treat responses as sensitive data.
3. Treat provider credentials as secrets.
4. Treat API keys as secrets.
5. Avoid storing raw content.
6. Prefer hashing and metadata.
7. Mask or avoid logging sensitive values.
8. Make privacy-preserving behavior the default.

---

## Final Reminder

Build TokenShield as a serious enterprise SaaS product.

The MVP should be simple but production-minded:

```text
OpenAI-compatible gateway
+ model catalog
+ tiered routing
+ budget enforcement
+ usage dashboard
+ audit logs
+ Azure-ready deployment
```

Avoid unnecessary complexity until the core value is working end-to-end.
