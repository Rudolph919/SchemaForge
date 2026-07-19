using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace SchemaForge.ArchitectureTests;

// Enforces Step 1 §2's dependency table mechanically. Domain/Application/Infrastructure have no
// types of their own yet (that starts with the identity vertical slice) - these assertions hold
// vacuously today and start doing real work the moment the first class lands in each project.
public class LayerDependencyTests
{
    private const string SharedKernel = "SchemaForge.SharedKernel";
    private const string Domain = "SchemaForge.Domain";
    private const string Application = "SchemaForge.Application";
    private const string Infrastructure = "SchemaForge.Infrastructure";
    private const string Contracts = "SchemaForge.Contracts";
    private const string Api = "SchemaForge.Api";

    private static Assembly Load(string assemblyName) => Assembly.Load(assemblyName);

    [Fact]
    public void SharedKernel_should_not_depend_on_any_other_project()
    {
        var result = Types.InAssembly(Load(SharedKernel))
            .Should()
            .NotHaveDependencyOnAny(Domain, Application, Infrastructure, Contracts, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Domain_should_only_depend_on_SharedKernel()
    {
        var result = Types.InAssembly(Load(Domain))
            .Should()
            .NotHaveDependencyOnAny(Application, Infrastructure, Contracts, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Domain_should_not_depend_on_EntityFrameworkCore_or_AspNetCore()
    {
        var result = Types.InAssembly(Load(Domain))
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure_Contracts_or_Api()
    {
        var result = Types.InAssembly(Load(Application))
            .Should()
            .NotHaveDependencyOnAny(Infrastructure, Contracts, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Application_should_not_depend_on_EntityFrameworkCore_or_AspNetCore()
    {
        var result = Types.InAssembly(Load(Application))
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Contracts_should_not_depend_on_Domain_or_Application()
    {
        var result = Types.InAssembly(Load(Contracts))
            .Should()
            .NotHaveDependencyOnAny(Domain, Application)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Api_should_not_depend_directly_on_EntityFrameworkCore()
    {
        // Api composes Infrastructure via DI registration extension methods, but must never
        // touch EF Core (or any Infrastructure concern) directly from a Controller.
        var result = Types.InAssembly(Load(Api))
            .That()
            .DoNotHaveNameEndingWith("ServiceCollectionExtensions")
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
