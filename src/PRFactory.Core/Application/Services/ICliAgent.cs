using PRFactory.Domain.ValueObjects;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Interface for CLI-based AI agents that can execute prompts and generate code.
/// Abstracts different CLI agent implementations (Claude Desktop, Codex CLI, etc.)
/// </summary>
public interface ICliAgent
{
    /// <summary>
    /// Name of the CLI agent (e.g., "Claude Desktop", "Codex CLI")
    /// </summary>
    string AgentName { get; }

    /// <summary>
    /// Indicates whether the agent supports streaming output
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Gets the capabilities of this CLI agent
    /// </summary>
    /// <returns>Capabilities information</returns>
    CliAgentCapabilities GetCapabilities();

    /// <summary>
    /// Executes a prompt and returns the response
    /// </summary>
    /// <param name="prompt">The prompt to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent's response</returns>
    Task<CliAgentResponse> ExecutePromptAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt with full project context
    /// </summary>
    /// <param name="prompt">The prompt to execute</param>
    /// <param name="projectPath">Path to the project directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent's response</returns>
    Task<CliAgentResponse> ExecuteWithProjectContextAsync(
        string prompt,
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt with streaming output
    /// </summary>
    /// <param name="prompt">The prompt to execute</param>
    /// <param name="onOutputReceived">Callback invoked when output is received</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The final agent response</returns>
    Task<CliAgentResponse> ExecuteStreamingAsync(
        string prompt,
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from a CLI agent execution
/// </summary>
public class CliAgentResponse
{
    /// <summary>
    /// Indicates whether the execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response content from the agent
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Metadata about the execution (tokens used, timing, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// File operations performed by the agent (if any)
    /// </summary>
    public List<FileOperation> FileOperations { get; set; } = new();

    /// <summary>
    /// Exit code from the CLI process
    /// </summary>
    public int? ExitCode { get; set; }
}

/// <summary>
/// Represents a file operation performed by the agent
/// </summary>
public class FileOperation
{
    /// <summary>
    /// Type of operation (Create, Update, Delete)
    /// </summary>
    public required string OperationType { get; init; }

    /// <summary>
    /// Path to the file
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Content of the file (for Create/Update operations)
    /// </summary>
    public string? Content { get; init; }
}
