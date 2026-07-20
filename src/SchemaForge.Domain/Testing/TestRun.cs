using SchemaForge.Domain.Testing.Events;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing;

// Immutable once recorded (Step 3 §3) - a TestRun is a historical execution record, not
// something a user edits. It's created Pending synchronously (so /run has an id to return in its
// 202's Location header) and completed exactly once by the background job that actually executes
// the suite; there's no path back from Completed to Pending.
public sealed class TestRun : TenantOwnedAggregateRoot<Guid>
{
    public Guid TestSuiteId { get; private set; }

    public Guid SchemaVersionId { get; private set; }

    public TestRunStatus Status { get; private set; }

    public DateTimeOffset ExecutedAt { get; private set; }

    public Guid ExecutedByUserId { get; private set; }

    private List<TestCaseResult> _results = [];
    public IReadOnlyList<TestCaseResult> Results => _results;

    private TestRun() { } // EF Core materialization

    private TestRun(Guid id, Guid organizationId, Guid testSuiteId, Guid schemaVersionId, Guid executedByUserId)
        : base(id, organizationId)
    {
        TestSuiteId = testSuiteId;
        SchemaVersionId = schemaVersionId;
        Status = TestRunStatus.Pending;
        ExecutedAt = DateTimeOffset.UtcNow;
        ExecutedByUserId = executedByUserId;
    }

    public static TestRun CreatePending(
        Guid organizationId, Guid testSuiteId, Guid schemaVersionId, Guid executedByUserId)
    {
        var run = new TestRun(Guid.NewGuid(), organizationId, testSuiteId, schemaVersionId, executedByUserId);
        run.RaiseDomainEvent(new TestRunStarted(organizationId, testSuiteId, schemaVersionId, run.Id));

        return run;
    }

    public Result Complete(IReadOnlyList<TestCaseResult> results)
    {
        if (Status == TestRunStatus.Completed)
        {
            return Result.Failure(Error.Conflict("TestRun.AlreadyCompleted", "This test run has already been completed."));
        }

        _results = [.. results];
        Status = TestRunStatus.Completed;
        RaiseDomainEvent(new TestRunCompleted(Id, TestSuiteId, results.Count, results.Count(r => r.Passed)));

        return Result.Success();
    }
}
