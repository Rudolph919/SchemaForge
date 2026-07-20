using FluentAssertions;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Domain.Validation;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Domain.Validation;

public class ValidationRunTests
{
    [Fact]
    public void Record_with_no_errors_is_valid()
    {
        var run = ValidationRun.Record(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hash", [], Guid.NewGuid());

        run.Outcome.Should().Be(ValidationOutcome.Valid);
        run.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Record_with_an_error_severity_entry_is_invalid()
    {
        var errors = new[] { new ValidationError(JsonPath.Root, "code", "message", ErrorSeverity.Error) };

        var run = ValidationRun.Record(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hash", errors, Guid.NewGuid());

        run.Outcome.Should().Be(ValidationOutcome.Invalid);
    }

    [Fact]
    public void Record_with_only_warning_severity_entries_is_still_valid()
    {
        var errors = new[] { new ValidationError(JsonPath.Root, "code", "message", ErrorSeverity.Warning) };

        var run = ValidationRun.Record(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hash", errors, Guid.NewGuid());

        run.Outcome.Should().Be(ValidationOutcome.Valid);
    }

    [Fact]
    public void Record_stamps_ExecutedAt_and_ExecutedByUserId()
    {
        var userId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        var run = ValidationRun.Record(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hash", [], userId);

        run.ExecutedByUserId.Should().Be(userId);
        run.ExecutedAt.Should().BeOnOrAfter(before);
    }
}
