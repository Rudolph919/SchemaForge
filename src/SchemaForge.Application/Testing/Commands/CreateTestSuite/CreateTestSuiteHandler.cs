using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Testing;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.CreateTestSuite;

public sealed class CreateTestSuiteHandler(
    ISchemaDefinitionRepository schemaDefinitionRepository, ITestSuiteRepository testSuiteRepository, ITenantContext tenantContext)
    : IRequestHandler<CreateTestSuiteCommand, Result<CreateTestSuiteResult>>
{
    public async Task<Result<CreateTestSuiteResult>> Handle(
        CreateTestSuiteCommand request, CancellationToken cancellationToken)
    {
        var schemaDefinition = await schemaDefinitionRepository.GetByIdAsync(request.SchemaDefinitionId, cancellationToken);
        if (schemaDefinition is null)
        {
            return Result<CreateTestSuiteResult>.Failure(Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        if (await testSuiteRepository.ExistsByNameAsync(request.SchemaDefinitionId, request.Name, cancellationToken))
        {
            return Result<CreateTestSuiteResult>.Failure(Error.Conflict(
                "TestSuite.NameAlreadyExists", "A test suite with this name already exists for this schema."));
        }

        var suite = TestSuite.Create(tenantContext.CurrentTenantId!.Value, request.SchemaDefinitionId, request.Name, request.Description);
        await testSuiteRepository.AddAsync(suite, cancellationToken);

        return new CreateTestSuiteResult(suite.Id);
    }
}
