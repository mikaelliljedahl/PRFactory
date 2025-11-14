using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.UI.Checklists;

/// <summary>
/// UI component for displaying and managing review checklists
/// Supports collapsible categories and progress tracking
/// </summary>
public partial class ReviewChecklistPanel
{
    [Parameter]
    public ReviewChecklistDto? Checklist { get; set; }

    [Parameter]
    public EventCallback<ChecklistItemDto> OnItemCheckedChanged { get; set; }

    private HashSet<string> expandedCategories = new();

    protected override void OnParametersSet()
    {
        if (Checklist != null && !expandedCategories.Any())
        {
            // Expand all categories by default on first load
            expandedCategories = GetCategories().ToHashSet();
        }
    }

    private IEnumerable<string> GetCategories()
    {
        return Checklist?.Items
            .Select(i => i.Category)
            .Distinct()
            .OrderBy(c => c)
            ?? Enumerable.Empty<string>();
    }

    private IEnumerable<ChecklistItemDto> GetItemsByCategory(string category)
    {
        return Checklist?.Items
            .Where(i => i.Category == category)
            .OrderBy(i => i.SortOrder)
            ?? Enumerable.Empty<ChecklistItemDto>();
    }

    private void ToggleCategory(string category)
    {
        if (expandedCategories.Contains(category))
            expandedCategories.Remove(category);
        else
            expandedCategories.Add(category);
    }

    private bool IsCategoryExpanded(string category)
    {
        return expandedCategories.Contains(category);
    }

    private string GetCategoryProgress(string category)
    {
        var items = GetItemsByCategory(category).ToList();
        if (!items.Any()) return "0/0";

        var checkedCount = items.Count(i => i.IsChecked);
        return $"{checkedCount}/{items.Count}";
    }

    private string GetProgressBarClass()
    {
        if (Checklist == null) return "bg-secondary";

        return Checklist.CompletionPercentage switch
        {
            100 => "bg-success",
            >= 70 => "bg-info",
            >= 40 => "bg-warning",
            _ => "bg-danger"
        };
    }

    private async Task HandleItemChecked(ChecklistItemDto item)
    {
        if (OnItemCheckedChanged.HasDelegate)
        {
            await OnItemCheckedChanged.InvokeAsync(item);
        }
    }
}
