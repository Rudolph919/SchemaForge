using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.UploadSourceDocument;

// Content is a live Stream, not a byte[] or serializable payload - fine here because MediatR
// dispatch in this codebase is strictly in-process (no wire serialization), so this rides the
// same pipeline (validation/transaction behaviors) as every other command without buffering the
// whole file in memory first. The Api layer owns the stream's lifetime (opened from the incoming
// multipart request) and disposes it after Handle returns.
public sealed record UploadSourceDocumentCommand(
    Guid ProjectId, string FileName, string ContentType, long SizeBytes, Stream Content)
    : ICommand<Result<UploadSourceDocumentResult>>;

public sealed record UploadSourceDocumentResult(Guid DocumentId);
