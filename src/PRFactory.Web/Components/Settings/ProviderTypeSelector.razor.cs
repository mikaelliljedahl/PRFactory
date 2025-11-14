using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components.Settings;

public partial class ProviderTypeSelector
{
    [Parameter]
    public EventCallback<string> OnProviderTypeSelected { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleSelectAsync(string type)
    {
        await OnProviderTypeSelected.InvokeAsync(type);
    }
}
