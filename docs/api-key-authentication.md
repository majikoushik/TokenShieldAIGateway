# TokenShield API Key Authentication

TokenShield secures model access endpoints using API Key credentials. This document describes the security standards, key formats, hashing structure, and context resolution workflows.

---

## 1. Credentials Header Contract
All client requests directed through protected gateway API routes (prefixed with `/v1/`) must provide:
- **Header Key**: `x-api-key`
- **Header Value**: `<raw_api_key>`

Example HTTP request:
```http
POST /v1/chat/completions HTTP/1.1
Host: gateway.tokenshield.local
x-api-key: ts_live_58c278912ef32ab1a04e5cd9b2c340
Content-Type: application/json
```

---

## 2. API Key Prefixes & Formats
TokenShield separates environments using explicit key prefixes:
1. **Live Production Keys**: Start with `ts_live_` (e.g. `ts_live_xxxx...`).
2. **Local Sandbox Development Keys**: Start with `ts_dev_` (e.g. `ts_dev_xxxx...`).

A raw key comprises the prefix followed by a cryptographically secure 48-character hex string representing 24 random bytes.

---

## 3. Cryptographic Hashing Model
To preserve security, **raw API keys are never stored inside the database, logged to persistent files, or exposed in administrative list endpoints**.
- **Hashing Algorithm**: SHA-256
- **Database Column**: `KeyHash` (`varchar(256)`)
- **Key Resolution**: When a client issues a request, the gateway computes the SHA-256 hash of the incoming header value and queries `ApiKeys` by hash matching.
- **Single Disclosure**: The raw API key string is returned *only once* in the payload response of the key creation endpoint (`POST /api/dev/api-keys`). It cannot be retrieved again.

---

## 4. Telemetry Correlation Context
To facilitate distributed request tracing, TokenShield supports:
- **Correlation Header**: `x-correlation-id`
- **Workflow**:
  - The `CorrelationIdMiddleware` checks the headers for an incoming `x-correlation-id` key.
  - If missing, it generates a new Guid correlation trace.
  - Registers the trace inside the scoped request container `IRequestContext`.
  - Appends `x-correlation-id` to the outgoing HTTP response header, ensuring clients can trace gateway execution logs.
