# TokenShield AI Gateway — Epic-Based Codex Prompt Pack

Use these prompts after placing the updated `AGENTS.md` in the repository root and, ideally, in your coding tool's global project rules.

These prompts intentionally avoid repeating the full architecture and security policy. Codex must treat `AGENTS.md` as the source of truth.

Recommended use:

1. Run one epic prompt at a time.
2. Let Codex inspect the repository before editing.
3. Ask Codex to commit or summarize changes after each epic.
4. Do not move to the next epic until build/test issues are resolved.
5. Use the final verification prompt after all epics are complete.

---

## Epic 1 — Product and Repository Foundation

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md fully and treat it as the project contract.

Epic objective:
Create the product and repository foundation for a production-minded MVP. The repository should be ready for backend, frontend, documentation, local development, infrastructure, and CI/CD work.

Implement a vertical foundation slice that includes:

1. A professional product specification in docs/product-spec.md.
2. A high-level architecture document in docs/architecture.md.
3. A clean monorepo structure aligned with AGENTS.md.
4. A runnable .NET 8 gateway API skeleton.
5. A runnable Next.js TypeScript admin console skeleton.
6. Basic Dockerfiles for backend and frontend.
7. A simple Docker Compose foundation.
8. Root README and app-specific README files.

Backend outcome:
- The gateway API runs locally.
- Swagger/OpenAPI is available in development.
- GET /health is public.
- GET /api/version is public and returns product name, version, and environment.
- Structured console logging is configured.
- Global exception handling placeholder exists.
- CORS configuration placeholder exists.

Frontend outcome:
- The admin console runs locally.
- The home page presents TokenShield AI Gateway as an AI cost control, model routing, and governance platform.
- The layout has placeholder navigation for Dashboard, Providers, Models, Routing Rules, Budgets, Usage Logs, API Keys, Audit Logs, and Settings.
- Tailwind CSS is configured.
- The structure is ready for shadcn/ui-compatible components.

Acceptance criteria:
- Repository structure follows AGENTS.md or clearly explains any deviation.
- Backend builds.
- Frontend builds.
- Health and version endpoints work.
- No business features are implemented yet.
- Documentation explains how to run backend, frontend, and Docker Compose locally.

Run relevant commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Commands run
4. Backend build result
5. Frontend build result
6. Known limitations
7. Recommended next step
```

---

## Epic 2 — Backend Persistence and Seed Foundation

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md, docs/product-spec.md, docs/architecture.md, and the existing backend code.

Epic objective:
Build the backend persistence foundation for TokenShield. The result should support tenants, applications, API keys, providers, models, model tiers, routing rules, budgets, request logs, and audit logs, with idempotent development seed data.

Implement a cohesive backend persistence slice using .NET 8, EF Core, and PostgreSQL.

Required outcomes:

1. Clean Architecture projects exist or are completed for Domain, Application, Infrastructure, and Api.
2. Domain model supports the MVP concepts defined in AGENTS.md.
3. EF Core DbContext and entity configurations are implemented.
4. PostgreSQL provider is configured.
5. Initial EF Core migration exists and compiles.
6. JSON-like rule/action/audit fields use PostgreSQL-compatible JSONB where appropriate.
7. Money/cost fields use decimal.
8. Tenant-scoped indexes are added for key query paths.
9. Soft delete and timestamps are handled consistently where appropriate.
10. Development seed data is available and idempotent.

Seed data outcome:
- Demo tenant exists.
- Demo admin user exists.
- Demo team exists.
- Demo client application exists.
- Mock, Azure OpenAI, OpenAI, and Anthropic providers exist.
- Cheap, standard, and premium model tiers exist.
- Demo models are mapped to tiers.
- Useful initial routing rules exist.
- Application and tenant budgets exist.
- No raw secrets are seeded.

Important implementation guidance:
- Do not overfit to a rigid property list if the existing code suggests a better clean design.
- Preserve the externally important concepts, constraints, and security rules from AGENTS.md.
- Do not implement authentication, routing execution, provider calls, budget enforcement, or admin APIs in this epic.

Acceptance criteria:
- Backend builds.
- Migration compiles.
- Seed data runs only in Development when enabled by configuration.
- Running seed multiple times does not create duplicates.
- Existing /health and /api/version endpoints still work.
- Tests cover at least DbContext construction, important enum/concept availability, and seed idempotency.
- docs/database-schema.md and docs/development-seed-data.md are created or updated.

Run relevant build/test commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Migration created
4. Seed data added
5. Tests added or updated
6. Commands run
7. Build/test result
8. Known limitations
9. Recommended next step
```

