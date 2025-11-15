using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.FileSystem;

/// <summary>
/// List files and directories within workspace.
/// </summary>
public class ListFilesTool : ToolBase
{
    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "ListFiles";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "List files and directories in a directory";

    /// <summary>
    /// Create a new ListFilesTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public ListFilesTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the tool to list files
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Newline-separated list of files</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var directory = context.GetOptionalParameter<string>("directory", ".") ?? ".";
        var recursive = context.GetOptionalParameter<bool>("recursive", false);
        var pattern = context.GetOptionalParameter<string>("pattern", "*") ?? "*";

        // 1. Validate directory
        var fullPath = PathValidator.ValidateAndResolve(directory, context.WorkspacePath);

        // 2. Check directory exists
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        // 3. List files
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(fullPath, pattern, searchOption);

        // 4. Get relative paths
        var relativeFiles = files
            .Select(f => Path.GetRelativePath(context.WorkspacePath, f))
            .OrderBy(f => f)
            .Take(ResourceLimits.MaxGlobResults)
            .ToList();

        if (files.Length > ResourceLimits.MaxGlobResults)
            relativeFiles.Add($"... (truncated, {files.Length} total files)");

        _logger.LogDebug(
            "Listed {Count} files in {Directory} for tenant {TenantId}",
            relativeFiles.Count, directory, context.TenantId);

        return string.Join(Environment.NewLine, relativeFiles);
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        // All parameters are optional
        return Task.CompletedTask;
    }
}
