# Step 1 — High-Level Architecture

Status: **Draft for review**
Decides: overall architectural style, layering rules, and which enterprise patterns apply where and why.
Does not decide: domain model, aggregates, entities, DB schema, API surface, folder layout, bounded context boundaries, or the roadmap — those are Steps 2–10.

---

## 1. Architectural style: Modular Monolith, Clean Architecture layering

**Decision**: SchemaForge is built as a single deployable ASP.NET Core service (a *modular monolith*), internally organized using Clean Architecture layers, with each business capability (Schema Designer, Validation Engine, Versioning, Documentation Generator, etc.) implemented as an internally-cohesive module with an explicit, narrow interface to the rest of the system.

**Why not microservices**: Microservices solve organizational scaling problems (independent teams, independent deploy cadences, independent scaling of hot paths) and impose real costs — distributed transactions, network failure handling, service discovery, distributed tracing, versioned inter-service contracts, and operational overhead (N deployments, N sets of infra). SchemaForge has one team (you) and no proven differential scaling need between, say, the Documentation Generator and the Audit Log. Choosing microservices here would be optimizing for a problem that doesn't exist yet, at the cost of demonstrating actual architectural judgment — a Principal Engineer's job includes *not* reaching for distributed systems when a well-modularized monolith is the right call. It would also make the portfolio harder to run and review (docker-compose with a service mesh vs. one API + Postgres + Redis).

**Why not a simple N-tier ("fat controller / fat service") design**: The stated goal is to demonstrate enterprise-grade backend design. An N-tier design (Controllers → Services → Repositories → EF entities used directly as the domain) tends toward an anemic domain model, business rules leaking into controllers/services, and tight coupling to EF Core throughout. That's the "easiest solution," explicitly ruled out by the brief.

**Why Clean Architecture specifically** (vs. plain Hexagonal/Ports-and-Adapters, vs. Vertical Slice Architecture): Clean Architecture gives us:
- A **strict, testable dependency rule** (dependencies point inward; Domain has zero outward dependencies) that's easy to enforce mechanically via `ArchitectureTests` (using `NetArchTest` or similar) — meaning the rule isn't just documentation, it's a CI gate.
- A natural home for the **AI-ready abstraction** requirement: AI schema-suggestion becomes an Application-layer port (`ISchemaSuggestionProvider`) with an Infrastructure-layer adapter, swappable without touching Domain or Application logic.
- A clean seam for **multi-tenancy enforcement**, which needs to live below Application (so no module can forget it) but above the raw EF Core `DbContext`.

I considered pure **Vertical Slice Architecture** (feature folders cutting across all layers, MediatR handler per use case, minimal shared abstraction) as an alternative — it reduces ceremony and is genuinely excellent for teams optimizing for feature velocity. I'm not recommending it as the *primary* structure here because the brief asks for explicit layer projects (`SchemaForge.Domain`, `.Application`, etc.) that are individually inspectable and testable — which is also more legible to a reviewer skimming a portfolio repo. That said, **within** the Application layer, we will organize by feature/module first, then by CQRS role second (see §7 folder structure in Step 7) — so we get most of vertical-slice's organizational clarity without abandoning the layer boundary.

---

## 2. The layers and the dependency rule

```
SchemaForge.Domain            <- depends on nothing (except SharedKernel)
SchemaForge.SharedKernel       <- depends on nothing
SchemaForge.Application        <- depends on Domain, SharedKernel
SchemaForge.Infrastructure     <- depends on Application, Domain, SharedKernel
SchemaForge.Contracts          <- depends on nothing (plain DTOs/records only)
SchemaForge.Api                <- depends on Application, Infrastructure, Contracts, SharedKernel
```

