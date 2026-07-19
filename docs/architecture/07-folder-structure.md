# Step 7 — Folder Structure

Status: **Draft for review**
Decides: how everything designed in Steps 1–6 physically lands in the solution's project/folder layout — the thing you'll actually see when you open the repo.
Does not decide: bounded context boundaries (this step assumes the modules already implied by Steps 2–6; Step 8 asks whether any of them should eventually become independently deployable) → **Step 8**.

---

## 1. Repository root

```
SchemaForge/
├── docs/
│   └── architecture/           # this document series
├── src/
│   ├── SchemaForge.Domain/
│   ├── SchemaForge.SharedKernel/
│   ├── SchemaForge.Application/
│   ├── SchemaForge.Infrastructure/
│   ├── SchemaForge.Contracts/
│   └── SchemaForge.Api/
├── tests/
│   ├── SchemaForge.ArchitectureTests/
│   ├── SchemaForge.UnitTests/
│   └── SchemaForge.IntegrationTests/
├── frontend/                   # Vue 3 SPA, own package.json — see §5
├── .github/workflows/
├── docker-compose.yml          # Postgres, Redis, MinIO, Api, (frontend dev server separately)
├── Directory.Build.props       # shared MSBuild settings: Nullable=enable, ImplicitUsings=enable, TreatWarningsAsErrors, LangVersion
├── .editorconfig
├── global.json                 # pins the .NET SDK version
└── SchemaForge.sln
```

`Directory.Build.props` at the root means every project inherits `Nullable`/`ImplicitUsings`/warnings-as-errors without repeating it six times — a small thing, but it's also what makes "nullable reference types enabled" from the brief an enforced fact rather than a per-project convention someone can forget on the seventh project added a year from now.

---

## 2. `SchemaForge.Domain` and `SchemaForge.SharedKernel` — organized by module, not by pattern-type

```
SchemaForge.Domain/
├── Identity/                   # User
│   └── User.cs
├── Organizations/              # Organization, OrganizationMembership, Team, TeamMembership
│   ├── Organization.cs
│   ├── OrganizationMembership.cs
│   ├── Team.cs
│   └── Events/
│       └── OrganizationMemberRevoked.cs
├── Workspaces/                 # Project, SourceDocument
│   ├── Project.cs
│   └── SourceDocument.cs
├── Schemas/                    # SchemaDefinition, SchemaVersion, SchemaNode, LocalDefinition, value objects
│   ├── SchemaDefinition.cs
│   ├── SchemaVersion.cs
│   ├── SchemaNode.cs
│   ├── LocalDefinition.cs
│   ├── ValueObjects/
│   │   ├── ComponentReference.cs
│   │   ├── StringConstraints.cs
│   │   ├── NumericConstraints.cs
│   │   ├── ArrayConstraints.cs
│   │   └── ObjectConstraints.cs
│   └── Events/
│       ├── SchemaVersionPublished.cs
│       └── SchemaVersionDeprecated.cs
├── Components/                 # ComponentDefinition, ComponentVersion — reuses Schemas/SchemaNode.cs directly
│   ├── ComponentDefinition.cs
│   └── ComponentVersion.cs
├── Testing/                    # TestSuite, TestCase, TestRun, TestCaseResult
├── Validation/                 # ValidationRun, ValidationError (VO)
└── Audit/                      # AuditLogEntry

SchemaForge.SharedKernel/
├── Entity.cs
├── AggregateRoot.cs
├── ValueObject.cs
├── AuditableEntity.cs
├── TenantOwnedAggregateRoot.cs
├── Result.cs
├── Error.cs
├── IDomainEvent.cs
└── Primitives/
    ├── Slug.cs
    ├── EmailAddress.cs
    ├── SemVer.cs
    └── JsonPath.cs
```

**Why module-first, not "all entities in one folder / all events in another"**: a pattern-first layout (`Entities/`, `ValueObjects/`, `Events/` each containing everything) optimizes for "I know this is a value object, where do I look" — but that question comes up far less often than "I'm working on Schema Versioning, show me everything relevant." Module-first folders mean opening `Schemas/` shows you the aggregate, its value objects, and its events together, which is what someone actually extending a feature needs. This is also what makes Step 8's bounded-context question answerable later by inspection — if `Schemas/` never reaches into `Organizations/` except by ID, the folder boundary already *is* most of the evidence for whether it could become a separable module.

---

## 3. `SchemaForge.Application` — module-first, then CQRS-role-second (per Step 1 §1)

