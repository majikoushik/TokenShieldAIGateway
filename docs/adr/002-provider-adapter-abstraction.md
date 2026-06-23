# ADR 002: Provider Adapter Abstraction for LLM Providers

## Status
Accepted

## Context
TokenShield must route requests to multiple AI providers (OpenAI, Azure OpenAI, Anthropic, Mock) seamlessly. Hardcoding integration logic into the proxy engine would lead to a rigid and unmaintainable system.

## Decision
We implemented a `ProviderAdapterFactory` and a common `IProviderAdapter` interface that abstracts away provider-specific SDKs and API shapes. Controllers and routing services interact exclusively with these generic interfaces.

## Consequences
- **Pros**: It allows developers to add new AI providers (e.g., Google Gemini, AWS Bedrock) easily without modifying the core routing logic. It also provides a unified interface for testing by injecting a Mock provider.
- **Cons**: Requires standardizing concepts (e.g., "messages", "tokens") across providers that might have slightly different API models, potentially limiting support for highly provider-specific features.

## Alternatives Considered
- **Direct SDK usage in controllers**: Rejected because it violates the Open-Closed principle and couples our routing logic to specific vendors.