---

## Epic 3 — Gateway Core Vertical Slice

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md and inspect the existing backend code, database model, and seed data.

Epic objective:
Implement the first working gateway vertical slice. A client application should be able to create a development API key, authenticate with x-api-key, call /v1/chat/completions with model=auto, and receive an OpenAI-compatible mock response without logging raw prompt content.

Required outcomes:

1. Secure API key generation and validation.
2. API key hashing with configurable pepper or equivalent secure design.
3. Raw API keys are shown only once at creation.
4. Raw API keys are never stored, returned by list endpoints, or logged.
5. Request context resolves TenantId, ClientApplicationId, ApiKeyId, and CorrelationId.
6. Correlation ID middleware supports x-correlation-id and returns x-correlation-id.
7. API key middleware protects /v1 routes.
8. /health, /api/version, Swagger, and development key endpoints remain public as appropriate.
9. Development-only API key creation and list endpoints exist.
10. POST /v1/chat/completions accepts OpenAI-compatible chat requests.
11. The endpoint validates model, messages, roles, temperature, max_tokens, and stream.
12. stream=true returns a clear MVP unsupported error.
13. Valid requests return an OpenAI-compatible mock chat completion response.
14. Response includes mock routing metadata.
15. Safe request metadata may be logged, but raw prompt/response content must not be logged.

External contracts that must be exact:
- Header: x-api-key
- Header: x-correlation-id
- Gateway endpoint: POST /v1/chat/completions
- Auth test endpoint may exist: GET /v1/auth-test
- Development key endpoint may exist under /api/dev/api-keys

Do not implement real provider calls, routing execution, token/cost calculation, or budget enforcement in this epic.

Acceptance criteria:
- A developer can create a dev API key for the seeded demo app.
- The developer can call /v1/auth-test successfully with that key.
- The developer can call /v1/chat/completions successfully with that key.
- Missing/invalid/revoked/expired keys are rejected.
- Validation errors are consistent and safe.
- Tests cover API key service, middleware, and chat completion validation/response behavior.
- docs/api-key-authentication.md and docs/chat-completions-endpoint.md are created or updated.

Run relevant build/test commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Security approach
4. Tests added or updated
5. Commands run
6. Build/test result
7. Known limitations
8. Recommended next step
```

---

## Epic 4 — Routing, Profiling, and Cost Vertical Slice

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md and inspect the existing gateway flow.

Epic objective:
Replace the mock-only gateway behavior with an explainable routing, request profiling, token estimation, and cost calculation flow. The gateway should still use the mock provider, but routing metadata and usage values should be real according to the MVP rules.

Required outcomes:

1. Request profiler creates a profile for every chat completion request.
2. Profiler uses metadata when provided and safe inference when missing.
3. Simple PII detection exists for email and phone-like values.
4. Complexity score follows the MVP logic in AGENTS.md.
5. Token estimator uses MVP approximation: 1 token ≈ 4 characters.
6. Cost calculator uses model pricing and decimal arithmetic.
7. Rule engine evaluates active routing rules by tenant and priority.
8. Supported rule fields/operators/actions match AGENTS.md.
9. Default routing uses standard tier when no rule matches.
10. High-risk requests require human review unless explicitly allowed.
11. Model selection chooses an enabled model from the selected tier.
12. /v1/chat/completions returns realistic usage and routing metadata.
13. Human-review and blocked decisions return controlled, safe responses.

Do not implement real provider calls or budget enforcement in this epic.

Acceptance criteria:
- Low-risk summarization routes to cheap tier using seeded rules.
- RAG/general medium-risk work routes to standard tier when appropriate.
- Complex/high-complexity work routes to premium tier when a matching rule exists.
- High-risk requests do not call a model by default.
- Response routing metadata explains selected tier, selected model, selected provider, matched rule, estimated cost, and fallback status.
- Unit tests cover token estimation, cost calculation, profiling, rule matching, default routing, and high-risk behavior.
- docs/request-profiler.md, docs/cost-engine.md, and docs/routing-rules.md are created or updated.

Run relevant build/test commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Routing behavior implemented
4. Tests added or updated
5. Commands run
6. Build/test result
7. Known limitations
8. Recommended next step
```

---

