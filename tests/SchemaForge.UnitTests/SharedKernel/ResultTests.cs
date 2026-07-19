using FluentAssertions;
using SchemaForge.SharedKernel;

namespace SchemaForge.UnitTests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Success_result_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_result_carries_the_given_error()
    {
        var error = Error.NotFound("Schema.NotFound", "Schema not found.");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Generic_success_result_exposes_its_value()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Accessing_value_of_a_failed_generic_result_throws()
    {
        var result = Result<int>.Failure(Error.Validation("Field.Required", "Field is required."));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Implicit_conversion_from_value_produces_a_success_result()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }
}
