# Step 4 — Entity Design

Status: **Draft for review**
Decides: field-by-field shape of every entity and value object cataloged in Step 3, and — the centerpiece of this step — the `SchemaNode` type hierarchy that has to represent everything Draft 2020-12 can express, including recursive schemas.
Does not decide: how any of this is physically stored (JSONB vs. normalized tables, indexes, constraints) → Step 5. Property/field shapes below are illustrative C#-style signatures to make the design concrete, not final implementation — no behavior, DI, or persistence code belongs in this step.

---

## 1. SharedKernel base types

Every entity in the system derives from one of these. Defining them precisely now means every subsequent field list can just say "inherits `AuditableEntity`" instead of repeating `CreatedAt`/`CreatedBy` everywhere.

```
abstract class Entity<TId>
    TId Id
    // equality by Id, not by reference or field-by-field

abstract class AggregateRoot<TId> : Entity<TId>
    IReadOnlyList<IDomainEvent> DomainEvents
    void ClearDomainEvents()  // called by the SaveChangesInterceptor post-commit

abstract class ValueObject
    // equality by all component values, immutable, no Id

abstract class AuditableEntity<TId> : Entity<TId>
    DateTimeOffset CreatedAt
    Guid CreatedByUserId
    DateTimeOffset? UpdatedAt
    Guid? UpdatedByUserId

abstract class TenantOwnedAggregateRoot<TId> : AggregateRoot<TId>
    Guid OrganizationId   // the column every EF Core global query filter (Step 1 §7) filters on

readonly record struct Result           // non-generic: success/failure, no payload
readonly record struct Result<T>        // success: T Value; failure: Error Error
readonly record struct Error(string Code, string Message, ErrorType Type)
enum ErrorType { Validation, NotFound, Conflict, Forbidden, Unexpected }
```

`OrganizationId` lives on `TenantOwnedAggregateRoot` rather than on every individual entity — child entities (e.g., a `SchemaNode` inside a `SchemaVersion`) inherit tenant scoping implicitly through their aggregate root and are never queried independently of it, so they don't carry their own `OrganizationId` column. `User` is the one aggregate root that does **not** derive from `TenantOwnedAggregateRoot` (global identity, per Step 3).

---

## 2. Value objects

