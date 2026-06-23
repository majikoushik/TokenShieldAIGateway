# Deployment Guide

## Local Development (Docker Compose)

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v2

### Start All Services

```bash
# From repository root
docker compose up --build
```

This starts three services:

| Service | URL | Description |
|---|---|---|
| PostgreSQL | `localhost:5432` | Database (user: postgres, pw: postgres) |
| gateway-api | `http://localhost:5000` | .NET 8 AI Gateway |
| web-admin | `http://localhost:3000` | Next.js Admin Console |

The gateway-api automatically runs EF Core migrations and seeds development data on startup.

### Development Data

After first startup, the following seed data is available:

- **Tenant**: `dev-tenant`
- **API Key**: A `ts_dev_xxx` key is printed to the gateway-api logs on first seed
- **Providers**: Mock, Azure OpenAI (placeholder), OpenAI (placeholder), Anthropic (placeholder)
- **Models**: One model per tier (cheap, standard, premium) under the mock provider
- **Routing Rules**: Sample rules for low/medium/high-risk routing

To get the development API key:

```bash
# Create a development API key via API
curl -X POST http://localhost:5000/api/dev/api-keys \
  -H "Content-Type: application/json" \
  -d '{"name": "local-dev", "description": "Local dev key"}'
```

### Swagger UI

Available at `http://localhost:5000/swagger` in Development mode.

### Stopping

```bash
docker compose down           # Stop containers (preserve data)
docker compose down -v        # Stop + remove PostgreSQL data volume
```

---

## Azure Deployment (Bicep)

> [!WARNING]
> **IaC Networking Status**: Public network access for PostgreSQL, Key Vault, and Monitoring has been deliberately disabled to harden the infrastructure for portfolio review. However, the subsequent required **Private Networking / VNet integration** for Container Apps has not been fully implemented or validated. Deployments using these Bicep files may fail to interconnect in Azure until VNet integration is completed and tested.

### Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) ≥ 2.58
- [Bicep CLI](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/install) ≥ 0.28
- An Azure subscription
- Contributor role on the target subscription or resource group

### Step 1: Create Resource Group

```bash
az group create \
  --name rg-tokenshield-dev \
  --location eastus
```

### Step 2: Configure Parameters

Edit `infra/bicep/main.bicepparam` with your environment values.

> **Never commit real passwords.** Pass `dbAdminPassword` via CLI or Key Vault reference.

### Step 3: Preview Deployment

```bash
az deployment group what-if \
  --resource-group rg-tokenshield-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/main.bicepparam \
  --parameters dbAdminPassword="$(openssl rand -base64 24)"
```

### Step 4: Deploy

```bash
export DB_PASSWORD=$(openssl rand -base64 24)
az deployment group create \
  --resource-group rg-tokenshield-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/main.bicepparam \
  --parameters dbAdminPassword="$DB_PASSWORD"
```

Save `DB_PASSWORD` to Azure Key Vault immediately:

```bash
KV_NAME=$(az deployment group show \
  --resource-group rg-tokenshield-dev \
  --name main \
  --query properties.outputs.keyVaultName.value -o tsv)

az keyvault secret set --vault-name "$KV_NAME" --name "DbPassword" --value "$DB_PASSWORD"
```

### Step 5: Run Migrations

Run EF Core migrations against the Azure database (one-time, or after schema changes):

```bash
cd apps/gateway-api
dotnet ef database update \
  --project src/TokenShield.Infrastructure \
  --startup-project src/TokenShield.Api \
  --connection "Host=<fqdn>;Database=tokenshield;Username=tsadmin;Password=$DB_PASSWORD;Ssl Mode=VerifyFull"
```

### Step 6: Verify

```bash
GATEWAY_URL=$(az deployment group show \
  --resource-group rg-tokenshield-dev \
  --name main \
  --query properties.outputs.gatewayApiUrl.value -o tsv)

curl "$GATEWAY_URL/health"
curl "$GATEWAY_URL/health/ready"
```

---

## Environment Variables Reference

### gateway-api

| Variable | Required | Default | Description |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | ✅ | - | PostgreSQL connection string |
| `ASPNETCORE_ENVIRONMENT` | - | `Development` | `Development` or `Production` |
| `SeedDatabase` | - | `true` | Set to `false` in production |
| `ApplicationInsights__ConnectionString` | - | _(blank)_ | Enables cloud telemetry when set |
| `OpenTelemetry__ServiceName` | - | `tokenshield-gateway` | Service name in traces |
| `Cors__AllowedOrigins__0` | - | - | Allowed origin (Production CORS) |
| `ProviderSettings__EnableRealCalls` | - | `false` | Enable real LLM provider calls |

### web-admin

| Variable | Required | Default | Description |
|---|---|---|---|
| `NEXT_PUBLIC_API_URL` | ✅ | `http://localhost:5000` | Gateway API base URL |
| `NODE_ENV` | - | `production` | Node environment |
| `PORT` | - | `3000` | HTTP port |

---

## CI/CD (GitHub Actions)

See `.github/workflows/`:

| Workflow | Trigger | Description |
|---|---|---|
| `ci.yml` | Push/PR to `main` | Build + test backend; lint + build frontend |
| `cd-staging.yml` | Push to `main` / manual | Build images → push to ACR → update Container Apps |

### Required GitHub Secrets for CD

| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | App registration client ID (OIDC federated) |
| `AZURE_TENANT_ID` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `ACR_NAME` | ACR name without `.azurecr.io` |
| `RESOURCE_GROUP` | Azure resource group name |
| `GATEWAY_APP_NAME` | Container App name for gateway-api |
| `WEBADMIN_APP_NAME` | Container App name for web-admin |

### OIDC Setup

The CD pipeline uses OIDC (no stored passwords). Configure a federated credential on your App Registration:

- **Issuer**: `https://token.actions.githubusercontent.com`
- **Subject**: `repo:<org>/<repo>:environment:staging`
- **Audience**: `api://AzureADTokenExchange`
