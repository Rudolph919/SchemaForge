using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand<Result>;
