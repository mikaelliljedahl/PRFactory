using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Factory for creating and configuring agent instances based on database configuration.
/// Supports multi-tenant agent customization with per-tenant settings.
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// Creates an agent instance with configuration loaded from the database.
    /// </summary>
    /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
    /// <param name="agentName">The agent name (e.g., "AnalysisAgent", "PlanningAgent")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configured agent instance ready for execution</returns>
    /// <exception cref="AgentConfigurationNotFoundException">Thrown when no configuration exists for the agent</exception>
    /// <exception cref="AgentTypeNotRegisteredException">Thrown when the agent type is not registered in the factory</exception>
    /// <remarks>
    /// Returns object type to avoid circular dependency between Core and Infrastructure layers.
    /// The actual return type is BaseAgent from PRFactory.Infrastructure.Agents.Base namespace.
    /// </remarks>
    Task<object> CreateAgentAsync(Guid tenantId, string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the agent configuration from the database without creating an agent instance.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="agentName">The agent name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent configuration if found, null otherwise</returns>
    Task<AgentConfiguration?> GetConfigurationAsync(Guid tenantId, string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an agent configuration to ensure it's properly configured.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="agentName">The agent name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any errors or warnings</returns>
    Task<ValidationResult> ValidateConfigurationAsync(Guid tenantId, string agentName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of agent configuration validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the configuration is valid and can be used.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors that prevent agent creation.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings that don't prevent agent creation but should be addressed.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new ValidationResult { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(params string[] errors) =>
        new ValidationResult
        {
            IsValid = false,
            Errors = new List<string>(errors)
        };

    /// <summary>
    /// Creates a successful validation result with warnings.
    /// </summary>
    public static ValidationResult SuccessWithWarnings(params string[] warnings) =>
        new ValidationResult
        {
            IsValid = true,
            Warnings = new List<string>(warnings)
        };
}

/// <summary>
/// Exception thrown when an agent configuration is not found in the database.
/// </summary>
public class AgentConfigurationNotFoundException : Exception
{
    public Guid TenantId { get; }
    public string AgentName { get; }

    public AgentConfigurationNotFoundException(Guid tenantId, string agentName)
        : base($"Agent configuration not found for tenant '{tenantId}' and agent '{agentName}'")
    {
        TenantId = tenantId;
        AgentName = agentName;
    }
}

/// <summary>
/// Exception thrown when an agent type is not registered in the factory.
/// </summary>
public class AgentTypeNotRegisteredException : Exception
{
    public string AgentName { get; }

    public AgentTypeNotRegisteredException(string agentName)
        : base($"Agent type '{agentName}' is not registered in the agent factory. Available agents must be registered in the type registry.")
    {
        AgentName = agentName;
    }
}
