namespace PRFactory.Web.Models;

/// <summary>
/// DTO for review checklist data transfer to UI
/// </summary>
public class ReviewChecklistDto
{
    public Guid Id { get; set; }
    public Guid PlanReviewId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public List<ChecklistItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Calculate completion percentage (0-100)
    /// </summary>
    public int CompletionPercentage =>
        Items.Any() ? (Items.Count(i => i.IsChecked) * 100) / Items.Count : 0;

    /// <summary>
    /// Check if all required items are checked
    /// </summary>
    public bool AllRequiredItemsChecked =>
        Items.Where(i => i.Severity == "required").All(i => i.IsChecked);
}

/// <summary>
/// DTO for individual checklist item
/// </summary>
public class ChecklistItemDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "recommended";
    public bool IsChecked { get; set; }
    public DateTime? CheckedAt { get; set; }
    public int SortOrder { get; set; }
}
