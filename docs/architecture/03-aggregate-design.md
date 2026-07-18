# Step 3 — Aggregate Design

Status: **Draft for review**
Decides: which Step 2 concepts are aggregate roots, where each aggregate's consistency boundary ends, and how invariants spanning multiple aggregates get enforced.
Does not decide: field-level entity detail → Step 4. Physical DB schema (tables, indexes, FKs) → Step 5.

---

## 1. Ground rules

Three rules govern every boundary decision below — stated up front so each individual call in §3 can just cite one instead of re-arguing it:

1. **Aggregates reference other aggregates by ID only, never by object reference.** A `SchemaVersion` holds a `SchemaDefinitionId` (a Guid), never a loaded `SchemaDefinition`. This is what makes aggregates independently loadable, independently testable, and prevents "load one thing, accidentally pull in half the database" object graphs.
2. **An aggregate must not contain an unbounded-over-time collection.** If a child collection grows forever for the lifetime of the system (versions accumulate for years, test runs accumulate on every CI-like execution), it does not belong *inside* the parent aggregate — it becomes its own aggregate, referencing the parent by ID. This is the single most important rule in this document and the reasoning behind the largest decision below (§2).
3. **One command = one aggregate = one transaction.** A single MediatR command handler loads and mutates exactly one aggregate and calls `SaveChangesAsync` once (the `TransactionBehavior` from Step 1 wraps this). Invariants that span two aggregates are never enforced by loading both into one transaction and hoping — they're enforced by a domain service performing a targeted check *before* the mutating aggregate method runs, backed by a database constraint as the actual concurrency-safe guarantee (§4).

---

## 2. The big one: `SchemaDefinition` and `SchemaVersion` are separate aggregate roots

Per Ground Rule 2: a `SchemaDefinition` accumulates versions for as long as it exists — years, potentially hundreds of versions for a heavily-iterated schema like "Invoice." If `SchemaVersion` (each one potentially containing a deep, recursive `SchemaNode` tree) were nested inside the `SchemaDefinition` aggregate, then *renaming a schema* would require loading its entire version history and node-tree content into memory. That's a correctness-irrelevant operation paying an unbounded, ever-growing cost — a textbook aggregate-design mistake.

**Decision**:
- **`SchemaDefinition`** (root): `Id`, `ProjectId`, `Name`, `Description`, `Tags`. Small, stable, cheap to load. Invariant: `Name` non-empty; `ProjectId` immutable after creation (a schema doesn't migrate between Projects — that's a delete-and-recreate, not a move, because Project is part of its tenant-scoping identity).
- **`SchemaVersion`** (root, one row/aggregate instance per version ever created): `Id`, `SchemaDefinitionId`, `VersionNumber` (SemVer), `Status` (`Draft`/`Published`/`Deprecated`), `ChangeSummary`, `CreatedBy`, `CreatedAt`, `PublishedAt`, and the actual `SchemaNode` tree (root node + descendants) as entities owned *within this aggregate*. This is where node-tree structural invariants are enforced atomically — see below.

The exact same reasoning applies to **`ComponentDefinition`/`ComponentVersion`** — identical split, identical justification, not repeated in full.

