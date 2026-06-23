# AGENTS.md

## Project Name

**TokenShield AI Gateway**

## Agent Operating Mode

This file is the permanent project contract for AI coding agents such as Codex, Cursor, Windsurf, Claude Code, or similar tools.

Agents must read and follow this file before making changes. Individual prompts should not repeat this file. Feature prompts should describe the desired outcome, acceptance criteria, and explicit exclusions only.

The preferred build style is **epic-based vertical-slice implementation**, not micro-task implementation. A vertical slice may include database, backend API, frontend UI, tests, and documentation when they are part of the same product outcome.

Agents must optimize for:

1. Working end-to-end product behavior
2. Production-minded architecture
3. Security and privacy by default
4. Tenant isolation
5. Maintainable code
6. Clear tests and documentation
7. Minimal unnecessary complexity

---

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

The product must help enterprise teams answer these questions:

1. Which applications are spending the most on AI?
2. Which models are being used and why?
3. Are developers using premium models unnecessarily?
4. Can low-risk requests be routed to cheaper models?
5. Are budgets being enforced before provider cost is incurred?
6. Can provider failures be handled with controlled fallback?
7. Can AI usage be audited without storing sensitive prompts or responses?

---

## MVP Scope

The MVP must include:

1. OpenAI-compatible AI gateway API
2. API key authentication for client applications
3. Tenant-aware model provider catalog
4. AI model catalog
5. Three model tiers: `cheap`, `standard`, `premium`
6. Rule-based model routing
7. Request profiling
8. Token estimation
9. Cost calculation
10. Budget enforcement
11. Provider adapters for Azure OpenAI, OpenAI, Anthropic, and mock provider
12. Fallback logic
13. Usage logging
14. Audit logging
15. Admin APIs
16. Next.js admin console
17. Usage dashboard
18. API key management
19. Observability
20. Docker Compose local development
21. Azure-ready deployment foundation
22. CI/CD foundation

---

## Out of Scope for MVP

Do not implement these unless explicitly requested:

1. Streaming responses
2. Semantic cache
3. Prompt compression
4. LLM-based router
5. ML-based cost optimizer
6. Stripe billing
7. Full SAML/SSO implementation
8. Microsoft Entra ID implementation beyond placeholders
9. Kubernetes / AKS / Helm deployment
10. Complex approval workflow
11. Fine-tuning
12. Prompt marketplace
13. Multi-region active-active deployment
14. Air-gapped deployment
15. Advanced prompt injection detection
16. Full data residency policy engine
17. Redis or Service Bus unless specifically requested

---

## Technology Stack

Use this stack unless a prompt explicitly changes it.

### Frontend

- Next.js
- TypeScript
- Tailwind CSS
- shadcn/ui-compatible component structure
- TanStack Query
- TanStack Table
- React Hook Form
- Zod
- Recharts or Apache ECharts for charts

### Backend

- .NET 8 Web API
- C#
- ASP.NET Core Controllers or Minimal APIs where consistent with the existing codebase
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
- EF Core migrations
- JSONB-compatible columns where useful
- `Guid` primary keys
- `CreatedAtUtc` and `UpdatedAtUtc` timestamps
- Soft delete where appropriate
- `decimal` for money and cost values

### Azure Target

- Azure Container Apps
- Azure API Management
- Azure Database for PostgreSQL Flexible Server
- Azure Key Vault
- Azure Application Insights
- Azure Monitor
- Log Analytics Workspace
- Azure Container Registry

### Local Development

Docker Compose should support:

- PostgreSQL
- gateway-api
- web-admin

---

## Repository Structure

Use this structure unless the existing repository already has an equivalent structure.

