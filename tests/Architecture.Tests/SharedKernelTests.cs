using NetArchTest.Rules;
using SharedKernel;
using static Architecture.Tests.Solution;

namespace Architecture.Tests;

/// <summary>The Shared Kernel stays as small as possible and carries no business concept (AD10).</summary>
public sealed class SharedKernelTests
{
    private static readonly Type[] AllowedPublicTypes =
    [
        typeof(AggregateRoot<>),
        typeof(Entity<>),
        typeof(IDomainEvent),
        typeof(Result),
        typeof(Result<>),
        typeof(Error),
    ];

    [Fact]
    public void Shared_kernel_exposes_only_the_building_blocks()
    {
        var exported = typeof(IDomainEvent).Assembly.GetExportedTypes();

        Assert.Equal(
            AllowedPublicTypes.Select(type => type.FullName).Order(),
            exported.Select(type => type.FullName).Order());
    }

    [Fact]
    public void Shared_kernel_project_has_no_dependency()
    {
        var project = Projects["SharedKernel"];

        Assert.Empty(project.ProjectReferences);
        Assert.Empty(project.PackageReferences);
        Assert.Empty(project.FrameworkReferences);
    }

    [Fact]
    public void Shared_kernel_types_only_use_the_base_library()
    {
        var result = Types.InAssembly(typeof(IDomainEvent).Assembly)
            .Should()
            .OnlyHaveDependencyOn("System", "SharedKernel")
            .GetResult();

        ArchAssert.Passes(result, "SharedKernel must only depend on the base class library.");
    }
}
