using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Schemas.ValueObjects;

public sealed record VersionConstraint
{
    public VersionConstraintKind Kind { get; }

    public SemVer? Version { get; }

    private VersionConstraint(VersionConstraintKind kind, SemVer? version)
    {
        Kind = kind;
        Version = version;
    }

    public static VersionConstraint ExactVersion(SemVer version) => new(VersionConstraintKind.ExactVersion, version);

    public static VersionConstraint MinimumVersion(SemVer version) =>
        new(VersionConstraintKind.MinimumVersion, version);

    public static VersionConstraint Latest => new(VersionConstraintKind.Latest, null);
}
