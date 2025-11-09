using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PRFactory.Infrastructure.Agents.Services;

/// <summary>
/// Service responsible for loading agent prompts from .claude/agents folder
/// and populating them in the database as system templates.
/// </summary>
public class AgentPromptLoaderService
{
    private readonly IAgentPromptTemplateRepository _repository;
    private readonly ILogger<AgentPromptLoaderService> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public AgentPromptLoaderService(
        IAgentPromptTemplateRepository repository,
        ILogger<AgentPromptLoaderService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Loads all agent prompts from the specified directory and creates/updates system templates
    /// </summary>
    /// <param name="agentsDirectory">Path to .claude/agents directory</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of templates loaded</returns>
    public async Task<int> LoadAgentPromptsAsync(string agentsDirectory, CancellationToken ct = default)
    {
        if (!Directory.Exists(agentsDirectory))
        {
            _logger.LogWarning("Agents directory not found: {Directory}", agentsDirectory);
            return 0;
        }

        var markdownFiles = Directory.GetFiles(agentsDirectory, "*.md");
        _logger.LogInformation("Found {Count} agent prompt files in {Directory}", markdownFiles.Length, agentsDirectory);

        var loadedCount = 0;
        var templates = new List<AgentPromptTemplate>();

        foreach (var filePath in markdownFiles)
        {
            try
            {
                var template = await ParseAgentPromptFileAsync(filePath, ct);
                if (template != null)
                {
                    templates.Add(template);
                    loadedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load agent prompt from file: {FilePath}", filePath);
            }
        }

        // Bulk insert templates
        if (templates.Any())
        {
            await _repository.AddRangeAsync(templates, ct);
            _logger.LogInformation("Successfully loaded {Count} agent prompt templates", loadedCount);
        }

        return loadedCount;
    }

    /// <summary>
    /// Parses a single agent prompt markdown file with YAML frontmatter
    /// </summary>
    private async Task<AgentPromptTemplate?> ParseAgentPromptFileAsync(string filePath, CancellationToken ct)
    {
        var content = await File.ReadAllTextAsync(filePath, ct);

        // Extract YAML frontmatter
        var frontmatter = ExtractYamlFrontmatter(content, out var promptContent);
        if (frontmatter == null)
        {
            _logger.LogWarning("No YAML frontmatter found in file: {FilePath}", filePath);
            return null;
        }

        try
        {
            var metadata = _yamlDeserializer.Deserialize<AgentMetadata>(frontmatter);

            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                _logger.LogWarning("Agent name is missing in file: {FilePath}", filePath);
                return null;
            }

            // Check if template already exists
            var existing = await _repository.GetByNameAsync(metadata.Name, tenantId: null, ct);
            if (existing != null)
            {
                _logger.LogInformation("Template {Name} already exists, skipping", metadata.Name);
                return null;
            }

            // Determine category from description or use a default
            var category = DetermineCategory(metadata.Description);

            var template = AgentPromptTemplate.CreateSystemTemplate(
                name: metadata.Name,
                description: metadata.Description ?? "No description provided",
                promptContent: promptContent,
                category: category,
                recommendedModel: metadata.Model,
                color: metadata.Color
            );

            _logger.LogDebug("Parsed template {Name} from {FilePath}", metadata.Name, filePath);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse YAML frontmatter in file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Extracts YAML frontmatter from markdown content
    /// </summary>
    private string? ExtractYamlFrontmatter(string content, out string remainingContent)
    {
        remainingContent = content;

        if (!content.StartsWith("---"))
            return null;

        var lines = content.Split('\n');
        var frontmatterLines = new List<string>();
        var inFrontmatter = false;
        var frontmatterEndIndex = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (i == 0 && line == "---")
            {
                inFrontmatter = true;
                continue;
            }

            if (inFrontmatter && line == "---")
            {
                frontmatterEndIndex = i;
                break;
            }

            if (inFrontmatter)
            {
                frontmatterLines.Add(lines[i]);
            }
        }

        if (frontmatterEndIndex > 0)
        {
            // Extract remaining content after frontmatter
            var remainingLines = lines.Skip(frontmatterEndIndex + 1);
            remainingContent = string.Join('\n', remainingLines).Trim();
            return string.Join('\n', frontmatterLines);
        }

        return null;
    }

    /// <summary>
    /// Determines the category based on the description or name
    /// </summary>
    private string DetermineCategory(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "General";

        var lowerDesc = description.ToLowerInvariant();

        if (lowerDesc.Contains("test"))
            return "Testing";
        if (lowerDesc.Contains("implement") || lowerDesc.Contains("coding"))
            return "Implementation";
        if (lowerDesc.Contains("plan") || lowerDesc.Contains("design"))
            return "Planning";
        if (lowerDesc.Contains("analysis") || lowerDesc.Contains("analyz"))
            return "Analysis";
        if (lowerDesc.Contains("evaluat") || lowerDesc.Contains("quality"))
            return "Evaluation";
        if (lowerDesc.Contains("fix") || lowerDesc.Contains("debug"))
            return "Debugging";

        return "General";
    }
}

/// <summary>
/// Metadata extracted from YAML frontmatter in agent prompt files
/// </summary>
public class AgentMetadata
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
}
