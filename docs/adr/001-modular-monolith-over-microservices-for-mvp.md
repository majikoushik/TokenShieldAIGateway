# ADR 001: Modular Monolith over Microservices for MVP

## Status
Accepted

## Context
We need to build a production-minded Minimum Viable Product (MVP) for the TokenShield AI Gateway. The system requires capabilities such as an AI routing proxy, an admin console, and rule enforcement. Choosing between a microservices architecture and a monolith is crucial for development speed and operational simplicity.

## Decision
We chose a Modular Monolith architecture for the MVP built in .NET 8, maintaining strict physical project boundaries (TokenShield.Api, TokenShield.Domain, TokenShield.Application, etc.) to enforce Clean Architecture.

## Consequences
- **Pros**: Reduces deployment complexity, allows for simpler local development (via a single `docker-compose.yml`), simplifies transaction boundaries, and is easier to maintain with a smaller team initially.
- **Cons**: Less granular scaling out of the box. However, the modular nature allows us to break out specific components (e.g., the cost engine or the proxy routing) into separate services if needed in the future.

## Alternatives Considered
- **Microservices**: Deemed too complex for an MVP. Would require complex orchestration (Kubernetes), distributed tracing overhead, and significant DevOps effort, distracting from core feature delivery.
