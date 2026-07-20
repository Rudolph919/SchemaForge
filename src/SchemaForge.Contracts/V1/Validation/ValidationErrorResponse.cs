namespace SchemaForge.Contracts.V1.Validation;

public sealed record ValidationErrorResponse(string Path, string Code, string Message, ErrorSeverity Severity);
