namespace PRFactory.AgentTools.Core;

/// <summary>
/// Base interface for all agent tools.
/// Tools provide capabilities to agents (file I/O, git, Jira, analysis, etc.)
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique tool name (used for whitelisting and invocation)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description for LLM (what the tool does)
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Execute the tool with given context and parameters
    /// </summary>
    /// <param name="context">Execution context (tenant, ticket, workspace)</param>
    /// <returns>Tool execution result (output, success, metadata)</returns>
    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context);
}
