# TokenShield AI Gateway Product Specification

## Executive Summary
TokenShield AI Gateway is an enterprise-grade AI FinOps and model-routing platform. By positioning itself as a central proxy between client applications and downstream AI model providers (such as OpenAI, Azure OpenAI, and Anthropic), TokenShield enables companies to monitor, route, audit, and limit their artificial intelligence spend while maintaining security and privacy by default.

---

## 1. Core Objectives & Problem Statement
Enterprise adoption of LLMs faces several obstacles:
1. **Unchecked Cost**: Developers using premium models (e.g., GPT-4o, Claude 3.5 Sonnet) for simple tasks like classification or translation.
2. **Lack of Visibility**: No consolidated dashboard showing which internal department or application is spending the most.
3. **Budget Governance**: Inability to cap spending *before* downstream bills are incurred.
4. **Reliability/Lock-in**: Single provider outages causing full service failure.
5. **Security Audits**: The need to audit LLM usage without storing sensitive prompts or PII in plaintext database logs.

TokenShield solves this by routing requests through a policy-driven gateway proxy.

---

## 2. Scope of the MVP

### In-Scope
1. **OpenAI-Compatible Gateway API**: Supports `POST /v1/chat/completions`.
2. **API Key Authentication**: Secure, hashed client credentials.
3. **Tenant & Provider Catalog**: Management of LLM providers and underlying endpoints.
4. **Model Tiers**: Logic tiers (`cheap`, `standard`, `premium`) for auto-routing.
5. **Rule-Based Routing Engine**: Context-aware model routing based on metadata.
6. **Request Profiling**: Automatic inference of task type, complexity, and PII presence.
7. **Token & Cost Estimation**: Instant character-based pre-call estimations.
8. **Budget Enforcement**: Hard and soft caps at tenant, app, key, or model scope.
9. **Provider Adapters**: Integration with Azure OpenAI, OpenAI, Anthropic, and a local Mock provider.
10. **Resilience & Fallbacks**: Retry logic and fallback routing on downstream failures.
11. **Privacy-Preserving Logs**: Audit and usage logs saving only prompt/response hashes and token metadata.
12. **Admin Console**: Next.js single-tenant / multi-tenant dashboard.

### Out-of-Scope (Deferred to Future Phases)
1. Streaming completions (`stream: true` is rejected with a validation error).
2. Semantic caching.
3. ML-based cost optimization or LLM-based intelligent routers.
4. Stripe or external subscription billing.
5. Full SAML/SSO integration.
6. Redis cache or service bus infrastructure.

---

## 3. Key Domain Concepts

### Tenant
An enterprise customer representing an isolated organization. Multi-tenancy is enforced in all API requests and database queries.

### Client Application
An internal or external software application owned by a tenant. Each request must be attributed to an application.

### Provider
An upstream model vendor (e.g., Azure OpenAI). Credentials for providers are stored securely as Key Vault references.

### AI Model
A specific model deployment (e.g., `gpt-4o-mini`) mapped to a tier, provider, and deployment pricing.

### Model Tier
Classification of models to simplify application routing:
- **Cheap**: Fast, cost-efficient models for low-risk tasks (e.g., GPT-4o-mini, Claude 3 Haiku).
- **Standard**: Well-rounded models for general-purpose requests (e.g., GPT-4o, Claude 3.5 Sonnet).
- **Premium**: Advanced reasoning or highly specialized models (e.g., GPT-4, Claude 3 Opus, o1).

### Budget Limit
Monthly financial limits configured with three distinct enforcement options:
- `warn_only`: Logs a warning event but allows the request.
- `block`: Fails the request before downstream call is made.
- `downgrade`: Downgrades the request to a cheaper model tier.

---

## 4. Lifecycle of a Request
```text
[Client App] ──> [API Authentication] ──> [Request Profiler] ──> [Budget Check]
                                                                        |
[OpenAI Response] <── [Usage Log] <── [Provider Call] <── [Routing Rules]
```

1. **Authenticate**: Resolve API key, Tenant, and Application.
2. **Profile & Estimate**: Check character count, check PII, determine complexity.
3. **Validate Budgets**: Reject request if budget is exceeded and action is set to `block`.
4. **Evaluate Rules**: Map request profile signals (e.g., `riskLevel`, `complexityScore`) to selected model tier.
5. **Select Model & Adapter**: Match chosen tier to active providers.
6. **Execute Polly resilience**: Retry transient errors. Fallback to another tier if the selected model is down.
7. **Compute Final Cost**: Update monthly budget spend and write to usage log (storing prompt/response hashes).
8. **Return Response**: Send standard OpenAI response back to client with custom `routing` header/metadata.
