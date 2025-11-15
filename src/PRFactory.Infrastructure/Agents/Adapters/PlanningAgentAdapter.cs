using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter for PlanningAgent that applies database-driven configuration.
/// Wraps the PlanningAgent and enriches it with tenant-specific settings before execution.
/// </summary>
public class PlanningAgentAdapter : BaseAgentAdapter
{
    private readonly PlanningAgent _wrappedAgent;

    public override string Name => "PlanningAgentAdapter";
    public override string Description => "Generates implementation plan with database-driven configuration";

    protected override string ConfiguredAgentName => "PlannerAgent";

    public PlanningAgentAdapter(
        IAgentFactory agentFactory,
        PlanningAgent wrappedAgent,
        ILogger<PlanningAgentAdapter> logger)
        : base(agentFactory, logger)
    {
        _wrappedAgent = wrappedAgent ?? throw new ArgumentNullException(nameof(wrappedAgent));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Parse tenant ID from context
        if (!Guid.TryParse(context.TenantId, out var tenantId))
        {
            Logger.LogError("Invalid TenantId in context: {TenantId}", context.TenantId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "Invalid TenantId format"
            };
        }

        // Load configuration from database or use defaults
        var config = await LoadConfigurationAsync(tenantId, cancellationToken);

        // Apply configuration to context metadata
        ApplyConfigurationToContext(context, config);

        Logger.LogInformation(
            "Executing PlanningAgent with configuration: MaxTokens={MaxTokens}, Temperature={Temperature}",
            config.MaxTokens,
            config.Temperature);

        // Delegate to wrapped agent
        return await _wrappedAgent.ExecuteWithMiddlewareAsync(context, cancellationToken);
    }

    protected override string GetDefaultInstructions()
    {
        return @"You are an expert software architect creating a detailed implementation plan.

Create a comprehensive implementation plan in Markdown format that includes:
1. Overview and objectives
2. Step-by-step implementation instructions with specific file paths and code examples
3. Files to create/modify with clear descriptions
4. Testing strategy including unit tests and integration tests
5. Potential risks and mitigation strategies
6. Dependencies and prerequisites
7. Rollback plan in case of issues

Be specific, actionable, and follow the architectural patterns identified in the codebase analysis.
The plan should be detailed enough that another developer can implement it without additional context.";
    }
}