```text
/
|-- apps/
|   |-- gateway-api/
|   `-- web-admin/
|
|-- docs/
|   |-- product-spec.md
|   |-- architecture.md
|   |-- database-schema.md
|   |-- gateway-api.md
|   |-- routing-rules.md
|   |-- cost-engine.md
|   |-- provider-adapters.md
|   |-- budget-enforcement.md
|   |-- admin-api.md
|   |-- frontend-admin-console.md
|   |-- observability.md
|   `-- deployment.md
|
|-- infra/
|   |-- bicep/
|   |-- docker/
|   `-- README.md
|
|-- scripts/
|-- .github/
|   `-- workflows/
|-- AGENTS.md
|-- docker-compose.yml
`-- README.md
```

---

## Backend Architecture

The backend must follow Clean Architecture principles.

Recommended backend structure:

```text
apps/gateway-api/
|-- src/
|   |-- TokenShield.Api/
|   |-- TokenShield.Domain/
|   |-- TokenShield.Application/
|   |-- TokenShield.Infrastructure/
|   |-- TokenShield.ProviderAdapters/
|   |-- TokenShield.PolicyEngine/
|   |-- TokenShield.CostEngine/
|   |-- TokenShield.Guardrails/
|   `-- TokenShield.Observability/
|
`-- tests/
    |-- TokenShield.UnitTests/
    `-- TokenShield.IntegrationTests/
```

### Layer Responsibilities

#### TokenShield.Api

Responsible for:

- Controllers or endpoint definitions
- Middleware
- Request validation
- API key authentication
- Swagger/OpenAPI
- Health checks
- Dependency injection setup
- HTTP response shaping

Controllers must stay thin. Business logic must live in application/domain services.

#### TokenShield.Domain

Responsible for:

- Domain entities
- Value objects
- Enums
- Domain constants
- Core business rules that do not depend on infrastructure

#### TokenShield.Application

Responsible for:

- Use cases
- DTOs
- Interfaces
- Request profiling orchestration
- Routing orchestration
- Budget orchestration
- Gateway orchestration
- Application services

#### TokenShield.Infrastructure

Responsible for:

- EF Core DbContext
- Entity configurations
- Migrations
- Repository implementations if needed
- PostgreSQL integration
- Key Vault integration placeholders
- Application settings

#### TokenShield.ProviderAdapters

Responsible for:

- Provider abstraction
- Azure OpenAI adapter
- OpenAI adapter
- Anthropic adapter
- Mock provider adapter
- Provider adapter factory

Controllers must never call provider SDKs directly.

#### TokenShield.PolicyEngine

Responsible for:

- Routing rule evaluation
- Rule matching
- Condition parsing
- Action selection
- Explainable routing decisions

#### TokenShield.CostEngine

Responsible for:

- Token estimation
- Token usage calculation
- Cost calculation
- Budget-related calculation helpers

#### TokenShield.Guardrails

Responsible for:

- Simple PII detection
- Secret detection placeholders
- Prompt safety placeholders
- Future prompt-injection checks

#### TokenShield.Observability

Responsible for:

- Structured logging
- Audit logging
- Metrics
- Tracing
- Application Insights events

---

## Frontend Architecture

The frontend must be a polished SaaS admin console.

Recommended structure:

