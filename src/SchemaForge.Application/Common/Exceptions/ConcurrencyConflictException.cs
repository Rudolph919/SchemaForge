namespace SchemaForge.Application.Common.Exceptions;

// A dependency-free stand-in for EF Core's DbUpdateConcurrencyException (Application must not
// reference EF Core - Step 1 §2's layer rule). UnitOfWork (Infrastructure) catches the real EF
// exception and throws this instead; TransactionBehavior (Application) catches this and maps it
// to a Result.Failure(Error.Conflict(...)) the same way every other domain-level failure surfaces.
public sealed class ConcurrencyConflictException : Exception;
