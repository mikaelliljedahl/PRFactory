using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Search;

/// <summary>
/// Search and replace text in files using regex.
/// </summary>
public class SearchReplaceTool : ToolBase
{
    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "SearchReplace";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Find and replace text in files using regex";

    /// <summary>
    /// Create a new SearchReplaceTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public SearchReplaceTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    /// <summary>
    /// Execute the search and replace operation
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Result message indicating number of replacements</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var pattern = context.GetParameter<string>("pattern");
        var replacement = context.GetParameter<string>("replacement");
        var filePath = context.GetParameter<string>("filePath");
        var caseSensitive = context.GetOptionalParameter<bool>("caseSensitive", false);

        // 1. Validate path
        var fullPath = PathValidator.ValidateAndResolve(filePath, context.WorkspacePath);

        // 2. Check file exists
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // 3. Read file
        var content = await File.ReadAllTextAsync(fullPath);

        // 4. Perform replacement
        var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        var regex = new Regex(pattern, regexOptions);
        var matches = regex.Matches(content);
        var newContent = regex.Replace(content, replacement);

        // 5. Check content size
        ResourceLimits.ValidateContentSize(newContent);

        // 6. Write file atomically (if changes)
        if (content != newContent)
        {
            var tempPath = Path.Combine(Path.GetDirectoryName(fullPath)!, $".{Guid.NewGuid()}.tmp");
            try
            {
                await File.WriteAllTextAsync(tempPath, newContent);
                File.Move(tempPath, fullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }

            _logger.LogInformation(
                "Replaced {Count} occurrences of '{Pattern}' in {FilePath}",
                matches.Count, pattern, filePath);

            return $"Replaced {matches.Count} occurrences in {filePath}";
        }

        return $"No matches found for pattern '{pattern}' in {filePath}";
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are missing or invalid</exception>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("pattern"))
            throw new ArgumentException("Parameter 'pattern' is required");

        if (!context.Parameters.ContainsKey("replacement"))
            throw new ArgumentException("Parameter 'replacement' is required");

        if (!context.Parameters.ContainsKey("filePath"))
            throw new ArgumentException("Parameter 'filePath' is required");

        // Validate regex pattern
        try
        {
            var pattern = context.GetParameter<string>("pattern");
            _ = new Regex(pattern);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid regex pattern: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }
}
