namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Represents the capabilities of a CLI-based AI agent.
/// This value object defines what features and limits an agent supports.
/// </summary>
public class CliAgentCapabilities
{
    /// <summary>
    /// Indicates whether the agent can generate code
    /// </summary>
    public bool SupportsCodeGeneration { get; init; }

    /// <summary>
    /// Indicates whether the agent can perform file operations (create, update, delete)
    /// </summary>
    public bool SupportsFileOperations { get; init; }

    /// <summary>
    /// Indicates whether the agent can use project context for better understanding
    /// </summary>
    public bool SupportsProjectContext { get; init; }

    /// <summary>
    /// Indicates whether the agent supports streaming output
    /// </summary>
    public bool SupportsStreaming { get; init; }

    /// <summary>
    /// Maximum number of tokens the agent can process in a single request
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Supported output formats (e.g., "json", "markdown", "text")
    /// </summary>
    public List<string> SupportedFormats { get; init; } = new();

    /// <summary>
    /// Model name or version identifier
    /// </summary>
    public string? ModelName { get; init; }

    /// <summary>
    /// Additional metadata about capabilities
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Creates a new CliAgentCapabilities instance with the specified features
    /// </summary>
    public static CliAgentCapabilities Create(
        bool supportsCodeGeneration,
        bool supportsFileOperations,
        bool supportsProjectContext,
        bool supportsStreaming,
        int? maxTokens = null,
        List<string>? supportedFormats = null,
        string? modelName = null)
    {
        return new CliAgentCapabilities
        {
            SupportsCodeGeneration = supportsCodeGeneration,
            SupportsFileOperations = supportsFileOperations,
            SupportsProjectContext = supportsProjectContext,
            SupportsStreaming = supportsStreaming,
            MaxTokens = maxTokens,
            SupportedFormats = supportedFormats ?? new List<string>(),
            ModelName = modelName
        };
    }

    /// <summary>
    /// Creates capabilities for Claude Code CLI agent
    /// </summary>
    public static CliAgentCapabilities ForClaudeCode()
    {
        return new CliAgentCapabilities
        {
            SupportsCodeGeneration = true,
            SupportsFileOperations = true,
            SupportsProjectContext = true,
            SupportsStreaming = true,
            MaxTokens = 200000,
            SupportedFormats = new List<string> { "json", "markdown", "text" },
            ModelName = "claude-sonnet-4-5"
        };
    }

    /// <summary>
    /// Creates capabilities for Codex CLI agent (placeholder)
    /// </summary>
    public static CliAgentCapabilities ForCodexCli()
    {
        return new CliAgentCapabilities
        {
            SupportsCodeGeneration = true,
            SupportsFileOperations = true,
            SupportsProjectContext = true,
            SupportsStreaming = false,
            MaxTokens = 8000,
            SupportedFormats = new List<string> { "json", "text" },
            ModelName = "codex"
        };
    }
}
