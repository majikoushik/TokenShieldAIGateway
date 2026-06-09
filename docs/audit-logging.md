# Audit Logging

TokenShield AI Gateway enforces accountability by writing structured audit trails for all configuration mutations and sensitive tenant lifecycle actions.

---

## Logging Invariants

- **Non-Repudiation**: Every admin mutation generates a database audit record that cannot be modified or soft-deleted.
- **Privacy Enforcement**: Audit details are stored as JSON blobs but are strictly sanitized to exclude raw API keys, provider credentials, and passwords.
- **Actor Identity**: Every log records the email/identity of the actor making the change (e.g. `dev-environment@tokenshield.local` or the logged-in admin email).

---

## Audited Events

Mutations on the following entities generate audit log entries:

- **API Keys**: Creation, revocation, or expiration changes.
- **Budget Limits**: Creation, updates, resets, or threshold actions.
- **Routing Rules**: Creation, updates, deletion, or priority modifications.
- **AI Models & Providers**: Activation, configuration changes, or secret reference updates.

---

## Log Schema

Each entry in the `AuditLogs` table captures:

- **Id**: Guid primary key.
- **TenantId**: Tenant context associated with the action (or `null` for system-wide operations).
- **ActionName**: Name of the mutation action (e.g., `CreateApiKey`, `UpdateBudgetLimit`).
- **EntityName**: Name of the modified database entity (e.g., `ApiKey`, `BudgetLimit`).
- **EntityId**: Primay key Guid of the modified entity.
- **ActorEmail**: Identifiable actor email.
- **DetailsJson**: PostgreSQL JSONB column storing structured metadata of the change (e.g. before/after values, parameters, prefixes) but excluding secrets.
- **CreatedAtUtc**: Timestamp of the mutation event.
