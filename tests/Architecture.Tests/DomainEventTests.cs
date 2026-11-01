using NetArchTest.Rules;
using SharedKernel;
using static Architecture.Tests.Solution;

namespace Architecture.Tests;

/// <summary>Domain events are plain facts: the event store maps them, they know nothing about serialization (section 9).</summary>
public sealed class DomainEventTests
{
    private static readonly string[] Serializers =
    [
        "System.Text.Json",
        "Newtonsoft.Json",
        "System.Runtime.Serialization",
        "System.Xml.Serialization",
    ];

    public static TheoryData<string> DomainProjects => ProjectsInLayer(Domain);

    [Theory]
    [MemberData(nameof(DomainProjects))]
    public void Domain_events_do_not_depend_on_serialization(string domain)
    {
        var result = Types.InAssembly(LoadAssembly(domain))
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .ShouldNot()
            .HaveDependencyOnAny(Serializers)
            .GetResult();

        ArchAssert.Passes(result, $"Domain events of {domain} must stay serialization-agnostic.");
    }
}
