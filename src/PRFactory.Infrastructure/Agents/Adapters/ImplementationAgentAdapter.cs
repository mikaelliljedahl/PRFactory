using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter for ImplementationAgent that applies database-driven configuration.
/// Wraps the ImplementationAgent and enriches it with tenant-specific settings before execution.
/// </summary>
public class ImplementationAgentAdapter : BaseAgentAdapter
{
    private readonly ImplementationAgent _wrappedAgent;

    public override string Name => "ImplementationAgentAdapter";
    public override string Description => "Generates code implementation with database-driven configuration";

    protected override string ConfiguredAgentName => "ImplementationAgent";

    public ImplementationAgentAdapter(
        IAgentFactory agentFactory,
        ImplementationAgent wrappedAgent,
        ILogger<ImplementationAgentAdapter> logger)
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
            "Executing ImplementationAgent with configuration: MaxTokens={MaxTokens}, Temperature={Temperature}",
            config.MaxTokens,
            config.Temperature);

        // Delegate to wrapped agent
        return await _wrappedAgent.ExecuteWithMiddlewareAsync(context, cancellationToken);
    }

    protected override string GetDefaultInstructions()
    {
        return @"You are an expert software developer implementing code based on an approved plan.

Generate production-ready code implementation that:
1. Follows the approved implementation plan exactly
2. Implements complete, functional code (no TODOs or placeholders)
3. Follows the codebase's existing patterns and conventions
4. Includes appropriate error handling and validation
5. Adds inline comments for complex logic
6. Ensures code is testable and maintainable
7. Follows SOLID principles and clean code practices

For each file, provide:
- Complete file path
- Full file contents
- Whether it's a new file or modification

The code should be production-ready and ready for code review.";
    }
}
