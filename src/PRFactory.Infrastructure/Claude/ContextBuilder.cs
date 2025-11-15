using System.Text;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents.Base;

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

    /// <summary>
    /// Build context for API design by extracting existing API patterns
    /// </summary>
    Task<string> BuildApiDesignContextAsync(object repository, string repositoryPath, CancellationToken cancellationToken);

    /// <summary>
    /// Build context for database schema design by extracting existing schema
    /// </summary>
    Task<string> BuildDatabaseSchemaContextAsync(object repository, string repositoryPath, CancellationToken cancellationToken);
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

        var ticketId = (string)ticket.Id;
        _logger.LogInformation("Building analysis context for ticket {TicketId}", ticketId);

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

        var ticketId = (string)ticket.Id;
        _logger.LogInformation("Building planning context for ticket {TicketId}", ticketId);

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
                // Find answer matching this question
                dynamic? answer = null;
                if (ticket.Answers != null)
                {
                    foreach (var a in ticket.Answers)
                    {
                        if (a.QuestionId == question.Id)
                        {
                            answer = a;
                            break;
                        }
                    }
                }
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

        var ticketId = (string)ticket.Id;
        _logger.LogInformation("Building implementation context for ticket {TicketId}", ticketId);

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
    private static bool IsIgnoredDirectory(string name)
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
    public static string TruncateContent(string content, int maxLines)
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
    private static async Task<string?> TryReadFileAsync(string repoPath, string relativePath)
    {
        var fullPath = Path.Combine(repoPath, relativePath);
        if (!File.Exists(fullPath))
            return null;

        try
        {
            return await File.ReadAllTextAsync(fullPath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string> BuildApiDesignContextAsync(object repository, string repositoryPath, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        _logger.LogInformation("Building API design context for repository at {RepositoryPath}", repositoryPath);

        sb.AppendLine("## Existing API Patterns");
        sb.AppendLine();

        // Find existing controllers
        var controllerFiles = Directory.GetFiles(
            repositoryPath,
            "*Controller.cs",
            SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/"))
            .ToArray();

        if (controllerFiles.Length == 0)
        {
            sb.AppendLine("No existing API controllers found.");
            return sb.ToString();
        }

        sb.AppendLine($"Found {controllerFiles.Length} existing controllers:");
        sb.AppendLine();

        // Extract patterns from first 3 controllers (avoid context overload)
        foreach (var controllerFile in controllerFiles.Take(3))
        {
            var fileName = Path.GetFileName(controllerFile);
            var content = await File.ReadAllTextAsync(controllerFile, cancellationToken);

            sb.AppendLine($"### {fileName}");
            sb.AppendLine();

            // Extract route patterns
            var routePattern = @"\[Route\(""([^""]+)""\)\]";
            var routeMatches = System.Text.RegularExpressions.Regex.Matches(content, routePattern);
            if (routeMatches.Count > 0)
            {
                sb.AppendLine("Routes:");
                foreach (System.Text.RegularExpressions.Match match in routeMatches)
                {
                    sb.AppendLine($"- {match.Groups[1].Value}");
                }
                sb.AppendLine();
            }

            // Extract HTTP method patterns
            var httpMethodPattern = @"\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\(""?([^""\]]*?)""?\)\]";
            var methodMatches = System.Text.RegularExpressions.Regex.Matches(content, httpMethodPattern);
            if (methodMatches.Count > 0)
            {
                sb.AppendLine("Endpoints:");
                foreach (System.Text.RegularExpressions.Match match in methodMatches)
                {
                    var method = match.Groups[1].Value;
                    var path = match.Groups[2].Value;
                    sb.AppendLine($"- {method}: {path}");
                }
                sb.AppendLine();
            }

            // Extract DTOs used
            var dtoPattern = @"Task<ActionResult<(\w+)>>";
            var dtoMatches = System.Text.RegularExpressions.Regex.Matches(content, dtoPattern);
            if (dtoMatches.Count > 0)
            {
                sb.AppendLine("Response DTOs:");
                var dtos = dtoMatches.Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Groups[1].Value)
                    .Distinct();
                foreach (var dto in dtos)
                {
                    sb.AppendLine($"- {dto}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task<string> BuildDatabaseSchemaContextAsync(object repository, string repositoryPath, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        _logger.LogInformation("Building database schema context for repository at {RepositoryPath}", repositoryPath);

        sb.AppendLine("## Existing Database Schema");
        sb.AppendLine();

        // Find Entity files
        var entityFiles = Directory.GetFiles(
            repositoryPath,
            "*.cs",
            SearchOption.AllDirectories)
            .Where(f => f.Contains("/Entities/") && !f.Contains("/bin/") && !f.Contains("/obj/"))
            .ToArray();

        if (entityFiles.Length > 0)
        {
            sb.AppendLine($"### Entity Classes ({entityFiles.Length} found)");
            foreach (var entityFile in entityFiles.Take(10))
            {
                var fileName = Path.GetFileName(entityFile);
                sb.AppendLine($"- {fileName}");
            }
            sb.AppendLine();
        }

        // Find DbContext file
        var dbContextFiles = Directory.GetFiles(
            repositoryPath,
            "*DbContext.cs",
            SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/"))
            .ToArray();

        if (dbContextFiles.Length > 0)
        {
            var dbContextFile = dbContextFiles[0];
            var fileName = Path.GetFileName(dbContextFile);
            var content = await File.ReadAllTextAsync(dbContextFile, cancellationToken);

            sb.AppendLine($"### {fileName}");
            sb.AppendLine();

            // Extract DbSet declarations
            var dbSetPattern = @"public\s+DbSet<(\w+)>\s+(\w+)";
            var dbSetMatches = System.Text.RegularExpressions.Regex.Matches(content, dbSetPattern);
            if (dbSetMatches.Count > 0)
            {
                sb.AppendLine("Database Tables (DbSets):");
                foreach (System.Text.RegularExpressions.Match match in dbSetMatches)
                {
                    var entityType = match.Groups[1].Value;
                    var tableName = match.Groups[2].Value;
                    sb.AppendLine($"- {tableName} (Entity: {entityType})");
                }
                sb.AppendLine();
            }
        }

        // Find migration files
        var migrationFiles = Directory.GetFiles(
            repositoryPath,
            "*_*.cs",
            SearchOption.AllDirectories)
            .Where(f => f.Contains("/Migrations/") && !f.Contains("Designer.cs") && !f.Contains("Snapshot.cs") &&
                        !f.Contains("/bin/") && !f.Contains("/obj/"))
            .OrderByDescending(f => f)
            .ToArray();

        if (migrationFiles.Length > 0)
        {
            sb.AppendLine($"### Recent Migrations ({migrationFiles.Length} total, showing last 3)");
            foreach (var migrationFile in migrationFiles.Take(3))
            {
                var fileName = Path.GetFileName(migrationFile);
                sb.AppendLine($"- {fileName}");
            }
            sb.AppendLine();
        }

        // Find configuration files
        var configFiles = Directory.GetFiles(
            repositoryPath,
            "*Configuration.cs",
            SearchOption.AllDirectories)
            .Where(f => f.Contains("/Configurations/") && !f.Contains("/bin/") && !f.Contains("/obj/"))
            .ToArray();

        if (configFiles.Length > 0)
        {
            sb.AppendLine($"### Entity Configurations ({configFiles.Length} found)");
            foreach (var configFile in configFiles.Take(5))
            {
                var fileName = Path.GetFileName(configFile);
                sb.AppendLine($"- {fileName}");
            }
            sb.AppendLine();
        }

        if (entityFiles.Length == 0 && dbContextFiles.Length == 0 && migrationFiles.Length == 0)
        {
            sb.AppendLine("No existing database schema found.");
        }

        return sb.ToString();
    }
}
