# TokenShield AI Gateway Database Schema

TokenShield utilizes a PostgreSQL database using Entity Framework Core. This document details the tables, relations, precision constraints, indexes, and isolation policies.

---

## 1. Entity Architecture & Multi-Tenancy
To support multi-tenancy, critical tables contain a `TenantId` reference. Composite indexes on `(TenantId, IsDeleted)` optimize partition lookups and guarantee isolation.

### Common Base Properties
All primary entities inherit tracking metadata:
- **Id**: `Guid` (Primary Key, default `Guid.NewGuid()`).
- **CreatedAtUtc**: `timestamp with time zone` (Populated automatically on creation).
- **UpdatedAtUtc**: `timestamp with time zone` (Modified automatically on updates).
- **IsDeleted**: `boolean` (Soft delete indicator).

---

## 2. Table Specifications

### 2.1 Tenants
Tracks Tenant entities. Soft-deletable.
- `Id`: `Guid` (PK)
- `Name`: `varchar(200)` (Required)
- `CreatedAtUtc` / `UpdatedAtUtc` / `IsDeleted`

### 2.2 ClientApplications
Registered software applications utilizing the gateway.
- `Id`: `Guid` (PK)
- `TenantId`: `Guid` (FK referencing Tenants)
- `Name`: `varchar(200)` (Required)
- `CreatedAtUtc` / `UpdatedAtUtc` / `IsDeleted`
- **Indexes**:
  - `IX_ClientApplications_TenantId`
  - `IX_ClientApplications_TenantId_IsDeleted`

### 2.3 ApiKeys
SHA-256 hashed keys for client application authorization.
- `Id`: `Guid` (PK)
- `TenantId`: `Guid` (FK referencing Tenants)
- `ClientApplicationId`: `Guid` (FK referencing ClientApplications)
- `Name`: `varchar(200)` (Required)
- `KeyHash`: `varchar(256)` (Required, Unique index)
- `Prefix`: `varchar(20)` (Prefix identifier e.g. `ts_live_` or `ts_dev_`)
- `LastUsedAtUtc`: `timestamp with time zone` (Nullable)
- `ExpiresAtUtc`: `timestamp with time zone` (Nullable)
- `IsRevoked`: `boolean`
- `CreatedAtUtc` / `UpdatedAtUtc` / `IsDeleted`
- **Indexes**:
  - `IX_ApiKeys_KeyHash` (Unique)
  - `IX_ApiKeys_TenantId`
  - `IX_ApiKeys_TenantId_IsDeleted`

### 2.4 ModelProviders
Upstream hosting endpoints configurations.
- `Id`: `Guid` (PK)
- `TenantId`: `Guid` (FK referencing Tenants)
- `Name`: `varchar(100)` (Required)
- `ApiUrl`: `varchar(500)` (Required)
- `ApiKeySecretRef`: `varchar(500)` (Required KeyVault Secret key)
- `IsActive`: `boolean`
- `CreatedAtUtc` / `UpdatedAtUtc` / `IsDeleted`
- **Indexes**:
  - `IX_ModelProviders_TenantId`
  - `IX_ModelProviders_TenantId_IsDeleted`

### 2.5 AiModels
Registered models mapped to providers.
- `Id`: `Guid` (PK)
- `ProviderId`: `Guid` (FK referencing ModelProviders)
- `Name`: `varchar(100)` (Required)
- `DeploymentName`: `varchar(200)` (Required Deployment identifier)
- `Tier`: `varchar(50)` (String-converted enum: `Cheap`, `Standard`, `Premium`)
- `InputTokenPricePerMillion`: `decimal(18,4)`
- `OutputTokenPricePerMillion`: `decimal(18,4)`
- `ContextWindow`: `integer`
- `IsActive`: `boolean`
- `CreatedAtUtc` / `UpdatedAtUtc` / `IsDeleted`
- **Indexes**:
  - `IX_AiModels_ProviderId`

### 2.6 RoutingRules
Tenant routing rule priority maps.
- `Id`: `Guid` (PK)
- `TenantId`: `Guid` (FK referencing Tenants)
- `Name`: `varchar(200)` (Required)
- `Priority`: `integer`
- `ConditionsJson`: `jsonb` (Rule expressions array)
- `Action`: `varchar(50)` (String-converted enum: `RouteToTier`, `HumanReview`, `Block`)
- `TargetTier`: `varchar(50)` (String-converted enum: `Cheap`, `Standard`, `Premium`, Nullable)
- `IsActive`: `boolean`
- `CreatedAtUtc` / `UpdatedAtUtc` / `IsDeleted`
- **Indexes**:
  - `IX_RoutingRules_TenantId`
  - `IX_RoutingRules_TenantId_IsDeleted`

### 2.7 BudgetLimits
Financial spend tracking allocations.
- `Id`: `Guid` (PK)
- `TenantId`: `Guid` (FK referencing Tenants)
- `Scope`: `varchar(50)` (String-converted enum: `Tenant`, `Application`, `ApiKey`, `Model`)
- `TargetId`: `Guid` (Target entity reference matching scope, Nullable)
- `MonthlyLimit`: `decimal(18,4)`
- `WarningThresholdPercent`: `decimal(5,2)`
- `CurrentSpend`: `decimal(18,4)`
- `LastResetAtUtc`: `timestamp with time zone`
- `Action`: `varchar(50)` (String-converted enum: `WarnOnly`, `Block`, `Downgrade`)
- `CreatedAtUtc` / `UpdatedAtUtc` / `IsDeleted`
- **Indexes**:
  - `IX_BudgetLimits_TenantId`
  - `IX_BudgetLimits_TenantId_Scope_TargetId`

### 2.8 AiRequestLogs
Telemetric access metrics (Append-only audit trail logs).
- `Id`: `Guid` (PK)
- `CorrelationId`: `Guid` (Trace tracking)
- `RequestId`: `varchar(100)` (Unique ID)
- `TenantId`: `Guid`
- `ApplicationId`: `Guid`
- `ApiKeyId`: `Guid`
- `PromptHash`: `varchar(64)` (SHA-256 string hash)
- `ResponseHash`: `varchar(64)` (SHA-256 string hash)
- `InputTokens`: `integer`
- `OutputTokens`: `integer`
- `EstimatedCost`: `decimal(18,6)`
- `SelectedProvider`: `varchar(100)`
- `SelectedModel`: `varchar(100)`
- `SelectedTier`: `varchar(50)`
- `MatchedRuleName`: `varchar(200)` (Nullable)
- `FallbackUsed`: `boolean`
- `BudgetStatus`: `varchar(50)`
- `RequestStatus`: `varchar(50)` (Success, Failed, Blocked)
- `LatencyMs`: `integer`
- `CreatedAtUtc`: `timestamp with time zone`
- **Indexes**:
  - `IX_AiRequestLogs_TenantId`
  - `IX_AiRequestLogs_CorrelationId`
  - `IX_AiRequestLogs_CreatedAtUtc`

### 2.9 AuditLogs
System administration updates history (Append-only logs).
- `Id`: `Guid` (PK)
- `TenantId`: `Guid` (FK referencing Tenants, Nullable)
- `ActionName`: `varchar(100)`
- `EntityName`: `varchar(100)`
- `EntityId`: `Guid`
- `ActorEmail`: `varchar(256)`
- `DetailsJson`: `jsonb` (Before/After mutations maps)
- `CreatedAtUtc`: `timestamp with time zone`
- **Indexes**:
  - `IX_AuditLogs_TenantId`
  - `IX_AuditLogs_CreatedAtUtc`
