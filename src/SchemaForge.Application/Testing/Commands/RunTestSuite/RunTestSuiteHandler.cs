using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Testing;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.RunTestSuite;

// The TestRun row is created Pending, synchronously, in this handler - not by the background job
// - so /run has a real id to hand back in its 202's Location header the moment it returns (Step
// 6 §2.7/§4). The job itself (ITestRunExecutor, dispatched below) only has to fill it in later.
public sealed class RunTestSuiteHandler(
    ITestSuiteRepository testSuiteRepository,
    ISchemaVersionRepository schemaVersionRepository,
    ITestRunRepository testRunRepository,
    IJobDispatcher jobDispatcher,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<RunTestSuiteCommand, Result<RunTestSuiteResult>>
{
    public async Task<Result<RunTestSuiteResult>> Handle(RunTestSuiteCommand request, CancellationToken cancellationToken)
    {
        var suite = await testSuiteRepository.GetByIdAsync(request.TestSuiteId, cancellationToken);
        if (suite is null)
        {
            return Result<RunTestSuiteResult>.Failure(Error.NotFound("TestSuite.NotFound", "No such test suite."));
        }

        var version = await schemaVersionRepository.GetByIdAsync(request.TargetSchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result<RunTestSuiteResult>.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        // Cross-aggregate check (Step 3 §4): a suite can only be run against a version of the
        // SchemaDefinition it belongs to - running "Invoice"'s suite against "PurchaseOrder"'s
        // version would just produce meaningless results, not a useful cross-schema comparison.
        if (version.SchemaDefinitionId != suite.SchemaDefinitionId)
        {
            return Result<RunTestSuiteResult>.Failure(Error.Validation(
                "TestRun.SchemaMismatch", "The target version does not belong to this suite's schema."));
        }

        var run = TestRun.CreatePending(
            tenantContext.CurrentTenantId!.Value, suite.Id, version.Id, currentUserContext.UserId!.Value);
        await testRunRepository.AddAsync(run, cancellationToken);

        // Unlike every other handler, this one calls SaveChangesAsync itself instead of leaving
        // it to TransactionBehavior (which still runs afterward - a harmless no-op second call,
        // since nothing new is tracked by then). Hangfire enqueues jobs against its own storage
        // immediately, independent of this request's DB transaction; a worker could dequeue and
        // start ExecuteAsync before TransactionBehavior's later SaveChangesAsync ever runs,
        // finding no TestRun row yet. Committing first, then enqueuing, removes that race.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        jobDispatcher.Enqueue<ITestRunExecutor>(x => x.ExecuteAsync(run.OrganizationId, run.Id, CancellationToken.None));

        return new RunTestSuiteResult(run.Id);
    }
}
