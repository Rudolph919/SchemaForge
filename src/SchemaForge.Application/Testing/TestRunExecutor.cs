using System.Text.Json;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas;
using SchemaForge.Application.Schemas.Validation;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Domain.Testing;

namespace SchemaForge.Application.Testing;

public sealed class TestRunExecutor(
    ITestRunRepository testRunRepository,
    ITestSuiteRepository testSuiteRepository,
    ISchemaVersionRepository schemaVersionRepository,
    ISchemaValidator schemaValidator,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : ITestRunExecutor
{
    public async Task ExecuteAsync(Guid organizationId, Guid testRunId, CancellationToken cancellationToken)
    {
        // Must happen before any repository call - see the interface's own comment. There's no
        // HttpContext in a background job for HttpTenantContext to resolve from otherwise.
        tenantContext.SetTenant(organizationId);

        var run = await testRunRepository.GetByIdAsync(testRunId, cancellationToken);
        // Defensive, not expected in practice: RunTestSuiteHandler commits the TestRun row before
        // this job is ever enqueued (see its own comment), so a missing row here would mean the
        // row was deleted out from under an in-flight run, not a normal race to guard against.
        if (run is null || run.Status == TestRunStatus.Completed)
        {
            return;
        }

        var suite = await testSuiteRepository.GetByIdAsync(run.TestSuiteId, cancellationToken);
        var version = await schemaVersionRepository.GetByIdAsync(run.SchemaVersionId, cancellationToken);
        if (suite is null || version is null)
        {
            return;
        }

        var results = suite.Cases
            .Select(testCase => Evaluate(testCase, version))
            .ToList();

        run.Complete(results);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private TestCaseResult Evaluate(TestCase testCase, SchemaVersion version)
    {
        var payload = JsonDocument.Parse(testCase.InputJson).RootElement;
        var errors = schemaValidator.Validate(version.RootNode, version.LocalDefinitions, payload);
        var passed = Matches(testCase.Expectation, errors);

        return new TestCaseResult(testCase.Id, testCase.Name, passed, errors);
    }

    // Warnings are advisories, not failures, same reasoning as ValidationRun.Record's outcome
    // derivation - only Error-severity entries count against ExpectValid or against the expected
    // set for ExpectErrors.
    private static bool Matches(TestExpectation expectation, IReadOnlyList<ValidationError> actualErrors)
    {
        var actualFailures = actualErrors.Where(e => e.Severity == ErrorSeverity.Error).ToList();

        if (expectation.Kind == TestExpectationKind.Valid)
        {
            return actualFailures.Count == 0;
        }

        var expected = expectation.ExpectedErrors!
            .Select(e => (Path: e.Path.Value, Code: e.ErrorCodePattern))
            .ToHashSet();
        var actual = actualFailures
            .Select(e => (Path: e.Path.Value, Code: e.Code))
            .ToHashSet();

        return expected.SetEquals(actual);
    }
}
