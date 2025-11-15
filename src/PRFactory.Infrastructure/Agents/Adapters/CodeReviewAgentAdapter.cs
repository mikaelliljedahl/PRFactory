using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Specialized;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter for CodeReviewAgent that applies database-driven configuration.
/// Wraps the CodeReviewAgent and enriches it with tenant-specific settings before execution.
/// </summary>
public class CodeReviewAgentAdapter : BaseAgentAdapter
{
    private readonly CodeReviewAgent _wrappedAgent;

    public override string Name => "CodeReviewAgentAdapter";
    public override string Description => "Reviews pull requests with database-driven configuration";

    protected override string ConfiguredAgentName => "CodeReviewAgent";

    public CodeReviewAgentAdapter(
        IAgentFactory agentFactory,
        CodeReviewAgent wrappedAgent,
        ILogger<CodeReviewAgentAdapter> logger)
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
            "Executing CodeReviewAgent with configuration: MaxTokens={MaxTokens}, Temperature={Temperature}",
            config.MaxTokens,
            config.Temperature);

        // Delegate to wrapped agent
        return await _wrappedAgent.ExecuteWithMiddlewareAsync(context, cancellationToken);
    }

    protected override string GetDefaultInstructions()
    {
        return @"You are an expert code reviewer analyzing a pull request for quality, security, and best practices.

Review the code changes and provide:

1. **Critical Issues** - Must-fix problems that would prevent approval:
   - Security vulnerabilities
   - Breaking changes without migration path
   - Logic errors or bugs
   - Performance issues
   - Violations of architectural principles

2. **Suggestions** - Improvements that would enhance code quality:
   - Better error handling
   - Code organization improvements
   - Performance optimizations
   - Better naming or documentation
   - Test coverage improvements

3. **Praise** - Things done well:
   - Good architectural decisions
   - Well-written tests
   - Clear documentation
   - Elegant solutions

Be constructive, specific, and actionable in your feedback.
Focus on helping the developer improve the code while maintaining a positive tone.";
    }
}
