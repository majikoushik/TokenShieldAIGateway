# TokenShield Infrastructure

Azure deployment foundation using [Azure Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/).

## Azure Architecture

```
Azure Container Apps Environment
  ├── tokenshield-gateway   (gateway-api, port 5000, internal + external ingress)
  └── tokenshield-webadmin  (web-admin, port 3000, external ingress)

Azure Database for PostgreSQL Flexible Server
Azure Container Registry (ACR)
Azure Key Vault  (provider credentials, pepper, secrets)
Azure Application Insights + Log Analytics Workspace
```

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) ≥ 2.58
- [Bicep CLI](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/install) ≥ 0.28
- An Azure subscription and a resource group

## Quick Deploy

```bash
# 1. Log in
az login

# 2. Create a resource group (once)
az group create \
  --name rg-tokenshield-dev \
  --location eastus

# 3. Preview changes
az deployment group what-if \
  --resource-group rg-tokenshield-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/main.bicepparam

# 4. Deploy
az deployment group create \
  --resource-group rg-tokenshield-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/main.bicepparam
```

## Secrets Management

**Never store secrets in bicepparam or source control.**

After initial deployment:

1. Open the Key Vault in the Azure Portal.
2. Add the following secrets manually or via `az keyvault secret set`:

| Secret Name | Description |
|---|---|
| `ApiKeyPepper` | Random 256-bit string for API key hashing |
| `DbPassword` | PostgreSQL admin password |
| `OpenAiApiKey` | OpenAI provider key (if using real OpenAI) |
| `AzureOpenAiApiKey` | Azure OpenAI key (if using Azure OpenAI) |
| `AnthropicApiKey` | Anthropic Claude key (if using Anthropic) |

The gateway-api Container App is granted Key Vault Secret Reader via Managed Identity — no credentials stored in app config.

## Environment Variables

| Variable | Container App | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | gateway-api | PostgreSQL connection string (auto-set by Bicep) |
| `ApplicationInsights__ConnectionString` | gateway-api | Application Insights connection string (auto-set) |
| `Cors__AllowedOrigins__0` | gateway-api | Web admin origin URL |
| `Providers__UseRealProviders` | gateway-api | `true` to enable real LLM provider calls |
| `NEXT_PUBLIC_API_URL` | web-admin | Gateway API URL seen from browser |

## CI/CD

See `.github/workflows/` for CI and CD pipelines.
The CD pipeline uses OIDC federated credentials — no long-lived service principal secrets stored in GitHub.
