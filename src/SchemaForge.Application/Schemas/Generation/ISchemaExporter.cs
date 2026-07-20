using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

// Registered as IEnumerable<ISchemaExporter> and dispatched by matching FormatKey against the
// ?format= query parameter (Step 9 §3) - a fifth format later is one new class implementing this
// interface, registered in one line, with zero changes to the controller or any existing
// exporter. Task-returning to match the interface the architecture doc specifies, even though
// today's implementations are pure in-memory transformations with no actual I/O.
public interface ISchemaExporter
{
    string FormatKey { get; }

    Task<string> ExportAsync(SchemaVersion version, CancellationToken cancellationToken);
}
