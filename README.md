# TokenShield AI Gateway

> **Production-grade AI FinOps and model-routing gateway for enterprises.**

TokenShield sits between your client applications and downstream AI model providers (OpenAI, Azure OpenAI, Anthropic). It enforces budget policies, routes requests to the right model tier, tracks costs, and gives operators full visibility via an admin console.

[![CI](https://github.com/your-org/TokenShieldAIGateway/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/TokenShieldAIGateway/actions/workflows/ci.yml)

---

## Quick Start (Docker Compose)

```bash
# Clone and start all services
git clone https://github.com/your-org/TokenShieldAIGateway.git
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
├── apps/
│   ├── gateway-api/          # .NET 8 Clean Architecture AI gateway
│   └── web-admin/            # Next.js TypeScript admin console
├── docs/                     # Product specs & architecture docs
│   ├── observability.md      # Logging, tracing, metrics guide
│   └── deployment.md         # Local + Azure deployment guide
├── infra/
│   ├── bicep/                # Azure Bicep deployment foundation
│   └── README.md             # Infrastructure setup guide
├── .github/
│   └── workflows/            # GitHub Actions CI/CD
├── docker-compose.yml        # Local development stack
└── README.md
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

## Azure Deployment

See [docs/deployment.md](docs/deployment.md) for full instructions including Bicep deployment, Key Vault secret management, and CI/CD setup.

---

## Documentation

| Document | Description |
|---|---|
| [Product Spec](docs/product-spec.md) | MVP goals and feature scope |
| [Architecture](docs/architecture.md) | System design and component overview |
| [Database Schema](docs/database-schema.md) | Entity model and EF Core design |
| [Gateway API](docs/chat-completions-endpoint.md) | OpenAI-compatible gateway endpoint |
| [Routing Rules](docs/routing-rules.md) | Rule engine and routing logic |
| [Cost Engine](docs/cost-engine.md) | Token estimation and cost calculation |
| [Budget Enforcement](docs/budget-enforcement.md) | Budget limits and actions |
| [Provider Adapters](docs/provider-adapters.md) | Provider abstraction and adapters |
| [Admin API](docs/admin-api.md) | Admin REST API reference |
| [Observability](docs/observability.md) | Logging, tracing, metrics, AppInsights |
| [Deployment](docs/deployment.md) | Local + Azure deployment guide |
| [MVP Verification](docs/mvp-verification-report.md) | Final MVP test results and security review |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8, ASP.NET Core, EF Core, PostgreSQL |
| Frontend | Next.js 14, TypeScript, Tailwind CSS |
| Observability | Serilog, OpenTelemetry, Application Insights |
| Infrastructure | Azure Container Apps, PostgreSQL Flexible Server, Key Vault |
| CI/CD | GitHub Actions (OIDC, no stored secrets) |
