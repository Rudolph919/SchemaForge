namespace SchemaForge.Contracts.V1.Testing;

public enum TestExpectationKind
{
    Valid,
    Errors
}

public sealed record ExpectedErrorDto(string Path, string ErrorCodePattern);

public sealed record TestExpectationDto(TestExpectationKind Kind, IReadOnlyList<ExpectedErrorDto>? ExpectedErrors);
