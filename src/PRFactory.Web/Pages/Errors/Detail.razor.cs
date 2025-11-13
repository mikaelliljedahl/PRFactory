using Microsoft.AspNetCore.Components;
using PRFactory.Web.Components.Errors;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Errors;

public partial class Detail
{
    [Parameter]
    public Guid ErrorId { get; set; }

    [Inject]
    private IErrorService ErrorService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<Detail> Logger { get; set; } = null!;

    private ErrorDto? Error { get; set; }
    private List<ErrorDto>? RelatedErrors { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool ShowResolution { get; set; } = false;
    private string? SuccessMessage { get; set; }
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadError();
        await LoadRelatedErrors();
    }

    private async Task LoadError()
    {
        IsLoading = true;
        try
        {
            Error = await ErrorService.GetErrorByIdAsync(ErrorId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRelatedErrors()
    {
        if (Error?.EntityType != null && Error.EntityId.HasValue)
        {
            try
            {
                RelatedErrors = await ErrorService.GetErrorsByEntityAsync(
                    Error.EntityType,
                    Error.EntityId.Value);

                // Remove the current error from related errors
                RelatedErrors = RelatedErrors
                    .Where(e => e.Id != ErrorId)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load related errors for entity {EntityType}:{EntityId}", Error.EntityType, Error.EntityId);
            }
        }
    }

    private void ShowResolutionForm()
    {
        ShowResolution = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private async Task HandleResolve(ErrorResolutionForm.ResolutionFormModel model)
    {
        try
        {
            await ErrorService.MarkErrorResolvedAsync(
                ErrorId,
                model.ResolvedBy,
                model.ResolutionNotes);

            SuccessMessage = "Error marked as resolved successfully";
            ShowResolution = false;

            // Reload error to show updated status
            await LoadError();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to resolve error: {ex.Message}";
        }
    }

    private async Task RetryOperation()
    {
        try
        {
            var success = await ErrorService.RetryFailedOperationAsync(ErrorId);
            if (success)
            {
                SuccessMessage = "Retry operation initiated successfully";
                await LoadError();
            }
            else
            {
                ErrorMessage = "Failed to retry operation. Check that the associated entity exists.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to retry operation: {ex.Message}";
        }
    }

    private async Task CopyToClipboard()
    {
        if (Error != null)
        {
            // Note: In a real implementation, use JS interop to copy to clipboard
            // For now, this is a placeholder
            SuccessMessage = "Error details copied to clipboard (placeholder - implement JS interop)";
            await Task.CompletedTask;
        }
    }
}
