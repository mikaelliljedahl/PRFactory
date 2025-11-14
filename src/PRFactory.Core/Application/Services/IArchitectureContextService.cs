namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for retrieving architectural context to enhance AI planning prompts.
/// Provides codebase patterns, technology stack, and relevant code snippets.
/// </summary>
public interface IArchitectureContextService
{
    /// <summary>
    /// Gets architectural patterns from ARCHITECTURE.md or codebase analysis.
    /// </summary>
    /// <param name="repositoryPath">Path to the repository root</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Formatted architectural patterns summary</returns>
    Task<string> GetArchitecturePatternsAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the technology stack with versions.
    /// </summary>
    /// <returns>Formatted technology stack information</returns>
    string GetTechnologyStack();

    /// <summary>
    /// Gets code style guidelines from .editorconfig or project conventions.
    /// </summary>
    /// <returns>Formatted code style guidelines</returns>
    string GetCodeStyleGuidelines();

    /// <summary>
    /// Gets relevant code snippets based on the ticket description.
    /// Uses simple keyword matching to find related files.
    /// </summary>
    /// <param name="repositoryPath">Path to the repository root</param>
    /// <param name="ticketDescription">Ticket description for context</param>
    /// <param name="maxSnippets">Maximum number of snippets to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of code snippets with file paths and content</returns>
    Task<List<CodeSnippet>> GetRelevantCodeSnippetsAsync(
        string repositoryPath,
        string ticketDescription,
        int maxSnippets = 3,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a code snippet with file context.
/// </summary>
public class CodeSnippet
{
    /// <summary>
    /// File path relative to repository root
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Programming language (e.g., "csharp", "json", "yaml")
    /// </summary>
    public string Language { get; set; } = "csharp";

    /// <summary>
    /// Code content (limited to first N lines)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the snippet's purpose
    /// </summary>
    public string? Description { get; set; }
}
