using NetArchTest.Rules;
using static Architecture.Tests.Solution;

namespace Architecture.Tests;

/// <summary>
/// HrAdministration is split into modules, i.e. namespaces of its Domain and Application (AD28, section 1).
/// HrCalendar knows neither leave types nor employees.
/// </summary>
public sealed class HrAdministrationModuleTests
{
    private static readonly string[] Modules = ["Employees", "HrCalendar", "Balances", "Leaves"];

    public static TheoryData<string> ModularLayers => new($"HrAdministration.{Domain}", $"HrAdministration.{Application}");

    [Theory]
    [MemberData(nameof(ModularLayers))]
    public void Hr_calendar_does_not_depend_on_other_modules(string layer)
    {
        var result = Types.InAssembly(LoadAssembly(layer))
            .That()
            .ResideInNamespaceMatching(ModuleNamespacePattern(layer, "HrCalendar"))
            .ShouldNot()
            .HaveDependencyOnAny([
                .. Modules
                    .Where(module => module != "HrCalendar")
                    .SelectMany(module => new[] { $"HrAdministration.{Domain}.{module}", $"HrAdministration.{Application}.{module}" }),
            ])
            .GetResult();

        ArchAssert.Passes(result, $"{layer}.HrCalendar must not know Employees, Balances nor Leaves.");
    }

    public static TheoryData<string> DomainModules => new(Modules);

    /// <summary>Modules only communicate through the Application layer (section 1), so no Domain module knows another one.</summary>
    [Theory]
    [MemberData(nameof(DomainModules))]
    public void Domain_modules_do_not_depend_on_each_other(string module)
    {
        var domain = $"HrAdministration.{Domain}";

        var result = Types.InAssembly(LoadAssembly(domain))
            .That()
            .ResideInNamespaceMatching(ModuleNamespacePattern(domain, module))
            .ShouldNot()
            .HaveDependencyOnAny([.. Modules.Where(other => other != module).Select(other => $"{domain}.{other}")])
            .GetResult();

        ArchAssert.Passes(result, $"{domain}.{module} must not know the other Domain modules.");
    }

    [Fact]
    public void Domain_types_live_in_a_module()
    {
        var domain = $"HrAdministration.{Domain}";

        var result = Types.InAssembly(LoadAssembly(domain))
            .Should()
            .ResideInNamespaceMatching(ModuleNamespacePattern(domain, $"({string.Join('|', Modules)})"))
            .GetResult();

        ArchAssert.Passes(result, $"{domain} types must reside in {domain}.{{{string.Join('|', Modules)}}}.");
    }

    private static string ModuleNamespacePattern(string layer, string module) =>
        $@"^{layer.Replace(".", @"\.")}\.{module}(\.|$)";
}
