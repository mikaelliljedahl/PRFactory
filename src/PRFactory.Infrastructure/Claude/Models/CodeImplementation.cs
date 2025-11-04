namespace PRFactory.Infrastructure.Claude.Models;

/// <summary>
/// Represents the result of code implementation
/// </summary>
/// <param name="ModifiedFiles">Dictionary mapping file paths to their new contents</param>
/// <param name="CreatedFiles">List of newly created file paths</param>
/// <param name="Summary">Summary of the implementation</param>
public record CodeImplementation(
    Dictionary<string, string> ModifiedFiles,
    List<string> CreatedFiles,
    string Summary
);
