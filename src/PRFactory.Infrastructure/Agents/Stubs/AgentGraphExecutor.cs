using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Stubs;

/// <summary>
/// Stub implementation of IAgentGraphExecutor for build purposes.
/// This should be replaced with a real implementation that executes agent graphs.
/// </summary>
public class AgentGraphExecutor : IAgentGraphExecutor
{
    private readonly ILogger<AgentGraphExecutor> _logger;

    public AgentGraphExecutor(ILogger<AgentGraphExecutor> logger)
    {
        _logger = logger;
    }

    public Task<WorkflowExecutionResult> ExecuteGraphAsync(
        string workflowType,
        IAgentMessage initialMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Using stub implementation of IAgentGraphExecutor");
        _logger.LogInformation("Would execute workflow type: {WorkflowType}", workflowType);

        return Task.FromResult(new WorkflowExecutionResult
        {
            IsSuccess = false,
            Message = "Stub implementation - no actual execution performed"
        });
    }
}
