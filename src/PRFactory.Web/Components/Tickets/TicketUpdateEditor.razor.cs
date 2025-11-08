using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Tickets;

public partial class TicketUpdateEditor
{
    [Parameter, EditorRequired]
    public TicketUpdateDto TicketUpdate { get; set; } = new();

    [Parameter]
    public EventCallback OnSaved { get; set; }

    [Inject]
    public ITicketService TicketService { get; set; } = null!;

    private bool isSaving;
    private string? errorMessage;
    private string? successMessage;

    private void HandleSuccessCriteriaChanged(List<SuccessCriterionDto> criteria)
    {
        TicketUpdate.SuccessCriteria = criteria;
    }

    private async Task HandleSave()
    {
        try
        {
            errorMessage = null;
            successMessage = null;
            isSaving = true;
            StateHasChanged();

            // Validate that we have at least one success criterion
            if (TicketUpdate.SuccessCriteria == null || !TicketUpdate.SuccessCriteria.Any())
            {
                errorMessage = "At least one success criterion is required.";
                return;
            }

            // Validate that all success criteria have descriptions
            if (TicketUpdate.SuccessCriteria.Any(sc => string.IsNullOrWhiteSpace(sc.Description)))
            {
                errorMessage = "All success criteria must have a description.";
                return;
            }

            await TicketService.UpdateTicketUpdateAsync(TicketUpdate.Id, TicketUpdate);

            successMessage = "Ticket update saved successfully.";
            await OnSaved.InvokeAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error saving ticket update: {ex.Message}";
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }
}
