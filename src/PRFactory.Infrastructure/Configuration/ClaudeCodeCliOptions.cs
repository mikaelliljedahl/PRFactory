namespace PRFactory.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Claude Code CLI adapter
/// </summary>
public class ClaudeCodeCliOptions
{
    /// <summary>
    /// Path to the Claude Code CLI executable.
    /// Defaults to "claude" (assumes it's in PATH).
    /// </summary>
    public string ExecutablePath { get; set; } = "claude";

    /// <summary>
    /// Timeout in seconds for standard prompt execution.
    /// Default: 300 seconds (5 minutes)
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Timeout in seconds for prompts with full project context.
    /// Default: 600 seconds (10 minutes)
    /// </summary>
    public int ProjectContextTimeoutSeconds { get; set; } = 600;

    /// <summary>
    /// Timeout in seconds for streaming operations.
    /// Default: 300 seconds (5 minutes)
    /// </summary>
    public int StreamingTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Enable verbose logging of CLI interactions.
    /// Default: false
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;
}
