using Microsoft.AspNetCore.Components;
using PRFactory.Web.Services;

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

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private ILogger<PlanRevisionCard> Logger { get; set; } = null!;

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
            Logger.LogInformation("Requesting plan revision for ticket {TicketId}", TicketId);

            // Call service to refine plan with user feedback
            await TicketService.RefinePlanAsync(TicketId, RevisionFeedback);

            SuccessMessage = "Plan revision started. Please wait for agents to regenerate artifacts.";
            RevisionFeedback = null;

            // Notify parent component
            await OnRevisionStarted.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start plan revision for ticket {TicketId}", TicketId);
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
            Logger.LogInformation("Approving plan for ticket {TicketId}", TicketId);

            // Call service to approve plan
            await TicketService.ApprovePlanAsync(TicketId);

            SuccessMessage = "Plan approved. Proceeding to next phase.";

            // Notify parent component
            await OnApproved.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to approve plan for ticket {TicketId}", TicketId);
            ErrorMessage = $"Failed to approve plan: {ex.Message}";
        }
        finally
        {
            IsApproving = false;
        }
    }
}
