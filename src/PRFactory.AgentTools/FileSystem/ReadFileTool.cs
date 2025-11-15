using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.FileSystem;

/// <summary>
/// Read the contents of a file within the workspace.
/// </summary>
public class ReadFileTool : ToolBase
{
    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "ReadFile";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Read the contents of a file from the workspace";

    /// <summary>
    /// Create a new ReadFileTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public ReadFileTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the tool to read a file
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>File contents</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var filePath = context.GetParameter<string>("filePath");

        // 1. Validate path
        var fullPath = PathValidator.ValidateAndResolve(filePath, context.WorkspacePath);

        // 2. Check file exists
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // 3. Check file size
        var fileInfo = new FileInfo(fullPath);
        ResourceLimits.ValidateFileSize(fileInfo.Length);

        // 4. Read file
        var content = await File.ReadAllTextAsync(fullPath);

        _logger.LogDebug(
            "Read file {FilePath} ({Size} bytes) for tenant {TenantId}",
            filePath, fileInfo.Length, context.TenantId);

        return content;
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        // filePath is required
        if (!context.Parameters.ContainsKey("filePath"))
            throw new ArgumentException("Parameter 'filePath' is required");

        return Task.CompletedTask;
    }
}
