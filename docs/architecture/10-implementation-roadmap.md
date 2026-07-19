# Step 10 — Phased Implementation Roadmap

Status: **Draft for review**
Decides: delivery order across everything designed in Steps 1–9 — what gets built first, why that order specifically, and where the riskiest architectural bets get retired earliest.
This is the last architecture-phase document. Once approved, implementation begins.

---

## 1. Sequencing principle: retire infrastructure risk before building feature breadth

The ordering below is driven by one question at each step: **what's the cheapest way to find out if a foundational bet is wrong?** Steps 1–9 made several bets that are expensive to unwind late (JSONB for the node tree, dual-layer EF-filter-plus-RLS tenant isolation, MediatR-everywhere) — the roadmap front-loads exactly enough real code to prove or disprove each one, before investing in feature breadth that would be painful to rework if a foundational bet turned out wrong.

Each phase is still built the way the architecture docs were: small, reviewed, individually-committed changes — the incremental-commit discipline doesn't stop once design becomes code.

---

## 2. Phase 0 — Walking skeleton

**Goal**: one thin, real vertical slice through every layer, plus the tooling that makes every subsequent phase cheaper. Disproportionately valuable relative to its size — this is where the most foundational risk gets retired.

- Solution scaffolding: all six `src/` projects, three `tests/` projects, `Directory.Build.props`, `.editorconfig`, `global.json`, `docker-compose.yml` (Postgres, Redis, MinIO, Api)
- `SharedKernel` base types (`Entity`, `AggregateRoot`, `ValueObject`, `Result`/`Error`, `IDomainEvent`) — everything else depends on these
- `ArchitectureTests` wired up **immediately**, even against a nearly-empty solution — cheap insurance that's far more valuable started before any dependency-rule violation exists than retrofitted after one has already crept in
- CI (`GitHub Actions`): build + test on every PR, from the first PR — same "earlier is cheaper" logic as architecture tests
- **One real vertical slice: user registration, login, and Organization creation.** Chosen specifically because it exercises almost the entire stack without needing the `SchemaNode`/JSONB complexity yet: an EF Core migration against real Postgres, the full MediatR pipeline (`ValidationBehavior`, `TransactionBehavior`, `LoggingBehavior`), JWT issuance/validation, the `ProblemDetails` error mapping, and — because `Organization` is the first tenant-scoped table — the **EF Core global query filter and the RLS session-variable plumbing, together, for the first time**. If the dual-layer tenant isolation design from Step 5 §3 has a problem, this is the cheapest possible place to discover it.
- `IntegrationTests` fixture: Testcontainers-backed Postgres + Redis, `WebApplicationFactory` — established once here, reused by every later phase
- Minimal frontend scaffold: Vite + Vue 3 + TS + Pinia + Tailwind, wired to a real login screen hitting the real API — retires CORS, token storage, and API-client integration risk early, before any real feature UI is built on top of an unproven connection

---

## 3. Phase 1 — Workspace foundation

**Goal**: everything a schema needs to live inside before schema design itself begins.

- Full Identity & Access: `OrganizationMembership` (roles, invites, the last-Owner guard from Step 3 §4), `Team`/`TeamMembership`
- `Project` (create/archive), `SourceDocument` upload — first real exercise of `IFileStorage` → MinIO
- RLS policies extended to every table introduced so far, plus the dedicated `RowLevelSecurityTests` suite from Step 7 §5 — this is where the tenant-isolation testing pattern gets fully established for every table that follows
- Org-level `Settings`
- Frontend: org switcher, project list/create, team management, member invites, settings screens

---

## 4. Phase 2 — Schema Design core

**Goal**: the core domain itself. The largest, highest-stakes phase — worth treating as two sub-phases given its size.

**2a — Node model & API**: `SchemaDefinition`/`SchemaVersion` aggregates, the full `SchemaNode` hierarchy, JSONB owned-column mapping (the Step 5 §2 bet gets proven here), Draft/Publish/Deprecate lifecycle with the partial-unique-index enforcement from Step 3 §4, node CRUD commands (§2.4/§3 of Step 6), the Validation Engine, `/validate` + `ValidationRun` persistence (hash-only, per Step 4 §7).

