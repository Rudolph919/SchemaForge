namespace SchemaForge.Application.Testing;

// Resolved from the DI container and invoked directly by Hangfire's activator (via the
// Expression<Func<TJob, Task>> IJobDispatcher.Enqueue captures) - not a MediatR command, so it
// doesn't run through TransactionBehavior and must call IUnitOfWork itself once it's done.
//
// organizationId is passed explicitly, not re-derived inside the job: ITenantContext
// (HttpTenantContext) resolves from the current HttpContext's JWT claims, and a background job
// has no HttpContext at all - CurrentTenantId would silently be null, which makes both the EF
// global query filter and Postgres RLS treat every tenant-scoped row as invisible. The caller
// (RunTestSuiteHandler) already knows the organization from its own authenticated request, so
// capturing it as a job argument sidesteps the problem entirely instead of needing an RLS bypass.
public interface ITestRunExecutor
{
    Task ExecuteAsync(Guid organizationId, Guid testRunId, CancellationToken cancellationToken);
}
