# C4

Dynamic architecture visualization for cloud-native systems. Connect your Azure environment, auto-discover resources, and get interactive C4-model diagrams with real-time telemetry overlays and AI-powered analysis.

## What it does

- **Auto-discovery** -- Connects to Azure subscriptions and discovers resources, relationships, and topology automatically via Azure Resource Graph.
- **C4 model diagrams** -- Renders interactive context, container, and component-level diagrams with drill-down navigation.
- **Live telemetry** -- Overlays Application Insights metrics on the graph. Connections are color-coded green/yellow/red by health score.
- **IaC drift detection** -- Compares Bicep/Terraform desired state against live Azure state and highlights discrepancies on the diagram.
- **AI analysis** -- Uses Semantic Kernel with local Ollama models for architecture analysis and STRIDE-based threat assessment.
- **Export** -- Download diagrams as SVG or PDF.

## Architecture

Modular monolith built with .NET 9 and React 19. Five bounded-context modules communicate through explicit contracts.

```
src/
  Modules/
    Identity/          Organizations, projects, members, RBAC
    Discovery/         Azure subscription connection, resource discovery, drift
    Graph/             C4-model graph construction and versioning
    Telemetry/         Application Insights ingestion and health scoring
    Visualization/     Diagram rendering, export, real-time updates via SignalR
  Shared/
    Kernel/            Result<T>, Entity, AggregateRoot, strongly-typed IDs
    Infrastructure/    EF Core base, MediatR pipeline, validation behavior
  Host/                ASP.NET Core composition root
web/                   React + TypeScript + React Flow frontend
infra/                 Terraform (Scaleway) + cloud-init
```

Each module follows Vertical Slice Architecture with Ports and Adapters:

```
Feature Request  ->  Endpoint  ->  MediatR Handler  ->  Domain  ->  Repository (Port)
                                                                          |
                                                              EF Core Adapter (Infrastructure)
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 22+](https://nodejs.org/)
- [Docker](https://docs.docker.com/get-docker/) and Docker Compose

## Quick start

### Docker Compose (recommended)

```bash
cp .env.example .env
docker compose up
```

Open [http://localhost:3000](http://localhost:3000). The backend API is at [http://localhost:5000](http://localhost:5000) with Swagger at [http://localhost:5000/swagger](http://localhost:5000/swagger).

### Local development

Backend:

```bash
dotnet restore
dotnet build
dotnet run --project src/Host/Host.csproj
```

Frontend:

```bash
cd web
npm install
npm run dev
```

## Running tests

```bash
# All backend tests (67 tests across 7 projects)
dotnet test

# Frontend tests
cd web && npm test
```

## Project structure

| Directory | Purpose |
|-----------|---------|
| `src/Modules/<M>/<M>.Api` | Minimal API endpoints (vertical slices) |
| `src/Modules/<M>/<M>.Application` | Commands, queries, handlers, ports |
| `src/Modules/<M>/<M>.Domain` | Domain model, aggregates, events |
| `src/Modules/<M>/<M>.Infrastructure` | EF Core, external adapters |
| `src/Modules/<M>/<M>.Tests` | Unit and integration tests |
| `web/src/features/<feature>` | React feature modules |
| `docs/architecture/` | ADRs and architecture guides |
| `docs/standards/` | Coding and testing standards |
| `infra/` | Terraform + cloud-init for Scaleway deployment |

## API endpoints

All endpoints are prefixed with `/api`. Key routes:

```
POST   /api/organizations                              Register organization
POST   /api/organizations/{id}/projects                Create project
POST   /api/projects/{id}/members                      Invite member
PUT    /api/projects/{id}/members/{memberId}/role       Update role

POST   /api/discovery/subscriptions                    Connect Azure subscription
POST   /api/discovery/subscriptions/{id}/discover      Trigger discovery
POST   /api/discovery/subscriptions/{id}/drift         Detect IaC drift
GET    /api/discovery/subscriptions/{id}/status         Discovery status

GET    /api/projects/{id}/graph                        Get architecture graph
GET    /api/projects/{id}/graph/diff                   Get graph diff
POST   /api/projects/{id}/analyze                      AI architecture analysis
GET    /api/projects/{id}/threats                       AI threat assessment

POST   /api/projects/{id}/telemetry                    Ingest telemetry
GET    /api/projects/{id}/telemetry/{service}/health   Service health

GET    /api/projects/{id}/diagram                      Get diagram data
POST   /api/projects/{id}/export                       Export SVG/PDF
POST   /api/visualization/presets                       Save view preset
GET    /api/visualization/presets                       List view presets

GET    /health                                         Health check
```

## Deployment

The project includes Terraform configuration for deploying to a Scaleway DEV1-S instance (~3 EUR/month).

### Infrastructure

```bash
cd infra
terraform init
terraform apply
```

This provisions:
- A DEV1-S VM with Ubuntu 24.04
- Public IP with security group (ports 22, 80, 443)
- Nginx reverse proxy via cloud-init
- Docker and Docker Compose pre-installed

### CI/CD

The GitHub Actions workflow (`.github/workflows/deploy.yml`) runs on push to `main`:

1. **Test** -- `dotnet test` and `npm test`
2. **Build** -- Docker images pushed to GitHub Container Registry
3. **Provision** -- Terraform apply (state stored on `terraform-state` branch)
4. **Deploy** -- SSH into server, pull images, `docker compose up`

Required repository secrets:

| Secret | Description |
|--------|-------------|
| `SCW_ACCESS_KEY` | Scaleway API access key |
| `SCW_SECRET` | Scaleway API secret key |
| `SCW_ORGANIZATION_ID` | Scaleway organization ID |
| `SCW_PROJECT_ID` | Scaleway project ID |
| `SSH_PRIVATE_KEY` | SSH key for server access |

## Tech stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 9, ASP.NET Core, EF Core 9 |
| AI | Semantic Kernel 1.48, Ollama |
| Frontend | React 19, TypeScript 5, React Flow, Vite |
| Database | PostgreSQL 17 |
| Messaging | MediatR (in-process) |
| Real-time | SignalR |
| Testing | xUnit, FluentAssertions, ArchUnitNET, Vitest |
| Infrastructure | Terraform, Docker Compose, Scaleway |
| CI/CD | GitHub Actions |

## Contributing

See `docs/standards/coding-standards.md` for conventions. Key rules:

- No code comments (code must be self-documenting)
- One type per file, `sealed` on leaf classes
- `Result<T>` over exceptions for expected failures
- Write the test first for new handlers and domain behaviors
- Strict TypeScript, no `any`

## License

Proprietary. All rights reserved.
