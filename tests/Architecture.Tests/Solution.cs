using System.Reflection;
using System.Xml.Linq;

namespace Architecture.Tests;

/// <summary>
/// The project graph as declared by the .csproj files under <c>src/</c>.
/// Rules are checked on it as well as on types: the compiler drops references that no code uses,
/// so a layer that is still empty would pass any type-level rule.
/// </summary>
internal static class Solution
{
    public const string Domain = "Domain";
    public const string Application = "Application";
    public const string Infrastructure = "Infrastructure";
    public const string Api = "Api";
    public const string Contracts = "Contracts";

    public static readonly string[] Layers = [Domain, Application, Infrastructure, Api, Contracts];

    /// <summary>Contexts that a host loads in-process besides its own (AD57: Payroll runs inside HrAdministration).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> HostedContexts = new Dictionary<string, string[]>
    {
        ["HrAdministration.Api"] = ["Payroll"],
    };

    private static readonly Lazy<string> LazyRoot = new(FindRoot);

    private static readonly Lazy<IReadOnlyDictionary<string, ProjectFile>> LazyProjects = new(Load);

    public static string Root => LazyRoot.Value;

    public static IReadOnlyDictionary<string, ProjectFile> Projects => LazyProjects.Value;

    /// <summary>Projects under <c>src/Services/{Context}/</c>, i.e. the layers of a bounded context.</summary>
    public static IEnumerable<ProjectFile> ServiceProjects => Projects.Values.Where(project => project.Context is not null);

    public static TheoryData<string> ProjectsInLayer(string layer) =>
        new(ServiceProjects.Where(project => project.Layer == layer).Select(project => project.Name).Order());

    public static TheoryData<string> ProjectsInContext(string context) =>
        new(ServiceProjects.Where(project => project.Context == context).Select(project => project.Name).Order());

    public static IReadOnlySet<ProjectFile> TransitiveReferences(string projectName)
    {
        var visited = new HashSet<ProjectFile>();
        var pending = new Stack<string>(Projects[projectName].ProjectReferences);

        while (pending.TryPop(out var current))
        {
            if (visited.Add(Projects[current]))
            {
                foreach (var next in Projects[current].ProjectReferences)
                {
                    pending.Push(next);
                }
            }
        }

        return visited;
    }

    /// <summary>Assembly names match project names, and every src project is copied next to the tests.</summary>
    public static Assembly LoadAssembly(string projectName) => Assembly.Load(new AssemblyName(projectName));

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LeaveManagement.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("LeaveManagement.slnx not found above the test output directory.");
    }

    private static IReadOnlyDictionary<string, ProjectFile> Load()
    {
        var servicesDirectory = Path.Combine(Root, "src", "Services");

        return Directory
            .EnumerateFiles(Path.Combine(Root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => ProjectFile.Parse(path, servicesDirectory))
            .ToDictionary(project => project.Name);
    }
}

internal sealed record ProjectFile(
    string Name,
    string? Sdk,
    string? Context,
    string? Layer,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences,
    IReadOnlyList<string> FrameworkReferences)
{
    /// <summary>Whether the project can host HTTP endpoints, through the Web SDK or the ASP.NET Core framework.</summary>
    public bool IsWeb => Sdk == "Microsoft.NET.Sdk.Web" || FrameworkReferences.Contains("Microsoft.AspNetCore.App");

    public static ProjectFile Parse(string path, string servicesDirectory)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var document = XDocument.Load(path);
        var directory = Path.GetDirectoryName(path)!;

        string? context = null;
        string? layer = null;
        var relativeToServices = Path.GetRelativePath(servicesDirectory, path);
        if (!relativeToServices.StartsWith("..", StringComparison.Ordinal))
        {
            context = relativeToServices.Split(Path.DirectorySeparatorChar)[0];
            layer = name.StartsWith(context + ".", StringComparison.Ordinal) ? name[(context.Length + 1)..] : null;
        }

        return new ProjectFile(
            name,
            document.Root?.Attribute("Sdk")?.Value,
            context,
            layer,
            Includes("ProjectReference")
                .Select(include => Path.GetFileNameWithoutExtension(
                    Path.GetFullPath(Path.Combine(directory, include.Replace('\\', Path.DirectorySeparatorChar)))))
                .ToList(),
            Includes("PackageReference").ToList(),
            Includes("FrameworkReference").ToList());

        IEnumerable<string> Includes(string item) =>
            document.Descendants(item).Select(element => element.Attribute("Include")?.Value).OfType<string>();
    }

    public override string ToString() => Name;
}
