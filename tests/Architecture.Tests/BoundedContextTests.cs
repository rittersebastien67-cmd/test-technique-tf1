using NetArchTest.Rules;
using static Architecture.Tests.Solution;

namespace Architecture.Tests;

/// <summary>
/// Contexts only see each other through their published language, <c>{Context}.Contracts</c>:
/// Assistant and Payroll know HrAdministration.Contracts only, and Payroll exposes no API (AD8, AD34, AD57).
/// </summary>
public sealed class BoundedContextTests
{
    public static TheoryData<string> AllServiceProjects => new(ServiceProjects.Select(project => project.Name).Order());

    public static TheoryData<string> PayrollProjects => ProjectsInContext("Payroll");

    [Theory]
    [MemberData(nameof(AllServiceProjects))]
    public void Project_reaches_other_contexts_only_through_their_contracts(string projectName)
    {
        ArchAssert.None(
            TransitiveReferences(projectName).Where(reference => !MayReference(Projects[projectName], reference)),
            $"{projectName} may only reference another context through its Contracts project.");
    }

    [Theory]
    [MemberData(nameof(AllServiceProjects))]
    public void Types_depend_on_other_contexts_only_through_their_contracts(string projectName)
    {
        var project = Projects[projectName];
        var forbidden = ServiceProjects
            .Where(other => !MayReference(project, other))
            .Select(other => other.Name)
            .ToArray();

        var result = Types.InAssembly(LoadAssembly(projectName))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        ArchAssert.Passes(result, $"{projectName} may only depend on another context through its Contracts namespace.");
    }

    [Fact]
    public void Payroll_has_no_api_project()
    {
        Assert.DoesNotContain(ServiceProjects, project => project is { Context: "Payroll", Layer: Api });
    }

    [Theory]
    [MemberData(nameof(PayrollProjects))]
    public void Payroll_cannot_expose_routes(string payroll)
    {
        ArchAssert.None(
            TransitiveReferences(payroll).Prepend(Projects[payroll]).Where(project => project.IsWeb),
            $"{payroll} must not reach the Web SDK nor ASP.NET Core.");

        var result = Types.InAssembly(LoadAssembly(payroll))
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.AspNetCore")
            .GetResult();

        ArchAssert.Passes(result, $"{payroll} must not declare controllers nor endpoints.");
    }

    [Fact]
    public void Gateway_bff_references_no_bounded_context()
    {
        ArchAssert.None(
            TransitiveReferences("Gateway.Bff").Where(project => project.Context is not null),
            "Gateway.Bff routes requests and holds no business rule (section 4).");
    }

    private static bool MayReference(ProjectFile project, ProjectFile reference) =>
        reference.Context is null
        || reference.Context == project.Context
        || reference.Layer == Contracts
        || (HostedContexts.TryGetValue(project.Name, out var hosted) && hosted.Contains(reference.Context));
}