| Layer | Responsibility | Must NOT contain |
|---|---|---|
| **Domain** | Entities, aggregates, value objects, domain events, domain services, invariants, enums, repository *interfaces* | EF Core, ASP.NET, HTTP, JSON serialization attributes, any package that isn't pure C# |
| **SharedKernel** | Cross-module primitives: `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject`, `Result`/`Result<T>`, `Error`, `IDomainEvent`, `TenantId`, `AuditableEntity` base | Anything module-specific (no `Schema`, no `Organization`) |
| **Application** | Use cases (commands/queries/handlers), orchestration, validation via FluentValidation, port interfaces for infrastructure (`IFileStorage`, `ISchemaSuggestionProvider`, `ICacheService`), DTO ↔ domain mapping | EF Core implementations, HTTP concerns, direct SQL |
| **Infrastructure** | EF Core `DbContext` + configurations + migrations, repository implementations, Redis cache implementation, external service adapters, Hangfire-backed background job processing | Business rules, validation logic beyond data-integrity constraints |
| **Contracts** | Request/response records exposed at the HTTP boundary (and reused to generate the TypeScript/C# client artifacts) | References to Domain or Application types — Contracts must be mappable *from* Application DTOs but never leak Domain types outward |
| **Api** | Controllers/minimal API endpoints, DI composition root, middleware pipeline, auth policies, OpenAPI setup | Business logic, direct EF Core queries |

This dependency graph is what `ArchitectureTests` will assert (e.g., "no type in Domain references a type from any other project," "no type in Application references EF Core," "Controllers don't reference `Infrastructure` types directly, only through `Application` interfaces").

---

## 3. Where CQRS applies — and where it deliberately doesn't

The brief explicitly says: don't introduce CQRS blindly. Here's the actual split:

**Use CQRS (separate command/query models via MediatR) for:**
- **Schema Validation Engine** — validating a JSON payload against a schema is a pure query-shaped operation (no state mutation, needs to be fast, benefits from a model shaped purely for the validation report) that also needs a materially different read shape (nested `ValidationResult` tree with JSONPath annotations) than any command would ever return.
- **Schema Library / search & listing** — org-wide schema browsing needs pagination, filtering, sorting, and projections optimized for list display (denormalized read DTOs), which is a different shape than the rich aggregate used when editing a schema. A dedicated query handler hitting a lean projection (via EF Core `.Select()` into a DTO, not loading the aggregate) avoids over-fetching.
- **Schema Diffing / Versioning reports** — comparing two schema versions is a read-only, computation-heavy query independent of the command that created either version.

**Do NOT use CQRS/MediatR for:**
- **Settings, Teams, Projects CRUD** — a straightforward "load aggregate, mutate, save" flow through an Application service is more direct, easier to read, and doesn't benefit from separate read/write models. Wrapping trivial CRUD in MediatR commands/handlers here would be ceremony for its own sake — exactly the "don't blindly implement patterns" instruction.

**MediatR's actual job in this system**, even outside full CQRS use cases, is the **pipeline behavior chain** — a legitimate, high-value use regardless of CQRS: `ValidationBehavior` (runs FluentValidation validators before a handler executes), `LoggingBehavior` (structured request/response logging with correlation IDs), `TenantAuthorizationBehavior` (asserts the caller's tenant context matches the resource), and `TransactionBehavior` (wraps command handlers in a DB transaction/unit-of-work commit). This is what makes MediatR worth its dependency cost here, independent of whether a given module is "CQRS" or "plain service."

---

## 4. Repository pattern: targeted, not generic

**Decision**: No generic `IRepository<T>`. EF Core's `DbContext` already *is* a Unit-of-Work/Repository-ish abstraction (`DbSet<T>` + `SaveChangesAsync`), and wrapping it in a generic repository just adds an indirection layer that leaks EF Core semantics anyway (e.g., `IQueryable<T>` in the "abstraction" defeats the point). Instead:

- **Aggregate-specific repository interfaces** are defined in `Application` (e.g., `ISchemaDefinitionRepository`) only where the aggregate has real query complexity worth centralizing (e.g., "get schema with all versions and active test suite eagerly loaded") or where we want the Application layer to remain fully unit-testable without a real `DbContext`.
- **Simple lookups/read models bypass repositories entirely** and query `DbContext`/read-only projections directly from query handlers in Infrastructure, since forcing a repository interface in front of a `.Select()` projection used by exactly one query handler is unnecessary ceremony.
- Every repository implementation still lives in `Infrastructure`; only the interface is visible to `Application`.

This is a deliberate middle ground: repository *where it earns its keep* (encapsulating a genuinely reusable aggregate-loading shape, and enabling clean unit tests against Application logic), direct EF Core access *where a repository would just be a pass-through*.

---

## 5. Domain model: rich, not anemic — with Domain Events for cross-aggregate effects

