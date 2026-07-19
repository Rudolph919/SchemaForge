# Step 5 — Database Schema Design

Status: **Draft for review**
Decides: physical PostgreSQL schema — the JSONB-vs-normalized call for `SchemaNode` trees (deferred twice now, resolved here), table definitions, indexes, and the concrete constraint mechanisms invariants from Step 3 §4 rely on.
Does not decide: API resource shapes → Step 6.

---

## 1. Schema-wide conventions

Stated once here rather than repeated per table:

- **Naming**: `snake_case`, plural table names (`schema_versions`, not `SchemaVersion`) — standard Postgres/EF Core-migrations convention, keeps generated SQL idiomatic rather than looking like C# with underscores.
- **Primary keys: plain `Guid.NewGuid()` (v4), generated application-side.** UUIDv7's time-ordering would give better B-tree locality under heavy write load, but that benefit is currently theoretical at this project's realistic scale, and it costs a custom generation helper (.NET doesn't uniformly provide `Guid.CreateVersion7()` across target versions) for a problem that doesn't exist yet. Plain v4 is the standard, zero-friction choice — nothing to build, nothing to explain, revisit only if write-volume ever makes the fragmentation cost real and measurable. *(Simplified from an earlier UUIDv7 recommendation — the original reasoning wasn't wrong, just optimizing for a scale this project doesn't have.)*
- **Timestamps**: always `timestamptz`, never bare `timestamp` — an enterprise SaaS with users across time zones storing naive timestamps is a well-known source of subtle bugs.
- **No generic soft-delete flag.** Consistent with Step 4: every aggregate that needs a "removed" state models it as an explicit status value (`Archived`, `Deprecated`, `Revoked`) on its own status enum, not a bolted-on `is_deleted` boolean. A true hard delete is reserved for genuinely erroneous data (e.g., a `SourceDocument` uploaded by mistake) and is itself audit-logged. This keeps "why is this row hidden" always answerable from the row's own state, rather than requiring a `WHERE is_deleted = false` convention that's easy to forget in a one-off query.
- **Foreign keys default to `ON DELETE RESTRICT`.** Given the "no hard delete for real records" convention above, cascading deletes are rarely correct here — a stray `DELETE FROM organizations` should fail loudly, not silently cascade through a tenant's entire history. The few genuine parent-owns-children-in-one-aggregate cases (e.g. `test_cases` under `test_suites`) use `ON DELETE CASCADE` deliberately, called out per-table below.
- **Audit columns**: every table backing an `AuditableEntity` gets `created_at timestamptz not null`, `created_by_user_id uuid not null references users(id)`, `updated_at timestamptz`, `updated_by_user_id uuid references users(id)`.

---

## 2. The `SchemaNode` tree: JSONB, not normalized self-referencing tables

This was explicitly deferred in Step 3 §7 and Step 4 §8 — it's the single most consequential physical schema decision in this step.

**Decision: `SchemaVersion.RootNode` and `LocalDefinitions` are stored as a `jsonb` column, not as rows in a `schema_nodes` table with a self-referencing `parent_node_id`.**

**Why not normalized tables** (the more "obviously relational" choice): a `SchemaNode` tree can be arbitrarily deep (nested objects within arrays within objects — the brief explicitly calls out "deep object graphs"). Loading an arbitrarily-deep self-referencing tree from normalized rows requires either a **recursive CTE** (`WITH RECURSIVE`) per load, or N+1 round trips walking level by level — and EF Core has no first-class, efficient way to materialize an arbitrary-depth self-referencing entity graph in one query. Every single load of a `SchemaVersion` (opening the Designer, running validation, generating docs) would pay this cost, for data that Step 3's Ground Rule 3 already established is loaded and saved **as one atomic unit** — the aggregate boundary *is* "the whole tree, all at once." Normalizing something that's always read/written as one blob just to get individually-addressable rows we never actually query independently is complexity without a corresponding benefit.

**Why JSONB fits**: the access pattern is exactly "load the whole `SchemaVersion` aggregate, work with its tree in memory, save the whole thing back" — a single `jsonb` column read/write matches that perfectly, with EF Core 9 + Npgsql's native JSON column mapping (`OwnsOne`/`ToJson()`, a first-class supported feature, not a workaround) deserializing straight into the `SchemaNode` object graph. Per-node `Id`s (Step 4 §6) are preserved as fields *within* the JSON, so identity for diffing/UI-binding purposes is unaffected — it's specifically *relational row identity* we're giving up, not *domain* identity. Postgres JSONB also supports **GIN indexes** for containment/path queries, so if a future need emerges (e.g. an admin tool: "find every schema across the org using `format: email`"), it's answerable with `jsonb_path_ops` indexing rather than requiring a schema change — noted as a deferred, not-needed-for-MVP index in §6.

**Honest trade-off being accepted**: a normalized `schema_nodes` table could carry a real foreign key from a node's `ComponentReference` to `component_versions(id)`, giving the database itself a referential-integrity guarantee. A JSONB column can't — the reference lives inside a blob, invisible to the FK system. This is not a new gap, though: Step 3 §4 already decided `ComponentReference` validity is enforced at the application layer (checked in the `PublishSchemaVersion` handler), specifically *because* a Draft schema is allowed to reference a still-Draft component. So JSONB isn't giving up a guarantee we were otherwise going to have — it's consistent with a decision already made for independent reasons.

---

## 3. Multi-tenancy at the physical layer

Every tenant-scoped table carries `organization_id uuid not null references organizations(id)`. Per Step 1 §7, this is enforced in application code via an EF Core global query filter (`HasQueryFilter`) plus a `SaveChangesInterceptor` that stamps/validates it on write. That's one layer of defense — entirely dependent on every query going through EF Core with the filter correctly applied.

**I want your input on whether to add a second, database-level layer: Postgres Row-Level Security (RLS).**

RLS would add a policy per tenant table like:

```sql
ALTER TABLE schema_definitions ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON schema_definitions
    USING (organization_id = current_setting('app.current_tenant_id')::uuid);
```

with the application setting `SET LOCAL app.current_tenant_id = '...'` at the start of every transaction. This means a cross-tenant data leak would require **both** the EF Core filter **and** the RLS policy to fail — a raw ad-hoc SQL query, a future engineer's `.IgnoreQueryFilters()` mistake, or a bug in a background job that bypasses the normal request pipeline would still be caught at the database level. This is a real, well-established defense-in-depth pattern for shared-schema multi-tenant systems and would be a strong, demonstrable piece of the portfolio story. The cost is real too: every tenant table needs a maintained policy (one more thing to keep in sync via migrations), every DB connection/transaction must reliably set the session variable (this gets genuinely fiddly with connection pooling — `SET LOCAL` is transaction-scoped so it's safe with PgBouncer transaction-pooling mode, but it's an easy detail to get wrong), and integration tests need to cover RLS behavior specifically, not just the EF Core filter.

---

## 4. Physical tables

### 4.1 Identity & access

```sql
users (
    id uuid primary key,
    email citext not null unique,          -- case-insensitive equality, Postgres citext extension
    password_hash text not null,
    display_name text not null,
    email_verified boolean not null default false,
    created_at timestamptz not null
)

organizations (
    id uuid primary key,
    name text not null,
    slug text not null unique,
    plan_tier text not null,               -- small closed set; plain text + CHECK constraint over an enum table (see §5)
    status text not null,                  -- 'active' | 'suspended'
    created_at timestamptz not null
)

organization_memberships (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    user_id uuid not null references users(id) on delete restrict,
    role text not null,                    -- 'owner' | 'admin' | 'member'
    status text not null,                  -- 'invited' | 'active' | 'revoked'
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    unique (organization_id, user_id)
)

teams (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    name text not null,
    description text,
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    unique (organization_id, name)
)

team_memberships (
    id uuid primary key,
    team_id uuid not null references teams(id) on delete cascade,   -- owned by the Team aggregate (Step 3) — cascade is correct here
    user_id uuid not null references users(id) on delete restrict,
    joined_at timestamptz not null,
    unique (team_id, user_id)
)
```

### 4.2 Workspace

```sql
projects (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    name text not null,
    description text,
    status text not null,                  -- 'active' | 'archived'
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    updated_at timestamptz,
    updated_by_user_id uuid references users(id),
    unique (organization_id, name)
)

source_documents (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    project_id uuid not null references projects(id) on delete restrict,
    file_name text not null,
    storage_key text not null,             -- opaque IFileStorage key, not a raw path
    content_type text not null,
    size_bytes bigint not null,
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id)
)
```

### 4.3 Schema core

```sql
schema_definitions (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    project_id uuid not null references projects(id) on delete restrict,
    name text not null,
    description text,
    tags text[] not null default '{}',
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    updated_at timestamptz,
    updated_by_user_id uuid references users(id),
    unique (project_id, name)
)

schema_versions (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    schema_definition_id uuid not null references schema_definitions(id) on delete restrict,
    version_major int not null,
    version_minor int not null,
    version_patch int not null,
    status text not null,                  -- 'draft' | 'published' | 'deprecated'
    change_summary text,
    root_node jsonb not null,              -- see §2
    local_definitions jsonb not null default '[]',
    published_at timestamptz,
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    unique (schema_definition_id, version_major, version_minor, version_patch)
)

-- enforces the Step 3 §4 "one Draft at a time" invariant at the DB level
create unique index ux_schema_versions_one_draft
    on schema_versions (schema_definition_id)
    where status = 'draft';
```

### 4.4 Components

```sql
component_definitions (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    name text not null,
    description text,
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    unique (organization_id, name)
)

component_versions (
    -- identical shape to schema_versions, same partial-unique-draft index pattern
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    component_definition_id uuid not null references component_definitions(id) on delete restrict,
    version_major int not null,
    version_minor int not null,
    version_patch int not null,
    status text not null,
    change_summary text,
    root_node jsonb not null,
    local_definitions jsonb not null default '[]',
    published_at timestamptz,
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    unique (component_definition_id, version_major, version_minor, version_patch)
)

create unique index ux_component_versions_one_draft
    on component_versions (component_definition_id)
    where status = 'draft';
```

### 4.5 Testing

```sql
test_suites (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    schema_definition_id uuid not null references schema_definitions(id) on delete restrict,
    name text not null,
    description text,
    created_at timestamptz not null,
    created_by_user_id uuid not null references users(id),
    unique (schema_definition_id, name)
)

test_cases (
    id uuid primary key,
    test_suite_id uuid not null references test_suites(id) on delete cascade,  -- owned by TestSuite aggregate — cascade correct
    name text not null,
    input_json jsonb not null,
    expectation jsonb not null,            -- { kind: 'valid' } | { kind: 'errors', expected: [...] }
    unique (test_suite_id, name)
)

test_runs (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    test_suite_id uuid not null references test_suites(id) on delete restrict,
    schema_version_id uuid not null references schema_versions(id) on delete restrict,
    executed_at timestamptz not null,
    executed_by_user_id uuid not null references users(id),
    results jsonb not null                 -- TestCaseResult[] — bounded, immutable execution record; JSONB for the same reasons as §2
)
```

### 4.6 Validation & audit

```sql
validation_runs (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    project_id uuid not null references projects(id) on delete restrict,
    schema_version_id uuid not null references schema_versions(id) on delete restrict,
    input_payload_hash char(64) not null,  -- SHA-256 hex — see Step 4 §7 for why hash-only
    outcome text not null,                 -- 'valid' | 'invalid'
    errors jsonb not null default '[]',
    executed_at timestamptz not null,
    executed_by_user_id uuid not null references users(id)
)

audit_log_entries (
    id uuid primary key,
    organization_id uuid not null references organizations(id) on delete restrict,
    actor_user_id uuid not null references users(id),
    action text not null,
    entity_type text not null,
    entity_id uuid not null,
    metadata jsonb,
    occurred_at timestamptz not null
)
```

### 4.7 Infrastructure: the outbox (Step 1 §8)

```sql
background_jobs (
    id uuid primary key,
    job_type text not null,
    payload jsonb not null,
    status text not null,                  -- 'pending' | 'processing' | 'completed' | 'failed'
    attempts int not null default 0,
    available_at timestamptz not null,     -- supports scheduled/retry-with-backoff jobs
    locked_at timestamptz,
    locked_by text,                        -- worker instance identifier, for lease-based dispatch
    created_at timestamptz not null,
    completed_at timestamptz
)

-- the worker's dispatch query — a partial index keeps this cheap even with a large historical table
create index ix_background_jobs_dispatch
    on background_jobs (available_at)
    where status = 'pending';
```

Not tenant-scoped by `organization_id` directly at the table level — the job `payload` carries whatever tenant context the specific job type needs, since jobs aren't queried or displayed to end users the way every other table here is.

---

## 5. Enum representation: `text` + `CHECK`, not Postgres native `enum` types

Every status/role/kind field above (`status`, `role`, `outcome`, ...) is `text` with an application-level C# enum on top, not a Postgres native `enum` type.

**Why**: Postgres native enums are notoriously painful to evolve — adding a value is fine, but reordering or removing one requires rebuilding the type, and `ALTER TYPE ... ADD VALUE` can't run inside a transaction in older Postgres versions (fixed in recent versions, but the historical friction is exactly why this is a well-known anti-pattern to avoid by default). A `text` column with a `CHECK (status IN (...))` constraint gives the same data-integrity guarantee, is trivial to evolve in a migration, and reads identically in query results — the friction gap it used to have has closed while the flexibility win remains. EF Core maps these cleanly to C# enums via a value converter either way, so there's no application-code cost to this choice.

---

## 6. Indexing strategy

- **Every tenant table gets a leading-`organization_id` composite index** matching its dominant query shape (e.g. `(organization_id, name)` on `projects`, already covered by the uniqueness constraints above, which double as indexes).
- **`audit_log_entries (organization_id, occurred_at desc)`** — the audit log browsing UI is fundamentally a paginated, most-recent-first feed; this index makes that the cheap case.
- **`audit_log_entries (organization_id, entity_type, entity_id, occurred_at desc)`** — supports "show me the history of this specific `SchemaVersion`," the other primary access pattern for the Audit Log module.
- **`validation_runs (schema_version_id, executed_at desc)`** — per-schema validation history view.
- **Deferred, not needed for MVP**: a `GIN` index on `schema_versions.root_node` using `jsonb_path_ops` for cross-schema structural search (§2). No current feature needs it; adding it later is a zero-downtime `CREATE INDEX CONCURRENTLY`, so there's no cost to deferring it.

---

## 7. Migrations

EF Core Migrations, one migration per logical schema change (not one giant initial migration for the whole system) — each migration PR-reviewable on its own, mirroring how the rest of this project is being built incrementally. `dotnet ef migrations script` output gets checked for destructive operations (column drops, type narrowing) in CI before it's allowed to merge — a lightweight but real safety net against an accidental data-loss migration reaching production.

---

## 8. What Step 5 deliberately defers

- API resource/route shapes → **Step 6**
- Exact `appsettings`/connection-pooling configuration (PgBouncer mode, pool sizing) — an implementation-time concern, revisited if/when the RLS question in §3 lands on "yes" (transaction-pooling mode compatibility matters specifically for `SET LOCAL`)

---

## Decision confirmed during review: Row-Level Security (§3)

**RLS added as a second layer, on top of the EF Core global query filter** — confirmed. Every tenant table gets an RLS policy keyed off `current_setting('app.current_tenant_id')`, set via `SET LOCAL` at the start of each transaction (safe under PgBouncer transaction-pooling mode, since `SET LOCAL` is transaction-scoped). A cross-tenant leak now requires both the EF Core filter and the RLS policy to fail simultaneously — catches raw SQL, a stray `.IgnoreQueryFilters()`, or a background job bypassing the normal request pipeline. **Accepted cost**: a policy to write and migrate per tenant table, session-variable plumbing through the connection/transaction lifecycle, and dedicated RLS-specific integration tests (Step 3 §4's tenant-isolation test suite now needs to assert both layers independently, not just the EF Core one).

---

**Next step once this is approved**: Step 6 — API design (REST resource modeling, routes, request/response contracts, and how the CQRS-vs-plain-service split from Step 1 §3 surfaces at the HTTP boundary).