```text
apps/web-admin/
|-- app/
|-- components/
|   |-- layout/
|   |-- dashboard/
|   |-- providers/
|   |-- models/
|   |-- routing-rules/
|   |-- budgets/
|   |-- api-keys/
|   |-- usage-logs/
|   |-- audit-logs/
|   `-- shared/
|
|-- lib/
|   |-- api/
|   |-- auth/
|   |-- constants/
|   |-- mock/
|   `-- utils/
|
|-- hooks/
|-- types/
`-- styles/
```

Required MVP pages:

1. Dashboard
2. Providers
3. Models
4. Routing Rules
5. Budgets
6. Usage Logs
7. API Keys
8. Audit Logs
9. Settings

Frontend UX must include:

- sidebar navigation
- top header
- metric cards
- charts
- tables
- filters
- create/edit forms using modals or side panels
- loading states
- empty states
- error states
- toast notifications where useful

---

## Core Product Concepts

### Tenant

A company or organization using TokenShield.

### Client Application

An application owned by a tenant that sends AI requests through the gateway.

Examples:

- Fraud Investigation App
- Customer Support Bot
- Internal Developer Assistant

### Provider

An AI provider.

MVP providers:

1. Azure OpenAI
2. OpenAI
3. Anthropic
4. Mock provider

Future providers may include Gemini, Bedrock, and Ollama.

### AI Model

A model registered under a provider. Models have pricing, context window, capabilities, status, and provider-specific deployment name.

### Model Tier

A logical cost/quality tier.

Required MVP tiers:

```text
cheap
standard
premium
```

### Routing Rule

A tenant-defined rule that decides which action should be taken for a request.

Supported MVP actions:

1. Route to tier
2. Human review
3. Block

### Budget Limit

A monthly spend limit for tenant, application, API key, or model scope.

Supported MVP budget actions:

1. Warn only
2. Block
3. Downgrade

### AI Request Log

A privacy-preserving record of an AI request processed through the gateway.

Do not store raw prompt or raw response by default.

Store metadata such as:

- prompt hash
- response hash
- token counts
- estimated cost
- selected provider
- selected model
- selected tier
- matched rule
- fallback status
- budget status
- latency
- request status
- timestamp

---

## Gateway API Contract

The main gateway endpoint must be OpenAI-compatible.

```http
POST /v1/chat/completions
```

MVP behavior:

1. Require `x-api-key` authentication.
2. Support `model: "auto"` for routed mode.
3. Support `stream: false` only.
4. Return a clear validation error for `stream: true`.
5. Accept OpenAI-style `messages`.
6. Accept optional `metadata` for routing/profile signals.
7. Return OpenAI-compatible response shape.
8. Include routing metadata in the response.
9. Do not log raw prompts or responses.

Example request:

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

Example response:

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

## API Key Authentication Contract

Client applications authenticate using:

```http
x-api-key: ts_live_xxxxxx
```

Development keys may use:

```http
x-api-key: ts_dev_xxxxxx
```

Rules:

1. Never store raw API keys.
2. Store only hashed API keys.
3. Show raw API key only once during creation.
4. Never log raw API keys.
5. Resolve `TenantId`, `ClientApplicationId`, and `ApiKeyId` from the key.
6. Reject missing, invalid, revoked, or expired API keys.
7. Update `LastUsedAtUtc` when a key is successfully used.
8. Keep authentication middleware reusable for all `/v1` gateway routes.

---

## Routing Engine Contract

The routing engine must be rule-based for MVP.

Execution order:

1. Authenticate request.
2. Estimate input tokens.
3. Build request profile.
4. Run guardrail placeholders.
5. Check budget pre-call.
6. Match routing rules.
7. Select model tier.
8. Select concrete model.
9. Call provider adapter.
10. Calculate final cost.
11. Apply post-call usage logging.
12. Return OpenAI-compatible response.

Supported rule fields:

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

Supported actions:

```text
routeToTier
humanReview
block
```

Default behavior:

1. If no rule matches, route to `standard`.
2. If `riskLevel = high`, require human review unless an explicit approved rule allows routing.

---

## Request Profiling Contract

Create a request profile for every AI request.

Profile fields:

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
2. If `taskType` is missing, infer using simple keyword rules.
3. If `riskLevel` is missing, default to `medium`.
4. Detect simple PII using regex for email and phone-like values.
5. Complexity score:
   - base 20
   - +20 if input tokens > 4000
   - +30 if requiresReasoning = true
   - +20 if taskType = complex_reasoning
   - cap at 100

---

## Cost Engine Contract

For MVP token estimation:

```text
1 token ≈ 4 characters
```

Count all message content.

Model prices must be stored as:

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

Rules:

1. Use `decimal` for money.
2. Never use floating point for persisted cost values.
3. Store enough metadata to explain how cost was calculated.

---

## Budget Contract

For MVP, support monthly budgets for:

1. Tenant
2. Client application
3. API key
4. Model

Budget actions:

```text
warn_only
block
downgrade
```

Threshold behavior:

1. If usage reaches warning threshold, include warning metadata and log the decision.
2. If hard limit is exceeded and action is `block`, do not call the provider.
3. If action is `downgrade`, try a cheaper tier if available.
4. Log all budget decisions.

---

## Provider Adapter Contract

All providers must implement a common abstraction equivalent to:

```csharp
Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken);
```

Required adapters:

1. Mock provider
2. Azure OpenAI
3. OpenAI
4. Anthropic

Rules:

1. Controllers must not call provider SDKs directly.
2. Use a provider adapter factory or equivalent provider selection mechanism.
3. Provider credentials must come from secure configuration or secret references.
4. Do not store raw provider keys in the database.
5. Real provider calls must be controlled by configuration.
6. When real calls are disabled, use the mock provider.

---

## Fallback Contract

Use Polly or equivalent .NET resilience patterns.

Required behavior:

1. Retry transient provider errors once.
2. If a model fails, try another model in the same tier if available.
3. If still failing, try configured fallback tier.
4. If fallback succeeds, mark `FallbackUsed = true`.
5. If fallback fails, return a controlled error.
6. Never leak provider exception details to the client.
7. Store fallback metadata in request logs.

---

## Admin API Contract

Create tenant-aware admin APIs for:

1. Providers
2. Models
3. Model tiers
4. Routing rules
5. Budgets
6. API keys
7. Usage analytics
8. Audit logs
9. Settings where needed

MVP admin authentication may be a clearly marked placeholder, but code must be structured so Microsoft Entra ID can be added later.

All admin mutation endpoints must write audit logs.

Mutation means:

- create
- update
- delete
- enable
- disable
- revoke
- publish

Do not expose EF entities directly from API responses.

---

## Security and Privacy Rules

These rules are mandatory.

1. Never store raw API keys.
2. Never log raw API keys.
3. Never log raw prompts by default.
4. Never log raw responses by default.
5. Store prompt hash and response hash instead of raw content.
6. Use safe error messages.
7. Validate all public inputs.
8. Enforce tenant isolation in all tenant-scoped queries.
9. Add indexes on tenant-scoped query columns.
10. Store provider credentials only as secret references.
11. Prepare for Azure Key Vault integration.
12. Configure CORS explicitly.
13. Do not allow all origins in production.
14. All admin mutations must produce audit logs.
15. Do not return stack traces or provider exception details to clients.
16. Do not hardcode real secrets in code, docs, tests, or seed data.

---

## Observability Contract

Every gateway request must have structured telemetry with:

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
RequestStatus
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

## Testing Contract

Do not consider a feature complete unless tests are added or the reason for not adding tests is documented.

Backend tests should cover:

1. API key validation
2. Token estimation
3. Cost calculation
4. Request profiling
5. Rule matching
6. Default routing
7. High-risk human review behavior
8. Budget warning
9. Budget exceeded behavior
10. Provider adapter factory
11. Fallback behavior
12. Usage log creation
13. Audit log creation
14. Tenant isolation for key queries

Frontend requirements:

1. TypeScript must compile.
2. Lint should pass if configured.
3. Important forms should validate inputs.
4. API client errors should be handled.
5. Loading, empty, and error states must exist for main pages.

---

## Coding Standards

### C#

1. Use async/await correctly.
2. Pass `CancellationToken` where appropriate.
3. Use nullable reference types.
4. Use dependency injection.
5. Keep controllers thin.
6. Put business logic in services.
7. Use FluentValidation for request validation.
8. Use `decimal` for money.
9. Prefer enums/constants over magic strings.
10. Use clear DTOs for API requests and responses.
11. Do not expose EF entities directly.
12. Keep tenant filtering explicit.
13. Add XML comments only where they add real value.

### TypeScript

1. Use strict TypeScript.
2. Avoid `any` unless there is a strong reason.
3. Use Zod for form validation where useful.
4. Keep components small and reusable.
5. Put API calls in `lib/api`.
6. Use typed DTOs.
7. Handle loading, empty, and error states.
8. Avoid hardcoded mock data once backend integration exists.

---

## Vibe Coding Prompting Rules

Individual prompts should follow these rules:

1. Do not repeat this AGENTS.md file.
2. State the epic objective clearly.
3. Ask for a vertical slice, not isolated boilerplate.
4. Define outcomes and acceptance criteria.
5. Specify exact public contracts only where necessary.
6. Let the agent choose internal names unless consistency with existing code requires otherwise.
7. Ask the agent to inspect the existing repository before editing.
8. Ask the agent to preserve existing working behavior.
9. Ask the agent to run relevant build/test commands.
10. Ask the agent to report what actually passed and what failed.

Good prompt style:

```text
Implement the Gateway Core vertical slice. The result must allow a client app to create/use an API key, call /v1/chat/completions with model=auto, receive a mock OpenAI-compatible response, and produce safe usage metadata without logging raw prompt content.
```

Avoid prompt style:

```text
Create class X with method Y and property Z in folder A, then create file B, then create interface C...
```

Use exact names only for external contracts such as endpoint paths, headers, environment variables, public response fields, and security invariants.

---

## Epic-Based MVP Build Order

Use this build order unless explicitly instructed otherwise:

1. Product and repository foundation
2. Backend persistence and seed foundation
3. Gateway core vertical slice
4. Routing, profiling, and cost vertical slice
5. Budget, usage, and audit governance vertical slice
6. Provider integration and fallback vertical slice
7. Admin API vertical slice
8. Frontend admin console vertical slice
9. Production readiness vertical slice
10. Final MVP verification and hardening

---

## Definition of Done for Any Epic

An epic is complete only when:

1. The code builds successfully.
2. Relevant tests are added or updated.
3. Existing tests pass or failures are clearly explained.
4. No raw secrets are stored or logged.
5. No raw prompts/responses are logged by default.
6. Public endpoints validate inputs.
7. Tenant isolation is preserved.
8. Admin mutations write audit logs where applicable.
9. Documentation is updated if behavior changes.
10. README or relevant docs include setup/configuration changes.
11. The implementation follows MVP scope.
12. No unnecessary future features are added.

---

## Required Agent Completion Report

At the end of every epic, the coding agent must report:

1. Summary of changes
2. Files changed
3. Tests added or updated
4. Commands run
5. Build/test/lint result
6. Security/privacy notes
7. Known limitations
8. Recommended next step

The agent must not claim that tests passed unless they were actually run.

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

Prefer working vertical slices over disconnected micro-features. Avoid unnecessary complexity until the core value is working end-to-end.

## Codex Execution Rules

When working on this repository, always follow these rules.

### Source of Truth

Treat this `AGENTS.md` file as the primary source of truth for architecture, coding standards, security rules, testing expectations, and product boundaries.

If a feature prompt conflicts with `AGENTS.md`, stop and prefer `AGENTS.md` unless the prompt explicitly says it is intentionally changing the project direction.

### Task Execution Mode

Work in vertical slices.

For each requested epic or feature:

1. Read `AGENTS.md`.
2. Read only the requested epic or task instructions.
3. Inspect the existing repository before editing.
4. Reuse existing patterns, services, DTOs, folders, naming conventions, and tests.
5. Avoid creating parallel structures that duplicate existing functionality.
6. Implement only the requested epic or task.
7. Do not implement future epics unless explicitly requested.
8. Keep the implementation production-minded but not over-engineered.
9. Prefer cohesive, working increments over isolated boilerplate.

### Codebase Consistency Rules

Before adding a new file, class, interface, DTO, endpoint, component, or service:

1. Check whether an equivalent already exists.
2. Extend existing abstractions where appropriate.
3. Keep naming consistent with the current codebase.
4. Do not introduce a second pattern for the same concern.
5. Do not leave dead code, unused files, or duplicate implementations.

### Security and Privacy Rules

For every implementation:

1. Never store raw API keys.
2. Never log raw API keys.
3. Never log raw prompts by default.
4. Never log raw model responses by default.
5. Store prompt and response hashes instead of raw content where request logging is required.
6. Keep tenant isolation explicit in database queries.
7. Return safe error messages to clients.
8. Do not leak provider exception details.
9. Store provider credentials only as secure references.
10. Keep production CORS restrictive.

### Testing Rules

Do not consider a task complete unless relevant tests are added or updated.

At minimum:

1. Backend code must build.
2. Backend tests must pass where applicable.
3. Frontend TypeScript must compile where applicable.
4. Frontend build must pass where applicable.
5. Important service logic must have unit tests.
6. Public endpoints should have integration tests where practical.
7. Security-sensitive behavior must be tested.

### Documentation Rules

Update documentation only where it helps future development or operation.

Prefer concise documentation that explains:

1. What was implemented.
2. How to run or test it.
3. Important design decisions.
4. Known limitations.
5. Future extension points.

Do not duplicate long sections from `AGENTS.md` into every document.

### Completion Report

At the end of every task, provide a concise completion report with:

1. Files changed.
2. What was implemented.
3. Commands run.
4. Build/test status.
5. Assumptions made.
6. Known limitations.
7. Suggested next step.

### Prohibited Behavior

Do not:

1. Implement multiple future epics without being asked.
2. Rewrite large parts of the repository unnecessarily.
3. Replace working code with unrelated patterns.
4. Store or log secrets.
5. Store or log raw prompts/responses by default.
6. Skip tests for security-sensitive code.
7. Add frontend mock data when a backend API already exists unless explicitly needed.
8. Hide build or test failures.
