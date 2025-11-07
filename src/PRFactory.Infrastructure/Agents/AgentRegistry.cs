using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Registry for managing and resolving agent instances.
/// Provides a centralized way to register, retrieve, and manage agents in the system.
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentRegistry> _logger;
    private readonly Dictionary<string, AgentDescriptor> _agents = new();
    private readonly object _lock = new();

    public AgentRegistry(
        IServiceProvider serviceProvider,
        ILogger<AgentRegistry> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers an agent type with the registry.
    /// </summary>
    public void RegisterAgent<TAgent>(string agentName, string? description = null)
        where TAgent : BaseAgent
    {
        lock (_lock)
        {
            if (_agents.ContainsKey(agentName))
            {
                throw new InvalidOperationException($"Agent '{agentName}' is already registered");
            }

            var descriptor = new AgentDescriptor
            {
                Name = agentName,
                Type = typeof(TAgent),
                Description = description ?? $"Agent: {agentName}",
                RegisteredAt = DateTime.UtcNow
            };

            _agents[agentName] = descriptor;

            _logger.LogInformation(
                "Registered agent: {AgentName} ({AgentType})",
                agentName,
                typeof(TAgent).Name);
        }
    }

    /// <summary>
    /// Retrieves an agent instance by name.
    /// </summary>
    public BaseAgent GetAgent(string agentName)
    {
        if (!_agents.TryGetValue(agentName, out var descriptor))
        {
            throw new InvalidOperationException($"Agent '{agentName}' is not registered");
        }

        try
        {
            var agent = _serviceProvider.GetRequiredService(descriptor.Type) as BaseAgent;
            if (agent == null)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve agent '{agentName}' of type {descriptor.Type.Name}");
            }

            _logger.LogDebug("Retrieved agent instance: {AgentName}", agentName);
            return agent;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get agent instance for {AgentName}",
                agentName);
            throw;
        }
    }

    /// <summary>
    /// Tries to retrieve an agent instance by name.
    /// Returns null if the agent is not registered or cannot be resolved.
    /// </summary>
    public BaseAgent? TryGetAgent(string agentName)
    {
        if (!_agents.TryGetValue(agentName, out var descriptor))
        {
            return null;
        }

        try
        {
            return _serviceProvider.GetService(descriptor.Type) as BaseAgent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to get agent instance for {AgentName}",
                agentName);
            return null;
        }
    }

    /// <summary>
    /// Gets all registered agent descriptors.
    /// </summary>
    public IReadOnlyCollection<AgentDescriptor> GetAllAgents()
    {
        lock (_lock)
        {
            return _agents.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Checks if an agent is registered.
    /// </summary>
    public bool IsRegistered(string agentName)
    {
        return _agents.ContainsKey(agentName);
    }

    /// <summary>
    /// Gets the descriptor for a registered agent.
    /// </summary>
    public AgentDescriptor? GetDescriptor(string agentName)
    {
        _agents.TryGetValue(agentName, out var descriptor);
        return descriptor;
    }

    /// <summary>
    /// Unregisters an agent from the registry.
    /// </summary>
    public void UnregisterAgent(string agentName)
    {
        lock (_lock)
        {
            if (_agents.Remove(agentName))
            {
                _logger.LogInformation("Unregistered agent: {AgentName}", agentName);
            }
        }
    }
}

/// <summary>
/// Interface for the agent registry.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Registers an agent type.
    /// </summary>
    void RegisterAgent<TAgent>(string agentName, string? description = null)
        where TAgent : BaseAgent;

    /// <summary>
    /// Gets an agent instance by name.
    /// </summary>
    BaseAgent GetAgent(string agentName);

    /// <summary>
    /// Tries to get an agent instance by name.
    /// </summary>
    BaseAgent? TryGetAgent(string agentName);

    /// <summary>
    /// Gets all registered agent descriptors.
    /// </summary>
    IReadOnlyCollection<AgentDescriptor> GetAllAgents();

    /// <summary>
    /// Checks if an agent is registered.
    /// </summary>
    bool IsRegistered(string agentName);

    /// <summary>
    /// Gets the descriptor for a registered agent.
    /// </summary>
    AgentDescriptor? GetDescriptor(string agentName);

    /// <summary>
    /// Unregisters an agent.
    /// </summary>
    void UnregisterAgent(string agentName);
}

/// <summary>
/// Descriptor containing metadata about a registered agent.
/// </summary>
public class AgentDescriptor
{
    /// <summary>
    /// The name of the agent (unique identifier).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The CLR type of the agent.
    /// </summary>
    public Type Type { get; set; } = null!;

    /// <summary>
    /// Human-readable description of the agent.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the agent was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Optional tags for categorizing agents.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Optional metadata for the agent.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Extension methods for IServiceCollection to simplify agent registration.
/// </summary>
public static class AgentRegistryExtensions
{
    /// <summary>
    /// Adds the agent registry to the service collection.
    /// </summary>
    public static IServiceCollection AddAgentRegistry(this IServiceCollection services)
    {
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        return services;
    }

    /// <summary>
    /// Registers an agent with both DI container and the agent registry.
    /// </summary>
    public static IServiceCollection AddAgent<TAgent>(
        this IServiceCollection services,
        string agentName,
        string? description = null)
        where TAgent : BaseAgent
    {
        // Register the agent with DI
        services.AddTransient<TAgent>();

        // Configure the registry to register this agent on startup
        services.AddSingleton<IAgentRegistration>(sp =>
            new AgentRegistration<TAgent>(agentName, description));

        return services;
    }

    /// <summary>
    /// Configures the agent registry with all registered agents.
    /// Call this after all agents have been added via AddAgent.
    /// </summary>
    public static IServiceProvider ConfigureAgentRegistry(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IAgentRegistry>();
        var registrations = serviceProvider.GetServices<IAgentRegistration>();

        foreach (var registration in registrations)
        {
            registration.Register(registry);
        }

        return serviceProvider;
    }
}

/// <summary>
/// Internal interface for deferred agent registration.
/// </summary>
internal interface IAgentRegistration
{
    void Register(IAgentRegistry registry);
}

/// <summary>
/// Internal class for deferred agent registration.
/// </summary>
internal class AgentRegistration<TAgent> : IAgentRegistration
    where TAgent : BaseAgent
{
    private readonly string _agentName;
    private readonly string? _description;

    public AgentRegistration(string agentName, string? description)
    {
        _agentName = agentName;
        _description = description;
    }

    public void Register(IAgentRegistry registry)
    {
        registry.RegisterAgent<TAgent>(_agentName, _description);
    }
}
