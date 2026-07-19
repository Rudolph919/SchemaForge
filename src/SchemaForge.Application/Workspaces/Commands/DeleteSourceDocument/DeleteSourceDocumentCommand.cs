using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.DeleteSourceDocument;

public sealed record DeleteSourceDocumentCommand(Guid DocumentId) : ICommand<Result>;