Aggregates enforce their own invariants (e.g., a `SchemaDefinition` cannot transition to `Published` status without at least one passing test run — detailed in Step 3). Where an action needs to affect *other* aggregates or modules without direct coupling, we raise a **domain event**:

- `SchemaVersionPublished` → triggers Audit Log entry, invalidates the Documentation cache, notifies subscribers.
- `SchemaValidationFailed` (aggregated over a session) → feeds future analytics/AI-suggestion training data.

Domain events are collected on the aggregate root (`AggregateRoot.DomainEvents`), dispatched by a `SaveChangesInterceptor` in Infrastructure *after* a successful `SaveChangesAsync`, ensuring event side effects never fire on a transaction that gets rolled back. This is the one place "Domain Events" earns its complexity: cross-aggregate consistency boundaries that must not become a single mega-transaction, and that the Audit Log module (which must observe *everything*) can subscribe to generically instead of every other module manually calling into it.

---

## 6. Errors: `Result<T>` for expected failures, exceptions for the exceptional

**Decision**: Application-layer use cases return `Result` / `Result<T>` (a small `SharedKernel` type carrying success/failure + a typed `Error`) for *expected* failure modes — validation failures, business rule violations, "not found," "conflict." Exceptions are reserved for truly exceptional, unrecoverable conditions (DB connection loss, programming errors, third-party outages).

**Why**: Modeling expected failures as exceptions (a common anemic-CRUD-tutorial habit) makes control flow implicit, is measurably slower under load (stack unwinding), and makes it easy for a caller to forget a `catch`. A `Result<T>` return type forces every caller — including the API layer mapping to HTTP status codes — to explicitly handle the failure path, which reads better in code review and is trivially unit-testable (`result.IsFailure.Should().BeTrue()` vs. `Assert.Throws`). This is standard practice in Stripe/Shopify-caliber .NET codebases and pairs naturally with `ValidationBehavior` short-circuiting the MediatR pipeline on FluentValidation failures.

---

## 7. Multi-tenancy enforcement (mechanics, given the shared-schema decision)

Given the confirmed decision — shared database, shared schema, `TenantId` column — the mechanics that make this *safe* (the hard part of this model) are an architecture-level concern, not an implementation detail:

