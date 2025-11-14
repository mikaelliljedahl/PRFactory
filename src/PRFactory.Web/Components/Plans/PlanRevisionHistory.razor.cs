using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Plans;

public partial class PlanRevisionHistory
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Inject]
    private IPlanService PlanService { get; set; } = null!;

    private List<PlanRevisionDto> revisions = new();
    private PlanRevisionDto? viewingRevision;
    private PlanRevisionComparisonDto? comparison;
    private Guid? selectedRevision1;
    private Guid? selectedRevision2;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadRevisions();
    }

    private async Task LoadRevisions()
    {
        try
        {
            revisions = await PlanService.GetPlanRevisionsAsync(TicketId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load revision history: {ex.Message}";
        }
    }

    private async Task ViewRevision(Guid revisionId)
    {
        try
        {
            viewingRevision = await PlanService.GetPlanRevisionAsync(revisionId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load revision: {ex.Message}";
        }
    }

    private void CloseViewer()
    {
        viewingRevision = null;
    }

    private void SelectForComparison(Guid revisionId)
    {
        if (selectedRevision1 == null)
        {
            selectedRevision1 = revisionId;
        }
        else if (selectedRevision2 == null && selectedRevision1 != revisionId)
        {
            selectedRevision2 = revisionId;
        }
        else if (selectedRevision1 == revisionId)
        {
            // Deselect if clicking the same revision
            selectedRevision1 = selectedRevision2;
            selectedRevision2 = null;
        }
        else if (selectedRevision2 == revisionId)
        {
            // Deselect second revision
            selectedRevision2 = null;
        }
    }

    private async Task CompareSelected()
    {
        if (selectedRevision1 == null || selectedRevision2 == null)
        {
            errorMessage = "Please select two revisions to compare";
            return;
        }

        try
        {
            comparison = await PlanService.CompareRevisionsAsync(
                selectedRevision1.Value,
                selectedRevision2.Value);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to compare revisions: {ex.Message}";
        }
    }

    private void CloseComparison()
    {
        comparison = null;
        ClearSelection();
    }

    private void ClearSelection()
    {
        selectedRevision1 = null;
        selectedRevision2 = null;
    }
}
