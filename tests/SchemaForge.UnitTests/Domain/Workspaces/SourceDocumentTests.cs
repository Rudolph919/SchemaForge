using FluentAssertions;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.UnitTests.Domain.Workspaces;

public class SourceDocumentTests
{
    [Fact]
    public void Create_raises_a_SourceDocumentUploaded_domain_event()
    {
        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var document = SourceDocument.Create(
            organizationId, projectId, "invoice.pdf", "docs/abc123", "application/pdf", 1024);

        document.OrganizationId.Should().Be(organizationId);
        document.ProjectId.Should().Be(projectId);
        document.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SourceDocumentUploaded>();
    }

    [Theory]
    [InlineData("", "docs/abc123", 1024)]
    [InlineData("invoice.pdf", "", 1024)]
    [InlineData("invoice.pdf", "docs/abc123", 0)]
    [InlineData("invoice.pdf", "docs/abc123", -1)]
    public void Create_rejects_invalid_input(string fileName, string storageKey, long sizeBytes)
    {
        var act = () => SourceDocument.Create(
            Guid.NewGuid(), Guid.NewGuid(), fileName, storageKey, "application/pdf", sizeBytes);

        act.Should().Throw<ArgumentException>();
    }
}
