using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Plans;

/// <summary>
/// Component for revising or approving a plan
/// </summary>
public partial class PlanRevisionCard
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter]
    public string AdditionalClass { get; set; } = "mb-3";

    [Parameter]
    public EventCallback OnRevisionStarted { get; set; }

    [Parameter]
    public EventCallback OnApproved { get; set; }

    private string? RevisionFeedback { get; set; }
    private bool IsRevising { get; set; }
    private bool IsApproving { get; set; }
    private bool IsProcessing => IsRevising || IsApproving;
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }

    private async Task HandleRevise()
    {
        if (string.IsNullOrWhiteSpace(RevisionFeedback))
        {
            ErrorMessage = "Please provide revision instructions.";
            return;
        }

        IsRevising = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await OnRevisionStarted.InvokeAsync();
            SuccessMessage = "Plan revision started. Please wait for agents to regenerate artifacts.";
            RevisionFeedback = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start revision: {ex.Message}";
        }
        finally
        {
            IsRevising = false;
        }
    }

    private async Task HandleApprove()
    {
        IsApproving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await OnApproved.InvokeAsync();
            SuccessMessage = "Plan approved. Proceeding to next phase.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to approve plan: {ex.Message}";
        }
        finally
        {
            IsApproving = false;
        }
    }
}