| Value Object | Shape | Notes |
|---|---|---|
| `Slug` | `string Value` | Lowercase, URL-safe, validated on construction (throws on invalid input — this is a genuine "can't exist in an invalid state" case, not an expected business failure, so it's the one place a VO constructor throwing is appropriate even under the Step 1 §6 Result convention) |
| `EmailAddress` | `string Value` | Same rationale as `Slug` — validated at construction |
| `SemVer` | `int Major, int Minor, int Patch` | Comparable/sortable; `ToString()` → `"1.2.0"` |
| `JsonPath` | `string Value` | e.g. `"$.customer.address.postalCode"` — used to annotate every `ValidationError` |
| `ComponentReference` | `Guid ComponentVersionId, VersionConstraint Constraint` | `VersionConstraint` is itself a small VO: `ConstraintKind Kind` (`ExactVersion`/`MinimumVersion`/`Latest`) `+ SemVer? Version` |
| `ValidationError` | `JsonPath Path, string Code, string Message, ErrorSeverity Severity` | `Severity`: `Error`/`Warning` — schema authors may want non-fatal advisories (e.g. "no `example` provided") distinct from hard validation failures |
| `StringConstraints` | `int? MinLength, int? MaxLength, string? Pattern, SchemaFormat? Format` | See §4 for `SchemaFormat` |
| `NumericConstraints` | `decimal? Minimum, decimal? Maximum, bool ExclusiveMinimum, bool ExclusiveMaximum, decimal? MultipleOf` | Shared by `Number` and `Integer` kinds |
| `ArrayConstraints` | `int? MinItems, int? MaxItems, bool UniqueItems` | |
| `ObjectConstraints` | `int? MinProperties, int? MaxProperties, bool AdditionalPropertiesAllowed` | |

---

## 3. Identity, access and workspace entities

```
class User : AggregateRoot<Guid>            // not tenant-owned — see §1
    EmailAddress Email
    string PasswordHash
    string DisplayName
    bool EmailVerified

class Organization : TenantOwnedAggregateRoot<Guid>   // OrganizationId == its own Id here
    string Name
    Slug Slug
    PlanTier PlanTier
    OrganizationStatus Status               // Active, Suspended

class OrganizationMembership : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    Guid UserId
    OrganizationRole Role                   // Owner, Admin, Member
    MembershipStatus Status                 // Invited, Active, Revoked

class Team : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    string Name
    string? Description
    IReadOnlyList<TeamMembership> Members   // child entity, bounded (Ground Rule 2)

class TeamMembership : Entity<Guid>         // child of Team, not independently queried
    Guid UserId
    DateTimeOffset JoinedAt

class Project : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    string Name
    string? Description
    ProjectStatus Status                    // Active, Archived

class SourceDocument : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    Guid ProjectId
    string FileName
    string StorageKey                       // opaque key into IFileStorage (Step 1 §9)
    string ContentType
    long SizeBytes
```

---

## 4. Schema core: `SchemaDefinition`, `SchemaVersion`, and the `SchemaNode` hierarchy

This is the section that has to carry the full weight of the brief's Draft 2020-12 feature list: objects, arrays, enums, `$ref`-equivalents, nullable, formats, patterns, examples, descriptions, required fields, `if`/`then`/`else`, `oneOf`/`anyOf`/`allOf`, `dependentRequired`, `const`, recursive schemas, and reusable definitions.

```
class SchemaDefinition : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    Guid ProjectId
    string Name
    string? Description
    IReadOnlyList<string> Tags

class SchemaVersion : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    Guid SchemaDefinitionId
    SemVer VersionNumber
    SchemaLifecycleStatus Status             // Draft, Published, Deprecated
    string? ChangeSummary
    DateTimeOffset? PublishedAt
    SchemaNode RootNode                      // the tree — always exactly one root
    IReadOnlyList<LocalDefinition> LocalDefinitions   // this version's own "$defs" — see below
```

### 4.1 `SchemaNode` — one base type, not a type per JSON Schema keyword

A node has a **primitive `Kind`** (or none — see composition-only nodes below) plus a set of **optional constraint bundles**, rather than a separate subclass per JSON Schema keyword. This mirrors the spec's own shape: `type`, `enum`, `const`, `oneOf`, `if`/`then`/`else` etc. are all keywords that can co-occur on the *same* schema object — modeling them as mutually exclusive subclasses would misrepresent the spec (a node can simultaneously be `type: object` *and* carry a `oneOf`, e.g. shared base properties plus variant-specific fields via composition).

```
class SchemaNode : Entity<Guid>              // child of SchemaVersion (or ComponentVersion)
    Guid? ParentNodeId                       // null only for the tree root
    string? PropertyName                     // the key this node is bound to under its parent object; null for array items / root
    int Order                                // preserves declaration order (dictionaries don't guarantee it)
    NodeKind? Kind                           // Object, Array, String, Number, Integer, Boolean, Null — nullable: see composition-only nodes
    string? Description
    string? Notes                            // internal authoring notes — NEVER exported to JSON Schema; feeds the Documentation Generator's "Notes" field
    bool IsNullable                          // authoring convenience — see §4.4 for export translation
    bool IsRequiredByParent                  // authoring convenience — see §4.5 for export translation
    IReadOnlyList<JsonLiteral> Examples
    JsonLiteral? DefaultValue
    IReadOnlyList<JsonLiteral>? AllowedValues     // `enum` — applies regardless of Kind
    JsonLiteral? ConstValue                       // `const` — applies regardless of Kind

    // type-specific constraint bundles — populated only when Kind matches
    ObjectConstraints? ObjectConstraints
    ArrayConstraints? ArrayConstraints
    StringConstraints? StringConstraints
    NumericConstraints? NumericConstraints        // Number or Integer

    // structural children
    IReadOnlyList<SchemaNode> Properties           // Kind == Object
    IReadOnlyList<SchemaNode> PrefixItems           // Kind == Array, tuple-style (Draft 2020-12 `prefixItems`)
    SchemaNode? ItemsNode                          // Kind == Array, homogeneous list item schema
    IReadOnlyDictionary<string, IReadOnlyList<string>>? DependentRequired   // Kind == Object

    // composition — attachable to ANY node, including one with no Kind at all
    CompositionKind? Composition                  // OneOf, AnyOf, AllOf, Not
    IReadOnlyList<SchemaNode> CompositionBranches

    // conditional — spec allows this at any schema level, not just Object
    SchemaNode? IfNode
    SchemaNode? ThenNode
    SchemaNode? ElseNode

    // reuse — two distinct mechanisms, see §4.3
    ComponentReference? ComponentReference          // cross-schema reuse (org-shared)
    Guid? LocalDefinitionRef                        // within-this-version reuse (recursion)
```

A **composition-only node** (`Kind == null`, `Composition` set) represents patterns like `Payment` validated purely as `oneOf [CreditCardPayment, BankTransferPayment]` with no properties of its own — common for polymorphic document sections (e.g. a claim form's "Payer" section that's structurally different for an individual vs. an organization).

### 4.2 `SchemaFormat`

```
enum SchemaFormat
    Date, DateTime, Time, Email, Hostname, Ipv4, Ipv6, Uri, UriReference, Uuid, Custom

// when Custom, a sibling field on StringConstraints carries the raw format string:
record StringConstraints(..., string? CustomFormatValue)
```

**Reasoning**: JSON Schema's `format` keyword has a well-known set of values (the ones above cover the overwhelming majority of real document-field use — dates, emails, IDs) plus an open-ended extension mechanism. A closed enum for the common case gives the Designer UI a clean dropdown and lets the Validation Engine dispatch to purpose-built validators (proper date/email/UUID checks) instead of a generic string match; `Custom` with a free-text fallback keeps the model spec-complete without forcing every possible format string into the enum.

### 4.3 Two distinct reuse mechanisms — don't conflate them

- **`ComponentReference`** → points at an Organization-scoped `ComponentVersion` (Step 2 §3, Step 3). Cross-*schema* reuse: "Invoice" and "PurchaseOrder" both use the shared `PostalAddress` component.
- **`LocalDefinitionRef`** → points at a `LocalDefinition` (name + root `SchemaNode`) living inside the *same* `SchemaVersion`. Within-*schema* reuse, primarily to express **recursion**: a `Category` schema whose `subcategories` array contains items shaped exactly like `Category` itself can't reference itself as a `ComponentReference` (it isn't published yet — it's mid-edit, and promoting every recursive schema to a full Organization-shared component would be needless ceremony). A `LocalDefinition` is JSON Schema's `$defs` + local `$ref` in domain terms — scoped to one version, not independently versioned or shared.

```
class LocalDefinition : Entity<Guid>          // child of SchemaVersion
    string Name
    SchemaNode RootNode
```

### 4.4 Nullable — an authoring convenience translated at export time

JSON Schema 2020-12 has no native `nullable` keyword — a nullable string is expressed as `"type": ["string", "null"]` (or `enum` including `null`). Forcing schema authors to think in those terms in the Designer is bad UX for a document-extraction domain where "this field might legitimately be absent" is an extremely common, simple concept. `IsNullable` is a first-class flag on `SchemaNode`; the JSON Schema *exporter* (Step 9) is responsible for translating `IsNullable == true` into the correct `type` array form on output. The *importer* does the reverse translation on the way in. This keeps the authoring model simple while staying fully spec-compliant on the wire.

### 4.5 Required — modeled on the child, exported on the parent

Same category of translation: JSON Schema puts `required` as an array of property names on the **parent** object schema, not a flag on the child. `IsRequiredByParent` lives on the child node because that's how a Designer UI naturally works (a toggle next to the field itself, not a separately-maintained list on the parent that can drift out of sync with the property list). The exporter aggregates children's `IsRequiredByParent == true` into the parent `ObjectNode`'s `required` array on the way out; the importer does the reverse. This is the same translation pattern as §4.4 for the same underlying reason — **the authoring model optimizes for how a human (or a future AI) edits one field at a time; the wire format optimizes for JSON Schema spec compliance; the exporter/importer pair is the seam that reconciles them.**

---

## 5. Components — identical shape to Schema core

```
class ComponentDefinition : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    string Name
    string? Description

class ComponentVersion : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    Guid ComponentDefinitionId
    SemVer VersionNumber
    SchemaLifecycleStatus Status
    string? ChangeSummary
    DateTimeOffset? PublishedAt
    SchemaNode RootNode
    IReadOnlyList<LocalDefinition> LocalDefinitions
```

No new concepts — `ComponentVersion` reuses the entire `SchemaNode`/`LocalDefinition` machinery from §4. A `ComponentVersion`'s `RootNode` can itself hold `ComponentReference`s to *other* components (e.g. an `InvoiceLineItem` component referencing a `MoneyAmount` component), which is exactly how real-world reusable schema fragments compose.

---

## 6. Testing

```
class TestSuite : TenantOwnedAggregateRoot<Guid>, AuditableEntity
    Guid SchemaDefinitionId
    string Name
    string? Description
    IReadOnlyList<TestCase> Cases            // child entity, bounded

class TestCase : Entity<Guid>
    string Name
    string InputJson
    TestExpectation Expectation              // ExpectValid, or ExpectErrors(IReadOnlyList<ExpectedError>)

record ExpectedError(JsonPath Path, string ErrorCodePattern)   // pattern, not exact message — messages may be refined without breaking tests

class TestRun : TenantOwnedAggregateRoot<Guid>
    Guid TestSuiteId
    Guid SchemaVersionId
    DateTimeOffset ExecutedAt
    Guid ExecutedByUserId
    IReadOnlyList<TestCaseResult> Results    // child entity, bounded by suite size at run time

class TestCaseResult : Entity<Guid>
    Guid TestCaseId
    bool Passed
    IReadOnlyList<ValidationError> ActualErrors
```

`ExpectedError` matches on an **error code pattern**, not the literal message string — a deliberate choice so refining a validation error's wording later doesn't silently break every test suite that happened to assert on exact text, which is a common source of brittle test suites in systems that didn't think about this up front.

---

## 7. Validation and Audit

```
class ValidationRun : TenantOwnedAggregateRoot<Guid>
    Guid ProjectId
    Guid SchemaVersionId
    string InputPayloadHash                  // SHA-256 of the input; see note below
    ValidationOutcome Outcome                 // Valid, Invalid
    IReadOnlyList<ValidationError> Errors
    DateTimeOffset ExecutedAt
    Guid ExecutedByUserId

class AuditLogEntry : TenantOwnedAggregateRoot<Guid>
    Guid ActorUserId
    string Action                            // e.g. "SchemaVersion.Published"
    string EntityType
    Guid EntityId
    string? MetadataJson                     // small, structured, action-specific context
    DateTimeOffset OccurredAt
```

**`ValidationRun` stores a hash of the input payload, not necessarily the raw payload itself.** Given the document domain (invoices, medical forms, tax forms, passports), the JSON payloads users paste in to validate may well contain real PII/PHI. Persisting every validated payload verbatim, forever, in an audit-adjacent table would create a significant, easily-overlooked data-retention and compliance liability for a platform explicitly targeting insurance claims and medical forms. The hash preserves the useful signal (dedup, "has this exact payload been validated before," coverage analytics) without retaining sensitive content by default. **This is a decision I'd like your sign-off on** — see below.

---

## 8. What Step 4 deliberately defers

- Physical storage: whether `SchemaNode` trees are normalized tables (`schema_nodes` self-referencing via `parent_node_id`) or a single JSONB column per version, indexes, constraints → **Step 5**
- API request/response shapes (these entities are never serialized directly to clients — Contracts DTOs are separate) → **Step 6**

---

## Decision confirmed during review: `ValidationRun` payload retention

**Hash-only, no raw payload retention** — confirmed. `ValidationRun` stores a SHA-256 hash of the validated JSON payload, never the payload itself, given the PII/PHI exposure risk inherent to this domain (medical forms, passports, bank statements are explicitly in-scope example document types). This preserves dedup/coverage signal without indefinite raw retention. **Accepted trade-off**: the Step 2 §6 "future AI training data" motivation is weakened — a hash can't be replayed or inspected later, only equality-compared — but this is the safer starting posture, and it's easy to loosen later (add encrypted, retention-windowed storage) if a real training-data need emerges, versus impossible to un-retain PII already logged for months under a looser default.

---

**Next step once this is approved**: Step 5 — Database schema design (physical tables, the JSONB-vs-normalized decision for `SchemaNode`, indexes, constraints, and the partial-unique-index enforcement mechanisms referenced in Step 3 §4).
