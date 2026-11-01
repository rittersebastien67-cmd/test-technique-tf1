using NetArchTest.Rules;

namespace Architecture.Tests;

internal static class ArchAssert
{
    /// <summary>Fails with the offending types and NetArchTest's explanation of each dependency found.</summary>
    public static void Passes(TestResult result, string rule)
    {
        var failures = (result.FailingTypes ?? []).Select(type =>
            string.IsNullOrEmpty(type.Explanation) ? $"  - {type.FullName}" : $"  - {type.FullName}: {type.Explanation}");

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, [rule, .. failures]));
    }

    /// <summary>Fails with the projects that should not be there.</summary>
    public static void None(IEnumerable<ProjectFile> offending, string rule)
    {
        var names = offending.Select(project => project.Name).Order().ToList();

        Assert.True(names.Count == 0, $"{rule} Offending: {string.Join(", ", names)}.");
    }
}
