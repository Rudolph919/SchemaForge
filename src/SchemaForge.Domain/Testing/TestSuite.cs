using SchemaForge.Domain.Testing.Events;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing;

// Belongs to SchemaDefinition, not to a single SchemaVersion (Step 3 §5) - the same suite is
// meant to be re-run against version 1.0.0 today and 1.1.0 tomorrow (regression testing across
// versions), which each TestRun records via its own SchemaVersionId rather than the suite
// pinning to one.
public sealed class TestSuite : TenantOwnedAggregateRoot<Guid>, IHasRowVersion
{
    public Guid SchemaDefinitionId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public uint RowVersion { get; private set; }

    private readonly List<TestCase> _cases = [];
    public IReadOnlyList<TestCase> Cases => _cases.AsReadOnly();

    private TestSuite() { } // EF Core materialization

    private TestSuite(Guid id, Guid organizationId, Guid schemaDefinitionId, string name, string? description)
        : base(id, organizationId)
    {
        SchemaDefinitionId = schemaDefinitionId;
        Name = name;
        Description = description;
    }

    public static TestSuite Create(
        Guid organizationId, Guid schemaDefinitionId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Test suite name is required.", nameof(name));
        }

        var suite = new TestSuite(Guid.NewGuid(), organizationId, schemaDefinitionId, name, description);
        suite.RaiseDomainEvent(new TestSuiteCreated(organizationId, schemaDefinitionId, suite.Id, name));

        return suite;
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("TestSuite.NameRequired", "Test suite name is required."));
        }

        Name = newName;
        return Result.Success();
    }

    public void UpdateDescription(string? description) => Description = description;

    public Result<Guid> AddCase(string name, string inputJson, TestExpectation expectation)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Guid>.Failure(Error.Validation("TestCase.NameRequired", "Test case name is required."));
        }

        if (_cases.Any(c => c.Name == name))
        {
            return Result<Guid>.Failure(Error.Conflict(
                "TestCase.NameAlreadyExists", "A test case with this name already exists in this suite."));
        }

        var testCase = TestCase.Create(name, inputJson, expectation);
        _cases.Add(testCase);
        RaiseDomainEvent(new TestCaseAdded(Id, testCase.Id, name));

        return testCase.Id;
    }

    public Result UpdateCase(Guid caseId, string name, string inputJson, TestExpectation expectation)
    {
        var testCase = _cases.FirstOrDefault(c => c.Id == caseId);
        if (testCase is null)
        {
            return Result.Failure(Error.NotFound("TestCase.NotFound", "No such test case."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("TestCase.NameRequired", "Test case name is required."));
        }

        if (_cases.Any(c => c.Id != caseId && c.Name == name))
        {
            return Result.Failure(Error.Conflict(
                "TestCase.NameAlreadyExists", "A test case with this name already exists in this suite."));
        }

        testCase.Update(name, inputJson, expectation);
        RaiseDomainEvent(new TestCaseUpdated(Id, caseId));

        return Result.Success();
    }

    public Result RemoveCase(Guid caseId)
    {
        var testCase = _cases.FirstOrDefault(c => c.Id == caseId);
        if (testCase is null)
        {
            return Result.Failure(Error.NotFound("TestCase.NotFound", "No such test case."));
        }

        _cases.Remove(testCase);
        RaiseDomainEvent(new TestCaseRemoved(Id, caseId));

        return Result.Success();
    }
}
