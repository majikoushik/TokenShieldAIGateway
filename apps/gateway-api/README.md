# TokenShield Gateway API

This is the backend for **TokenShield AI Gateway**, built as a .NET 8 Web API following Clean Architecture principles.

---

## Project Structure
- **TokenShield.slnx**: Visual Studio / .NET CLI solution file.
- **src/TokenShield.Api**: Web API hosting endpoint routers, middleware, and dependency container configurations.
- **src/TokenShield.Domain**: Holds entity aggregates (`Tenant`, `ClientApplication`, etc.), constants, and domain rules.
- **src/TokenShield.Application**: Encapsulates workflow orchestrators, application services, and port interface contracts.
- **src/TokenShield.Infrastructure**: Connects persistence layers (EF Core, PostgreSQL config).
- **src/TokenShield.ProviderAdapters**: Houses individual adapters for external vendors (Azure OpenAI, OpenAI, Anthropic, Mock).
- **src/TokenShield.PolicyEngine**: Resolves conditional rules matching.
- **src/TokenShield.CostEngine**: Simple token and pricing estimations.
- **src/TokenShield.Guardrails**: Inspects inputs for PII masking.
- **src/TokenShield.Observability**: Structured Serilog context enrichment.
- **tests/TokenShield.UnitTests**: Holds unit tests.
- **tests/TokenShield.IntegrationTests**: Holds integration/functional endpoint tests.

---

## Local Development Setup

### Running the API Bare-Metal
Ensure you have the .NET 8 SDK or .NET 10 SDK installed. Run the API directly from the root of the gateway-api:
```bash
dotnet run --project src/TokenShield.Api
```

By default, the server runs on:
- HTTP: `http://localhost:5000`
- Swagger UI (Development only): `http://localhost:5000/swagger/index.html`

### Public Verification Endpoints
- **Health Check**: `GET http://localhost:5000/health`
- **Product Version**: `GET http://localhost:5000/api/version`
