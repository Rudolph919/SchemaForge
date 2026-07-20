using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing;

// Child entity of TestSuite, not an independent aggregate - suites stay bounded (a suite with
// thousands of hand-authored cases isn't a realistic shape, Step 3 §3), so a normalized child
// table via a private backing field on TestSuite is the right fit here, same reasoning as
// Team/TeamMembership.
public sealed class TestCase : Entity<Guid>
{
    public string Name { get; private set; } = null!;

    public string InputJson { get; private set; } = null!;

    public TestExpectation Expectation { get; private set; } = null!;

    private TestCase() { } // EF Core materialization

    private TestCase(Guid id, string name, string inputJson, TestExpectation expectation) : base(id)
    {
        Name = name;
        InputJson = inputJson;
        Expectation = expectation;
    }

    internal static TestCase Create(string name, string inputJson, TestExpectation expectation) =>
        new(Guid.NewGuid(), name, inputJson, expectation);

    internal void Update(string name, string inputJson, TestExpectation expectation)
    {
        Name = name;
        InputJson = inputJson;
        Expectation = expectation;
    }
}
