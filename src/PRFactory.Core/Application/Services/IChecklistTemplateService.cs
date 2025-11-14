using PRFactory.Domain.Entities;

namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for loading and managing review checklist templates
/// Templates are stored as YAML files in the config/checklists directory
/// </summary>
public interface IChecklistTemplateService
{
    /// <summary>
    /// Load a checklist template from YAML file by domain name
    /// </summary>
    /// <param name="domain">Domain identifier (e.g., "web_ui", "rest_api", "database")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed checklist template</returns>
    Task<ChecklistTemplate> LoadTemplateAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get metadata for all available templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of template metadata</returns>
    Task<List<ChecklistTemplateMetadata>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a ReviewChecklist entity from a template
    /// </summary>
    /// <param name="planReviewId">The plan review ID to associate with</param>
    /// <param name="template">The template to instantiate</param>
    /// <returns>ReviewChecklist entity ready to be persisted</returns>
    ReviewChecklist CreateChecklistFromTemplate(Guid planReviewId, ChecklistTemplate template);
}

/// <summary>
/// Represents a parsed checklist template
/// </summary>
public class ChecklistTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<ChecklistCategory> Categories { get; set; } = new();
}

/// <summary>
/// Represents a category within a checklist template
/// </summary>
public class ChecklistCategory
{
    public string Name { get; set; } = string.Empty;
    public List<ChecklistTemplateItem> Items { get; set; } = new();
}

/// <summary>
/// Represents an individual item in a checklist template
/// </summary>
public class ChecklistTemplateItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "recommended";
    public int SortOrder { get; set; }
}

/// <summary>
/// Metadata about an available checklist template
/// </summary>
public class ChecklistTemplateMetadata
{
    public string Domain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
