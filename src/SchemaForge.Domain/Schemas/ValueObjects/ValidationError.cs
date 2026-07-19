using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Schemas.ValueObjects;

public sealed record ValidationError(JsonPath Path, string Code, string Message, ErrorSeverity Severity);
