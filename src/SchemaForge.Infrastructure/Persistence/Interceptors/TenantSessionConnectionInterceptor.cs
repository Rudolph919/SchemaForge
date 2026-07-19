using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Persistence.Interceptors;

// Sets the RLS session variable on every logical connection open - covers reads AND writes
// uniformly, unlike setting it only inside SaveChangesAsync (which only ever protected writes;
// nothing built so far happened to need a tenant-scoped read, so that gap went unnoticed until
// the integration tests exercised one). Session-scoped (false), not transaction-scoped: it needs
// to persist for the connection's whole logical lifetime, which can span multiple commands
// within one request, not just one transaction. Safe under pooling specifically because
// ConnectionOpened(Async) fires - and is awaited - before any command runs, on every open, so a
// stale value from a previous pooled-connection borrower is always overwritten first.
public sealed class TenantSessionConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await SetTenantSessionVariableAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantSessionVariableAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    private async Task SetTenantSessionVariableAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant_id', @tenantId, false)";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenantId";
        parameter.Value = tenantContext.CurrentTenantId?.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
