using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Implementation of checklist template service using YAML files
/// Templates are loaded from the config/checklists directory
/// </summary>
public class ChecklistTemplateService : IChecklistTemplateService
{
    private readonly string _templateBasePath;
    private readonly ILogger<ChecklistTemplateService> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public ChecklistTemplateService(
        IConfiguration configuration,
        ILogger<ChecklistTemplateService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Get template path from configuration or use default
        _templateBasePath = configuration["ChecklistTemplatesPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "config", "checklists");

        // Create YAML deserializer with snake_case naming convention
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        _logger.LogInformation("ChecklistTemplateService initialized with template path: {TemplatePath}", _templateBasePath);
    }

    public async Task<ChecklistTemplate> LoadTemplateAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain is required", nameof(domain));

        var filePath = Path.Combine(_templateBasePath, $"{domain}.yaml");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Checklist template not found: {Domain} at path: {FilePath}", domain, filePath);
            throw new FileNotFoundException($"Template not found for domain: {domain}", filePath);
        }

        try
        {
            var yaml = await File.ReadAllTextAsync(filePath, cancellationToken);
            var template = _yamlDeserializer.Deserialize<ChecklistTemplate>(yaml);

            if (template == null)
            {
                throw new InvalidOperationException($"Failed to deserialize template: {domain}");
            }

            _logger.LogInformation("Loaded checklist template: {Domain} with {CategoryCount} categories",
                domain, template.Categories?.Count ?? 0);

            return template;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(ex, "Error loading checklist template: {Domain} from {FilePath}", domain, filePath);
            throw new InvalidOperationException($"Error loading template: {domain}", ex);
        }
    }

    public async Task<List<ChecklistTemplateMetadata>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = new List<ChecklistTemplateMetadata>();

        if (!Directory.Exists(_templateBasePath))
        {
            _logger.LogWarning("Template directory does not exist: {TemplatePath}", _templateBasePath);
            return templates;
        }

        var files = Directory.GetFiles(_templateBasePath, "*.yaml");
        _logger.LogInformation("Found {Count} template files in {TemplatePath}", files.Length, _templateBasePath);

        foreach (var file in files)
        {
            try
            {
                var yaml = await File.ReadAllTextAsync(file, cancellationToken);
                var template = _yamlDeserializer.Deserialize<ChecklistTemplate>(yaml);

                if (template != null)
                {
                    templates.Add(new ChecklistTemplateMetadata
                    {
                        Domain = template.Domain,
                        Name = template.Name,
                        Version = template.Version
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load template metadata from file: {File}", file);
                // Continue processing other files
            }
        }

        return templates;
    }

    public ReviewChecklist CreateChecklistFromTemplate(Guid planReviewId, ChecklistTemplate template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        var items = new List<ChecklistItem>();
        int sortOrder = 0;

        foreach (var category in template.Categories ?? new List<ChecklistCategory>())
        {
            foreach (var templateItem in category.Items ?? new List<ChecklistTemplateItem>())
            {
                var item = ChecklistItem.Create(
                    category.Name,
                    templateItem.Title,
                    templateItem.Description,
                    templateItem.Severity,
                    templateItem.SortOrder > 0 ? templateItem.SortOrder : sortOrder++);

                items.Add(item);
            }
        }

        var checklist = ReviewChecklist.Create(planReviewId, template.Name, items);

        _logger.LogInformation("Created checklist from template {TemplateName} with {ItemCount} items",
            template.Name, items.Count);

        return checklist;
    }
}
