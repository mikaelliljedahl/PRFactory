using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Tickets;

public partial class TicketUpdatePreview
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Parameter, EditorRequired]
    public TicketDto OriginalTicket { get; set; } = new();

    [Parameter]
    public EventCallback OnUpdateApproved { get; set; }

    [Parameter]
    public EventCallback OnUpdateRejected { get; set; }

    [Inject]
    public ITicketService TicketService { get; set; } = null!;

    private TicketUpdateDto? TicketUpdate;
    private string activeTab = "preview";
    private bool isLoading = true;
    private bool isSubmitting;
    private bool showRejectForm;
    private bool showRejectError;
    private string rejectionReason = string.Empty;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadTicketUpdate();
    }

    private async Task LoadTicketUpdate()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            TicketUpdate = await TicketService.GetLatestTicketUpdateAsync(TicketId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading ticket update: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleApprove()
    {
        try
        {
            errorMessage = null;
            isSubmitting = true;
            StateHasChanged();

            if (TicketUpdate != null)
            {
                await TicketService.ApproveTicketUpdateAsync(TicketUpdate.Id);
                await OnUpdateApproved.InvokeAsync();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error approving update: {ex.Message}";
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

            if (TicketUpdate != null)
            {
                await TicketService.RejectTicketUpdateAsync(TicketUpdate.Id, rejectionReason);
                await OnUpdateRejected.InvokeAsync();
            }

            // Reset form
            showRejectForm = false;
            rejectionReason = string.Empty;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error rejecting update: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task HandleSaved()
    {
        // Reload the ticket update after save
        await LoadTicketUpdate();
        activeTab = "preview"; // Switch back to preview
    }

    private string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        try
        {
            // Use default Markdig conversion without custom pipeline
            return Markdig.Markdown.ToHtml(markdown);
        }
        catch
        {
            // If markdown parsing fails, return the raw text
            return System.Web.HttpUtility.HtmlEncode(markdown);
        }
    }
}
