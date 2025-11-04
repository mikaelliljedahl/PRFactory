using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Claude.Models;

namespace PRFactory.Infrastructure.Claude;

/// <summary>
/// Interface for building context for Claude API calls
/// </summary>
public interface IContextBuilder
{
    /// <summary>
    /// Build context for codebase analysis
    /// </summary>
    Task<string> BuildAnalysisContextAsync(dynamic ticket, string repoPath);

    /// <summary>
    /// Build context for implementation planning
    /// </summary>
    Task<string> BuildPlanningContextAsync(dynamic ticket, CodebaseAnalysis analysis);

    /// <summary>
    /// Build context for code implementation
    /// </summary>
    Task<string> BuildImplementationContextAsync(dynamic ticket, string repoPath);
}

/// <summary>
/// Builds optimized context for Claude based on ticket and codebase
/// </summary>
public class ContextBuilder : IContextBuilder
{
    private readonly ILogger<ContextBuilder> _logger;

    public ContextBuilder(ILogger<ContextBuilder> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> BuildAnalysisContextAsync(dynamic ticket, string repoPath)
    {
        var sb = new StringBuilder();

        _logger.LogInformation("Building analysis context for ticket {TicketId}", ticket.Id);

        // Repository structure
        sb.AppendLine("## Repository Structure");
        sb.AppendLine("```");
        sb.AppendLine(await GetDirectoryTreeAsync(repoPath, maxDepth: 3));
        sb.AppendLine("```");
        sb.AppendLine();

        // Key files (README, architecture docs, etc.)
        sb.AppendLine("## Key Documentation");
        var readme = await TryReadFileAsync(repoPath, "README.md");
        if (readme != null)
        {
            sb.AppendLine("### README.md");
            sb.AppendLine(TruncateContent(readme, maxLines: 100));
            sb.AppendLine();
        }

        // Check for architecture docs
        var archDocs = await TryReadFileAsync(repoPath, "docs/architecture/overview.md");
        if (archDocs != null)
        {
            sb.AppendLine("### Architecture Overview");
            sb.AppendLine(TruncateContent(archDocs, maxLines: 50));
            sb.AppendLine();
        }

        // Ticket information
        sb.AppendLine("## Ticket Information");
        sb.AppendLine($"**Title**: {ticket.Title}");
        sb.AppendLine($"**Description**: {ticket.Description}");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task<string> BuildPlanningContextAsync(dynamic ticket, CodebaseAnalysis analysis)
    {
        var sb = new StringBuilder();

        _logger.LogInformation("Building planning context for ticket {TicketId}", ticket.Id);

        // Ticket & answers
        sb.AppendLine("## Ticket Information");
        sb.AppendLine($"**Title**: {ticket.Title}");
        sb.AppendLine($"**Description**: {ticket.Description}");
        sb.AppendLine();

        // Clarifying Questions & Answers (if available)
        if (ticket.Questions != null && ticket.Questions.Count > 0)
        {
            sb.AppendLine("## Clarifying Questions & Answers");
            foreach (var question in ticket.Questions)
            {
                var answer = ticket.Answers?.FirstOrDefault((dynamic a) => a.QuestionId == question.Id);
                sb.AppendLine($"**Q**: {question.Text}");
                sb.AppendLine($"**A**: {answer?.Text ?? "(No answer provided)"}");
                sb.AppendLine();
            }
        }

        // Codebase analysis
        sb.AppendLine("## Codebase Analysis");
        sb.AppendLine($"**Architecture**: {analysis.Architecture}");
        sb.AppendLine();

        if (analysis.Patterns.Any())
        {
            sb.AppendLine("**Patterns Identified**:");
            foreach (var pattern in analysis.Patterns)
            {
                sb.AppendLine($"- {pattern}");
            }
            sb.AppendLine();
        }

        if (analysis.Dependencies.Any())
        {
            sb.AppendLine("**Key Dependencies**:");
            foreach (var dep in analysis.Dependencies.Take(10))
            {
                sb.AppendLine($"- {dep}");
            }
            sb.AppendLine();
        }

        // Relevant Files
        sb.AppendLine("## Relevant Files");
        foreach (var file in analysis.RelevantFiles.Take(10))
        {
            sb.AppendLine($"### {file}");
            if (analysis.FileContents.TryGetValue(file, out var content))
            {
                sb.AppendLine("```");
                sb.AppendLine(TruncateContent(content, maxLines: 100));
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task<string> BuildImplementationContextAsync(dynamic ticket, string repoPath)
    {
        var sb = new StringBuilder();

        _logger.LogInformation("Building implementation context for ticket {TicketId}", ticket.Id);

        // Read the implementation plan from the branch
        var planPath = Path.Combine(repoPath, "IMPLEMENTATION_PLAN.md");
        if (File.Exists(planPath))
        {
            sb.AppendLine("## Approved Implementation Plan");
            sb.AppendLine(await File.ReadAllTextAsync(planPath));
            sb.AppendLine();
        }

        // Ticket information
        sb.AppendLine("## Ticket Information");
        sb.AppendLine($"**Title**: {ticket.Title}");
        sb.AppendLine($"**Description**: {ticket.Description}");
        sb.AppendLine();

        // Include relevant files mentioned in the plan
        sb.AppendLine("## Current Codebase");
        sb.AppendLine("(Relevant files will be included based on the implementation plan)");

        return sb.ToString();
    }

    /// <summary>
    /// Generate a tree view of directory structure
    /// </summary>
    private async Task<string> GetDirectoryTreeAsync(string path, int maxDepth, int currentDepth = 0)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', currentDepth * 2);

        if (!Directory.Exists(path))
        {
            _logger.LogWarning("Directory does not exist: {Path}", path);
            return "";
        }

        var dirInfo = new DirectoryInfo(path);
        sb.AppendLine($"{indent}{dirInfo.Name}/");

        if (currentDepth >= maxDepth) return sb.ToString();

        try
        {
            // Directories
            foreach (var dir in dirInfo.GetDirectories().Where(d => !IsIgnoredDirectory(d.Name)))
            {
                sb.Append(await GetDirectoryTreeAsync(dir.FullName, maxDepth, currentDepth + 1));
            }

            // Files (limit to 50 per directory)
            foreach (var file in dirInfo.GetFiles().Take(50))
            {
                sb.AppendLine($"{indent}  {file.Name}");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Path}", path);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Check if a directory should be ignored
    /// </summary>
    private bool IsIgnoredDirectory(string name)
    {
        var ignored = new[]
        {
            ".git", "node_modules", "bin", "obj", ".vs", "packages",
            ".idea", "dist", "build", "coverage", ".next", ".nuxt"
        };
        return ignored.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Truncate content to a maximum number of lines
    /// </summary>
    public string TruncateContent(string content, int maxLines)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var lines = content.Split('\n');
        if (lines.Length <= maxLines)
            return content;

        return string.Join('\n', lines.Take(maxLines)) + "\n... (truncated)";
    }

    /// <summary>
    /// Try to read a file, returning null if it doesn't exist or can't be read
    /// </summary>
    private async Task<string?> TryReadFileAsync(string repoPath, string relativePath)
    {
        var fullPath = Path.Combine(repoPath, relativePath);
        if (!File.Exists(fullPath))
            return null;

        try
        {
            return await File.ReadAllTextAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read file: {Path}", fullPath);
            return null;
        }
    }
}
