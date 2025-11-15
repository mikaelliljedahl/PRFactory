using Microsoft.Extensions.DependencyInjection;
using PRFactory.AgentTools.Core;
using CoreToolRegistry = PRFactory.Core.Application.Services.IToolRegistry;

namespace PRFactory.AgentTools.Extensions;

/// <summary>
/// Extension methods for registering agent tools in the DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all agent tools in DI container.
    /// Tools are auto-discovered via reflection.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAgentTools(this IServiceCollection services)
    {
        // Register ToolRegistry as singleton for both interfaces
        services.AddSingleton<ToolRegistry>();
        services.AddSingleton<CoreToolRegistry>(sp => sp.GetRequiredService<ToolRegistry>());

        // Auto-discover all ITool implementations and register them as transient
        var toolTypes = typeof(ITool).Assembly.GetTypes()
            .Where(t => typeof(ITool).IsAssignableFrom(t) &&
                       !t.IsAbstract &&
                       !t.IsInterface);

        foreach (var toolType in toolTypes)
        {
            // Register each tool type as itself (for GetService<SpecificToolType>)
            // and as ITool (for GetServices<ITool>)
            services.AddTransient(toolType);
            services.AddTransient(typeof(ITool), toolType);
        }

        return services;
    }
}
