using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Tickets;

public partial class PlanReviewSection
{
    [Parameter]
    public TicketDto Ticket { get; set; } = null!;

    [Parameter]
    public EventCallback OnPlanApproved { get; set; }

    [Parameter]
    public EventCallback OnPlanRejected { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<PlanReviewSection> Logger { get; set; } = null!;

    // Single-user review state
    private bool showRefineForm;
    private bool showRejectForm;
    private bool isSubmitting;
    private bool showRefineError;
    private bool showRejectError;
    private bool showExamples;
    private string refinementInstructions = string.Empty;
    private string rejectionReason = string.Empty;
    private string? errorMessage;

    // Team review state
    private bool showReviewerAssignment;
    private List<ReviewerDto> reviewers = new();
    private List<ReviewCommentDto> comments = new();
    private bool isLoadingReviewers;

    protected override async Task OnInitializedAsync()
    {
        await LoadReviewersAsync();
        await LoadCommentsAsync();
    }

    private async Task LoadReviewersAsync()
    {
        try
        {
            isLoadingReviewers = true;
            reviewers = await TicketService.GetReviewersAsync(Ticket.Id);
            Logger.LogInformation("Loaded {Count} reviewers for ticket {TicketId}", reviewers.Count, Ticket.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading reviewers for ticket {TicketId}", Ticket.Id);
            // Don't show error toast - team review is optional
        }
        finally
        {
            isLoadingReviewers = false;
        }
    }

    private async Task LoadCommentsAsync()
    {
        try
        {
            comments = await TicketService.GetCommentsAsync(Ticket.Id);
            Logger.LogInformation("Loaded {Count} comments for ticket {TicketId}", comments.Count, Ticket.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading comments for ticket {TicketId}", Ticket.Id);
            // Don't show error toast - comments are optional
        }
    }

    private async Task HandleApprove()
    {
        try
        {
            errorMessage = null;
            isSubmitting = true;
            StateHasChanged();

            // Check if team review is enabled
            if (reviewers.Any())
            {
                var hasSufficient = await TicketService.HasSufficientApprovalsAsync(Ticket.Id);
                if (!hasSufficient)
                {
                    ToastService.ShowWarning("Cannot approve: Insufficient reviewer approvals. All required reviewers must approve first.");
                    return;
                }
            }

            await TicketService.ApprovePlanAsync(Ticket.Id);

            ToastService.ShowSuccess("Implementation plan approved successfully! Implementation will begin shortly.");

            // Notify parent component
            await OnPlanApproved.InvokeAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error approving plan: {ex.Message}";
            ToastService.ShowError($"Failed to approve plan: {ex.Message}");
            Logger.LogError(ex, "Error approving plan for ticket {TicketId}", Ticket.Id);
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private void ShowRefineForm()
    {
        showRefineForm = true;
        showRefineError = false;
        refinementInstructions = string.Empty;
        errorMessage = null;
    }

    private void CancelRefine()
    {
        showRefineForm = false;
        showRefineError = false;
        refinementInstructions = string.Empty;
        errorMessage = null;
    }

    private async Task ConfirmRefine()
    {
        if (string.IsNullOrWhiteSpace(refinementInstructions))
        {
            showRefineError = true;
            StateHasChanged();
            return;
        }

        try
        {
            errorMessage = null;
            showRefineError = false;
            isSubmitting = true;
            StateHasChanged();

            await TicketService.RefinePlanAsync(Ticket.Id, refinementInstructions);

            ToastService.ShowInfo("Refinement instructions submitted. The plan will be updated based on your feedback.");

            // Notify parent component
            await OnPlanRejected.InvokeAsync();

            // Reset form
            showRefineForm = false;
            refinementInstructions = string.Empty;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error refining plan: {ex.Message}";
            ToastService.ShowError($"Failed to submit refinement: {ex.Message}");
            Logger.LogError(ex, "Error refining plan for ticket {TicketId}", Ticket.Id);
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private void ShowRejectForm()
    {
        showRejectForm = true;
        showRejectError = false;
        rejectionReason = string.Empty;
        errorMessage = null;
    }

    private void CancelReject()
    {
        showRejectForm = false;
        showRejectError = false;
        rejectionReason = string.Empty;
        errorMessage = null;
    }

    private async Task ConfirmReject()
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            showRejectError = true;
            StateHasChanged();
            return;
        }

        try
        {
            errorMessage = null;
            showRejectError = false;
            isSubmitting = true;
            StateHasChanged();

            await TicketService.RejectPlanAsync(Ticket.Id, rejectionReason, regenerateCompletely: true);

            ToastService.ShowInfo("Plan rejected. A completely new plan will be generated from scratch based on your feedback.");

            // Notify parent component
            await OnPlanRejected.InvokeAsync();

            // Reset form
            showRejectForm = false;
            rejectionReason = string.Empty;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error rejecting plan: {ex.Message}";
            ToastService.ShowError($"Failed to reject plan: {ex.Message}");
            Logger.LogError(ex, "Error rejecting plan for ticket {TicketId}", Ticket.Id);
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    // Team review methods

    private void ShowReviewerAssignment()
    {
        showReviewerAssignment = true;
    }

    private void HideReviewerAssignment()
    {
        showReviewerAssignment = false;
    }

    private async Task HandleReviewersAssigned()
    {
        showReviewerAssignment = false;
        await LoadReviewersAsync();
        StateHasChanged();
    }

    private async Task HandleCommentAdded()
    {
        await LoadCommentsAsync();
        StateHasChanged();
    }
}
