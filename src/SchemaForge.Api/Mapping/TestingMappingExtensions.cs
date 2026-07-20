using System.Text.Json;
using SchemaForge.Application.Testing;
using SchemaForge.Application.Testing.Commands.AddTestCase;
using SchemaForge.Application.Testing.Commands.CreateTestSuite;
using SchemaForge.Application.Testing.Commands.UpdateTestCase;
using SchemaForge.Application.Testing.Commands.UpdateTestSuiteDetails;
using SchemaForge.Application.Testing.Queries.GetTestSuite;
using SchemaForge.Contracts.V1.Testing;
using SchemaForge.SharedKernel.Primitives;
using DomainExpectation = SchemaForge.Domain.Testing.TestExpectation;
using DomainExpectationKind = SchemaForge.Domain.Testing.TestExpectationKind;
using DomainExpectedError = SchemaForge.Domain.Testing.ExpectedError;

namespace SchemaForge.Api.Mapping;

public static class TestingMappingExtensions
{
    public static CreateTestSuiteCommand ToCommand(this CreateTestSuiteRequest request, Guid schemaDefinitionId) =>
        new(schemaDefinitionId, request.Name, request.Description);

    public static CreateTestSuiteResponse ToResponse(this CreateTestSuiteResult result) => new(result.TestSuiteId);

    public static UpdateTestSuiteDetailsCommand ToCommand(this UpdateTestSuiteDetailsRequest request, Guid testSuiteId) =>
        new(testSuiteId, request.Name, request.Description);

    public static TestSuiteSummaryResponse ToResponse(this TestSuiteSummary summary) =>
        new(summary.Id, summary.Name, summary.Description, summary.CaseCount);

    public static TestSuiteDetailResponse ToResponse(this TestSuiteDetail detail) => new(
        detail.Id, detail.SchemaDefinitionId, detail.Name, detail.Description,
        detail.Cases.Select(ToResponse).ToList());

    private static TestCaseResponse ToResponse(this TestCaseDetail testCase) => new(
        testCase.Id, testCase.Name, JsonDocument.Parse(testCase.InputJson).RootElement, testCase.Expectation.ToDto());

    public static AddTestCaseCommand ToCommand(this AddTestCaseRequest request, Guid testSuiteId) =>
        new(testSuiteId, request.Name, request.InputJson, request.Expectation.ToDomain());

    public static AddTestCaseResponse ToResponse(this AddTestCaseResult result) => new(result.TestCaseId);

    public static UpdateTestCaseCommand ToCommand(this UpdateTestCaseRequest request, Guid testSuiteId, Guid testCaseId) =>
        new(testSuiteId, testCaseId, request.Name, request.InputJson, request.Expectation.ToDomain());

    private static DomainExpectation ToDomain(this TestExpectationDto dto) => new(
        dto.Kind switch
        {
            TestExpectationKind.Valid => DomainExpectationKind.Valid,
            TestExpectationKind.Errors => DomainExpectationKind.Errors,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto.Kind, "Unknown test expectation kind."),
        },
        dto.ExpectedErrors?.Select(e => new DomainExpectedError(JsonPath.Create(e.Path), e.ErrorCodePattern)).ToList());

    private static TestExpectationDto ToDto(this DomainExpectation expectation) => new(
        expectation.Kind switch
        {
            DomainExpectationKind.Valid => TestExpectationKind.Valid,
            DomainExpectationKind.Errors => TestExpectationKind.Errors,
            _ => throw new ArgumentOutOfRangeException(nameof(expectation), expectation.Kind, "Unknown test expectation kind."),
        },
        expectation.ExpectedErrors?.Select(e => new ExpectedErrorDto(e.Path.Value, e.ErrorCodePattern)).ToList());
}
