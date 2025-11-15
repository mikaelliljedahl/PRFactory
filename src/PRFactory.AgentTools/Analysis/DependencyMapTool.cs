using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Analysis;

/// <summary>
/// Analyze project dependencies from .csproj files.
/// </summary>
public class DependencyMapTool : ToolBase
{
    private const int MaxDependencies = 500;

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "DependencyMap";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Analyze project dependencies from .csproj files";

    /// <summary>
    /// Create a new DependencyMapTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public DependencyMapTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the dependency analysis
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Dependency map as formatted text</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryPath = context.GetParameter<string>("repositoryPath");
        var projectFile = context.GetOptionalParameter<string>("projectFile", "*.csproj") ?? "*.csproj";

        // 1. Validate repository path
        var fullPath = PathValidator.ValidateAndResolve(repositoryPath, context.WorkspacePath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Repository path '{repositoryPath}' does not exist");
        }

        // 2. Find project files
        var projectFiles = Directory.GetFiles(fullPath, projectFile, SearchOption.AllDirectories);
        if (projectFiles.Length == 0)
        {
            return $"No project files matching '{projectFile}' found in {repositoryPath}";
        }

        // 3. Parse dependencies
        var allDependencies = new Dictionary<string, ProjectDependencies>();
        var totalDependencyCount = 0;

        foreach (var file in projectFiles)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.Length > ResourceLimits.MaxFileSize)
                continue;

            var relativePath = Path.GetRelativePath(context.WorkspacePath, file);
            var dependencies = await ParseProjectFileAsync(file);

            totalDependencyCount += dependencies.PackageReferences.Count;
            if (totalDependencyCount > MaxDependencies)
            {
                throw new InvalidOperationException(
                    $"Total dependency count {totalDependencyCount} exceeds limit {MaxDependencies}");
            }

            allDependencies[relativePath] = dependencies;
        }

        _logger.LogInformation(
            "Analyzed {ProjectCount} project file(s) with {DependencyCount} total dependencies",
            projectFiles.Length, totalDependencyCount);

        return FormatDependencyMap(allDependencies);
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("repositoryPath"))
            throw new ArgumentException("Parameter 'repositoryPath' is required");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Parse project file for dependencies
    /// </summary>
    private static async Task<ProjectDependencies> ParseProjectFileAsync(string projectPath)
    {
        var dependencies = new ProjectDependencies();

        try
        {
            var content = await File.ReadAllTextAsync(projectPath);
            var doc = XDocument.Parse(content);

            // Parse PackageReference elements
            var packageRefs = doc.Descendants("PackageReference");
            foreach (var pkg in packageRefs)
            {
                var name = pkg.Attribute("Include")?.Value;
                var version = pkg.Attribute("Version")?.Value;

                if (!string.IsNullOrEmpty(name))
                {
                    dependencies.PackageReferences.Add(new PackageReference
                    {
                        Name = name,
                        Version = version ?? "Unknown"
                    });
                }
            }

            // Parse ProjectReference elements
            var projectRefs = doc.Descendants("ProjectReference");
            foreach (var proj in projectRefs)
            {
                var include = proj.Attribute("Include")?.Value;
                if (!string.IsNullOrEmpty(include))
                {
                    dependencies.ProjectReferences.Add(include);
                }
            }

            // Parse TargetFramework
            var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(targetFramework))
            {
                dependencies.TargetFramework = targetFramework;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse project file: {ex.Message}", ex);
        }

        return dependencies;
    }

    /// <summary>
    /// Format dependency map as readable text
    /// </summary>
    private static string FormatDependencyMap(Dictionary<string, ProjectDependencies> dependencies)
    {
        var output = new StringBuilder();
        output.AppendLine("Project Dependency Map");
        output.AppendLine("======================");
        output.AppendLine();

        foreach (var (projectPath, deps) in dependencies.OrderBy(x => x.Key))
        {
            output.AppendLine($"Project: {projectPath}");
            output.AppendLine($"Target Framework: {deps.TargetFramework}");
            output.AppendLine();

            if (deps.PackageReferences.Count > 0)
            {
                output.AppendLine("  NuGet Packages:");
                foreach (var pkg in deps.PackageReferences.OrderBy(p => p.Name))
                {
                    output.AppendLine($"    - {pkg.Name} ({pkg.Version})");
                }
                output.AppendLine();
            }

            if (deps.ProjectReferences.Count > 0)
            {
                output.AppendLine("  Project References:");
                foreach (var proj in deps.ProjectReferences.OrderBy(p => p))
                {
                    output.AppendLine($"    - {proj}");
                }
                output.AppendLine();
            }

            output.AppendLine();
        }

        // Summary
        var totalPackages = dependencies.Values.SelectMany(d => d.PackageReferences).DistinctBy(p => p.Name).Count();
        var totalProjects = dependencies.Count;

        output.AppendLine("Summary");
        output.AppendLine("-------");
        output.AppendLine($"Total Projects: {totalProjects}");
        output.AppendLine($"Unique NuGet Packages: {totalPackages}");

        return output.ToString();
    }

    private class ProjectDependencies
    {
        public List<PackageReference> PackageReferences { get; set; } = new();
        public List<string> ProjectReferences { get; set; } = new();
        public string TargetFramework { get; set; } = "Unknown";
    }

    private class PackageReference
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}
