using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Agents.Services;

/// <summary>
/// Provides architectural context for AI planning prompts by extracting
/// patterns, technology stack, and code snippets from the codebase.
/// </summary>
public class ArchitectureContextService : IArchitectureContextService
{
    private readonly ILogger<ArchitectureContextService> _logger;

    public ArchitectureContextService(ILogger<ArchitectureContextService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetArchitecturePatternsAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path is required", nameof(repositoryPath));

        try
        {
            var architectureDocPath = Path.Combine(repositoryPath, "docs", "ARCHITECTURE.md");

            if (File.Exists(architectureDocPath))
            {
                _logger.LogInformation("Reading architecture patterns from {Path}", architectureDocPath);
                var content = await File.ReadAllTextAsync(architectureDocPath, cancellationToken);

                // Extract key sections (simple approach - return first 2000 characters)
                var patterns = content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;
                return patterns;
            }
            else
            {
                _logger.LogWarning("ARCHITECTURE.md not found at {Path}, using default patterns", architectureDocPath);
                return GetDefaultArchitecturePatterns();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read architecture patterns from {Path}", repositoryPath);
            return GetDefaultArchitecturePatterns();
        }
    }

    public string GetTechnologyStack()
    {
        return @"
- .NET 10 with C# 13
- Entity Framework Core (Code First)
- Blazor Server (NOT WebAssembly)
- Radzen.Blazor v5.9.0 for UI components
- Bootstrap 5 for CSS styling
- LibGit2Sharp for git operations
- Markdig for markdown rendering
- Hangfire for background jobs
- SQLite for data storage (development)
        ".Trim();
    }

    public string GetCodeStyleGuidelines()
    {
        return @"
- UTF-8 encoding WITHOUT BOM (mandatory - CI will fail with BOM)
- File-scoped namespaces: namespace Foo.Bar;
- 4 spaces for indentation (no tabs)
- Use var for obvious types
- Code-behind pattern for Pages and business Components (.razor.cs files)
- Max line length: 120 characters
- Private setters on domain entities
- Factory methods for entity creation
        ".Trim();
    }

    public async Task<List<CodeSnippet>> GetRelevantCodeSnippetsAsync(
        string repositoryPath,
        string ticketDescription,
        int maxSnippets = 3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path is required", nameof(repositoryPath));

        var snippets = new List<CodeSnippet>();

        try
        {
            // Extract keywords from ticket description
            var keywords = ExtractKeywords(ticketDescription);

            _logger.LogInformation("Searching for code snippets with keywords: {Keywords}", string.Join(", ", keywords));

            // Search for relevant files based on keywords
            var relevantFiles = await FindRelevantFilesAsync(repositoryPath, keywords, cancellationToken);

            foreach (var file in relevantFiles.Take(maxSnippets))
            {
                var snippet = await CreateCodeSnippetAsync(repositoryPath, file, cancellationToken);
                if (snippet != null)
                {
                    snippets.Add(snippet);
                }
            }

            _logger.LogInformation("Found {Count} relevant code snippets", snippets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get relevant code snippets from {Path}", repositoryPath);
        }

        return snippets;
    }

    private string GetDefaultArchitecturePatterns()
    {
        return @"
# Clean Architecture

This project follows Clean Architecture with the following layers:

- **Domain Layer** (`PRFactory.Domain`): Entities, value objects, domain logic
  - Entities have private setters and factory methods
  - Domain events for state changes
  - Repository interfaces

- **Application Layer** (`PRFactory.Core`): Interfaces, DTOs, application services
  - Service interfaces
  - DTOs for data transfer
  - Application-level validation

- **Infrastructure Layer** (`PRFactory.Infrastructure`): Implementations
  - Repository implementations
  - EF Core DbContext and configurations
  - External service integrations
  - Background jobs

- **Web Layer** (`PRFactory.Web`): Blazor Server UI
  - Pages (routable components)
  - Components (business logic)
  - UI components (pure presentation)

Key patterns:
- Repository pattern for data access
- Service layer for business logic
- DTO mapping between layers
- Dependency injection throughout
        ".Trim();
    }

    private List<string> ExtractKeywords(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return new List<string>();

        var keywords = new List<string>();
        var words = description.ToLower()
            .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        // Common technical keywords to look for
        var technicalTerms = new HashSet<string>
        {
            "ticket", "repository", "tenant", "workflow", "agent", "plan", "review",
            "blazor", "component", "page", "entity", "service", "controller",
            "database", "migration", "configuration", "job", "background"
        };

        foreach (var word in words)
        {
            if (technicalTerms.Contains(word) || word.EndsWith("service") || word.EndsWith("repository"))
            {
                keywords.Add(word);
            }
        }

        return keywords.Distinct().Take(5).ToList();
    }

    private async Task<List<string>> FindRelevantFilesAsync(
        string repositoryPath,
        List<string> keywords,
        CancellationToken cancellationToken)
    {
        var relevantFiles = new List<string>();

        try
        {
            var srcPath = Path.Combine(repositoryPath, "src");
            if (!Directory.Exists(srcPath))
            {
                _logger.LogWarning("Source directory not found at {Path}", srcPath);
                return relevantFiles;
            }

            // Search for C# files in src directory
            var allFiles = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                // Skip test files, migrations, and generated files
                var fileName = Path.GetFileName(file);
                if (fileName.Contains("Tests") || fileName.Contains("Migration") ||
                    fileName.EndsWith(".Designer.cs") || fileName.EndsWith(".g.cs"))
                {
                    continue;
                }

                // Check if file name or path contains any keywords
                var filePath = file.Replace(repositoryPath, "").Replace("\\", "/").TrimStart('/');
                var fileNameLower = fileName.ToLower();

                if (keywords.Any(k => fileNameLower.Contains(k) || filePath.ToLower().Contains(k)))
                {
                    relevantFiles.Add(file);
                }
            }

            // Prioritize domain entities, services, and components
            relevantFiles = relevantFiles
                .OrderBy(f => f.Contains("Domain\\Entities") ? 0 :
                             f.Contains("Application") ? 1 :
                             f.Contains("Components") ? 2 : 3)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding relevant files in {Path}", repositoryPath);
        }

        return relevantFiles;
    }

    private async Task<CodeSnippet?> CreateCodeSnippetAsync(
        string repositoryPath,
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            // Take first 50 lines or entire file if smaller
            var codeLines = lines.Take(50).ToArray();
            var code = string.Join("\n", codeLines);

            var relativePath = filePath.Replace(repositoryPath, "").Replace("\\", "/").TrimStart('/');

            return new CodeSnippet
            {
                FilePath = relativePath,
                Language = "csharp",
                Code = code,
                Description = $"Example from {Path.GetFileName(filePath)}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read code snippet from {Path}", filePath);
            return null;
        }
    }
}
