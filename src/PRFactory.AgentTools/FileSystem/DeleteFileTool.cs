using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.FileSystem;

/// <summary>
/// Delete a file within workspace.
/// </summary>
public class DeleteFileTool : ToolBase
{
    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "DeleteFile";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Delete a file from the workspace";

    /// <summary>
    /// Create a new DeleteFileTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public DeleteFileTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the tool to delete a file
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Success message</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var filePath = context.GetParameter<string>("filePath");

        // 1. Validate path
        var fullPath = PathValidator.ValidateAndResolve(filePath, context.WorkspacePath);

        // 2. Check file exists
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // 3. Delete file
        File.Delete(fullPath);

        _logger.LogInformation(
            "Deleted file {FilePath} for tenant {TenantId}",
            filePath, context.TenantId);

        return $"Successfully deleted {filePath}";
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

        return Task.CompletedTask;
    }
}
