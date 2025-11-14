using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Search;

/// <summary>
/// Find files matching a glob pattern.
/// </summary>
public class GlobTool : ToolBase
{
    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "Glob";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Find files matching a glob pattern (e.g., '**/*.cs')";

    /// <summary>
    /// Create a new GlobTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public GlobTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    /// <summary>
    /// Execute the glob pattern matching
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>List of matching file paths (one per line)</returns>
    protected override Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var pattern = context.GetParameter<string>("pattern");
        var directory = context.GetOptionalParameter<string>("directory", ".") ?? ".";

        // 1. Validate directory
        var fullPath = PathValidator.ValidateAndResolve(directory, context.WorkspacePath);

        // 2. Check directory exists
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        // 3. Use Matcher for glob patterns
        var matcher = new Matcher();
        matcher.AddInclude(pattern);

        // 4. Execute glob
        var matchedFiles = matcher.GetResultsInFullPath(fullPath).ToList();

        var files = matchedFiles
            .Take(ResourceLimits.MaxGlobResults)
            .Select(f => Path.GetRelativePath(context.WorkspacePath, f))
            .OrderBy(f => f)
            .ToList();

        if (matchedFiles.Count > ResourceLimits.MaxGlobResults)
            files.Add($"... (truncated, {matchedFiles.Count} total files)");

        _logger.LogInformation(
            "Glob found {Count} files matching pattern '{Pattern}'",
            files.Count, pattern);

        return Task.FromResult(string.Join(Environment.NewLine, files));
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are missing</exception>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("pattern"))
            throw new ArgumentException("Parameter 'pattern' is required");

        return Task.CompletedTask;
    }
}
