using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Settings;

public partial class General
{
    [Inject] private ITenantConfigurationService ConfigService { get; set; } = null!;
    [Inject] private ITenantService TenantService { get; set; } = null!;
    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;

    private TenantDto? tenant;
    private TenantConfigurationDto? configuration;
    private List<TenantLlmProviderDto> llmProviders = new();

    private bool isLoading = true;
    private bool isSaving = false;
    private bool canEdit = false;

    private enum TabType
    {
        General = 1,
        Workflow = 2,
        CodeReview = 3,
        LlmProviders = 4
    }

    private TabType activeTab = TabType.General;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            isLoading = true;

            var tenantId = await CurrentUserService.GetCurrentTenantIdAsync();

            if (tenantId == null)
            {
                ToastService.ShowError("Unable to determine current tenant");
                return;
            }

            // Load tenant, configuration, and LLM providers in parallel
            var tenantTask = TenantService.GetTenantByIdAsync(tenantId.Value);
            var configTask = ConfigService.GetConfigurationAsync();
            var providersTask = ProviderService.GetProvidersForTenantAsync();

            await Task.WhenAll(tenantTask, configTask, providersTask);

            tenant = await tenantTask;
            configuration = await configTask;
            llmProviders = await providersTask;
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CheckPermissionsAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        canEdit = currentUser?.Role == UserRole.Owner;
    }

    private void OnTabChanged(TabType tab)
    {
        activeTab = tab;
    }

    private async Task HandleSaveAsync()
    {
        if (!canEdit)
        {
            ToastService.ShowError("Only Owners can update tenant settings");
            return;
        }

        if (configuration == null)
            return;

        try
        {
            isSaving = true;

            await ConfigService.UpdateConfigurationAsync(configuration);

            ToastService.ShowSuccess("Settings updated successfully");

            await LoadDataAsync(); // Reload to reflect changes
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to update settings: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }
}
