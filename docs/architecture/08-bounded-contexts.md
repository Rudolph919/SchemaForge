# Step 8 — Bounded Contexts

Status: **Draft for review**
Decides: which module boundaries from Steps 2–7 are genuine DDD bounded contexts, how they relate, and — the question Step 1 explicitly deferred to here — which one (if any) would be the first sensible candidate to extract from the modular monolith if SchemaForge ever outgrew it.
Does not decide: the AI provider seam and other pluggability points → **Step 9**.

---

## 1. An honest caveat before applying DDD context-mapping vocabulary

Classic DDD context-mapping patterns — Customer/Supplier, Conformist, Partnership — exist to describe the *political and organizational* reality between separate teams with separate release cadences and competing priorities. SchemaForge is one team, one codebase, one release cadence. Forcing that vocabulary onto module relationships that don't actually have inter-team dynamics would be exactly the kind of pattern-for-pattern's-sake the brief has repeatedly asked us to avoid — a "Customer/Supplier" label between two modules maintained by the same person in the same PR is decoration, not insight.

What *is* genuinely useful from DDD strategic design regardless of team structure: **subdomain classification** (§2, tells you where design investment is warranted), **dependency direction** (§3, tells you what can't compile without what), and the two places (§4) where a context-mapping pattern describes something real and useful even in a single-team monolith — because it's about *decoupling through events and translation*, not about *organizational boundaries*.

---

## 2. Subdomain classification

| Subdomain | Classification | Why |
|---|---|---|
| **Schema Design** (SchemaDefinition, SchemaVersion, SchemaNode, ComponentDefinition, ComponentVersion, the Validation Engine) | **Core** | This is the actual product. The structured-model-over-raw-JSON decision (Step 2 §2), the node hierarchy (Step 4 §4), the versioning lifecycle (Step 2 §4) — this is where SchemaForge is different from "a JSON Schema text editor," and where design investment has the highest return. |
| **Schema Testing** (TestSuite, TestCase, TestRun, TestCaseResult) | **Supporting** | Necessary and specific to this business (regression-testing a schema is a real, differentiated feature), but it's *in service of* the core domain rather than being the differentiator itself — it consumes the Validation Engine rather than reinventing validation logic. |
| **Workspace & Governance** (Project, SourceDocument, AuditLogEntry) | **Supporting** | Organizes and observes the work; specific to how this business operates, but not itself the reason a customer chooses SchemaForge. |
| **Identity & Access Management** (User, Organization, OrganizationMembership, Team, TeamMembership) | **Generic** | "Who is this, what can they do" is a solved problem across the entire software industry — conventional DDD guidance is *buy, don't build* here (an external IdP). |

**Direct callback to Step 1's confirmed decision**: conventional guidance says a Generic subdomain doesn't deserve deep investment — yet Step 1 confirmed self-hosted ASP.NET Core Identity over an external IdP. That's not a contradiction; it's a deliberate, acknowledged exception made for a reason external to the domain-design heuristic itself (portfolio demonstration value), which is exactly the kind of judgment call worth naming explicitly rather than pretending the heuristic wasn't overridden on purpose.

---

## 3. The bounded context map

| Context | Owns (from Step 3/7) | Depends on (by ID / event, never by object reference — Step 3 Ground Rule 1) |
|---|---|---|
| **Identity & Access** | `User`, `Organization`, `OrganizationMembership`, `Team`, `TeamMembership` | Nothing else in the system — truly foundational |
| **Workspace & Governance** | `Project`, `SourceDocument`, `AuditLogEntry` | Identity & Access (`OrganizationId`, `UserId`) |
| **Schema Design** | `SchemaDefinition`, `SchemaVersion`, `ComponentDefinition`, `ComponentVersion`, `SchemaNode`/`LocalDefinition`, Validation Engine, `ValidationRun` | Identity & Access, Workspace & Governance (`ProjectId`) |
| **Schema Testing** | `TestSuite`, `TestCase`, `TestRun`, `TestCaseResult` | Identity & Access, Schema Design (reads a `SchemaVersion`'s node tree, invokes its Validation Engine) |

**Why Components stays inside Schema Design, not its own context**: Step 4 §5 already established that `ComponentVersion` reuses the entire `SchemaNode`/`LocalDefinition` model verbatim — same ubiquitous language, same versioning lifecycle, same validation logic. Splitting it into a separate bounded context would fracture one coherent model into two contexts that would immediately need a translation layer between them for no actual gain — the opposite of what a bounded context boundary is for.

**Why Validation (the ad-hoc `/validate` endpoint) isn't its own context either**: validating a JSON payload against a `SchemaVersion` is intrinsic behavior *of* a `SchemaVersion` ("does this document conform to me"), not a separate concern layered on top. `ValidationRun` is that capability's activity log, riding along in the same context that owns the capability itself.

---

## 4. Where real context-mapping patterns actually apply here

Two relationships in this system are genuinely well-described by classic DDD context-mapping vocabulary, because they're about *decoupling*, which matters regardless of team structure:

**Audit Log as Open Host Service / Published Language.** Every other context raises domain events (`SchemaVersionPublished`, `OrganizationMemberRevoked`, ...) — a stable, well-known contract (`IDomainEvent` subtypes, Step 1 §5). Workspace & Governance's Audit Log subscribes to that published language and builds `AuditLogEntry` rows from it, **without ever reaching into another context's tables or aggregates**. This is the one relationship in the whole system where the dependency direction is inverted from what you'd naively expect — Schema Design doesn't know Audit Log exists, and a *new* context added a year from now gets audited for free the moment it raises events following the same contract, with zero code change in Audit Log itself. This is real decoupling value, not decoration.

**The JSON Schema Importer as Anti-Corruption Layer.** Step 4 §4.4's importer (parsing an externally-authored Draft 2020-12 document into SchemaForge's internal `SchemaNode` model) is a textbook ACL: it exists specifically to prevent an external format's quirks and conventions (arbitrary `$ref` styles, `type` arrays, however a given external tool chose to express nullability) from leaking into and shaping SchemaForge's internal authoring model. Everything on the internal side of that boundary stays exactly as designed in Step 4, regardless of what's thrown at the importer from outside.

Every other inter-context relationship in §3 is a plain, unremarkable dependency — a lower-level context referenced by ID from a higher-level one — and forcing a fancier label onto it would be noise.

---

## 5. If the monolith ever needs to split: Schema Testing is the extraction candidate, not Identity or Schema Design

Step 1 committed to a modular monolith specifically because there's no current, proven need for independent scaling or independent deploy cadences. If that ever changes, the module boundaries above are what make an extraction *possible* — but not all of them are good candidates, and it's worth being explicit about which one actually is, so this doesn't turn into "extract everything" the moment there's any pressure at all.

**Schema Testing is the strongest candidate**, for three concrete reasons:
1. **It already has a different scaling profile.** Running a large test suite is compute-bursty in a way that the rest of the API (mostly fast CRUD + JSONB reads) isn't — the kind of workload that genuinely benefits from scaling independently.
2. **The integration seam is already asynchronous.** Step 1 §8 and Step 6 §4 already route test runs through Hangfire, returning `202 Accepted` and polling for status. That's *already* a queue-shaped boundary, not a synchronous in-process call — extracting the worker into its own deployable means pointing a separate Hangfire server process at the same storage (something Hangfire supports natively) or swapping in a broker-backed `IJobDispatcher` implementation, either way without touching any caller.
3. **Its dependency on Schema Design is read-only and narrow** (fetch a `SchemaVersion`'s node tree, invoke validation) — turning that from an in-process query into an API call or a replicated read model is a contained, well-understood change, not a rewrite.

**Why not Identity & Access**: despite being classified Generic (§2, conventionally the "easiest to extract" kind of subdomain), it's synchronously depended on by every single request in the system for authentication — extracting it first would mean every other context takes on a network hop for something currently free, for a problem (scaling identity checks) that doesn't exist yet. Generic-subdomain status makes it a good candidate for *replacement* (swap for an external IdP, which Step 1 already designed the option to defer), not for *extraction*.

**Why not Schema Design**: it's the core domain — extracting the thing everything else depends on first would just relocate the monolith's center of gravity into a different single point of failure, while making the still-in-process Testing and Governance contexts pay a network hop to reach it. If anything is extracted, it should be a leaf, not the root.

---

## 6. Forward-looking implementation note (no change to Step 5, applied when migrations are actually written)

A cheap way to make these boundaries physically visible in Postgres without any service-splitting cost: namespace tables by **Postgres schema** (a native Postgres concept — unrelated to, and unfortunately homonymous with, JSON Schema) matching the bounded contexts above — `identity.users`, `workspace.projects`, `schema_design.schema_versions`, `testing.test_suites` — instead of everything in `public`. This doesn't require reopening Step 5 (which didn't specify a Postgres schema, implicitly `public`); it's a detail applied when the actual `IEntityTypeConfiguration`s and first migration are written, and it costs nothing beyond a `.ToTable("schema_versions", schema: "schema_design")` call per configuration. It buys a second, purely organizational reinforcement of exactly the boundaries this step just drew — useful for the same reason the folder structure in Step 7 is useful: it makes the architecture visible in the artifact itself, not just in a document describing it.

---

## 7. What Step 8 deliberately defers

- The AI schema-suggestion seam, storage-provider seam, and export-format seam — pluggability points that cut across these contexts rather than living inside one of them → **Step 9**

---

**Next step once this is approved**: Step 9 — Extension points (the AI-ready seam, the JSON Schema import/export seam, storage provider abstraction, and any other pluggability the roadmap should design for now without building yet).
