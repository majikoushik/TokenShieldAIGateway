# TokenShield AI Gateway

> **Production-minded MVP and portfolio-grade enterprise AI gateway foundation.**

TokenShield sits between your client applications and downstream AI model providers (OpenAI, Azure OpenAI, Anthropic). It enforces budget policies, routes requests to the right model tier, tracks costs, and gives operators full visibility via an admin console.

[![CI](https://github.com/majikoushik/TokenShieldAIGateway/actions/workflows/ci.yml/badge.svg)](https://github.com/majikoushik/TokenShieldAIGateway/actions/workflows/ci.yml)

---

## Current Implementation Status

| Feature | Status | Description |
|---|---|---|
| **API Key Auth** | Implemented | Validates hashed API keys via middleware. |
| **Model Routing** | Implemented | Routes requests to `cheap`, `standard`, or `premium` tiers based on rule evaluation. |
| **Pre-call Budgets** | Implemented | Validates budget limits before executing LLM calls. |
| **Privacy Logging** | Implemented | Logs cryptographic hashes instead of raw prompts/responses. |
| **Admin Auth** | Demo / Development | Relies on HTTP headers in Dev; Production implementation requires Azure Entra ID mapping. |
| **Real AI Providers** | Scaffolded | Integrated adapters exist, but `ProviderSettings__EnableRealCalls` defaults to `false` (mocked) to avoid unexpected costs. |

---

## Quick Start (Docker Compose)

```bash
# Clone and start all services
git clone https://github.com/majikoushik/TokenShieldAIGateway.git
cd TokenShieldAIGateway
docker compose up --build
```

| Service | URL |
|---|---|
| Gateway API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Admin Console | http://localhost:3000 |
| Health (liveness) | http://localhost:5000/health |
| Health (readiness) | http://localhost:5000/health/ready |

Create a development API key:

```bash
curl -X POST http://localhost:5000/api/dev/api-keys \
  -H "Content-Type: application/json" \
  -d '{"name":"local-dev","description":"Local dev key"}'
```

Test the gateway:

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "x-api-key: ts_dev_<key-from-above>" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "auto",
    "messages": [{"role": "user", "content": "Summarize this case"}],
    "metadata": {"taskType": "summarization", "riskLevel": "low"}
  }'
```

---

## Repository Structure

```text
.
|-- apps/
|   |-- gateway-api/          # .NET 8 Clean Architecture AI gateway
|   `-- web-admin/            # Next.js TypeScript admin console
|-- docs/                     # Product specs & architecture docs
|   |-- adr/                  # Architecture Decision Records
|   |-- observability.md      # Logging, tracing, metrics guide
|   `-- deployment.md         # Local + Azure deployment guide
|-- infra/
|   |-- bicep/                # Azure Bicep deployment foundation
|   `-- README.md             # Infrastructure setup guide
|-- .github/
|   `-- workflows/            # GitHub Actions CI/CD
|-- docker-compose.yml        # Local development stack
`-- README.md
```

---

## Option 1: Local (Bare Metal)

### Backend

```bash
cd apps/gateway-api/src/TokenShield.Api
dotnet run
```

Requires PostgreSQL running locally. Update `appsettings.Development.json` with your connection string.

### Frontend

```bash
cd apps/web-admin
npm install
npm run dev
```

---

## Option 2: Docker Compose

```bash
docker compose up --build       # Start all services
docker compose down             # Stop (preserve data)
docker compose down -v          # Stop + wipe PostgreSQL data
```

---

## Azure Deployment Foundation

See [docs/deployment.md](docs/deployment.md) for full instructions including Bicep deployment, Key Vault secret management, and CI/CD setup. This is a baseline setup intended for Development/Staging environments.

---

## MVP Verification Matrix

| Command | Purpose | Expected Result | Actual Result |
|---|---|---|---|
| `dotnet build` | Verify backend compilation | 0 errors | 0 errors (06/2026) |
| `dotnet test` | Verify integration tests | All tests pass | Aborted (missing .NET 8 local runtime) (06/2026) |
| `npm run build` | Verify frontend compilation | Successful static build | Aborted (missing local npm) (06/2026) |
| `docker compose config` | Verify container layout | Valid config output | Valid config (06/2026) |
| `rg "RawKey"` | Check for leaked secrets | Exists only in DTOs, 0 in seed logs | Clean (06/2026) |

---

## Documentation

| Document | Description |
|---|---|
| [Product Spec](docs/product-spec.md) | MVP goals and feature scope |
| [Architecture](docs/architecture.md) | System design and component overview |
| [ADRs](docs/adr/) | Key Architecture Decision Records |
| [Database Schema](docs/database-schema.md) | Entity model and EF Core design |
| [Gateway API](docs/chat-completions-endpoint.md) | OpenAI-compatible gateway endpoint |
| [Routing Rules](docs/routing-rules.md) | Rule engine and routing logic |
| [Cost Engine](docs/cost-engine.md) | Token estimation and cost calculation |
| [Budget Enforcement](docs/budget-enforcement.md) | Budget limits and actions |
| [Provider Adapters](docs/provider-adapters.md) | Provider abstraction and adapters |
| [Admin API](docs/admin-api.md) | Admin REST API reference |
| [Observability](docs/observability.md) | Logging, tracing, metrics, AppInsights |
| [Deployment](docs/deployment.md) | Local + Azure dev/staging deployment guide |
| [MVP Verification](docs/mvp-verification-report.md) | Final MVP test results and security review |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8, ASP.NET Core, EF Core, PostgreSQL |
| Frontend | Next.js 16, TypeScript, Tailwind CSS |
| Observability | Serilog, OpenTelemetry, Application Insights |
| Infrastructure | Azure Container Apps, PostgreSQL Flexible Server, Key Vault |
| CI/CD | GitHub Actions (OIDC, no stored secrets) |

> [!WARNING]
> **IaC Networking Status**: Public network access for PostgreSQL, Key Vault, and Monitoring has been deliberately disabled in the Bicep templates to harden the infrastructure for portfolio review. However, the subsequent required **Private Networking / VNet integration** for Container Apps has not been fully implemented or validated. Deployments using these Bicep files may fail to interconnect in Azure until VNet integration is completed and tested.
