namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents an individual item in a review checklist
/// Can be "required" or "recommended" severity
/// </summary>
public class ChecklistItem
{
    public Guid Id { get; private set; }
    public Guid ReviewChecklistId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Severity { get; private set; } = "recommended";
    public bool IsChecked { get; private set; }
    public DateTime? CheckedAt { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation property
    public ReviewChecklist ReviewChecklist { get; private set; } = null!;

    // EF Core constructor
    private ChecklistItem() { }

    /// <summary>
    /// Creates a new checklist item
    /// </summary>
    public static ChecklistItem Create(
        string category,
        string title,
        string description,
        string severity,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required", nameof(category));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (!new[] { "required", "recommended" }.Contains(severity))
            throw new ArgumentException("Severity must be 'required' or 'recommended'", nameof(severity));

        return new ChecklistItem
        {
            Id = Guid.NewGuid(),
            Category = category,
            Title = title,
            Description = description,
            Severity = severity,
            SortOrder = sortOrder,
            IsChecked = false
        };
    }

    /// <summary>
    /// Mark this item as checked
    /// </summary>
    public void Check()
    {
        IsChecked = true;
        CheckedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark this item as unchecked
    /// </summary>
    public void Uncheck()
    {
        IsChecked = false;
        CheckedAt = null;
    }

    /// <summary>
    /// Internal method to set parent checklist reference
    /// Called by ReviewChecklist.Create()
    /// </summary>
    internal void SetChecklist(Guid checklistId)
    {
        ReviewChecklistId = checklistId;
    }
}
