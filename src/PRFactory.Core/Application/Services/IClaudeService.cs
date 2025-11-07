namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service interface for Claude AI integration
/// </summary>
public interface IClaudeService
{
    /// <summary>
    /// Analyze codebase to understand structure and identify relevant files
    /// </summary>
    /// <param name="ticket">The ticket to analyze for</param>
    /// <param name="repositoryPath">Path to the repository</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Analysis results including relevant files and architecture details</returns>
    Task<CodebaseAnalysis> AnalyzeCodebaseAsync(
        dynamic ticket,
        string repositoryPath,
        CancellationToken ct = default
    );

    /// <summary>
    /// Generate clarifying questions based on ticket and analysis
    /// </summary>
    /// <param name="ticket">The ticket to generate questions for</param>
    /// <param name="analysis">The codebase analysis results</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of clarifying questions</returns>
    Task<List<Question>> GenerateQuestionsAsync(
        dynamic ticket,
        CodebaseAnalysis analysis,
        CancellationToken ct = default
    );

    /// <summary>
    /// Generate implementation plan based on ticket, answers, and codebase
    /// </summary>
    /// <param name="ticket">The ticket with answered questions</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed implementation plan</returns>
    Task<ImplementationPlan> GenerateImplementationPlanAsync(
        dynamic ticket,
        CancellationToken ct = default
    );

    /// <summary>
    /// Implement code changes based on approved plan
    /// </summary>
    /// <param name="ticket">The ticket with approved plan</param>
    /// <param name="repositoryPath">Path to the repository</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Code implementation results with file changes</returns>
    Task<CodeImplementation> ImplementCodeAsync(
        dynamic ticket,
        string repositoryPath,
        CancellationToken ct = default
    );
}

// DTOs - These should eventually be moved to a shared models project

/// <summary>
/// Represents the result of analyzing a codebase
/// </summary>
public record CodebaseAnalysis(
    List<string> RelevantFiles,
    Dictionary<string, string> FileContents,
    string Architecture,
    List<string> Patterns,
    List<string> Dependencies
);

/// <summary>
/// Represents a detailed implementation plan
/// </summary>
public record ImplementationPlan(
    string MainPlan,
    string AffectedFiles,
    string TestStrategy,
    int EstimatedComplexity
);

/// <summary>
/// Represents the result of code implementation
/// </summary>
public record CodeImplementation(
    Dictionary<string, string> ModifiedFiles,
    List<string> CreatedFiles,
    string Summary
);

/// <summary>
/// Represents a clarifying question
/// </summary>
public record Question(
    string Id,
    string Category,
    string Text,
    DateTime CreatedAt
);
