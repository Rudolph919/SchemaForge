using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Application.Audit.Queries.GetAuditLog;

namespace SchemaForge.Api.Controllers.V1;

// No {organizationId} route segment - matches the established convention (Step 6 §2.4's
// "components" decision): every org-scoped listing derives its org from the JWT tenant context,
// not a path param, and RLS/the EF query filter already fully scope this regardless.
[ApiController]
[Authorize]
[Route("api/v1/audit-log")]
public sealed class AuditLogController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? actorUserId,
        [FromQuery] DateTimeOffset? occurredFrom,
        [FromQuery] DateTimeOffset? occurredTo,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var query = new GetAuditLogQuery(
            entityType, entityId, actorUserId, occurredFrom, occurredTo,
            page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);

        var result = await sender.Send(query, cancellationToken);
        return result.ToActionResult(p => p.ToResponse());
    }
}
