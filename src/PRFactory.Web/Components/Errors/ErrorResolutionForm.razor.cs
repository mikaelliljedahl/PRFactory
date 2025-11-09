using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Errors;

public partial class ErrorResolutionForm
{
    [Parameter, EditorRequired]
    public Guid ErrorId { get; set; }

    [Parameter]
    public EventCallback<ResolutionFormModel> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private ResolutionFormModel Model { get; set; } = new();
    private bool IsSubmitting { get; set; }

    private async Task HandleSubmit()
    {
        IsSubmitting = true;
        try
        {
            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync(Model);
            }
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public class ResolutionFormModel
    {
        public string? ResolvedBy { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}
