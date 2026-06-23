# ADR 003: Privacy-Preserving Usage and Audit Logging

## Status
Accepted

## Context
TokenShield processes highly sensitive LLM prompts and responses. Storing these raw payloads in central databases for observability risks leaking PII, proprietary information, and API secrets.

## Decision
We decided to securely hash (`SHA-256`) prompts and responses, storing only the hashes (`PromptHash`, `ResponseHash`) alongside request metadata (token counts, latency, selected tier). Raw prompts and responses are **never** logged to the database or Application Insights by default.

## Consequences
- **Pros**: Reduces compliance risks (GDPR, CCPA), prevents accidental exposure of trade secrets, and lowers database storage costs.
- **Cons**: Makes it impossible to manually inspect a past prompt to understand why a model generated a specific response unless the client application logs the prompt themselves.

## Alternatives Considered
- **Opt-in raw logging**: Might be implemented in the future, but for MVP, an uncompromising default of "no raw logs" is the safest approach.
