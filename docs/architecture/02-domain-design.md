# Step 2 — Domain Design

Status: **Draft for review**
Decides: the core domain concepts (ubiquitous language), how they relate to one another conceptually, and — critically — what the *source of truth* for a schema actually is internally.
Does not decide: which concepts are aggregate roots vs. child entities, invariant enforcement, or field-level detail — those are Step 3 (aggregates) and Step 4 (entities).

---

## 1. Ubiquitous language — the domain glossary

This is the vocabulary every layer of the codebase (and every conversation about the system) should use consistently. Getting this wrong early is expensive — it's the thing that's hardest to refactor once controllers, DTOs and DB columns all disagree on names.

| Concept | Definition | Tenant-scoped | Mutable? |
|---|---|---|---|
| **Organization** | The billing/tenant boundary. Everything below this line belongs to exactly one Organization. | — (is the tenant) | Mutable (name, settings) |
| **Team** | An optional grouping of Users within an Organization, used for access scoping (e.g. "Finance", "Claims Processing"). | Org | Mutable |
| **Project** | A workspace within an Organization that groups related schemas and reference documents around a real-world use case (e.g. "Accounts Payable", "Loan Origination"). | Org | Mutable |
| **Membership** | The join between a User and an Organization (optionally a Team), carrying a Role. A User's identity is global; their access is per-Organization. | Org | Mutable |
| **SourceDocument** | An uploaded reference file (e.g. a sample invoice PDF) attached to a Project, used as design reference today and as future AI-suggestion input. | Org (via Project) | Immutable content, mutable metadata |
| **SchemaDefinition** | The *named, logical* schema (e.g. "Invoice Schema") — an identity that persists across versions. Owns metadata: name, description, owning Project, tags. | Org (via Project) | Mutable (metadata only) |
| **SchemaVersion** | An immutable, numbered snapshot of a SchemaDefinition's actual structure at a point in time (e.g. `1.2.0`). This is the thing that actually gets validated against, documented, and published. | Org (via SchemaDefinition) | **Immutable once published**; mutable while in `Draft` |
| **SchemaNode** | The internal structured representation of one node in the schema tree (object, array, string, number, enum, etc.) — see §2, this is *not* raw JSON. | — (owned by SchemaVersion) | Immutable once its SchemaVersion is published |
| **ComponentDefinition** | A named, independently versioned, reusable schema fragment (e.g. "PostalAddress", "MoneyAmount") that SchemaVersions can reference instead of redefining structure inline. | Org (shared across Projects) | Mutable while in `Draft`; immutable once published, same lifecycle as SchemaVersion |
| **ValidationRun** | A record of "this JSON payload was validated against this SchemaVersion, here's the result" — ad-hoc, user-initiated, persisted for audit/history. | Org | Immutable (it's a record of a past event) |
| **TestSuite** | A named collection of TestCases attached to a SchemaDefinition, exercised against a specific SchemaVersion. | Org | Mutable |
| **TestCase** | One input JSON payload + expected outcome (valid, or a specific set of expected errors) within a TestSuite. | Org | Mutable |
| **TestRun** | An execution record of a TestSuite against a SchemaVersion — pass/fail per TestCase, coverage summary. | Org | Immutable (it's a record of a past event) |
| **DocumentationArtifact** | Generated documentation (HTML/Markdown/JSON) for a specific SchemaVersion. Derived, cacheable, regenerable. | Org | Immutable per SchemaVersion (regenerated only if the version is regenerated, which it can't be — versions are immutable) |
| **ApiContractArtifact** | Generated OpenAPI spec / TypeScript interfaces / C# DTOs for a specific SchemaVersion. Same derived/cacheable nature as DocumentationArtifact. | Org | Immutable per SchemaVersion |
| **SchemaDiff** | A computed (not stored) comparison between two SchemaVersions — added/removed fields, type changes, validation changes. | Org | N/A — computed on demand |
| **AuditLogEntry** | An immutable record of "who did what, to what, when" — populated by domain event subscribers across every module. | Org | Immutable |

A few naming choices worth calling out explicitly, because they're easy to get wrong:

- **"Schema" is never used alone in code** — it's always `SchemaDefinition` (the identity) or `SchemaVersion` (the content), because conflating them is exactly how systems end up with "which schema, the one from yesterday or today?" bugs.
- **"Component" always means `ComponentDefinition`** (a reusable schema fragment) — never a UI/Vue component, which in this codebase is always referred to as, e.g., "Vue component" in prose to avoid ambiguity with the domain term.

---

## 2. The central domain decision: SchemaForge does not treat raw JSON Schema text as the source of truth

This is the most consequential decision in this step, so it gets its own section rather than a table row.

**The question**: when a user "designs a schema" in SchemaForge, what is actually being edited and persisted — the raw JSON Schema (Draft 2020-12) document itself, or SchemaForge's own structured internal model, with JSON Schema being one of several *generated outputs*?

**Decision: SchemaForge's own structured domain model (a `SchemaNode` tree) is the source of truth. Draft 2020-12 JSON Schema is a generated projection of it — one output among several (alongside OpenAPI, TypeScript, C# DTOs, and documentation).**

**Why not "the JSON Schema document is the source of truth" (Option A)**: This is the obviously easier path — parse/store/edit raw JSON, validate directly against it with an off-the-shelf validator (`JsonSchema.Net` or similar), done. But it fails several explicit requirements the moment you look closely:
- **Visual editing** (stated as part of the AI-ready roadmap) needs a *structured, addressable* model to bind a UI to. You can build a visual editor on top of raw JSON by parsing it into an ephemeral in-memory tree on every load, but then that tree isn't the persisted domain model — it's a throwaway UI artifact, and any validation of the *authoring process itself* (e.g. "this field has a `pattern` but no `example`, flag it in the Designer") has nowhere durable to live.
- **AI schema suggestion** (future) needs to *produce* something. If the source of truth is raw JSON text, the AI is generating a JSON string that then needs a second parse/validate pass to become editable — versus generating structured nodes directly into the same model a human edits, which is far more natural for a "human reviews, then visually edits" workflow.
- **Multi-format generation** (OpenAPI, TypeScript, C#, docs, JSON Schema itself) all becomes "parse the JSON Schema text, re-derive an AST, generate from that" *for every generator, independently* — versus generating from one canonical structured model. That's real duplicated complexity, and it's the kind of thing that quietly rots (one generator's JSON-Schema-parsing edge case handling drifts from another's).
- **Reusable components with referential integrity** (see §3) are much easier to enforce ($ref must point at a real, versioned ComponentDefinition the caller actually has access to) against a structured model with real foreign keys than against a string containing a `$ref: "#/$defs/..."` or an external URI.

**Why this is the harder path, honestly**: it means SchemaForge has to build and maintain its own structured schema model *and* a JSON-Schema-Draft-2020-12 exporter/importer (for the "paste/import an existing JSON Schema" use case, which the platform should still support). That's meaningfully more work than "store the JSON blob." I'm recommending it anyway because it's what separates a schema *design platform* (which is the stated product) from a schema *text editor with a validator bolted on* — and because the brief specifically lists Visual Editing and AI-assisted design as target capabilities, not hypothetical ones.

**Consequence**: SchemaForge needs a **JSON Schema Draft 2020-12 importer** (parse external/pasted JSON Schema → `SchemaNode` tree) as a first-class capability, not just an exporter — covered in Step 9 (extension points), since it's also the seam the future "AI suggests schema from PDF" flow will eventually populate through.

---

## 3. Reusable components as first-class versioned entities, not string `$ref`s

Following from §2: JSON Schema's native `$ref` (pointing at a URI or a local `$defs` entry) is a text-level indirection with no inherent versioning or access control. SchemaForge models reuse instead as a **`ComponentReference` value object** — `(ComponentDefinitionId, VersionConstraint)` — held by a `SchemaNode`, resolved against real `ComponentDefinition`/`ComponentVersion` rows.

This gets us three things raw `$ref` can't give us for free:
1. **Impact analysis** — "if I change the `PostalAddress` component, which SchemaDefinitions reference it?" is a real query against real foreign keys, not a text search across JSON blobs.
2. **Version pinning** — a SchemaVersion can pin to `PostalAddress@1.2.0` explicitly, so publishing a new component version never silently changes the meaning of an already-published schema (this mirrors semantic-versioned package management, e.g. npm/NuGet — a familiar, well-understood mental model).
3. **Cross-tenant safety for free** — a `ComponentReference` resolved through the same tenant-scoped query path as everything else can't accidentally resolve to another org's component, which a bare string `$ref` to an external URI could never guarantee.

When SchemaForge *exports* to standard JSON Schema (Draft 2020-12), `ComponentReference`s are compiled down to real `$defs` entries (inlined or referenced, depending on export options) — so the output is fully spec-compliant and portable, even though the authoring-time model isn't limited to what raw `$ref` can express.

---

## 4. Version lifecycle: Draft → Published → Deprecated

Both `SchemaVersion` and `ComponentVersion` (component versions follow the identical lifecycle) move through:

```
Draft ──(publish)──► Published ──(deprecate)──► Deprecated
  │                                                  ▲
  └──────────────(discard, no history kept)──────────┘  (Draft only — Published/Deprecated are permanent record)
```

- **Draft**: mutable, freely editable, not usable as a validation/publishing target for other consumers, not visible in the Schema Library's "active" view by default.
- **Published**: immutable from this point forward. Any further change requires creating a *new* Draft version (e.g. `1.1.0`) — this is the npm/NuGet "publish is forever" mental model, and it's what makes DocumentationArtifact/ApiContractArtifact caching (§1) safe: a Published version's generated outputs never need invalidation because the version itself can never change.
- **Deprecated**: still fully usable (existing integrations relying on it keep working), but flagged in the Schema Library and excluded from "recommended" listings. This exists because in a real document-extraction pipeline, consumers integrate against a specific schema version and need a deprecation *signal*, not a hard cutoff.

I'm deliberately **not** introducing an `InReview`/approval-workflow state in this step. An approval gate before publish is a legitimate enterprise feature, but it's an *extension point* (a pluggable policy: "who can publish, does it need N approvals") rather than a core lifecycle state — modeling it now would hard-code one approval policy into the aggregate itself. It's called out explicitly in Step 9.

---

## 5. Conceptual ownership hierarchy

```
Organization  (tenant boundary)
 ├─ Membership ──── User (User identity is global; Membership+Role is per-Org)
 ├─ Team
 ├─ ComponentDefinition ──► ComponentVersion (Draft|Published|Deprecated)
 │                                     (shared across all Projects in the Org — see open question below)
 └─ Project
     ├─ SourceDocument
     ├─ SchemaDefinition ──► SchemaVersion (Draft|Published|Deprecated)
     │                          ├─ SchemaNode tree (references ComponentVersion via ComponentReference)
     │                          ├─ DocumentationArtifact  (generated, cached)
     │                          ├─ ApiContractArtifact    (generated, cached)
     │                          └─ TestSuite ──► TestCase
     │                                            └─ TestRun (execution record)
     ├─ ValidationRun (references a SchemaVersion; ad-hoc validation history)
     └─ SchemaDiff (computed on demand between two SchemaVersions; not stored)

AuditLogEntry — cross-cutting, populated by domain events raised across every concept above
```

This is a conceptual map, not an ER diagram — physical foreign keys, indexes and cardinalities are Step 5.

---

## 6. Why `ValidationRun` is persisted, not ephemeral

The validation engine's basic use case ("paste JSON, pick a schema, see errors") *could* be entirely stateless — compute and return, store nothing. I'm treating it as a first-class persisted entity instead, because:
- The **Audit Log** module's value proposition depends on validation activity being visible history, not a one-off screen the user navigates away from.
- **Future AI training/suggestion data** (flagged as an AI-ready concern in Step 1) needs exactly this kind of "here's a real payload and how well the schema handled it" data, and retrofitting persistence after the fact means losing everything that happened before the retrofit.
- It's a natural **coverage signal** distinct from `TestRun` — `TestRun` tells you "does the schema pass its own author-written tests," `ValidationRun` tells you "how is the schema actually performing against real-world payloads users are throwing at it." Conflating the two would lose that distinction.

The cost is modest (one more table, one more write on the validation hot path) and it's a write to an immutable, append-only record — cheap and safe.

---

## 7. What Step 2 deliberately defers

- Which of these concepts are aggregate *roots* vs. entities owned by another aggregate, and what invariants each aggregate enforces → **Step 3**
- Field-by-field entity definitions (what properties `SchemaNode` actually has per node type, etc.) → **Step 4**
- Physical schema: tables, columns, indexes, constraints → **Step 5**
- How these concepts surface as API resources/routes → **Step 6**

---

## Decision confirmed during review

**`ComponentDefinition` is Organization-scoped, shared across all Projects** — confirmed. Reuse across Projects (e.g. `PostalAddress` used by both an Invoice schema in one Project and a Claim schema in another) is the point of the concept; version pinning (§3) protects a Project from being silently affected when another Project's team bumps a shared component to a new version.

---

**Next step once this is approved**: Step 3 — Aggregate design (which of these concepts are aggregate roots, their invariants, and their consistency boundaries).
