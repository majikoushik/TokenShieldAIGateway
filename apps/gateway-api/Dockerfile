FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files to cache NuGet restore
COPY src/TokenShield.Api/TokenShield.Api.csproj src/TokenShield.Api/
COPY src/TokenShield.Domain/TokenShield.Domain.csproj src/TokenShield.Domain/
COPY src/TokenShield.Application/TokenShield.Application.csproj src/TokenShield.Application/
COPY src/TokenShield.Infrastructure/TokenShield.Infrastructure.csproj src/TokenShield.Infrastructure/
COPY src/TokenShield.ProviderAdapters/TokenShield.ProviderAdapters.csproj src/TokenShield.ProviderAdapters/
COPY src/TokenShield.PolicyEngine/TokenShield.PolicyEngine.csproj src/TokenShield.PolicyEngine/
COPY src/TokenShield.CostEngine/TokenShield.CostEngine.csproj src/TokenShield.CostEngine/
COPY src/TokenShield.Guardrails/TokenShield.Guardrails.csproj src/TokenShield.Guardrails/
COPY src/TokenShield.Observability/TokenShield.Observability.csproj src/TokenShield.Observability/

# Restore packages
RUN dotnet restore src/TokenShield.Api/TokenShield.Api.csproj

# Copy source code and publish in Release mode
COPY src/ src/
RUN dotnet publish src/TokenShield.Api/TokenShield.Api.csproj -c Release -o /app/publish --no-restore

# Final runtime image — use curl-enabled image for healthcheck
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install curl for HEALTHCHECK (minimal footprint)
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# Liveness probe — fails fast if the process is unhealthy
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "TokenShield.Api.dll"]
