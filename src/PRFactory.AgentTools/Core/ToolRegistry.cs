using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PRFactory.AgentTools.Core;

/// <summary>
/// Interface for tool registry
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Get all registered tools
    /// </summary>
    /// <returns>All tools</returns>
    IEnumerable<ITool> GetAllTools();

    /// <summary>
    /// Get tools filtered by tenant permissions and enabled tool names
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="enabledToolNames">Tool names enabled for this tenant/agent</param>
    /// <returns>Filtered tools</returns>
    IEnumerable<ITool> GetTools(Guid tenantId, string[] enabledToolNames);

    /// <summary>
    /// Get a specific tool by name
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <returns>Tool instance or null if not found</returns>
    ITool? GetTool(string toolName);
}

/// <summary>
/// Tool registry for auto-discovery and dependency injection.
/// Discovers all ITool implementations from the service provider.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ToolRegistry> _logger;
    private readonly Dictionary<string, Type> _toolTypes;

    /// <summary>
    /// Create a new ToolRegistry
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving tool instances</param>
    /// <param name="logger">Logger</param>
    public ToolRegistry(
        IServiceProvider serviceProvider,
        ILogger<ToolRegistry> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _toolTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        // Auto-discover all ITool implementations
        DiscoverTools();
    }

    /// <summary>
    /// Discover all ITool implementations from the service provider
    /// </summary>
    private void DiscoverTools()
    {
        try
        {
            // Get all ITool instances registered in DI
            var tools = _serviceProvider.GetServices<ITool>();

            foreach (var tool in tools)
            {
                var toolType = tool.GetType();
                _toolTypes[tool.Name] = toolType;
                _logger.LogDebug("Discovered tool: {ToolName} ({ToolType})",
                    tool.Name, toolType.Name);
            }

            _logger.LogInformation("Discovered {Count} tools", _toolTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover tools from service provider");
        }
    }

    /// <summary>
    /// Get all registered tools
    /// </summary>
    /// <returns>All tools</returns>
    public IEnumerable<ITool> GetAllTools()
    {
        var tools = new List<ITool>();
        foreach (var toolType in _toolTypes.Values)
        {
            var tool = _serviceProvider.GetService(toolType) as ITool;
            if (tool != null)
                tools.Add(tool);
        }
        return tools;
    }

    /// <summary>
    /// Get tools filtered by tenant permissions and enabled tool names
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="enabledToolNames">Tool names enabled for this tenant/agent</param>
    /// <returns>Filtered tools</returns>
    public IEnumerable<ITool> GetTools(Guid tenantId, string[] enabledToolNames)
    {
        // Filter by enabled tool names
        var enabledSet = new HashSet<string>(enabledToolNames, StringComparer.OrdinalIgnoreCase);

        var tools = GetAllTools()
            .Where(t => enabledSet.Contains(t.Name))
            .ToList();

        _logger.LogDebug(
            "Filtered tools for tenant {TenantId}: {Count} tools enabled out of {Total}",
            tenantId, tools.Count, _toolTypes.Count);

        return tools;
    }

    /// <summary>
    /// Get a specific tool by name
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <returns>Tool instance or null if not found</returns>
    public ITool? GetTool(string toolName)
    {
        if (!_toolTypes.TryGetValue(toolName, out var toolType))
        {
            _logger.LogWarning("Tool '{ToolName}' not found in registry", toolName);
            return null;
        }

        return _serviceProvider.GetService(toolType) as ITool;
    }
}