1. Every tenant-scoped aggregate root inherits a `SharedKernel` base (`TenantOwnedEntity`) carrying `TenantId`.
2. `ITenantContext` (Application-layer interface, Infrastructure implementation) resolves the current tenant from the authenticated JWT's tenant claim, scoped per-request via DI (`Scoped` lifetime).
3. EF Core **global query filters** (`HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)`) are applied to every tenant-scoped entity in `OnModelCreating`, so a missing `Where` clause in a query handler *cannot* leak cross-tenant data — the filter is structural, not convention-based.
4. A `SaveChangesInterceptor` auto-stamps `TenantId` on insert (and rejects — via exception, since this is a programming-error case, not a business one — any attempt to modify a row whose `TenantId` doesn't match the current tenant context), so a handler can't accidentally write into another tenant's data by omission.
5. Integration tests specifically assert cross-tenant isolation (Step 10 roadmap includes a dedicated tenant-isolation test suite) — this is a case where the *test* is part of the architecture, not an afterthought, because the failure mode (cross-tenant data leak) is catastrophic and silent.

This gets full detail in Step 5 (database schema); it's listed here because "which multi-tenancy mechanism" is an architecture-level decision with system-wide consequences, not a per-module one.

---

## 8. Background & long-running work: Hangfire, not a hand-rolled outbox or a message broker

Several features are not safely synchronous within an HTTP request: **Schema Testing** (running a full test suite against a schema), **Documentation generation** for large schemas, and future **AI schema suggestion** (calling an LLM over an uploaded PDF).

**Decision**: Use **Hangfire** (backed by Postgres storage, `Hangfire.PostgreSql`) for background job scheduling and execution. No RabbitMQ/Azure Service Bus — that's still real infrastructure weight this project doesn't need. But a hand-rolled outbox table plus a custom `BackgroundService` polling/leasing loop isn't the right amount of restraint either — it's *more* code to write, test, and maintain than taking a mature, widely-used library, in exchange for a "demonstrates you can build it yourself" benefit that isn't worth the ongoing cost of owning retry logic, backoff, dead-lettering, and a dashboard from scratch. *(This reverses an earlier hand-rolled-outbox decision — see the confirmed-decision note below.)*

**Why Hangfire specifically**: it gives us the property that actually matters — reliable, persisted job scheduling with at-least-once execution — with retries, a real dashboard, and recurring-job support included, for the cost of one NuGet package and one Postgres storage schema it manages itself (no hand-designed `background_jobs` table, Step 5's schema no longer needs one). Critically, **the Application layer still only depends on a small `IJobDispatcher` port**, not on Hangfire directly — Clean Architecture's layer rule (Step 1 §2: Application must not reference Infrastructure-layer packages) requires that regardless of what sits behind it, so the AI-provider-style "build the seam now" principle from §9 still holds. What changed is only what's *behind* that port: `Infrastructure`'s implementation now wraps Hangfire's `IBackgroundJobClient` instead of a hand-rolled polling loop.

---

## 9. Cross-cutting concerns

| Concern | Approach | Reasoning |
|---|---|---|
| **Structured logging** | Serilog, JSON sink, enriched with `TenantId`, `CorrelationId`, `UserId` on every log line via middleware | Enables real log-based debugging across tenants/requests in a way `Console.WriteLine`-style logging never does; matches the "Observability" non-functional requirement |
| **Validation** | FluentValidation validators per command/query, invoked via `ValidationBehavior` MediatR pipeline step | Centralizes validation, keeps it out of handlers and controllers, matches stated stack |
| **Caching** | Redis, behind an `ICacheService` Application-layer port | Used for expensive-to-compute, read-heavy data: generated documentation, published schema JSON, schema diff results. Write-through invalidation triggered by domain events (`SchemaVersionPublished` → invalidate doc cache) |
| **AuthN** | ASP.NET Core Identity + JWT (access + refresh token), per confirmed decision | Self-hosted, demonstrates full auth engineering |
| **AuthZ** | Policy-based authorization (`[Authorize(Policy = "...")]`), roles scoped per-Organization/Team (not global roles) | Matches the Org → Team → Project hierarchy; a "global admin" role model wouldn't fit multi-tenant reality |
| **API versioning** | URL-segment versioning (`/api/v1/...`) | Explicit, cache-friendly, visible in every request/log line — simpler to reason about than header/media-type versioning for a REST API with external API-contract consumers |
| **OpenAPI** | Swashbuckle/Swagger generated from `Contracts` + XML doc comments, versioned per API version | Directly feeds the "API Contract Generator" feature — SchemaForge's own OpenAPI doc becomes a dogfooding example |

---

## 10. What Step 1 deliberately defers

- Exact aggregate boundaries and invariants → **Step 3**
- Entity-level field definitions → **Step 4**
- Physical database schema, indexes, constraints → **Step 5**
- Concrete API endpoints/routes → **Step 6**
- Solution/folder layout → **Step 7**
- Bounded context map (module boundaries in DDD terms) → **Step 8**
- Extension points (AI provider seam, storage provider seam, export format seam) in full detail → **Step 9**
- Delivery order / MVP slice → **Step 10**

---

## Decisions confirmed during review

1. **MediatR everywhere, not just in CQRS modules** — confirmed. All modules, including simple CRUD (Settings, Teams, Projects), route through MediatR so the full pipeline behavior chain (validation, logging, tenant-auth, transactions) applies uniformly. Trade-off accepted: simple modules take a MediatR dependency they don't strictly need for CQRS purposes, in exchange for one consistent, impossible-to-bypass cross-cutting pipeline instead of two parallel mechanisms.
2. **Hangfire over a hand-rolled outbox** — confirmed (revised). Originally decided the other way, optimizing for "hand-rolling it is more demonstrative of engineering depth." Revisited once simplicity was named as an explicit priority: hand-rolling a reliable job queue is real ongoing code to own (retries, backoff, dead-lettering, a dashboard) for a benefit — showing you can build one — that isn't worth the maintenance cost. Hangfire gets the same reliability property (persisted, at-least-once job execution against Postgres storage) for one dependency, still behind the `IJobDispatcher` Application-layer port §8 describes, so the architectural seam is unaffected — only the Infrastructure-layer implementation behind it changed.

---

**Next step once this is approved**: Step 2 — Domain design (identifying the core domain concepts, ubiquitous language, and ownership of each concept across the Organization/Team/Project/Schema hierarchy).
