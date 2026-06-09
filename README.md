# TokenShield AI Gateway

TokenShield AI Gateway is a production-grade AI FinOps, model-routing, and governance platform for enterprises. It acts as a centralized proxy between client applications and downstream AI model providers (such as OpenAI, Azure OpenAI, and Anthropic).

This repository is set up as a clean monorepo containing both the .NET 8 backend API and the Next.js TypeScript administration frontend.

---

## Repository Structure

```text
.
├── apps/
│   ├── gateway-api/      # Backend: .NET 8 Clean Architecture gateway proxy
│   └── web-admin/        # Frontend: Next.js TypeScript Admin console
├── docs/                 # Product Specifications & Architecture documents
├── infra/                # Infrastructure configurations & local Dockerfiles
├── docker-compose.yml    # Root Docker Compose orchestrating DB & app services
└── README.md             # This file
```

---

## Getting Started

### Prerequisites
- [.NET 8 SDK or .NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js (v22.x or higher) & npm](https://nodejs.org)
- [Docker & Docker Compose](https://www.docker.com)
- [PostgreSQL](https://www.postgresql.org) (optional, if running bare-metal locally)

---

## Option 1: Running Locally (Bare Metal)

### 1. Run the Backend API
Navigate to the backend API directory and run:
```bash
cd apps/gateway-api/src/TokenShield.Api
dotnet run
```
By default, the server runs on:
- HTTP: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger/index.html`

Verification endpoints:
- Health check: `GET http://localhost:5000/health`
- Version check: `GET http://localhost:5000/api/version`

### 2. Run the Next.js Admin Console
Navigate to the web admin directory, install dependencies, and run the development server:
```bash
cd apps/web-admin
npm install
npm run dev
```
By default, the web panel runs on:
- Web Admin: `http://localhost:3000`

---

## Option 2: Running via Docker Compose
To build and start all services (gateway-api, web-admin, and a local PostgreSQL database):
```bash
docker compose up --build
```
This boots:
- **Database**: PostgreSQL (port `5432` locally)
- **Backend API**: Gateway API (mapped to `http://localhost:5000` with Swagger active)
- **Frontend Panel**: Admin Web App (mapped to `http://localhost:3000`)
