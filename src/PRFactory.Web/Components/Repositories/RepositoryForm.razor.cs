using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Repositories;

public partial class RepositoryForm
{
    [Parameter, EditorRequired]
    public object Model { get; set; } = null!;

    [Parameter]
    public bool IsEditMode { get; set; }

    [Parameter]
    public bool ShowTenantSelection { get; set; } = true;

    [Parameter]
    public List<TenantDto>? Tenants { get; set; }

    [Parameter]
    public EventCallback<object> OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public bool IsSubmitting { get; set; }

    [Parameter]
    public string SubmitButtonText { get; set; } = "Save";

    private async Task HandleValidSubmit()
    {
        if (OnValidSubmit.HasDelegate)
        {
            await OnValidSubmit.InvokeAsync(Model);
        }
    }

    private async Task HandleCancel()
    {
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }
}