```
SchemaForge.Application/
├── Common/
│   ├── Behaviors/              # MediatR pipeline: ValidationBehavior, LoggingBehavior,
│   │                           # TenantAuthorizationBehavior, TransactionBehavior (Step 1 §3/§9)
│   ├── Abstractions/           # cross-module ports: IFileStorage, ICacheService,
│   │                           # IJobDispatcher, ISchemaSuggestionProvider (Step 1 §9, Step 9)
│   └── Pagination/             # cursor pagination helpers shared across every list query (Step 6 §1.3)
├── Schemas/
│   ├── Commands/
│   │   ├── CreateSchemaDefinition/
│   │   │   ├── CreateSchemaDefinitionCommand.cs
│   │   │   ├── CreateSchemaDefinitionHandler.cs
│   │   │   └── CreateSchemaDefinitionValidator.cs
│   │   ├── CreateSchemaVersion/
│   │   ├── AddSchemaNode/
│   │   ├── PublishSchemaVersion/
│   │   └── ...
│   ├── Queries/
│   │   ├── GetSchemaLibrary/    # the lean-projection listing query, Step 1 §3
│   │   ├── GetSchemaVersion/
│   │   └── GetSchemaDiff/       # computed SchemaDiff, Step 2 §5
│   └── ISchemaVersionRepository.cs   # the one repository interface this module actually needs (Step 1 §4)
├── Components/                 # same Commands/Queries shape as Schemas — reuses Schemas' node-tree handling logic via a shared internal helper, not a copy-paste (see note below)
├── Organizations/
├── Workspaces/
├── Testing/
├── Validation/
│   ├── Commands/ValidateJsonPayload/
│   └── ...
├── Audit/
└── Generation/                 # JSON Schema / OpenAPI / TypeScript / C# / documentation generators
    ├── IJsonSchemaExporter.cs
    ├── IJsonSchemaImporter.cs
    ├── IOpenApiGenerator.cs
    ├── ITypeScriptGenerator.cs
    ├── ICSharpDtoGenerator.cs
    └── IDocumentationGenerator.cs
```

**Where the generators live, and why they're `Application/Generation/`, not `Infrastructure/`**: it's tempting to file "generates OpenAPI/TypeScript/docs" under Infrastructure since it sounds like an adapter, but Infrastructure (Step 1 §2) is specifically for things that talk to the outside world (a database, a cache, a file store). A generator is pure computation over the `SchemaNode` tree — deterministic, no I/O of its own (whatever caches its *output* is a separate, genuinely-Infrastructure concern) — so it belongs with the rest of the Application layer's orchestration logic. This distinction matters for testability: every generator gets fast, no-database unit tests, exactly because it isn't Infrastructure.