## Epic 5 — Budget, Usage, and Audit Governance Vertical Slice

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md and inspect the existing gateway routing flow, persistence layer, and seed data.

Epic objective:
Implement governance controls around the gateway flow: budget enforcement, usage logging, and audit logging. The gateway should prevent avoidable provider cost when budgets are exceeded and should produce privacy-preserving usage records for analytics.

Required outcomes:

1. Budget service evaluates active monthly budgets for relevant scopes.
2. Supported scopes include tenant, application, API key, and model where available.
3. Warning threshold behavior is implemented.
4. Hard-limit block behavior is implemented before provider calls.
5. Downgrade behavior attempts a cheaper available tier when configured.
6. Budget decisions are included in gateway routing metadata where relevant.
7. Every gateway request writes a privacy-preserving AiRequestLog.
8. Request logs include tenant/application/key/model/tier/provider metadata, tokens, estimated cost, status, latency, matched rule, fallback flag, and budget status.
9. Raw prompts and raw responses are not stored.
10. Prompt and response hashes are stored where feasible.
11. Audit logging service exists for admin mutations and sensitive lifecycle events.
12. API key creation and budget/rule/model/provider mutations use audit logging where those mutations already exist.

Do not implement full admin API CRUD in this epic unless needed to support the governance flow.
Do not implement frontend changes in this epic.
Do not implement real provider integrations in this epic.

Acceptance criteria:
- Budget warning at threshold is visible in metadata/logs.
- Budget block prevents provider/model call.
- Budget downgrade selects a cheaper tier when possible.
- Usage log is created for successful, failed, blocked, and human-review requests.
- Audit log does not contain raw API keys, raw prompts, raw responses, or provider secrets.
- Tests cover budget warning, block, downgrade, usage log creation, audit log creation, and privacy expectations.
- docs/budget-enforcement.md, docs/usage-logging.md, and docs/audit-logging.md are created or updated.

Run relevant build/test commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Budget behavior implemented
4. Usage/audit behavior implemented
5. Tests added or updated
6. Commands run
7. Build/test result
8. Known limitations
9. Recommended next step
```

---

## Epic 6 — Provider Integration and Fallback Vertical Slice

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md and inspect the current provider selection and gateway orchestration code.

Epic objective:
Implement provider adapter abstraction, real provider adapters, provider call configuration, and controlled fallback behavior. The gateway should still be safe by default: real provider calls must be disabled unless explicitly configured.

Required outcomes:

1. Common provider adapter abstraction exists and is used by gateway orchestration.
2. Mock provider remains the default for local development and tests.
3. Azure OpenAI adapter exists.
4. OpenAI adapter exists.
5. Anthropic adapter exists.
6. Provider adapter factory or equivalent selection mechanism exists.
7. Provider credentials come from secure configuration or secret references, not database raw secrets.
8. Real provider calls are controlled by configuration.
9. Provider errors are normalized into safe internal errors.
10. Provider exception details are not leaked to API clients.
11. Fallback uses Polly or equivalent resilience patterns.
12. Fallback tries another model in same tier, then configured fallback tier where possible.
13. Fallback metadata is captured in response metadata and request logs.

External behavior:
- When real provider calls are disabled, routed requests use the mock provider.
- When enabled and configured, selected real providers can be called.
- Provider-specific request/response mapping preserves OpenAI-compatible external response shape.

Acceptance criteria:
- Gateway still works with mock provider by default.
- Adapter selection is based on selected model/provider.
- Missing provider credentials fail safely with controlled errors.
- Fallback behavior is tested without real provider calls.
- At least adapter mapping/unit tests exist for Azure OpenAI, OpenAI, and Anthropic.
- docs/provider-adapters.md, docs/provider-azure-openai.md, docs/provider-openai.md, docs/provider-anthropic.md, and docs/fallback-logic.md are created or updated.

Run relevant build/test commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Provider adapters implemented
4. Fallback behavior implemented
5. Tests added or updated
6. Commands run
7. Build/test result
8. Known limitations
9. Recommended next step
```

---

## Epic 7 — Admin API Vertical Slice

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md and inspect the persistence, audit logging, routing, budget, and provider code.

Epic objective:
Build tenant-aware admin APIs that allow an operator to manage the core configuration needed for AI cost control and governance.

Required admin API outcomes:

