using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Settings;

public partial class ApiKeyProviderForm
{
    [Parameter, EditorRequired]
    public CreateApiKeyProviderDto Model { get; set; } = null!;

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public bool IsEditMode { get; set; }

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private EditContext? editContext;
    private ValidationMessageStore? messageStore;
    private bool isTesting = false;
    private ConnectionTestResult? testResult;

    protected override void OnInitialized()
    {
        editContext = new EditContext(Model);
        messageStore = new ValidationMessageStore(editContext);
    }

    private async Task HandleTestConnectionAsync()
    {
        if (editContext == null || !editContext.Validate())
        {
            ToastService.ShowError("Please fill in all required fields before testing connection");
            return;
        }

        try
        {
            isTesting = true;
            testResult = null;

            // Create a simple test DTO with current form values
            // Note: We can't test without creating the provider first in the current service design
            // For now, just show a placeholder message
            testResult = new ConnectionTestResult
            {
                Success = false,
                Message = "Connection testing requires creating the provider first. Click 'Create Provider' to save."
            };

            ToastService.ShowInfo("To test the connection, please create the provider first");
        }
        catch (Exception ex)
        {
            testResult = new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message,
                ErrorDetails = ex.ToString()
            };
            ToastService.ShowError($"Connection test error: {ex.Message}");
        }
        finally
        {
            isTesting = false;
        }
    }

    private async Task HandleSubmitAsync()
    {
        await OnValidSubmit.InvokeAsync();
    }
}
