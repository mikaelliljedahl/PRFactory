using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Plans;

public partial class PlanValidationPanel
{
    [Parameter, EditorRequired]
    public Guid TicketId { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    private string selectedCheckType = "security";
    private bool isRunning = false;
    private bool hasValidationRun = false;
    private PlanValidationResult? validationResult;
    private string? errorMessage;

    private async Task RunValidation()
    {
        isRunning = true;
        errorMessage = null;

        try
        {
            validationResult = await TicketService.ValidatePlanAsync(TicketId, selectedCheckType);
            hasValidationRun = true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Validation failed: {ex.Message}";
        }
        finally
        {
            isRunning = false;
        }
    }

    private void ResetValidation()
    {
        hasValidationRun = false;
        validationResult = null;
        errorMessage = null;
    }

    private string GetAlertClass()
    {
        if (validationResult == null) return "info";

        return validationResult.Score switch
        {
            >= 90 => "success",
            >= 70 => "warning",
            _ => "danger"
        };
    }

    private string GetIcon()
    {
        if (validationResult == null) return "info-circle";

        return validationResult.Score switch
        {
            >= 90 => "check-circle",
            >= 70 => "exclamation-triangle",
            _ => "x-circle"
        };
    }
}