1. Provider management APIs.
2. Model management APIs.
3. Model tier and tier-model mapping APIs where needed.
4. Routing rule management APIs.
5. Budget management APIs.
6. API key management APIs.
7. Usage analytics APIs.
8. Audit log query APIs.
9. Settings/configuration endpoint where useful for the admin console.

Functional expectations:
- List, create, update, enable/disable where applicable.
- Revoke API keys.
- Publish/enable/disable routing rules where applicable.
- Query usage analytics by date, application, provider, model, tier, status, and budget state where feasible.
- Query audit logs by actor/action/entity/date where feasible.
- Return DTOs, not EF entities.
- Validate all inputs.
- Preserve tenant isolation.
- Write audit logs for all admin mutations.

Authentication note:
- MVP admin authentication may remain a clearly marked placeholder.
- Structure the code so Microsoft Entra ID can be added later without rewriting all controllers.

Do not implement frontend in this epic.

Acceptance criteria:
- Admin APIs can configure providers, models, tiers, routing rules, budgets, and API keys for the seeded/demo tenant.
- API key raw value is shown only once on creation.
- List/read endpoints never return raw key or hash.
- Usage analytics APIs can power the MVP dashboard.
- Audit log APIs can power the audit log page.
- Tests cover main CRUD/mutation paths, validation, tenant isolation, audit creation, and sensitive data exclusion.
- docs/admin-api.md is created or updated with endpoints and example requests.

Run relevant build/test commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Admin APIs implemented
4. Tests added or updated
5. Commands run
6. Build/test result
7. Known limitations
8. Recommended next step
```

---

## Epic 8 — Frontend Admin Console Vertical Slice

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md, inspect the existing frontend, and inspect the available backend admin APIs.

Epic objective:
Build the MVP admin console as a polished SaaS frontend that helps an operator understand AI spend, configure routing governance, manage models/providers, manage budgets, manage API keys, inspect usage logs, and review audit logs.

Required frontend outcomes:

1. Shared SaaS layout with sidebar, topbar, responsive content area, and page headers.
2. Dashboard page with spend, requests, tokens, latency, cost by model/application, token trend, premium usage, fallback rate, and recent requests.
3. Providers page.
4. Models page.
5. Routing Rules page with rule list, create/edit form, priority, conditions/actions, JSON preview, and rule test panel.
6. Budgets page with budget list, status indicators, warning/hard limit visualization, create/edit form.
7. API Keys page with create/revoke flow and raw key shown only once.
8. Usage Logs page with filters and routing metadata details.
9. Audit Logs page with filters and before/after/metadata expansion.
10. Settings page with useful MVP placeholders.
11. Typed API client layer under lib/api.
12. Zod validation for important forms.
13. TanStack Query integration for backend APIs where available.
14. Loading, empty, and error states.
15. Mock fallback only where backend APIs are not ready, clearly isolated in lib/mock.

UX expectations:
- The app should look like a sellable SaaS MVP, not a classroom demo.
- Use clean cards, charts, tables, filters, badges, and forms.
- Keep components reusable and typed.
- Avoid raw prompt/response display.
- Do not hardcode real secrets.

Acceptance criteria:
- Frontend builds.
- Lint passes if configured.
- Main pages are accessible from navigation.
- Forms validate important inputs.
- API errors are handled cleanly.
- API key creation clearly displays the raw key once and warns the user to copy it.
- Dashboard can render from backend data or clearly isolated mock data.
- docs/frontend-admin-console.md is created or updated.

Run relevant build/lint commands and report the actual results.

At completion, report:
1. Summary of changes
2. Files changed
3. Pages implemented
4. API integration status
5. Tests/lint/build commands run
6. Build/lint result
7. Known limitations
8. Recommended next step
```

---

## Epic 9 — Production Readiness Vertical Slice

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md and inspect backend, frontend, Docker, infrastructure, and CI/CD files.

Epic objective:
Make the MVP production-ready enough for demonstration, pilot deployment, and future enterprise hardening. Focus on observability, Docker, Azure deployment foundation, CI/CD, configuration, and security checks.

Required outcomes:

