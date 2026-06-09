# Admin APIs Reference

This document describes the Admin REST APIs exposed by **TokenShield AI Gateway** for configuration management, usage statistics, and auditing.

---

## Headers & Context Resolution

All admin endpoints reside under the `/api/admin/` path prefix. In this MVP phase, authentication utilizes request headers for context resolution (with Microsoft Entra ID placeholders structured internally):

- `x-tenant-id` (Guid): The tenant ID to target. If omitted, the gateway defaults to the first tenant (Acme Enterprise).
- `x-user-email` (string): The email of the actor executing the action (used for audit logs). Defaults to `admin@tokenshield.local`.

---

## 1. Provider Management

### List Providers
- **Endpoint**: `GET /api/admin/providers`
- **Response** (`200 OK`):
```json
[
  {
    "id": "e2a39d48-3bb0-4d40-bbd4-5390e1f7c191",
    "name": "Mock Provider",
    "apiUrl": "http://localhost:5000/v1/mock",
    "apiKeySecretRef": "kv-secret-provider-mock-provider",
    "isActive": true,
    "createdAtUtc": "2026-06-09T03:00:00Z",
    "updatedAtUtc": "2026-06-09T03:00:00Z"
  }
]
```

### Create Provider
- **Endpoint**: `POST /api/admin/providers`
- **Request Body**:
```json
{
  "name": "OpenAI",
  "apiUrl": "https://api.openai.com/v1",
  "apiKeySecretRef": "kv-secret-openai",
  "isActive": true
}
```
- **Response** (`201 Created`)

---

## 2. Model Management

### List Models
- **Endpoint**: `GET /api/admin/models`
- **Response** (`200 OK`):
```json
[
  {
    "id": "f5f190e8-07b9-4089-a548-cde813c9aa12",
    "providerId": "e2a39d48-3bb0-4d40-bbd4-5390e1f7c191",
    "providerName": "Mock Provider",
    "name": "mock-standard",
    "deploymentName": "mock-standard-deployment",
    "tier": 1,
    "inputTokenPricePerMillion": 1.000,
    "outputTokenPricePerMillion": 2.000,
    "contextWindow": 16384,
    "isActive": true
  }
]
```

### Create Model
- **Endpoint**: `POST /api/admin/models`
- **Request Body**:
```json
{
  "providerId": "e2a39d48-3bb0-4d40-bbd4-5390e1f7c191",
  "name": "gpt-4o",
  "deploymentName": "gpt-4o",
  "tier": 1,
  "inputTokenPricePerMillion": 2.50,
  "outputTokenPricePerMillion": 10.00,
  "contextWindow": 128000,
  "isActive": true
}
```

---

## 3. Routing Rules

### List Rules
- **Endpoint**: `GET /api/admin/routing-rules`
- **Response** (`200 OK`):
```json
[
  {
    "id": "1ad0c948-26f8-43d9-95e2-6cfab66f8ea9",
    "name": "Low-risk summarization to Cheap",
    "priority": 1,
    "conditionsJson": "[{\"field\":\"riskLevel\",\"operator\":\"Equals\",\"value\":\"low\"}]",
    "action": 0,
    "targetTier": 0,
    "isActive": true
  }
]
```

### Create Routing Rule
- **Endpoint**: `POST /api/admin/routing-rules`
- **Request Body**:
```json
{
  "name": "Block high risk PII",
  "priority": 2,
  "conditionsJson": "[{\"field\":\"containsPii\",\"operator\":\"Equals\",\"value\":\"true\"}]",
  "action": 2,
  "targetTier": null,
  "isActive": true
}
```

---

## 4. Budget Limits

### List Budgets
- **Endpoint**: `GET /api/admin/budgets`
- **Response** (`200 OK`):
```json
[
  {
    "id": "7820da12-c2e8-46bf-8461-846fbd8a002c",
    "scope": 1,
    "targetId": "8bda91a8-cbbd-4ef1-be19-f9a8cf11ab7c",
    "targetName": "Acme Developer Portal",
    "monthlyLimit": 1000.00,
    "warningThresholdPercent": 90.00,
    "currentSpend": 420.50,
    "action": 1,
    "lastResetAtUtc": "2026-06-01T00:00:00Z"
  }
]
```

### Create Budget
- **Endpoint**: `POST /api/admin/budgets`
- **Request Body**:
```json
{
  "scope": 1,
  "targetId": "8bda91a8-cbbd-4ef1-be19-f9a8cf11ab7c",
  "monthlyLimit": 500.00,
  "warningThresholdPercent": 80.00,
  "action": 0
}
```

---

## 5. API Keys

### Create API Key
- **Endpoint**: `POST /api/admin/api-keys`
- **Request Body**:
```json
{
  "clientApplicationId": "8bda91a8-cbbd-4ef1-be19-f9a8cf11ab7c",
  "name": "New API Key",
  "prefix": "ts_live_"
}
```
- **Response** (`201 Created` - Exposes the raw key exactly **once**):
```json
{
  "id": "c8da9a81-d1f2-4919-86f2-1a2c3d4e5f6a",
  "name": "New API Key",
  "prefix": "ts_live_",
  "rawKey": "ts_live_d8a9fbc18c0e2a39fd843e...",
  "expiresAtUtc": "2027-06-09T03:00:00Z"
}
```

### List API Keys
- **Endpoint**: `GET /api/admin/api-keys`
- **Response** (`200 OK` - Excludes sensitive raw key and key hashes):
```json
[
  {
    "id": "c8da9a81-d1f2-4919-86f2-1a2c3d4e5f6a",
    "clientApplicationId": "8bda91a8-cbbd-4ef1-be19-f9a8cf11ab7c",
    "clientApplicationName": "Acme Developer Portal",
    "name": "New API Key",
    "prefix": "ts_live_",
    "lastUsedAtUtc": null,
    "expiresAtUtc": "2027-06-09T03:00:00Z",
    "isRevoked": false,
    "createdAtUtc": "2026-06-09T03:00:00Z"
  }
]
```

### Revoke API Key
- **Endpoint**: `POST /api/admin/api-keys/{id}/revoke`
- **Response**: `240 NoContent`

---

## 6. Usage Analytics

### Query Logs
- **Endpoint**: `GET /api/admin/usage-analytics/logs`
- **Parameters**: `page`, `pageSize`, `startDate`, `endDate`, `applicationId`, `provider`, `model`, `tier`, `status`, `budgetStatus`.
- **Response**: Paginated request log details with prompts and responses hashed for privacy.

### Dashboard Summary
- **Endpoint**: `GET /api/admin/usage-analytics/summary`
- **Response**:
```json
{
  "totalCost": 1245.89,
  "totalRequests": 10500,
  "totalInputTokens": 1500000,
  "totalOutputTokens": 300000,
  "averageLatencyMs": 185.5,
  "costByProvider": [
    { "groupKey": "OpenAI", "cost": 900.50, "requestCount": 7000 }
  ]
}
```

---

## 7. Configuration Settings

### Get Catalog
- **Endpoint**: `GET /api/admin/settings/catalog`
- **Response**: Available tiers, scopes, actions, and routing actions.
