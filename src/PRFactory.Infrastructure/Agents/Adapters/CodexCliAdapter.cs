using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Execution;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter for Codex CLI (placeholder implementation).
/// This is a stub for future Codex CLI support.
/// </summary>
public class CodexCliAdapter : ICliAgent
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger<CodexCliAdapter> _logger;
    private readonly string _codexExecutablePath;

    public string AgentName => "Codex CLI";
    public bool SupportsStreaming => false;

    public CodexCliAdapter(
        IProcessExecutor processExecutor,
        ILogger<CodexCliAdapter> logger,
        string? codexExecutablePath = null)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _codexExecutablePath = codexExecutablePath ?? "codex";
    }

    /// <summary>
    /// Gets the capabilities of Codex CLI
    /// </summary>
    public CliAgentCapabilities GetCapabilities()
    {
        return CliAgentCapabilities.ForCodexCli();
    }

    /// <summary>
    /// Executes a prompt using Codex CLI (not yet implemented)
    /// </summary>
    public Task<CliAgentResponse> ExecutePromptAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("CodexCliAdapter is not yet implemented");

        throw new NotImplementedException(
            "Codex CLI adapter is a placeholder for future implementation. " +
            "To implement this adapter, add the logic to execute Codex CLI commands " +
            "similar to ClaudeDesktopCliAdapter.");
    }

    /// <summary>
    /// Executes a prompt with project context (not yet implemented)
    /// </summary>
    public Task<CliAgentResponse> ExecuteWithProjectContextAsync(
        string prompt,
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("CodexCliAdapter is not yet implemented");

        throw new NotImplementedException(
            "Codex CLI adapter is a placeholder for future implementation. " +
            "To implement this adapter, add the logic to execute Codex CLI commands " +
            "with project context similar to ClaudeDesktopCliAdapter.");
    }

    /// <summary>
    /// Executes a prompt with streaming output (not supported by Codex CLI)
    /// </summary>
    public Task<CliAgentResponse> ExecuteStreamingAsync(
        string prompt,
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("CodexCliAdapter does not support streaming");

        throw new NotSupportedException(
            "Codex CLI does not support streaming output. " +
            "Use ExecutePromptAsync or ExecuteWithProjectContextAsync instead.");
    }
}

/// <summary>
/// Implementation notes for future developers:
///
/// To complete this adapter:
/// 1. Research Codex CLI documentation and command-line interface
/// 2. Implement BuildArguments() method to construct Codex CLI commands
/// 3. Implement ParseCliResponse() to parse Codex output format
/// 4. Add error handling specific to Codex CLI
/// 5. Test with actual Codex CLI installation
/// 6. Update capabilities in CliAgentCapabilities.ForCodexCli() if needed
///
/// Example implementation approach:
/// - Use _processExecutor.ExecuteAsync() to run codex commands
/// - Parse JSON or text output from Codex
/// - Extract file operations if Codex supports them
/// - Handle Codex-specific errors and rate limits
/// </summary>
