using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Factory for creating agent instances with database-driven configuration.
/// Maps agent names to types and instantiates agents using dependency injection.
/// </summary>
public class AgentFactory : IAgentFactory
{
    private readonly IAgentConfigurationRepository _configurationRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentFactory> _logger;

    /// <summary>
    /// Agent type registry mapping agent names to concrete types.
    /// </summary>
    private readonly Dictionary<string, Type> _agentTypeRegistry;

    public AgentFactory(
        IAgentConfigurationRepository configurationRepository,
        IServiceProvider serviceProvider,
        ILogger<AgentFactory> logger)
    {
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize agent type registry
        _agentTypeRegistry = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Map agent names to their concrete types
            ["AnalyzerAgent"] = typeof(AnalysisAgent),
            ["AnalysisAgent"] = typeof(AnalysisAgent),
            ["PlannerAgent"] = typeof(PlanningAgent),
            ["PlanningAgent"] = typeof(PlanningAgent),
            ["ImplementationAgent"] = typeof(ImplementationAgent),
            ["CodeReviewAgent"] = typeof(Specialized.CodeReviewAgent),
            ["RepositoryCloneAgent"] = typeof(RepositoryCloneAgent),
            ["QuestionGenerationAgent"] = typeof(QuestionGenerationAgent),
            ["AnswerProcessingAgent"] = typeof(AnswerProcessingAgent),
            ["GitPlanAgent"] = typeof(GitPlanAgent),
            ["GitCommitAgent"] = typeof(GitCommitAgent),
            ["PullRequestAgent"] = typeof(PullRequestAgent),
            ["JiraPostAgent"] = typeof(JiraPostAgent),
            ["TicketUpdateGenerationAgent"] = typeof(TicketUpdateGenerationAgent),
            ["TicketUpdatePostAgent"] = typeof(TicketUpdatePostAgent),
            ["PostReviewCommentsAgent"] = typeof(Specialized.PostReviewCommentsAgent),
            ["PostApprovalCommentAgent"] = typeof(Specialized.PostApprovalCommentAgent),

            // AF-based agents (Epic 05)
            ["AFAnalyzerAgent"] = typeof(AI.AFAnalyzerAgent),
            // Future: ["AFPlannerAgent"] = typeof(AI.AFPlannerAgent),
        };
    }

    public async Task<object> CreateAgentAsync(
        Guid tenantId,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating agent {AgentName} for tenant {TenantId}",
            agentName,
            tenantId);

        // Load configuration from database
        var configuration = await _configurationRepository.GetByTenantAndNameAsync(
            tenantId,
            agentName,
            cancellationToken);

        if (configuration == null)
        {
            _logger.LogError(
                "Agent configuration not found for tenant {TenantId} and agent {AgentName}",
                tenantId,
                agentName);
            throw new AgentConfigurationNotFoundException(tenantId, agentName);
        }

        // Get agent type from registry
        if (!_agentTypeRegistry.TryGetValue(agentName, out var agentType))
        {
            _logger.LogError(
                "Agent type {AgentName} is not registered in the factory",
                agentName);
            throw new AgentTypeNotRegisteredException(agentName);
        }

        // Create agent instance using DI
        var agent = ActivatorUtilities.CreateInstance(_serviceProvider, agentType) as BaseAgent;

        if (agent == null)
        {
            throw new InvalidOperationException(
                $"Failed to create agent instance for type {agentType.Name}. " +
                $"Ensure the agent type inherits from BaseAgent.");
        }

        _logger.LogInformation(
            "Successfully created agent {AgentName} (type: {TypeName}) for tenant {TenantId}",
            agentName,
            agentType.Name,
            tenantId);

        // Note: Agent configuration (Instructions, MaxTokens, etc.) is loaded from the database
        // but not applied to the agent instance in this phase. This will be implemented in Phase 2.
        // For now, agents use their default constructor parameters and hard-coded prompts.

        return agent;
    }

    public async Task<AgentConfiguration?> GetConfigurationAsync(
        Guid tenantId,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        return await _configurationRepository.GetByTenantAndNameAsync(
            tenantId,
            agentName,
            cancellationToken);
    }

    public async Task<ValidationResult> ValidateConfigurationAsync(
        Guid tenantId,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationRepository.GetByTenantAndNameAsync(
            tenantId,
            agentName,
            cancellationToken);

        if (configuration == null)
        {
            return ValidationResult.Failure(
                $"Agent configuration not found for tenant '{tenantId}' and agent '{agentName}'");
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate agent type is registered
        if (!_agentTypeRegistry.ContainsKey(agentName))
        {
            errors.Add($"Agent type '{agentName}' is not registered in the factory");
        }

        // Validate instructions are not empty
        if (string.IsNullOrWhiteSpace(configuration.Instructions))
        {
            warnings.Add("Instructions are empty. Agent will use default behavior.");
        }

        // Validate MaxTokens is reasonable
        if (configuration.MaxTokens <= 0)
        {
            errors.Add("MaxTokens must be greater than 0");
        }
        else if (configuration.MaxTokens > 200000)
        {
            warnings.Add($"MaxTokens ({configuration.MaxTokens}) is very high and may cause performance issues");
        }

        // Validate Temperature is in valid range
        if (configuration.Temperature < 0.0f || configuration.Temperature > 1.0f)
        {
            errors.Add("Temperature must be between 0.0 and 1.0");
        }

        // Validate EnabledTools JSON is valid (basic check)
        if (!string.IsNullOrWhiteSpace(configuration.EnabledTools))
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(configuration.EnabledTools);
            }
            catch (System.Text.Json.JsonException ex)
            {
                errors.Add($"EnabledTools is not valid JSON: {ex.Message}");
            }
        }

        if (errors.Any())
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }

        if (warnings.Any())
        {
            return ValidationResult.SuccessWithWarnings(warnings.ToArray());
        }

        return ValidationResult.Success();
    }
}
