# Step 6 — API Design

Status: **Draft for review**
Decides: REST resource modeling, route catalog, request/response conventions, error contract, and how the Step 1 CQRS/MediatR decisions actually surface at the HTTP boundary.
Does not decide: solution/folder layout → Step 7.

---

## 1. Conventions

### 1.1 Controllers, not Minimal APIs

**Decision**: attribute-routed MVC Controllers (`SchemaForge.Api.Controllers`), not Minimal API endpoint delegates.

With 15+ distinct resource groups (organizations, teams, projects, schemas, versions, components, test suites, validation runs, audit log, ...), Minimal APIs' one-lambda-per-endpoint style — which genuinely shines for small, focused APIs or perf/AOT-sensitive services — would scatter this many routes across a large `Program.cs` or a sprawl of extension methods with no natural per-resource grouping. Controllers give each resource a home (one class, filters/attributes for cross-cutting concerns, conventional XML-doc-comment-driven OpenAPI generation), which is both more maintainable at this scale and more legible to a reviewer looking for "where do Schema Version endpoints live." This isn't a performance-sensitive edge service where Minimal API's lower overhead would matter.

### 1.2 URL versioning, resource-oriented routes

`/api/v1/...`, per Step 1 §9. Routes are resource-oriented (nouns, not verbs) with **explicit action sub-resources for state transitions** rather than overloading `PATCH` — e.g. `POST /schema-versions/{id}/publish`, not `PATCH /schema-versions/{id} { status: "published" }`. A generic status-PATCH can't express the transition-specific validation ("publishing requires all component references to resolve to Published versions," Step 3 §4) as clearly in the API surface, and a dedicated action endpoint gives each transition its own place to document preconditions and its own authorization policy if publish rights ever differ from edit rights. This is the same modeling convention Stripe and GitHub's APIs use for exactly this reason.

### 1.3 Response shapes

