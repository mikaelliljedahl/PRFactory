using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PRFactory.AgentTools.Core;
using PRFactory.AgentTools.Security;
using PRFactory.Core.Application.Services;

namespace PRFactory.AgentTools.Search;

/// <summary>
/// Search for pattern in files using regex.
/// </summary>
public class GrepTool : ToolBase
{
    /// <summary>
    /// Tool name
    /// </summary>
    public override string Name => "Grep";

    /// <summary>
    /// Tool description
    /// </summary>
    public override string Description => "Search for a regex pattern in files";

    /// <summary>
    /// Create a new GrepTool instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tenantContext">Tenant context</param>
    public GrepTool(ILogger<ToolBase> logger, ITenantContext tenantContext)
        : base(logger, tenantContext) { }

    /// <summary>
    /// Execute the grep search
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <returns>Search results (file:line:content format)</returns>
    protected override async Task<string> ExecuteToolAsync(ToolExecutionContext context)
    {
        var pattern = context.GetParameter<string>("pattern");
        var directory = context.GetOptionalParameter<string>("directory", ".") ?? ".";
        var filePattern = context.GetOptionalParameter<string>("filePattern", "*.*") ?? "*.*";
        var caseSensitive = context.GetOptionalParameter<bool>("caseSensitive", false);

        // 1. Validate directory
        var fullPath = PathValidator.ValidateAndResolve(directory, context.WorkspacePath);

        // 2. Compile regex
        var regexOptions = caseSensitive
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;
        var regex = new Regex(pattern, regexOptions);

        // 3. Search files
        var results = new List<string>();
        var files = Directory.GetFiles(fullPath, filePattern, SearchOption.AllDirectories);

        foreach (var file in files)
        {
            // Skip large files
            if (new FileInfo(file).Length > ResourceLimits.MaxFileSize)
                continue;

            var lines = await File.ReadAllLinesAsync(file);
            var relativePath = Path.GetRelativePath(context.WorkspacePath, file);

            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    results.Add($"{relativePath}:{i + 1}:{lines[i]}");

                    if (results.Count >= ResourceLimits.MaxResultLines)
                    {
                        results.Add($"... (truncated, max {ResourceLimits.MaxResultLines} results)");
                        goto Done;
                    }
                }
            }
        }

    Done:
        _logger.LogInformation(
            "Grep found {Count} matches for pattern '{Pattern}' in {FileCount} files",
            results.Count, pattern, files.Length);

        return string.Join(Environment.NewLine, results);
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
