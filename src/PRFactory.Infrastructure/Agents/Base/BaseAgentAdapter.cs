using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Agents.Base;

/// <summary>
/// Abstract base class for agent adapters that wrap existing agents with database-driven configuration.
/// Adapters load configuration from the database via IAgentFactory and apply it to the wrapped agent's execution context.
/// </summary>
public abstract class BaseAgentAdapter : BaseAgent
{
    private readonly IAgentFactory _agentFactory;
    protected AgentConfiguration? _configuration;

    /// <summary>
    /// The name of the agent configuration to load from the database.
    /// This name is used to look up the configuration in the AgentConfiguration table.
    /// </summary>
    protected abstract string ConfiguredAgentName { get; }

    /// <summary>
    /// Gets the default instructions to use when no database configuration is found.
    /// Implementations should provide meaningful default prompts for their agent type.
    /// </summary>
    /// <returns>Default instructions/prompt for this agent</returns>
    protected abstract string GetDefaultInstructions();

    protected BaseAgentAdapter(
        IAgentFactory agentFactory,
        ILogger logger,
        string? agentId = null)
        : base(logger, agentId)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
    }

    /// <summary>
    /// Loads agent configuration from the database for the given tenant.
    /// Falls back to default configuration if database configuration is not found.
    /// </summary>
    /// <param name="tenantId">The tenant ID to load configuration for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded or default configuration</returns>
    protected async Task<AgentConfiguration> LoadConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Try to load from database first
        var dbConfig = await _agentFactory.GetConfigurationAsync(
            tenantId,
            ConfiguredAgentName,
            cancellationToken);

        if (dbConfig != null)
        {
            Logger.LogInformation(
                "Loaded configuration for agent {AgentName} from database for tenant {TenantId}",
                ConfiguredAgentName,
                tenantId);
            _configuration = dbConfig;
            return dbConfig;
        }

        // Fall back to default configuration
        Logger.LogWarning(
            "No database configuration found for agent {AgentName} and tenant {TenantId}, using default configuration",
            ConfiguredAgentName,
            tenantId);

        var defaultConfig = CreateDefaultConfiguration(tenantId);
        _configuration = defaultConfig;
        return defaultConfig;
    }

    /// <summary>
    /// Creates a default configuration when no database configuration is found.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Default agent configuration</returns>
    protected virtual AgentConfiguration CreateDefaultConfiguration(Guid tenantId)
    {
        return new AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = ConfiguredAgentName,
            Instructions = GetDefaultInstructions(),
            EnabledTools = "[]",
            MaxTokens = 8000,
            Temperature = 0.3f,
            StreamingEnabled = true,
            RequiresApproval = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies agent configuration to the context metadata.
    /// Configuration values are stored in context.Metadata for downstream consumption.
    /// </summary>
    /// <param name="context">The agent context to apply configuration to</param>
    /// <param name="config">The configuration to apply</param>
    protected virtual void ApplyConfigurationToContext(AgentContext context, AgentConfiguration config)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        // Apply configuration to metadata dictionary
        context.Metadata["AgentInstructions"] = config.Instructions;
        context.Metadata["MaxTokens"] = config.MaxTokens;
        context.Metadata["Temperature"] = config.Temperature;
        context.Metadata["EnabledTools"] = config.EnabledTools;
        context.Metadata["StreamingEnabled"] = config.StreamingEnabled;
        context.Metadata["RequiresApproval"] = config.RequiresApproval;

        Logger.LogDebug(
            "Applied configuration to context: MaxTokens={MaxTokens}, Temperature={Temperature}, StreamingEnabled={StreamingEnabled}",
            config.MaxTokens,
            config.Temperature,
            config.StreamingEnabled);
    }
}