**Why the node tree lives inside `SchemaVersion` specifically (not further split out)**: unlike the version list on a `SchemaDefinition`, a single version's node tree is *bounded* — it's however deep and wide one schema actually is (dozens to low hundreds of nodes even for a complex nested document schema, not thousands accumulating over time) — and it needs atomic, all-or-nothing consistency: adding a property, changing a `pattern`, wiring a `ComponentReference` are all edits to the *same* version that must commit together or not at all (you never want a save that leaves a version's tree half-updated). That's exactly what an aggregate boundary is for.

**Consequence — versions are immutable after publish, enforced at the aggregate method level, not just by convention**: every mutating method on `SchemaVersion` (`AddNode`, `RemoveNode`, `UpdateNode`, `AddComponentReference`, ...) begins with a guard: if `Status != Draft`, the operation fails (returns a domain `Result` failure — per Step 1 §6 — not an exception, since "tried to edit a published version" is a legitimate, anticipatable caller error, not a bug). This means immutability is a property the aggregate itself defends, not something callers have to remember to respect.

---

## 3. Aggregate root catalog

| Aggregate Root | Owns (entities/VOs within the boundary) | Key invariants enforced | References other aggregates via |
|---|---|---|---|
| **User** | `Email`, `PasswordHash`, `DisplayName` | Email unique globally; not tenant-scoped (a person's identity spans Organizations) | — |
| **Organization** | `Name`, `Slug`, `PlanTier`, `Status` | `Slug` globally unique, URL-safe | — |
| **OrganizationMembership** | `Role` (Owner/Admin/Member), `Status` (Invited/Active/Revoked) | Unique `(OrganizationId, UserId)`; last-Owner protection (§4) | `OrganizationId`, `UserId` |
| **Team** | `Name`, `Description`, child entities `TeamMembership[]` (bounded — realistically dozens per team) | `TeamMembership.UserId` must have an active `OrganizationMembership` in the same Org (§4) | `OrganizationId` |
| **Project** | `Name`, `Description`, `Status` (Active/Archived) | `Name` unique within Organization | `OrganizationId` |
| **SourceDocument** | `FileName`, `StorageKey`, `ContentType`, `SizeBytes` | Immutable once uploaded (re-upload = new `SourceDocument`, never in-place replace, to preserve audit trail) | `ProjectId` |
| **SchemaDefinition** | `Name`, `Description`, `Tags` | See §2 | `ProjectId` |
| **SchemaVersion** | `VersionNumber`, `Status`, `ChangeSummary`, `SchemaNode` tree (entities), `ComponentReference[]` (VOs) | See §2; immutable post-publish; internal tree has no dangling internal references | `SchemaDefinitionId`, and (via `ComponentReference`) `ComponentVersionId` |
| **ComponentDefinition** | `Name`, `Description` | Name unique within Organization | `OrganizationId` |
| **ComponentVersion** | Same shape as `SchemaVersion` (`VersionNumber`, `Status`, node tree) | Same as `SchemaVersion` | `ComponentDefinitionId` |
| **TestSuite** | `Name`, `Description`, child entities `TestCase[]` (bounded — a suite with thousands of hand-authored cases is not a realistic shape) | Unique `TestCase` name within suite | `SchemaDefinitionId` (a suite is reusable across that schema's versions — see §5) |
| **TestRun** | `TargetSchemaVersionId`, `ExecutedAt`, child entities `TestCaseResult[]` (bounded by the suite's case count at run time) | Immutable once recorded (it's a historical execution record) | `TestSuiteId`, `SchemaVersionId` |
| **ValidationRun** | `SchemaVersionId`, `InputPayloadHash`, `ResultSummary`, `ValidationError[]` (VOs, bounded by errors-per-run) | Immutable once recorded | `ProjectId`, `SchemaVersionId` |
| **AuditLogEntry** | `Action`, `EntityType`, `EntityId`, `Metadata` | Immutable, append-only | `OrganizationId`, `ActorUserId` |

**Deliberately not modeled as aggregates**: `DocumentationArtifact` and `ApiContractArtifact` (Step 2 concepts) carry no business invariants of their own — "belongs to exactly one immutable `SchemaVersion`, therefore is generated once and cached forever" is a caching rule, not a domain rule. These are generated/cached records owned by the Infrastructure layer (keyed by `(SchemaVersionId, Format)`, produced on demand by a generator service, stored in Redis/blob storage), not Domain aggregates. Forcing them into the aggregate model would be exactly the kind of pattern-for-pattern's-sake the brief asks us to avoid. `SchemaDiff` remains what Step 2 already established: a pure computed value, never persisted, not an entity at all.

---

## 4. Cross-aggregate invariants: how they're actually enforced

These are the interesting cases — invariants that are real business rules but cross an aggregate boundary, so Ground Rule 3 says they can't be enforced by nesting.

| Invariant | Spans | Enforcement mechanism |
|---|---|---|
| An Organization must always have ≥ 1 active Owner | `Organization` ↔ `OrganizationMembership` (many) | Application-layer domain service `IOrganizationOwnershipGuard`, invoked before any command that revokes/demotes a Membership, counts active Owners via a targeted query (not a full aggregate load). **Accepted risk**: two simultaneous demote requests for two different Owners could theoretically both pass the check if it left exactly one Owner in a race window. Given this is a rare, human-initiated, low-frequency action (not a hot path), I'm accepting that narrow window rather than adding a DB-level trigger for it — flagged below as worth a second opinion. |
| A `TeamMembership` requires the user already hold an `OrganizationMembership` in the same Org | `Team` ↔ `OrganizationMembership` | Application-layer check before `Team.AddMember()` is called — the command handler queries `OrganizationMembership` existence first, and the aggregate method itself takes a pre-validated `UserId` rather than re-deriving trust from it |
| A `SchemaVersion` can only be **published** if every `ComponentReference` it holds resolves to a **Published** `ComponentVersion` (never a Draft or Deprecated one) | `SchemaVersion` ↔ `ComponentVersion` (many) | Enforced in the `PublishSchemaVersion` command handler: resolves every referenced `ComponentVersionId`, verifies `Status == Published` for each, fails the command (via `Result`) before calling `SchemaVersion.Publish()` if any reference doesn't resolve. **While in Draft**, a `SchemaVersion` is allowed to reference a `ComponentVersion` that is *also* still Draft — necessary so a schema and a new component can be co-designed together before either is finalized. |
| Only one `Draft` version may exist per `SchemaDefinition` at a time; version numbers increase monotonically | `SchemaDefinition` ↔ `SchemaVersion` (many) | A domain service (`ISchemaVersionFactory`) checks for an existing Draft and computes the next version number via a targeted query before creating a new `SchemaVersion`. **Backed by a Postgres partial unique index** (`UNIQUE (schema_definition_id) WHERE status = 'draft'`, detailed in Step 5) as the actual concurrency-safe guarantee — the application check is for a fast, friendly error message; the DB constraint is what actually prevents a race from creating two Drafts. |
| A `Project.Name` and a `SchemaDefinition.Name` (within a Project) must be unique | Within one aggregate type, across instances | Standard uniqueness — enforced the same way any uniqueness is: DB unique index (`(organization_id, name)` / `(project_id, name)`), application-layer check for a friendly pre-flight error |

The pattern across all of these: **the domain service or command handler performs a best-effort, friendly-error-message check; the database constraint is the actual source of truth for concurrency safety.** This is a deliberate two-layer approach — relying on application-level checks alone under concurrent load is a well-known source of subtle bugs (the classic "check-then-act" race), and relying on DB constraints alone gives you an ugly generic constraint-violation exception instead of a clean domain error. Doing both gets you clean errors in the common case and correctness in the race case.

---

## 5. Why `TestSuite` belongs to `SchemaDefinition`, not to a single `SchemaVersion`

Worth calling out explicitly because it's a real fork: a test suite could be pinned to one specific version (immutable together with it) or live at the `SchemaDefinition` level and be re-run against successive versions.

**Decision: `TestSuite` belongs to `SchemaDefinition`.** The valuable behavior this enables is **regression testing across versions** — "I changed the schema, did my existing test cases still pass?" is only answerable if the same suite can target version `1.0.0` today and `1.1.0` tomorrow. Each individual **`TestRun`** records which specific `SchemaVersionId` it targeted (§3), so history isn't lost — you can always see "suite X passed against version 1.0.0, then failed two cases against version 1.1.0," which is exactly the signal a schema author needs when iterating. Pinning suites to versions would mean every new version starts with zero tests, which defeats the purpose of having executable tests at all.

---

## 6. `SchemaNode` is an entity, not a value object

Worth a one-line justification since Step 2 didn't settle this: two structurally identical nodes (e.g., two `string` fields with the same `pattern`) are **not interchangeable** — each occupies a distinct position in the tree, needs a stable identity for the Visual Designer to bind to, for `SchemaDiff` to track "this field was renamed" vs. "this field was deleted and a new one added," and for `ValidationRun` errors to reference a specific node by ID rather than by a fragile structural path alone. So `SchemaNode` is a child **entity** (has an `Id`, persists identity across edits) inside the `SchemaVersion` aggregate, while genuinely interchangeable pieces — `ComponentReference`, `ValidationError`, a node's `Format`/`Pattern` constraints themselves — are **value objects** (no identity, compared by value, immutable, replaced wholesale rather than mutated in place).

---

## 7. What Step 3 deliberately defers

- Full field lists per entity/value object (e.g., every `SchemaNode` subtype's specific properties) → **Step 4**
- How `SchemaNode` trees, version headers, and everything else physically land in Postgres (JSONB vs. normalized tables — a real open question for the node tree specifically) → **Step 5**
- Repository interfaces per aggregate (only warranted for the ones with genuine query complexity, per Step 1 §4) → touched on in Step 4/5, finalized when we write the actual interfaces

---

## Decision confirmed during review

The **Organization "at least one Owner" race window** (§4, first row) stays an accepted risk, not closed with a DB trigger — confirmed. The action is rare and currently only human-initiated through Settings, so the narrow concurrent-demote window isn't worth the added complexity yet. **Revisit trigger**: if Organization membership management ever grows a programmatic/bulk API path (rather than only a human clicking in the UI), close this with a Postgres trigger or serializable-isolation check at that point, not before.

---

**Next step once this is approved**: Step 4 — Entity design (field-by-field definitions for every entity and value object cataloged above, including the `SchemaNode` type hierarchy needed to represent Draft 2020-12's full feature set — objects, arrays, enums, conditionals, etc.).