1. Structured logging is consistent across gateway flows.
2. Correlation IDs flow through logs and responses.
3. OpenTelemetry and Application Insights integration are configured or cleanly prepared.
4. Required custom events and metrics from AGENTS.md are emitted or clearly stubbed with documentation.
5. Health checks include basic app/database readiness where appropriate.
6. Docker Compose supports local development with PostgreSQL, gateway-api, and web-admin.
7. Backend and frontend Dockerfiles are production-minded.
8. Azure Bicep foundation exists for Container Apps, PostgreSQL, Key Vault, Application Insights, Log Analytics, ACR, and API Management where feasible.
9. GitHub Actions CI workflow builds/tests backend and builds/lints frontend.
10. Optional deploy-dev workflow is included if reasonable.
11. Configuration documentation clearly distinguishes local, development, and production settings.
12. No real secrets are committed.
13. CORS and provider-call settings are production-safe by default.
14. README files are updated for local and Azure-ready execution.

Do not add AKS, Helm, Redis, Service Bus, Stripe, semantic cache, streaming, or full Entra ID implementation.

Acceptance criteria:
- Docker Compose can be used for local MVP startup or limitations are documented.
- CI workflow is present and logically valid.
- Bicep files are present and validate if tooling is available.
- Observability docs explain logs, metrics, traces, events, and dashboards.
- Security docs explain secret handling and production configuration expectations.
- Build/test/lint commands are run where possible.
- docs/observability.md and docs/deployment.md are created or updated.

At completion, report:
1. Summary of changes
2. Files changed
3. Observability implemented
4. Docker changes
5. Azure/Bicep changes
6. CI/CD changes
7. Commands run
8. Validation results
9. Known limitations
10. Recommended next step
```

---

## Epic 10 — Final MVP Verification and Hardening

```text
You are working on TokenShield AI Gateway.

Before making changes, read AGENTS.md and inspect the entire repository.

Epic objective:
Perform the final MVP verification and hardening pass. This is not a new feature epic. It is a consistency, security, quality, documentation, and demo-readiness pass before calling the MVP complete.

Review and verify:

1. Product scope matches AGENTS.md.
2. Out-of-scope features were not accidentally implemented.
3. Backend builds.
4. Backend tests pass or failures are clearly documented.
5. Frontend builds.
6. Frontend lint passes if configured.
7. Docker Compose is usable or limitations are documented.
8. Database migrations are consistent.
9. Development seed data is idempotent.
10. API key auth is secure.
11. /v1/chat/completions works end-to-end.
12. Routing decisions are explainable.
13. Token and cost calculation work.
14. Budget warning/block/downgrade behavior works.
15. Provider adapters are safe by default.
16. Fallback behavior is controlled and logged.
17. Usage logs are privacy-preserving.
18. Audit logs capture admin mutations.
19. Admin APIs do not expose raw secrets or hashes.
20. Frontend pages are connected or mock gaps are clearly isolated.
21. Observability emits required logs/metrics/events or documents remaining gaps.
22. Documentation is accurate and not stale.
23. No raw API keys, provider secrets, prompts, or responses are logged/stored by default.
24. No real secrets are committed.

Fix issues that are clearly within MVP scope.
Do not add major new features.
Do not implement streaming, semantic cache, prompt compression, LLM router, Stripe, AKS, full Entra ID, or other out-of-scope items.

Create or update docs/mvp-verification-report.md with:

1. Executive summary
2. What is complete
3. What was tested
4. What passed
5. What failed or could not be verified
6. Security/privacy review
7. Known MVP limitations
8. Recommended post-MVP roadmap

Acceptance criteria:
- Repository is internally consistent.
- Main MVP flow can be demonstrated.
- Critical security/privacy issues are fixed or explicitly documented.
- Verification report exists.
- Final README points users to the correct docs and commands.

Run the strongest feasible verification commands and report actual results.

At completion, report:
1. Final MVP status
2. Changes made during hardening
3. Commands run
4. Build/test/lint/Docker/Bicep validation results
5. Remaining risks
6. Recommended next post-MVP epic
```

---

# Optional Post-MVP Prompt — Roadmap Planning

```text
You are working on TokenShield AI Gateway.

Read AGENTS.md and docs/mvp-verification-report.md.

Create docs/post-mvp-roadmap.md.

The roadmap should prioritize sellable enterprise value after the MVP, including:

1. Microsoft Entra ID / enterprise authentication
2. Team and role-based admin authorization
3. Semantic cache
4. Prompt compression
5. Advanced policy engine
6. Advanced budget forecasting
7. Approval workflow
8. Multi-provider benchmarking
9. Provider health scoring
10. Real-time alerts
11. Stripe or enterprise billing integration
12. Deployment hardening
13. Compliance and retention controls
14. Customer onboarding flow
15. Multi-region strategy

For each roadmap item, include business value, technical scope, dependencies, and suggested priority.
```
