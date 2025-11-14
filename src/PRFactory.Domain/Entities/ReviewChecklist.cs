namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a structured checklist for plan review evaluation
/// Domain-specific checklists ensure consistent review quality
/// </summary>
public class ReviewChecklist
{
    public Guid Id { get; private set; }
    public Guid PlanReviewId { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public PlanReview PlanReview { get; private set; } = null!;
    public ICollection<ChecklistItem> Items { get; private set; } = new List<ChecklistItem>();

    // EF Core constructor
    private ReviewChecklist() { }

    /// <summary>
    /// Creates a new review checklist from a template
    /// </summary>
    public static ReviewChecklist Create(
        Guid planReviewId,
        string templateName,
        List<ChecklistItem> items)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        var checklist = new ReviewChecklist
        {
            Id = Guid.NewGuid(),
            PlanReviewId = planReviewId,
            TemplateName = templateName,
            CreatedAt = DateTime.UtcNow
        };

        // Set parent reference for all items
        foreach (var item in items)
        {
            item.SetChecklist(checklist.Id);
        }

        checklist.Items = items;
        return checklist;
    }

    /// <summary>
    /// Calculate completion percentage (0-100)
    /// </summary>
    public int CompletionPercentage()
    {
        if (!Items.Any())
            return 0;

        var checkedCount = Items.Count(i => i.IsChecked);
        return (checkedCount * 100) / Items.Count;
    }

    /// <summary>
    /// Check if all required items are checked
    /// Used to validate approval eligibility
    /// </summary>
    public bool AllRequiredItemsChecked()
    {
        var requiredItems = Items.Where(i => i.Severity == "required").ToList();
        if (!requiredItems.Any())
            return true; // No required items = validation passes

        return requiredItems.All(i => i.IsChecked);
    }
}
