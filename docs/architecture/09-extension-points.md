# Step 9 — Extension Points

Status: **Draft for review**
Decides: which future capabilities get a designed seam now (an interface, a boundary, a quarantine pattern) versus which get a one-line "the existing pattern already supports this" note and nothing built.
Does not decide: delivery order / which of any of this actually gets built in which phase → **Step 10**.

---

## 1. The criterion for earning a seam now

Speculatively abstracting everything a system *might* need is its own anti-pattern — exactly the "don't blindly implement patterns" instruction that's applied throughout this series applies here too. A capability earns a designed interface in this step only if at least one of these is true:

1. **The brief explicitly names it as a target capability** (AI schema suggestion, visual editing feeding off it).
2. **An earlier step already committed to swappability for an independent reason** (storage, background job dispatch — Step 1 §9/§8) — this step just needs to restate the seam, not invent a new one.
3. **It's a genuine, near-zero-cost Open/Closed win riding on infrastructure the MVP needs anyway** (the export-format registry, §3 — adding a format later costs one class, not a redesign).

Anything that doesn't clear one of these bars gets acknowledged in §6 and nothing else — no interface, no stub, no premature generality.

---

## 2. AI Schema Suggestion — the flagship seam

This is the one piece of infrastructure genuinely worth building an interface for today, specifically so the "Upload PDF → AI suggests schema → human reviews → visual editing → validation → publishing" lifecycle from the brief is **structurally possible from day one**, even though no AI call happens until a real provider is registered.

```
interface ISchemaSuggestionProvider
    Task<Result<SchemaSuggestion>> SuggestAsync(SourceDocument document, CancellationToken ct)

// a quarantined proposal — NOT a domain aggregate, never touches SchemaVersion directly
record SchemaSuggestion(
    string ProviderName,             // "null-provider", "claude-vision-suggest-v1", ... — provenance, always recorded
    decimal? OverallConfidence,
    IReadOnlyList<SuggestedNode> Nodes)

record SuggestedNode(
    string? PropertyName,
    NodeKind Kind,
    string? Description,
    decimal Confidence,
    IReadOnlyList<SuggestedNode> Children)
```

**The critical design decision: `SchemaSuggestion` is a quarantined proposal object, not a `SchemaVersion`.** The provider never gets write access to a real aggregate — it can only produce a `SchemaSuggestion`, which is inert data. A separate, ordinary Application command, `CreateDraftFromSuggestion(schemaDefinitionId, suggestion, acceptedNodeIds)`, is what actually materializes a real Draft `SchemaVersion`, and it does so by calling the exact same `SchemaVersion.AddNode(...)` domain methods a human editing in the Designer would call — meaning **every invariant the aggregate enforces on a human-authored node applies identically to an AI-suggested one**, and a human can accept some suggested nodes and reject others rather than an all-or-nothing import. This is what makes "human reviews" in the brief's lifecycle diagram a structural property of the design, not a UI convention someone could accidentally bypass — there is no code path where AI output becomes a Published schema without passing through the same command a human uses.

