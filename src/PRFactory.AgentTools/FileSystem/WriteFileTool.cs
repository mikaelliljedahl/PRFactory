using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.FileSystem;

/// <summary>
/// Write content to a file with atomic operations.
/// </summary>
public class WriteFileTool : ToolBase
{
    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "WriteFile";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Write content to a file (atomic operation)";

    /// <summary>
    /// Create a new WriteFileTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public WriteFileTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the tool to write a file
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Success message</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var filePath = context.GetParameter<string>("filePath");
        var content = context.GetParameter<string>("content");

        // 1. Validate path
        var fullPath = PathValidator.ValidateAndResolve(filePath, context.WorkspacePath);

        // 2. Check content size
        ResourceLimits.ValidateContentSize(content);

        // 3. Atomic write (temp file + rename)
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Guid.NewGuid()}.tmp");

        try
        {
            // Write to temp file
            await File.WriteAllTextAsync(tempPath, content);

            // Rename to target (atomic operation)
            File.Move(tempPath, fullPath, overwrite: true);

            _logger.LogInformation(
                "Wrote file {FilePath} ({Size} bytes) for tenant {TenantId}",
                filePath, content.Length, context.TenantId);

            return $"Successfully wrote {content.Length} bytes to {filePath}";
        }
        finally
        {
            // Cleanup temp file if still exists
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("filePath"))
            throw new ArgumentException("Parameter 'filePath' is required");

        if (!context.Parameters.ContainsKey("content"))
            throw new ArgumentException("Parameter 'content' is required");

        return Task.CompletedTask;
    }
}
