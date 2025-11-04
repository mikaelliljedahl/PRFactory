namespace PRFactory.Infrastructure.Claude.Models;

/// <summary>
/// Represents the result of analyzing a codebase
/// </summary>
/// <param name="RelevantFiles">List of file paths relevant to the ticket</param>
/// <param name="FileContents">Dictionary mapping file paths to their contents</param>
/// <param name="Architecture">Identified architecture style (e.g., MVC, Clean Architecture)</param>
/// <param name="Patterns">Coding patterns and conventions observed</param>
/// <param name="Dependencies">Key dependencies identified</param>
public record CodebaseAnalysis(
    List<string> RelevantFiles,
    Dictionary<string, string> FileContents,
    string Architecture,
    List<string> Patterns,
    List<string> Dependencies
);
