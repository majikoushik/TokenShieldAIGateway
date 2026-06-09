# TokenShield Development Seed Data Specification

When running the application in a local `Development` environment and with `"SeedDatabase": true` enabled, TokenShield will automatically create idempotent seed data to enable rapid local testing.

---

## 1. Demo Tenant & Team
- **Tenant Name**: `Acme Enterprise`
- **Primary Client App**: `Acme Developer Portal`
- **Initial Audit Actor**: `admin@acme.com`

---

## 2. API Key Access
We seed a primary hashed development API key. This raw key is ready for immediate proxy call testing:
- **Credential Header**: `x-api-key`
- **Raw API Key**: `ts_dev_acmedeveloperkey12345`
- **Prefix**: `ts_dev_`
- **Key Hash (Stored in Database)**: `cf53c1fb85e3a891e4a13d72cd1a4030612c6a084c7942cc65a23053bb09a56e`
- **Expiry**: 1 Year from creation.

---

## 3. Seeded Providers & Models Catalog

### 3.1 Mock Provider
Local mock target for sandbox debugging.
- **API URL**: `http://localhost:5000/v1/mock`
- **Models**:
  - `mock-cheap` (Tier: Cheap | Input: $0.10/M | Output: $0.20/M | Context: 8K)
  - `mock-standard` (Tier: Standard | Input: $1.00/M | Output: $2.00/M | Context: 16K)
  - `mock-premium` (Tier: Premium | Input: $10.00/M | Output: $20.00/M | Context: 32K)

### 3.2 OpenAI
- **API URL**: `https://api.openai.com/v1`
- **Models**:
  - `gpt-4o-mini` (Tier: Cheap | Input: $0.15/M | Output: $0.60/M | Context: 128K)
  - `gpt-4o` (Tier: Standard | Input: $2.50/M | Output: $10.00/M | Context: 128K)
  - `o1-preview` (Tier: Premium | Input: $15.00/M | Output: $60.00/M | Context: 128K)

### 3.3 Azure OpenAI
- **API URL**: `https://acme-openai.openai.azure.com`
- **Models**:
  - `gpt-4o-mini` (Tier: Cheap | DeployName: `deploy-gpt-4o-mini` | Input: $0.15/M | Output: $0.60/M | Context: 128K)
  - `gpt-4o` (Tier: Standard | DeployName: `deploy-gpt-4o` | Input: $2.50/M | Output: $10.00/M | Context: 128K)

### 3.4 Anthropic
- **API URL**: `https://api.anthropic.com/v1`
- **Models**:
  - `claude-3-5-haiku` (Tier: Cheap | Input: $0.80/M | Output: $4.00/M | Context: 200K)
  - `claude-3-5-sonnet` (Tier: Standard | Input: $3.00/M | Output: $15.00/M | Context: 200K)
  - `claude-3-opus` (Tier: Premium | Input: $15.00/M | Output: $75.00/M | Context: 200K)

---

## 4. Budgets Limits
- **Tenant Budget**:
  - Scope: Tenant
  - Limit: $5,000.00 / month
  - Warning Threshold: 80%
  - Current Spend: $1,245.89
  - Action: `WarnOnly`
- **Application Budget**:
  - Scope: Application (`Acme Developer Portal`)
  - Limit: $1,000.00 / month
  - Warning Threshold: 90%
  - Current Spend: $420.50
  - Action: `Block`

---

## 5. Seeded Routing Rules
- **Rule 1**: `"Low-risk summarization to Cheap"`
  - Priority: 1
  - Action: `RouteToTier` (Target: `Cheap`)
  - Conditions: `riskLevel == low` AND `taskType == summarization`
- **Rule 2**: `"High complexity requests to Premium"`
  - Priority: 2
  - Action: `RouteToTier` (Target: `Premium`)
  - Conditions: `complexityScore > 80`
- **Rule 3**: `"Block suspicious PII request logs"`
  - Priority: 3
  - Action: `Block`
  - Conditions: `containsPii == true`
