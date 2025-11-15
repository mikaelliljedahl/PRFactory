namespace PRFactory.Core.Application.Services;

/// <summary>
/// Interface for tool registry.
/// Provides access to registered tools for agent execution.
/// Note: ITool is from PRFactory.AgentTools.Core namespace,
/// but we use object here to avoid circular dependency.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Get all registered tools.
    /// </summary>
    /// <returns>All tools as objects (cast to ITool when used)</returns>
    IEnumerable<object> GetAllTools();

    /// <summary>
    /// Get tools filtered by tenant permissions and enabled tool names.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="enabledToolNames">Tool names enabled for this tenant/agent</param>
    /// <returns>Filtered tools as objects (cast to ITool when used)</returns>
    IEnumerable<object> GetTools(Guid tenantId, string[] enabledToolNames);

    /// <summary>
    /// Get a specific tool by name.
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <returns>Tool instance as object or null if not found</returns>
    object? GetTool(string toolName);
}
