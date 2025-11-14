using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Components.Settings;

public partial class OAuthProviderForm
{
    [Parameter, EditorRequired]
    public CreateOAuthProviderDto Model { get; set; } = null!;

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private EditContext? editContext;
    private ValidationMessageStore? messageStore;

    protected override void OnInitialized()
    {
        editContext = new EditContext(Model);
        messageStore = new ValidationMessageStore(editContext);
    }

    private async Task HandleSubmitAsync()
    {
        await OnValidSubmit.InvokeAsync();
    }
}
