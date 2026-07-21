namespace SchemaForge.SharedKernel;

// Backed by Postgres's built-in xmin system column (Step 6 §1.5) - every mutable resource that
// needs optimistic concurrency implements this and maps RowVersion to "xmin" in its EF
// configuration via IsRowVersion(). A real property, not an EF shadow property, specifically so
// Application-layer query handlers can read it directly (entity.RowVersion) without needing an
// EF Core reference - only setting the client's claimed *expected* value back onto the tracked
// entity's OriginalValue (to make EF's own concurrency check compare against it) still needs
// EF's EntityEntry API, which stays in Infrastructure behind each repository's
// ApplyExpectedVersion method.
public interface IHasRowVersion
{
    uint RowVersion { get; }
}
