using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Testing;

public enum TestExpectationKind
{
    Valid,
    Errors
}

// Matches on the error's Code at a given Path, not the literal Message - refining a validation
// error's wording later shouldn't silently break every test suite that happened to assert on
// exact text (Step 4 §6's rationale for this shape). "Pattern" in the name reflects the
// architecture doc's field name; matching itself is an exact Path+Code comparison, not wildcard
// matching - nothing in this domain needs looser matching than that yet.
public sealed record ExpectedError(JsonPath Path, string ErrorCodePattern);

public sealed record TestExpectation(TestExpectationKind Kind, IReadOnlyList<ExpectedError>? ExpectedErrors)
{
    public static TestExpectation Valid() => new(TestExpectationKind.Valid, null);

    public static TestExpectation Errors(IReadOnlyList<ExpectedError> errors) => new(TestExpectationKind.Errors, errors);
}
