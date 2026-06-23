# ADR 005: Azure Container Apps for Portfolio Deployment Foundation

## Status
Accepted

## Context
We need a deployment target for the TokenShield gateway and web-admin that balances operational simplicity, security, and scalability. Kubernetes (AKS) provides full control but is too heavy and complex for an MVP/Portfolio demonstration. Azure App Services lacks native container networking simplicity for micro-components.

## Decision
We selected Azure Container Apps (ACA) as the default deployment foundation, managed via Azure Bicep templates.

## Consequences
- **Pros**: Serverless container execution, native scaling (including KEDA scale-to-zero if needed), integrated Dapr (optional future use), and seamless integration with Azure Container Registry (ACR) and Key Vault via Managed Identity. It provides a credible, production-minded staging environment.
- **Cons**: Less granular control over the underlying cluster than raw AKS. Vendor lock-in to Azure's specific serverless container model.

## Alternatives Considered
- **Azure Kubernetes Service (AKS)**: Too complex for MVP.
- **Docker Compose on a VM**: Not robust enough for a cloud-native architecture showcase.
