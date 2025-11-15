using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter for AnalysisAgent that applies database-driven configuration.
/// Wraps the AnalysisAgent and enriches it with tenant-specific settings before execution.
/// </summary>
public class AnalysisAgentAdapter : BaseAgentAdapter
{
    private readonly AnalysisAgent _wrappedAgent;

    public override string Name => "AnalysisAgentAdapter";
    public override string Description => "Analyzes codebase with database-driven configuration";

    protected override string ConfiguredAgentName => "AnalyzerAgent";

    public AnalysisAgentAdapter(
        IAgentFactory agentFactory,
        AnalysisAgent wrappedAgent,
        ILogger<AnalysisAgentAdapter> logger)
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
            "Executing AnalysisAgent with configuration: MaxTokens={MaxTokens}, Temperature={Temperature}",
            config.MaxTokens,
            config.Temperature);

        // Delegate to wrapped agent
        return await _wrappedAgent.ExecuteWithMiddlewareAsync(context, cancellationToken);
    }

    protected override string GetDefaultInstructions()
    {
        return @"You are an expert software architect analyzing a codebase to understand how to implement a new feature.

Your task is to analyze the provided codebase and ticket requirements, then provide:
1. A summary of the codebase architecture
2. List of files that will likely be affected by this change
3. Technical considerations for implementation
4. Any potential risks or challenges

Focus on understanding the existing patterns, dependencies, and architectural decisions.
Provide actionable insights that will help with implementation planning.";
    }
}
