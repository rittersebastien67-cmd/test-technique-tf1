using NetArchTest.Rules;
using static Architecture.Tests.Solution;

namespace Architecture.Tests;

/// <summary>DDD layering inside each bounded context (AD2, AD5): Domain ← Application ← Infrastructure ← Api.</summary>
public sealed class LayeringTests
{
    private static readonly string[] PersistenceAndWeb = ["Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore"];

    public static TheoryData<string> DomainProjects => ProjectsInLayer(Domain);

    public static TheoryData<string> ApplicationProjects => ProjectsInLayer(Application);

    public static TheoryData<string> InfrastructureProjects => ProjectsInLayer(Infrastructure);

    [Fact]
    public void Service_projects_are_named_after_their_context_and_layer()
    {
        ArchAssert.None(
            ServiceProjects.Where(project => !Layers.Contains(project.Layer)),
            $"Projects under src/Services/{{Context}}/ must be named {{Context}}.{{{string.Join('|', Layers)}}}.");
    }

    [Fact]
    public void Every_context_has_domain_application_and_infrastructure_layers()
    {
        var missing = ServiceProjects
            .GroupBy(project => project.Context!)
            .SelectMany(context => new[] { Domain, Application, Infrastructure }
                .Where(layer => context.All(project => project.Layer != layer))
                .Select(layer => $"{context.Key}.{layer}"));

        Assert.Empty(missing);
    }

    [Theory]
    [MemberData(nameof(DomainProjects))]
    public void Domain_project_references_only_the_shared_kernel(string domain)
    {
        var project = Projects[domain];

        Assert.Equal("Microsoft.NET.Sdk", project.Sdk);
        Assert.All(project.ProjectReferences, reference => Assert.Equal("SharedKernel", reference));
        Assert.Empty(project.PackageReferences);
        Assert.Empty(project.FrameworkReferences);
    }

    [Theory]
    [MemberData(nameof(DomainProjects))]
    public void Domain_types_depend_on_neither_outer_layers_nor_infrastructure_libraries(string domain)
    {
        var context = Projects[domain].Context;

        var result = Types.InAssembly(LoadAssembly(domain))
            .ShouldNot()
            .HaveDependencyOnAny([
                .. PersistenceAndWeb,
                "Microsoft.Extensions.AI",
                $"{context}.{Application}",
                $"{context}.{Infrastructure}",
                $"{context}.{Api}",
            ])
            .GetResult();

        ArchAssert.Passes(result, $"{domain} must only depend on the SharedKernel.");
    }

    [Theory]
    [MemberData(nameof(ApplicationProjects))]
    public void Application_project_does_not_reference_infrastructure_nor_api(string application)
    {
        ArchAssert.None(
            TransitiveReferences(application).Where(project => project.Layer is Infrastructure or Api || project.IsWeb),
            $"{application} must not reach Infrastructure, Api or ASP.NET Core.");
    }

    [Theory]
    [MemberData(nameof(ApplicationProjects))]
    public void Application_types_depend_on_ports_not_on_infrastructure(string application)
    {
        var context = Projects[application].Context;

        var result = Types.InAssembly(LoadAssembly(application))
            .ShouldNot()
            .HaveDependencyOnAny([.. PersistenceAndWeb, $"{context}.{Infrastructure}", $"{context}.{Api}"])
            .GetResult();

        ArchAssert.Passes(result, $"{application} must declare ports and leave EF Core and ASP.NET Core to outer layers.");
    }

    [Theory]
    [MemberData(nameof(InfrastructureProjects))]
    public void Infrastructure_does_not_reference_api(string infrastructure)
    {
        var context = Projects[infrastructure].Context;

        ArchAssert.None(
            TransitiveReferences(infrastructure).Where(project => project.Layer == Api),
            $"{infrastructure} must not reference an Api project.");

        var result = Types.InAssembly(LoadAssembly(infrastructure))
            .ShouldNot()
            .HaveDependencyOnAny($"{context}.{Api}")
            .GetResult();

        ArchAssert.Passes(result, $"{infrastructure} must not depend on {context}.{Api}.");
    }
}
