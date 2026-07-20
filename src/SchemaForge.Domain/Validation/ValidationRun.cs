using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Validation;

// A record of "this payload was validated against this SchemaVersion, here's the result" -
// persisted (not ephemeral) so the Audit Log gets real history and so there's a distinct
// coverage signal from TestRun: TestRun answers "does the schema pass its own author-written
// tests," ValidationRun answers "how is it actually performing against real-world payloads"
// (Step 2 §6).
//
// InputPayloadHash, never the raw payload: this domain's example document types (invoices,
// medical forms, passports) mean a pasted validation payload may well contain real PII/PHI.
// Persisting every validated payload verbatim, forever, in an audit-adjacent table would be a
// significant, easily-overlooked data-retention liability. The hash preserves dedup/coverage
// signal without retaining sensitive content (Step 4 §7, confirmed decision).
public sealed class ValidationRun : TenantOwnedAggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }

    public Guid SchemaVersionId { get; private set; }

    public string InputPayloadHash { get; private set; } = null!;

    public ValidationOutcome Outcome { get; private set; }

    private List<ValidationError> _errors = [];
    public IReadOnlyList<ValidationError> Errors => _errors;

    public DateTimeOffset ExecutedAt { get; private set; }

    public Guid ExecutedByUserId { get; private set; }

    private ValidationRun() { } // EF Core materialization

    private ValidationRun(
        Guid id, Guid organizationId, Guid projectId, Guid schemaVersionId, string inputPayloadHash,
        ValidationOutcome outcome, IReadOnlyList<ValidationError> errors, Guid executedByUserId)
        : base(id, organizationId)
    {
        ProjectId = projectId;
        SchemaVersionId = schemaVersionId;
        InputPayloadHash = inputPayloadHash;
        Outcome = outcome;
        _errors = [.. errors];
        ExecutedAt = DateTimeOffset.UtcNow;
        ExecutedByUserId = executedByUserId;
    }

    // Outcome is derived, not caller-supplied: a payload with only Warning-severity errors is
    // still Valid overall - warnings are advisories, not failures (Step 2 §1's ValidationError
    // Severity distinction).
    public static ValidationRun Record(
        Guid organizationId, Guid projectId, Guid schemaVersionId, string inputPayloadHash,
        IReadOnlyList<ValidationError> errors, Guid executedByUserId)
    {
        var outcome = errors.Any(e => e.Severity == ErrorSeverity.Error)
            ? ValidationOutcome.Invalid
            : ValidationOutcome.Valid;

        return new ValidationRun(
            Guid.NewGuid(), organizationId, projectId, schemaVersionId, inputPayloadHash, outcome, errors, executedByUserId);
    }
}
