namespace SchemaForge.Application.Schemas;

// Selects which of SemVer's NextMajor/NextMinor/NextPatch a CreateSchemaVersion caller wants -
// an Application-layer orchestration concept (which bump the user asked for), not a Domain one
// (SemVer already knows how to compute each kind of "next" version itself).
public enum VersionBumpKind
{
    Major,
    Minor,
    Patch
}