**`Infrastructure/Ai/NullSchemaSuggestionProvider.cs`** (already placed in Step 7's folder layout) implements this interface today, returning a `Result` failure ("AI suggestion is not configured for this environment"). This means the *entire* pipeline — a future `POST /source-documents/{id}/suggest-schema` endpoint, the `CreateDraftFromSuggestion` command, the Designer UI's "review AI suggestions" screen — can be built, wired end-to-end, and demoed today, with the only missing piece being a real provider implementation dropped in later behind the same interface. This is precisely what "the AI component should be replaceable and abstracted behind interfaces" (the brief's own words) means in practice, not just in principle. No specific vendor is named at the architecture level on purpose — a real implementation would call *some* multimodal model capable of reading a document image/PDF and proposing structure, and which one is an implementation-time choice, not an architectural commitment.

---

## 3. Export/generation formats: a registry, not a growing `if/switch`

Step 6 §2.4 already needs `GET /schema-versions/{id}/export?format=json-schema|openapi|typescript|csharp` for the MVP. Rather than one generator service with a growing `switch (format)`, each format is a small, independently registered implementation of a shared interface:

```
interface ISchemaExporter
    string FormatKey { get; }              // "json-schema", "openapi", "typescript", "csharp"
    Task<string> ExportAsync(SchemaVersion version, CancellationToken ct)
```

registered as `IEnumerable<ISchemaExporter>` in DI and dispatched by matching `FormatKey` against the `?format=` query parameter. **This costs nothing extra for the MVP's four formats** — it's the natural way to implement "four independent output formats" regardless of future plans — and it means a fifth format later (Python Pydantic models, GraphQL SDL, a Protobuf/Avro schema for a future streaming pipeline — none of these are commitments, just illustrative of what "later" could mean) is *one new class implementing one interface*, registered in one line, with zero changes to the controller, the routing, or any existing exporter. Same registry shape applies to `IDocumentationRenderer` (HTML/Markdown/JSON, Step 6 §2.4) and to a future `IJsonSchemaDialectHandler` if SchemaForge ever needs to import/export a dialect other than Draft 2020-12 (e.g. Draft-07 for legacy interoperability, or OpenAPI 3.1's schema dialect) — the importer/exporter pair from Step 4 §4.4 is already scoped to "Draft 2020-12" specifically so that a second dialect handler can be added the same way, without the first one needing to change.

---

## 4. Storage and background job dispatch — already resolved, restated for completeness

Two seams are already fully designed by earlier confirmed decisions; nothing new here, just closing the loop on the "extension points" catalog:

- **`IFileStorage`** (Step 1 §9): local disk today, MinIO in docker-compose — genuinely S3-API-compatible, so a later move to real AWS S3 (or an Azure Blob adapter behind the same interface) is a pure `Infrastructure/Storage/` swap, zero change above that layer.
- **`IJobDispatcher`** (Step 1 §8): in-process outbox worker today; a real broker (RabbitMQ, SQS, Azure Service Bus) later is the same kind of swap, made concretely relevant by Step 8 §5's finding that Schema Testing — the job type actually running through this dispatcher — is the most plausible first thing to ever need it.

---

## 5. A natural (not built) consequence of Step 8's Published Language pattern: outbound webhooks

Not requested by the brief, not being built now, but worth naming because it costs nothing to *notice*: Step 8 §4 established that Audit Log subscribes to every context's domain events without any context needing to know it exists. A future "notify an external system when a `SchemaVersion` publishes" feature would be **the same subscriber pattern**, not a new architectural capability — a `WebhookDispatcher` subscribing to `SchemaVersionPublished` the same way `AuditLogEntryProjector` does today. This is deliberately left as an observation, not an interface — building `IWebhookDispatcher` today with no confirmed customer need would be exactly the premature abstraction §1's criteria rule out.

---

## 6. Explicitly not built, and why that's fine

| Idea | Why it's not getting a seam now |
|---|---|
| Custom organization-defined format validators (beyond `SchemaFormat`'s enum + `Custom` fallback, Step 4 §4.2) | The `Custom` string fallback already covers the real near-term need (a format the enum doesn't have a name for); a true plugin-validator system is speculative until a concrete request exists |
| Pluggable authentication providers / OIDC abstraction | Step 1 confirmed **plain** self-hosted Identity (not the abstracted-hybrid option that was on the table) — building an abstraction layer now would second-guess a decision already made deliberately. If this is revisited later, it's a real migration (Infrastructure implementation swap + a user-data migration), not a pre-built seam, and that's an honest trade-off, not an oversight |
| Multi-region / multi-database sharding | No current scale justifies it, and Step 3's shared-schema multi-tenancy decision would need to be revisited first if it ever did — premature at this stage by a wide margin |

---

## 7. What Step 9 deliberately defers

- Everything about *when* any of this — including the MVP features these seams support — actually gets built → **Step 10**

---

**Next step once this is approved**: Step 10 — Phased implementation roadmap (the delivery order across everything designed in Steps 1–9, from first runnable slice to a feature-complete platform).
