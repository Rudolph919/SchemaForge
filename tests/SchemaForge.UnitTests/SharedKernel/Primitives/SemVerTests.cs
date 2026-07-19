using FluentAssertions;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.SharedKernel.Primitives;

public class SemVerTests
{
    [Fact]
    public void Initial_version_is_one_zero_zero()
    {
        SemVer.Initial.ToString().Should().Be("1.0.0");
    }

    [Fact]
    public void NextMajor_resets_minor_and_patch()
    {
        var version = SemVer.Create(1, 4, 7).NextMajor();

        version.ToString().Should().Be("2.0.0");
    }

    [Fact]
    public void NextMinor_resets_patch_only()
    {
        var version = SemVer.Create(1, 4, 7).NextMinor();

        version.ToString().Should().Be("1.5.0");
    }

    [Fact]
    public void NextPatch_increments_patch_only()
    {
        var version = SemVer.Create(1, 4, 7).NextPatch();

        version.ToString().Should().Be("1.4.8");
    }

    [Fact]
    public void Versions_compare_correctly()
    {
        var earlier = SemVer.Create(1, 2, 0);
        var later = SemVer.Create(1, 10, 0);

        (earlier < later).Should().BeTrue();
        (later > earlier).Should().BeTrue();
    }

    [Fact]
    public void Negative_components_are_rejected()
    {
        var act = () => SemVer.Create(-1, 0, 0);

        act.Should().Throw<ArgumentException>();
    }
}
