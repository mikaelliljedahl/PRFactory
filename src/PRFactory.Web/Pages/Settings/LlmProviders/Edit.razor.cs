using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Settings.LlmProviders;

public partial class Edit
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private TenantLlmProviderDto? provider;
    private UpdateProviderDto? updateModel;
    private EditContext? editContext;
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadProviderAsync();
    }

    private async Task LoadProviderAsync()
    {
        try
        {
            isLoading = true;
            provider = await ProviderService.GetProviderByIdAsync(Id);

            if (provider != null)
            {
                updateModel = new UpdateProviderDto
                {
                    Name = provider.Name,
                    ApiBaseUrl = provider.ApiBaseUrl,
                    DefaultModel = provider.DefaultModel,
                    TimeoutMs = provider.TimeoutMs,
                    DisableNonEssentialTraffic = false, // Not stored in DTO, using default
                    ModelOverrides = null, // Not stored in DTO
                    IsActive = provider.IsActive
                };

                editContext = new EditContext(updateModel);
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to load provider: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleSubmitAsync()
    {
        if (updateModel == null)
            return;

        try
        {
            isSaving = true;

            await ProviderService.UpdateProviderAsync(Id, updateModel);

            ToastService.ShowSuccess("Provider updated successfully");

            Navigation.NavigateTo("/settings/llm-providers");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to update provider: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/settings/llm-providers");
    }
}
