namespace SchemaForge.SharedKernel.Primitives;

public sealed record SemVer : IComparable<SemVer>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    private SemVer(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public static SemVer Create(int major, int minor, int patch)
    {
        if (major < 0 || minor < 0 || patch < 0)
            throw new ArgumentException("Version components cannot be negative.");

        return new SemVer(major, minor, patch);
    }

    public static SemVer Initial => new(1, 0, 0);

    public SemVer NextMajor() => new(Major + 1, 0, 0);
    public SemVer NextMinor() => new(Major, Minor + 1, 0);
    public SemVer NextPatch() => new(Major, Minor, Patch + 1);

    public int CompareTo(SemVer? other)
    {
        if (other is null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        return minorComparison != 0 ? minorComparison : Patch.CompareTo(other.Patch);
    }

    public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
    public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
    public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
