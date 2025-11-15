using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Analysis;

/// <summary>
/// Search codebase with semantic context (lines before/after matches).
/// </summary>
public class CodeSearchTool : ToolBase
{
    private const int ContextLines = 5;
    private const int MaxResults = 100;

    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "CodeSearch";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Search codebase with semantic context (5 lines before/after matches)";

    /// <summary>
    /// Create a new CodeSearchTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public CodeSearchTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext)
    {
    }

    /// <summary>
    /// Execute the code search
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Search results with context</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var repositoryPath = context.GetParameter<string>("repositoryPath");
        var query = context.GetParameter<string>("query");
        var filePattern = context.GetOptionalParameter<string>("filePattern", "*.*") ?? "*.*";
        var caseSensitive = context.GetOptionalParameter<bool>("caseSensitive", false);

        // 1. Validate repository path
        var fullPath = PathValidator.ValidateAndResolve(repositoryPath, context.WorkspacePath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Repository path '{repositoryPath}' does not exist");
        }

        // 2. Compile regex
        var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        var regex = new Regex(query, regexOptions);

        // 3. Search files with context
        var results = new List<SearchResult>();
        var files = Directory.GetFiles(fullPath, filePattern, SearchOption.AllDirectories);

        foreach (var file in files)
        {
            // Skip large files
            var fileInfo = new FileInfo(file);
            if (fileInfo.Length > ResourceLimits.MaxFileSize)
                continue;

            // Skip binary files and common non-code directories
            var relativePath = Path.GetRelativePath(context.WorkspacePath, file);
            if (ShouldSkipFile(relativePath))
                continue;

            var lines = await File.ReadAllLinesAsync(file);

            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    // Extract context (5 lines before and after)
                    var startLine = Math.Max(0, i - ContextLines);
                    var endLine = Math.Min(lines.Length - 1, i + ContextLines);

                    var contextLines = new List<string>();
                    for (int j = startLine; j <= endLine; j++)
                    {
                        var prefix = j == i ? ">>>" : "   ";
                        contextLines.Add($"{prefix} {j + 1:D4}: {lines[j]}");
                    }

                    results.Add(new SearchResult
                    {
                        FilePath = relativePath,
                        LineNumber = i + 1,
                        MatchedLine = lines[i],
                        Context = string.Join(Environment.NewLine, contextLines)
                    });

                    if (results.Count >= MaxResults)
                    {
                        goto Done;
                    }
                }
            }
        }

    Done:
        _logger.LogInformation(
            "Code search found {Count} matches for query '{Query}' in {FileCount} files",
            results.Count, query, files.Length);

        if (results.Count == 0)
        {
            return $"No matches found for query: {query}";
        }

        return FormatResults(results, results.Count >= MaxResults);
    }

    /// <summary>
    /// Validate input parameters
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Task</returns>
    protected override Task ValidateInputAsync(ToolExecutionContext context)
    {
        if (!context.Parameters.ContainsKey("repositoryPath"))
            throw new ArgumentException("Parameter 'repositoryPath' is required");

        if (!context.Parameters.ContainsKey("query"))
            throw new ArgumentException("Parameter 'query' is required");

        // Validate regex pattern
        try
        {
            var query = context.GetParameter<string>("query");
            _ = new Regex(query);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid regex pattern: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check if file should be skipped
    /// </summary>
    private static bool ShouldSkipFile(string relativePath)
    {
        var skipPatterns = new[]
        {
            "bin/", "obj/", ".git/", ".vs/", "node_modules/",
            "packages/", "dist/", "build/", ".idea/", "out/"
        };

        var normalizedPath = relativePath.Replace("\\", "/");
        return skipPatterns.Any(pattern => normalizedPath.Contains(pattern));
    }

    /// <summary>
    /// Format search results
    /// </summary>
    private static string FormatResults(List<SearchResult> results, bool truncated)
    {
        var output = new StringBuilder();
        output.AppendLine($"Found {results.Count} match(es):");
        output.AppendLine();

        foreach (var result in results)
        {
            output.AppendLine($"=== {result.FilePath}:{result.LineNumber} ===");
            output.AppendLine(result.Context);
            output.AppendLine();
        }

        if (truncated)
        {
            output.AppendLine($"... (truncated, max {MaxResults} results shown)");
        }

        return output.ToString();
    }

    private class SearchResult
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string MatchedLine { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }
}