- **Single resource**: returned directly, no wrapper envelope (`{ "id": "...", "name": "..." }`, not `{ "data": { ... } }`) — an envelope with no metadata to carry is pure noise.
- **Paginated list**: a structured envelope, because pagination metadata needs somewhere to live: `{ "items": [...], "nextCursor": "opaque-token-or-null" }`.
- **Pagination is cursor-based (keyset), not offset-based**, for every list endpoint. Tables like `audit_log_entries` and `validation_runs` grow unboundedly (Step 3 Ground Rule 2's "unbounded over time" concern applies here too, now at the API surface) — offset pagination (`?page=50000`) degrades badly on large, ever-growing tables (the database still has to skip 50,000 rows) and is unstable under concurrent inserts (a new audit entry shifts every subsequent page). A cursor encodes the last-seen sort key, so "give me the next page" is always an efficient indexed range scan regardless of table size or concurrent writes.

### 1.4 Error contract: RFC 7807 Problem Details

Every non-2xx response is a `ProblemDetails` (`application/problem+json`), extended with two fields: `errorCode` (matches the domain `Error.Code` from Step 4 §1) and, for validation failures specifically, an `errors` array of `{ path, code, message }` — reusing the same `JsonPath`-annotated shape the domain already uses (Step 4 §2). The `Error → HTTP status` mapping is mechanical, done once in a shared `ApiExceptionFilter`/result-mapping helper, not hand-rolled per controller action:

| Domain `ErrorType` | HTTP status |
|---|---|
| `Validation` | 400 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Forbidden` | 403 |
| `Unexpected` | 500 |

**Important distinction — validating JSON against a schema and getting "invalid" back is *not* an API error.** `POST /schema-versions/{id}/validate` returns **200 OK** with a body describing `{ "outcome": "invalid", "errors": [...] }` when the input payload fails validation — the endpoint's entire purpose is to answer "does this payload conform," and "no" is a completely successful answer to that question. A 400 response is reserved for the request *itself* being malformed (e.g. the request body isn't valid JSON at all, or `schemaVersionId` doesn't exist) — a subtle but important line, easy to get backwards, and a real Stripe/GitHub-caliber API never gets it backwards.

### 1.5 Optimistic concurrency

Every mutable resource (`Project`, `SchemaDefinition`, `TestSuite`, and — while in Draft — `SchemaVersion`/`ComponentVersion`) exposes a `ETag` response header backed by a Postgres `xmin`-derived row version, and requires `If-Match` on `PATCH`/`DELETE`. Two people editing a `TestSuite` at the same time is a realistic scenario (multiple team members iterating on the same schema), and silently last-write-wins would lose one of their edits without either person knowing — `If-Match` failure returns `409 Conflict`, forcing the client to reload and retry.

### 1.6 Idempotency for side-effecting POSTs

State-transition and creation endpoints (`publish`, `deprecate`, `POST .../versions`, `POST .../test-suites/{id}/run`) accept an optional `Idempotency-Key` header. A retried request (client timeout, network blip — the concrete failure mode this protects against for a "publish" action people really don't want to accidentally double-fire) with the same key returns the original response instead of re-executing. Backed by a short-lived Redis entry (key → response, TTL matching a reasonable client retry window), reusing the Redis dependency already in the stack rather than adding a new one.

### 1.7 Authorization

Every request authenticates via JWT bearer (Step 1). Authorization is **resource-based**, not just role-attribute-based: an `[Authorize]`-decorated action additionally runs an `IAuthorizationHandler` that resolves the target resource's `OrganizationId` (from the route, e.g. `{projectId}` → its `Organization`) and checks the caller's `OrganizationMembership` role for that specific organization — never a global role. This mirrors the RLS `organization_id` check from Step 5 §3 at the application layer, and specific actions (invite a member, publish a schema, delete a project) carry their own minimum-role requirement (e.g. publish requires `Admin` or `Owner`) rather than a blanket "any member can do anything" policy.

---

## 2. Route catalog

`Cmd` = dispatched as a MediatR command (mutates one aggregate, per Step 1 §3 / Step 3 Ground Rule 3). `Qry` = dispatched as a MediatR query (CQRS read path) or, for simple lookups, a direct Application-layer read (Step 1 §3 — not every `GET` needs a full query handler).

### 2.1 Auth

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/auth/register` | Create a `User` + their first `Organization` |
| POST | `/api/v1/auth/login` | Issue access + refresh token pair |
| POST | `/api/v1/auth/refresh` | Rotate access token |
| POST | `/api/v1/auth/logout` | Revoke refresh token |
| POST | `/api/v1/auth/password/forgot`, `/password/reset` | Password reset flow |

### 2.2 Organizations, Teams, Membership

| Method | Route | Purpose |
|---|---|---|
| GET / PATCH | `/api/v1/organizations/{id}` | Read / update org settings |
| GET | `/api/v1/organizations/{id}/memberships` | List members (paginated) |
| POST | `/api/v1/organizations/{id}/memberships` | Invite a member |
| PATCH | `/api/v1/organizations/{id}/memberships/{membershipId}` | Change role |
| DELETE | `/api/v1/organizations/{id}/memberships/{membershipId}` | Revoke (→ `Status = Revoked`, not a hard delete — the `DELETE` verb models "no longer available," not literal row deletion, consistent with Step 5 §1) |
| POST / GET | `/api/v1/organizations/{id}/teams` | Create / list Teams |
| GET / PATCH | `/api/v1/teams/{id}` | Read / rename Team |
| POST / DELETE | `/api/v1/teams/{id}/members/{userId}` | Add / remove Team member |

### 2.3 Projects & source documents

| Method | Route | Purpose |
|---|---|---|
| POST / GET | `/api/v1/organizations/{orgId}/projects` | Create / list Projects |
| GET / PATCH | `/api/v1/projects/{id}` | Read / update Project |
| POST | `/api/v1/projects/{id}/archive` | Explicit state transition (§1.2) |
| POST | `/api/v1/projects/{id}/documents` | Upload a `SourceDocument` (multipart) |
| GET | `/api/v1/projects/{id}/documents` | List documents |
| DELETE | `/api/v1/documents/{id}` | Hard delete (the one legitimate hard-delete case, Step 5 §1) |

### 2.4 Schema Designer & Library

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/projects/{projectId}/schemas` | Create `SchemaDefinition` |
| GET | `/api/v1/projects/{projectId}/schemas` | **Qry** — Schema Library listing: paginated, filterable (tags, name), sortable — the lean-projection query from Step 1 §3 |
| GET / PATCH | `/api/v1/schemas/{id}` | Read / rename `SchemaDefinition` |
| POST | `/api/v1/schemas/{schemaId}/versions` | **Cmd** — create a new Draft `SchemaVersion` (`ISchemaVersionFactory`, Step 3 §4) |
| GET | `/api/v1/schemas/{schemaId}/versions` | List versions (headers only — no node trees, matching Step 3 §2's "don't load the tree for metadata operations") |
| GET | `/api/v1/schema-versions/{id}` | Full version including node tree — the one endpoint that *does* pay the JSONB deserialize cost, because the Designer genuinely needs the whole tree |
| POST | `/api/v1/schema-versions/{id}/nodes` | **Cmd** — add a `SchemaNode` (see §3 below) |
| PATCH | `/api/v1/schema-versions/{id}/nodes/{nodeId}` | **Cmd** — update one node's properties/constraints |
| DELETE | `/api/v1/schema-versions/{id}/nodes/{nodeId}` | **Cmd** — remove a node |
| POST | `/api/v1/schema-versions/{id}/nodes/{nodeId}/move` | **Cmd** — reorder/reparent |
| POST | `/api/v1/schema-versions/{id}/publish` | **Cmd** — explicit transition, runs the component-reference check from Step 3 §4 |
| POST | `/api/v1/schema-versions/{id}/deprecate` | **Cmd** — explicit transition |
| GET | `/api/v1/schema-versions/{id}/diff?against={otherVersionId}` | **Qry** — computed `SchemaDiff` (Step 2 §1/§5 — never persisted) |
| GET | `/api/v1/schema-versions/{id}/export?format=json-schema\|openapi\|typescript\|csharp` | **Qry** — API Contract Generator + JSON Schema exporter (Step 4 §4.4/§4.5's export seam) |
| GET | `/api/v1/schema-versions/{id}/documentation?format=html\|markdown\|json` | **Qry** — Documentation Generator, Redis-cached (Step 1 §9) keyed on the immutable version id |

### 2.5 Validation

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/schema-versions/{id}/validate` | **Qry**-shaped (Step 1 §3) — validates the request body against this version, **persists** a `ValidationRun` (hash-only, Step 4 §7), returns the result inline. **200 OK regardless of valid/invalid outcome** (§1.4) |
| GET | `/api/v1/schema-versions/{id}/validation-runs` | Paginated validation history |

### 2.6 Reusable Components

Mirrors §2.4 exactly — `/api/v1/organizations/{orgId}/components`, `/components/{id}`, `/components/{id}/versions`, `/component-versions/{id}` (+ `/nodes`, `/publish`, `/deprecate`) — not repeated in full; identical shape, per Step 4 §5's "no new concepts."

### 2.7 Schema Testing

| Method | Route | Purpose |
|---|---|---|
| POST / GET | `/api/v1/schemas/{schemaId}/test-suites` | Create / list Test Suites |
| GET | `/api/v1/test-suites/{id}` | Read suite incl. cases |
| POST / PATCH / DELETE | `/api/v1/test-suites/{id}/cases[/{caseId}]` | Manage `TestCase`s |
| POST | `/api/v1/test-suites/{id}/run?targetVersionId={id}` | Dispatches a `TestRun` via the outbox worker (§4 below) — **202 Accepted**, `Location` header pointing at the new `TestRun` |
| GET | `/api/v1/test-runs/{id}` | Poll status / read results (`Pending` → `Completed`) |

### 2.8 Audit Log & Settings

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/organizations/{orgId}/audit-log` | Paginated, filterable by `entityType`, `entityId`, `actorUserId`, date range — backed by the Step 5 §6 indexes |
| GET / PATCH | `/api/v1/organizations/{orgId}/settings` | Org-level settings |

---

## 3. The Designer's editing model: fine-grained commands over the wire, coarse-grained persistence underneath

Worth making explicit since it reconciles two decisions that could otherwise look contradictory: Step 5 §2 stores the whole node tree as one JSONB blob, but §2.4 above exposes per-node `POST`/`PATCH`/`DELETE` endpoints rather than one big "replace the whole tree" `PUT`.

**Reasoning**: these operate at different levels. Over the wire, the Designer UI issues one HTTP call per meaningful edit (add a field, change a constraint, reorder properties) — a whole-tree `PUT` on every keystroke would be wasteful for a deeply nested schema and would lose "what specifically changed" semantics that make for a good `AuditLogEntry` (`"SchemaNode.Updated: field 'postalCode' pattern changed"` is a useful audit line; `"SchemaVersion.Replaced"` is not). Underneath, each of those commands still loads the *one* `SchemaVersion` aggregate, calls the matching domain method (`AddNode`/`UpdateNode`/...), and calls `SaveChangesAsync` exactly once — still Step 3 Ground Rule 3's "one command, one aggregate, one transaction," and still one JSONB write. The granularity difference is purely at the API/command level, not the persistence level — the aggregate boundary from Step 3 is unaffected either way.

---

## 4. Async test runs: always via the outbox, never inline

`POST /test-suites/{id}/run` always dispatches through the Step 1 §8 outbox/background worker and returns `202 Accepted`, even for a three-case suite that would finish in milliseconds. A dual sync-for-small/async-for-large path was considered and rejected: it means the client has to handle two different response shapes for the same logical action depending on suite size (a threshold that's also awkward to pick and will eventually be wrong in one direction), whereas a single always-async contract is simpler for every client to implement once, and scales unmodified as suites grow larger over the product's life. `GET /test-runs/{id}` is cheap to poll; a future iteration can upgrade this to push-based (SignalR) without changing the resource model at all.

---

## 5. What Step 6 deliberately defers

- Physical project/folder layout for controllers, contracts, and the mapping layer between them → **Step 7**
- Whether some of these route groups eventually become genuinely separable services (this is where the bounded-context map matters) → **Step 8**

---

**Next step once this is approved**: Step 7 — Folder structure (how the layers, modules, and everything designed in Steps 1–6 actually land in the solution's project/folder layout).
