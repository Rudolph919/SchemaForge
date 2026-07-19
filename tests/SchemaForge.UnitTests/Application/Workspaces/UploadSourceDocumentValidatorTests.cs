using FluentAssertions;
using SchemaForge.Application.Workspaces.Commands.UploadSourceDocument;

namespace SchemaForge.UnitTests.Application.Workspaces;

public class UploadSourceDocumentValidatorTests
{
    private readonly UploadSourceDocumentValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new UploadSourceDocumentCommand(
            Guid.NewGuid(), "schema.json", "application/json", 1024, Stream.Null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_project_id_fails()
    {
        var result = _validator.Validate(new UploadSourceDocumentCommand(
            Guid.Empty, "schema.json", "application/json", 1024, Stream.Null));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_file_name_fails(string fileName)
    {
        var result = _validator.Validate(new UploadSourceDocumentCommand(
            Guid.NewGuid(), fileName, "application/json", 1024, Stream.Null));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_size_fails(long sizeBytes)
    {
        var result = _validator.Validate(new UploadSourceDocumentCommand(
            Guid.NewGuid(), "schema.json", "application/json", sizeBytes, Stream.Null));

        result.IsValid.Should().BeFalse();
    }
}
