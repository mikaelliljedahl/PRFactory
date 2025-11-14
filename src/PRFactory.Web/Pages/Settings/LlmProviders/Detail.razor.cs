using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Settings.LlmProviders;

public partial class Detail
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private TenantLlmProviderDto? provider;
    private bool isLoading = true;
    private bool canEdit = false;
    private bool isTesting = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadProviderAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadProviderAsync()
    {
        try
        {
            isLoading = true;
            provider = await ProviderService.GetProviderByIdAsync(Id);
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

    private async Task CheckPermissionsAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        var roles = user.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        canEdit = roles.Contains("Owner") || roles.Contains("Admin");
    }

    private void NavigateToEdit()
    {
        Navigation.NavigateTo($"/settings/llm-providers/edit/{Id}");
    }

    private void NavigateToList()
    {
        Navigation.NavigateTo("/settings/llm-providers");
    }

    private async Task HandleSetDefaultAsync()
    {
        if (!canEdit)
        {
            ToastService.ShowError("You don't have permission to set default provider");
            return;
        }

        try
        {
            await ProviderService.SetDefaultProviderAsync(Id);
            ToastService.ShowSuccess("Default provider updated successfully");
            await LoadProviderAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to set default provider: {ex.Message}");
        }
    }

    private async Task HandleTestConnectionAsync()
    {
        try
        {
            isTesting = true;
            var result = await ProviderService.TestProviderConnectionAsync(Id);

            if (result.Success)
            {
                ToastService.ShowSuccess($"Connection test successful! ({result.ResponseTimeMs}ms)");
            }
            else
            {
                ToastService.ShowError($"Connection test failed: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Connection test error: {ex.Message}");
        }
        finally
        {
            isTesting = false;
        }
    }
}
