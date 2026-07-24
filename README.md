# SchemaForge

[![CI](https://github.com/Rudolph919/SchemaForge/actions/workflows/ci.yml/badge.svg)](https://github.com/Rudolph919/SchemaForge/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](global.json)

Enterprise JSON Schema Design, Validation & API Contract Platform — a multi-tenant SaaS for designing, versioning, testing, and publishing JSON Schemas through a visual editor, backed by a .NET 10 Clean Architecture API and a Vue 3 frontend.

Built as a portfolio project to demonstrate enterprise-grade backend design: strict layering enforced by architecture tests, CQRS via MediatR, dual-layer multi-tenancy (EF Core global query filters + Postgres row-level security), optimistic concurrency, idempotent writes, and a swappable AI-suggestion seam — not just CRUD over a database.

The full architecture design (10 documents, written and approved before any code) lives in [`docs/architecture/`](docs/architecture/01-architecture.md), starting with [01-architecture.md](docs/architecture/01-architecture.md). [`docs/WORKFLOW.md`](docs/WORKFLOW.md) covers branching/merge conventions.

## Features

- **Identity & Organizations** — registration, login, refresh-token sessions (rotation + reuse detection), org switching, teams, member invites and roles
- **Schema Design** — a visual node-tree editor for JSON Schema (objects, arrays, composition, conditionals, local definitions), Draft → Published → Deprecated lifecycle, semantic versioning
- **Validation Engine** — validate arbitrary JSON payloads against a published schema version, with persisted validation runs
- **Reusable Components** — versioned, shared schema fragments referenced from multiple schemas
- **Generation** — export to JSON Schema / OpenAPI / TypeScript / C# DTOs, JSON Schema import, generated documentation (HTML/Markdown/JSON), and a computed schema diff viewer
- **Schema Testing** — test suites/cases run asynchronously against a schema version via a background job queue
- **Audit Log** — every domain event across every module projected into a searchable, filterable audit trail
- **AI Schema Suggestion** — suggest a draft schema from an uploaded source document, review/accept individual nodes, materialize into a real draft version (behind a swappable provider seam; ships with a null provider)
- **Hardening** — per-user/IP rate limiting, `Idempotency-Key` on side-effecting POSTs, `ETag`/`If-Match` optimistic concurrency, full RLS coverage, CI dependency/secret scanning, accessibility pass

## Tech stack

**Backend**: .NET 10, ASP.NET Core, MediatR (CQRS), EF Core + PostgreSQL (JSONB node storage + row-level security), Redis (caching), MinIO (S3-compatible file storage), Hangfire (background jobs), Serilog.

**Frontend**: Vue 3 (Composition API), TypeScript, Vite, Pinia, Tailwind CSS.

**Architecture**: Clean Architecture (`Domain` → `Application` → `Infrastructure`/`Api`, plus `Contracts` and `SharedKernel`), dependency rules enforced by a dedicated `SchemaForge.ArchitectureTests` project.

## Running it locally

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in [`global.json`](global.json)), [Node.js](https://nodejs.org/) 20+, Docker.

```bash
git clone https://github.com/Rudolph919/SchemaForge.git
cd SchemaForge
cp .env.example .env   # defaults are fine for local dev
```

**1. Start Postgres, Redis, and MinIO:**

```bash
docker compose up -d postgres redis minio
```

**2. Apply database migrations:**

```bash
dotnet ef database update --project src/SchemaForge.Infrastructure --startup-project src/SchemaForge.Api
```

**3. Run the API** (defaults to `http://localhost:5001`, see `src/SchemaForge.Api/Properties/launchSettings.json`):

```bash
dotnet run --project src/SchemaForge.Api
```

**4. Run the frontend** (defaults to `http://localhost:5173`; see `frontend/.env.development` for the API URL it expects):

```bash
cd frontend
npm install
npm run dev
```

Then open the frontend URL and register an account — the first registration also creates your organization.

### Running everything in Docker instead

```bash
docker compose up -d
```

This also builds and starts the API itself (`src/SchemaForge.Api/Dockerfile`) alongside Postgres/Redis/MinIO. Migrations still need to be applied separately as in step 2 above.

## Tests

```bash
dotnet test
```

Runs three suites: `SchemaForge.UnitTests` (domain/application logic), `SchemaForge.IntegrationTests` (real Postgres via Testcontainers, full HTTP pipeline), and `SchemaForge.ArchitectureTests` (enforces the Clean Architecture layer/dependency rules on every run — a real CI gate, not just documentation).

Frontend type-checking:

```bash
cd frontend
npx vue-tsc --noEmit
```

CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs the full backend test suite plus a NuGet vulnerability scan and gitleaks secret scanning on every push and PR to `main`.

## Project structure

```
src/
  SchemaForge.Domain          # Entities, aggregates, value objects, domain events — no framework dependencies
  SchemaForge.Application     # CQRS commands/queries, validators, port interfaces (MediatR)
  SchemaForge.Infrastructure  # EF Core, Postgres, Redis, MinIO, JWT, Hangfire — the port implementations
  SchemaForge.Contracts       # Wire-format request/response DTOs shared with the frontend's TypeScript types
  SchemaForge.SharedKernel    # Cross-cutting base types (Entity, AggregateRoot, Result/Error, ...)
  SchemaForge.Api             # Controllers, mapping, DI wiring, middleware
tests/
  SchemaForge.UnitTests
  SchemaForge.IntegrationTests
  SchemaForge.ArchitectureTests
frontend/                     # Vue 3 SPA — see frontend/README.md
docs/architecture/             # The 10-document architecture design, written before implementation
```

## License

[MIT](LICENSE)