**Note on `Components` vs. `Schemas` code reuse**: since `ComponentVersion` and `SchemaVersion` share the entire node-tree shape (Step 4 §5), the node-mutation logic (`AddNode`, `UpdateNode`, tree traversal for the generators) is implemented once against a shared abstraction the Domain layer exposes (both aggregates expose the same node-tree manipulation surface), not duplicated per module. The *commands* are still separate per module (`AddSchemaNodeCommand` vs. `AddComponentNodeCommand`) because they have different authorization/validation context (a schema node-add checks Project-level access; a component node-add checks Organization-level access, per Step 2 §7's Org-scoped components) — but the underlying tree logic is one implementation, reused.

---

## 4. `SchemaForge.Infrastructure`, `SchemaForge.Contracts`, `SchemaForge.Api`

```
SchemaForge.Infrastructure/
├── Persistence/
│   ├── SchemaForgeDbContext.cs
│   ├── Configurations/          # IEntityTypeConfiguration<T> per entity, module-first subfolders
│   ├── Migrations/
│   ├── Interceptors/            # SaveChangesInterceptor: TenantId stamping, domain event dispatch (Step 1 §5/§7)
│   └── Repositories/            # the few repository implementations Application actually asked for
├── Security/
│   ├── TenantContext.cs         # ITenantContext implementation, resolves from JWT claims
│   ├── RlsSessionInitializer.cs # issues `SET LOCAL app.current_tenant_id` per transaction (Step 5 §3)
│   └── JwtTokenService.cs
├── Caching/                     # ICacheService → Redis
├── Storage/                     # IFileStorage → local disk (dev) / MinIO (docker-compose) — never Azure/AWS-specific (Step 1 confirmed decision)
├── BackgroundJobs/
│   ├── OutboxBackgroundService.cs   # the IHostedService worker (Step 1 §8)
│   └── Handlers/                    # one handler per background_jobs.job_type, e.g. RunTestSuiteJobHandler
└── Ai/
    └── NullSchemaSuggestionProvider.cs   # no-op placeholder implementation of ISchemaSuggestionProvider — see Step 9

SchemaForge.Contracts/
└── V1/
    ├── Organizations/
    ├── Schemas/
    │   ├── SchemaDefinitionResponse.cs
    │   ├── SchemaVersionResponse.cs
    │   ├── CreateSchemaDefinitionRequest.cs
    │   └── ...
    ├── Components/
    ├── Testing/
    └── Validation/

SchemaForge.Api/
├── Controllers/
│   └── V1/                      # mirrors Contracts/V1 and the Step 6 route catalog 1:1
├── Middleware/                  # exception → ProblemDetails mapping, correlation ID enrichment
├── Authorization/               # resource-based IAuthorizationHandler implementations (Step 6 §1.7)
├── Mapping/                     # explicit Application-DTO ↔ Contracts-DTO mapping — see note below
├── Extensions/
│   ├── ApplicationServiceCollectionExtensions.cs   # AddApplication()
│   ├── InfrastructureServiceCollectionExtensions.cs # AddInfrastructure()
│   └── ApiServiceCollectionExtensions.cs            # AddApi() — auth, versioning, Swagger, rate limiting
└── Program.cs
```

**Mapping is explicit hand-written methods/extension methods, not AutoMapper/Mapster.** Consistent with the brief's "favor readability over cleverness": a reflection-based mapper hides what's actually happening (which Contract field comes from which domain field) behind configuration, and it's exactly the kind of magic that makes a `SchemaNode` → `SchemaVersionResponse` mapping bug hard to spot in review. An explicit `ToResponse()` extension method is more lines of code and is *also* the more senior choice here — it's directly debuggable, directly greppable, and the compiler catches a missed field instead of a runtime reflection miss.

---

## 5. `tests/`

```
SchemaForge.ArchitectureTests/
└── LayerDependencyTests.cs      # NetArchTest rules enforcing Step 1 §2's dependency table mechanically:
                                  # "Domain has no dependency on X", "Application doesn't reference EF Core",
                                  # "Controllers only reference Infrastructure through Application interfaces"

SchemaForge.UnitTests/
├── Domain/                      # mirrors SchemaForge.Domain's module folders 1:1
├── Application/                 # mirrors SchemaForge.Application's module folders 1:1
└── Generation/                  # generators get particularly thorough unit test coverage —
                                  # pure functions over a SchemaNode tree, cheap to test exhaustively

SchemaForge.IntegrationTests/
├── Fixtures/                    # Testcontainers-backed Postgres + Redis, WebApplicationFactory<Program>
├── MultiTenancy/
│   ├── QueryFilterIsolationTests.cs   # asserts the EF Core layer (Step 5 §3)
│   └── RowLevelSecurityTests.cs       # asserts the RLS layer independently — a request that somehow
│                                       # bypasses the EF filter must still be blocked at the DB
├── Concurrency/                 # ETag/If-Match conflict tests (Step 6 §1.5)
├── Idempotency/                 # Idempotency-Key replay tests (Step 6 §1.6)
└── Endpoints/                   # one folder per Controller, module-first again
```

The **`MultiTenancy/RowLevelSecurityTests.cs`** file existing as its own thing, separate from the EF Core filter test, is a direct consequence of Step 5 §3's confirmed decision — if RLS is only ever exercised incidentally through normal EF-Core-mediated requests, it's not actually being tested as an independent layer; the test suite needs to deliberately construct the "EF filter would have failed to protect this" scenario (e.g. a raw `context.Database.ExecuteSqlRaw` query) and confirm RLS catches it alone.

---

## 6. `frontend/` — mirrors backend module boundaries

```
frontend/
├── src/
│   ├── modules/
│   │   ├── schemas/              # Designer + Library — the largest module, matching backend Schemas/
│   │   │   ├── components/       # Vue SFCs — "Vue component" per Step 2's naming disambiguation
│   │   │   ├── composables/
│   │   │   ├── stores/           # Pinia — holds the client-side SchemaNode tree during editing (Step 6 §3)
│   │   │   ├── api/              # typed HTTP client calls, using generated TypeScript interfaces (Step 6 §2.4 export!)
│   │   │   └── views/
│   │   ├── components-library/   # Reusable Components module (named to avoid clashing with "Vue components")
│   │   ├── organizations/
│   │   ├── projects/
│   │   ├── testing/
│   │   ├── validation/
│   │   └── audit-log/
│   ├── shared/                   # cross-module UI primitives, design-system pieces
│   ├── router/
│   ├── stores/                   # app-wide Pinia stores: auth/session, current organization context
│   └── types/                    # imports the TypeScript interfaces generated by the API itself
├── vite.config.ts
├── tailwind.config.ts
└── package.json
```

Frontend modules mirror backend modules **deliberately** — `frontend/src/modules/schemas/` talks primarily to `/api/v1/schemas/*` and `/api/v1/schema-versions/*`, mirroring `SchemaForge.Domain/Schemas/`. This isn't a hard rule (a "Dashboard" view will legitimately pull from several modules), but as a default it keeps "where does the code for X live" answerable identically on both sides of the stack, and it means the future TypeScript-interface generator (Step 6 §2.4) has an obvious place to drop its output per module (`types/generated/schemas.ts`) rather than one giant undifferentiated file.

---

## 7. What Step 7 deliberately defers

- Whether any module boundary here is a real bounded-context seam worth defending more formally (shared kernel vs. customer/supplier vs. anti-corruption layer relationships between modules) → **Step 8**
- The AI provider abstraction's concrete folder placement beyond the `Infrastructure/Ai/` stub above → **Step 9**

---

**Next step once this is approved**: Step 8 — Bounded contexts (formalizing which of these module boundaries are genuine DDD bounded contexts, their relationships, and which — if any — would be the first candidate to extract into a separate service if SchemaForge ever outgrew the modular monolith).