**2b — Visual Designer UI**: the property-tree editor, per-node-kind constraint editors, drag/drop reordering, nullable/required toggles with the Step 4 §4.4/§4.5 translation semantics made visible in the UI. This is realistically the single largest piece of frontend work in the whole project and can proceed once 2a's API is stable, largely in parallel with later backend phases if useful.

---

## 5. Phase 3 — Reusable Components

`ComponentDefinition`/`ComponentVersion` — mostly a thin layer over Phase 2's already-built node machinery (Step 7 §3's shared-implementation note pays off directly here), plus `ComponentReference` wiring in the Designer and the publish-time all-references-must-resolve-to-Published check from Step 3 §4. Frontend: component library browser, "insert component" in the Designer.

---

## 6. Phase 4 — Generation: export, import, documentation, diff

`ISchemaExporter` registry (Step 9 §3) with the four MVP formats (JSON Schema, OpenAPI, TypeScript, C# DTOs), the JSON Schema importer (Step 4 §4.4), `IDocumentationGenerator` (HTML/Markdown/JSON, Redis-cached), and the computed `SchemaDiff` (Step 2 §5) with a diff viewer. Frontend: export/download UI, documentation viewer, "import existing schema" flow.

---

## 7. Phase 5 — Schema Testing

`TestSuite`/`TestCase`/`TestRun`/`TestCaseResult`. **This is where the background job infrastructure (Hangfire, the `IJobDispatcher` port from Step 1 §8) actually gets wired up** — deliberately not in Phase 0, because nothing before this phase needed asynchronous execution; Phase 0's auth/org creation is legitimately synchronous. Integrating Hangfire for its first real consumer, rather than speculatively in Phase 0, keeps Phase 0 focused on its own risks. `202 Accepted` + polling API per Step 6 §4. Frontend: test suite editor, run trigger, results/coverage view.

---

## 8. Phase 6 — Audit Log

Domain-event → `AuditLogEntry` projector, subscribing to every event already being raised by Phases 1–5's aggregates. **Worth pausing on**: because of the Open Host Service pattern established in Step 8 §4, adding Audit Log *after* every other context already exists costs those contexts nothing — they were raising domain events all along without knowing Audit Log was coming, which is exactly the decoupling payoff that pattern was chosen for. Frontend: audit log browser with entity/actor/date filters.

---

## 9. Phase 7 — Hardening

Rate limiting, `Idempotency-Key` on side-effecting POSTs, `ETag`/`If-Match` concurrency (Step 6 §1.5–1.6) across every mutable resource introduced by this point, a full RLS-policy coverage audit across every table from every phase, an index/N+1-query pass, a security review pass (dependency audit, secrets scanning in CI, OWASP Top 10 checklist — the brief's Security NFR), and an accessibility pass on the frontend (the brief's Accessibility NFR). This phase deliberately comes after feature breadth, not interleaved with it — hardening a moving target is wasted effort; hardening a feature-complete surface is not.

---

## 10. Phase 8 — AI Schema Suggestion (real provider)

A real `ISchemaSuggestionProvider` implementation (a multimodal LLM call), the `CreateDraftFromSuggestion` command flow and its review UI (Step 9 §2). Deliberately last: it depends on `SourceDocument` upload (Phase 1) and Draft `SchemaVersion` creation (Phase 2) already existing, and — per Step 9's own framing — the seam is designed to be *addable later without disruption*, which this phase is the proof of. Everything before it works, and is fully demoable, with `NullSchemaSuggestionProvider` in place.

---

## Your call: build in this risk-retiring order, or reprioritize for portfolio impact?

One real tension worth naming rather than deciding unilaterally: **the order above is the architecturally disciplined one** (retire infrastructure risk first, defer the flashiest feature to last), but it's not necessarily the order that best serves a *portfolio's* goals if you want to be sure the most visually/conceptually impressive pieces — the Visual Designer (2b) or AI Suggestion (8) — exist and are demoable even if the project doesn't reach full completion. Building AI suggestion last is the right engineering call in isolation, but if this project's timeline is uncertain and showing AI integration matters to you specifically, it may be worth pulling a thin version of Phase 8 forward once Phases 0–2 land, accepting some rework risk in exchange for having the showcase feature working sooner rather than only if everything before it gets finished first.

---

**This closes the architecture design phase (Steps 1–10).** Once you confirm the phase order (or adjust it), implementation begins with Phase 0.
