using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Settings.LlmProviders;

public partial class Index
{
    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private List<TenantLlmProviderDto> providers = new();
    private List<TenantLlmProviderDto> filteredProviders = new();
    private bool isLoading = true;
    private string searchTerm = string.Empty;
    private string? selectedType = null;
    private bool showActiveOnly = true;

    private bool canAddProvider;
    private bool canEditProvider;

    protected override async Task OnInitializedAsync()
    {
        await LoadProvidersAsync();
        await CheckPermissionsAsync();
    }

    private async Task LoadProvidersAsync()
    {
        try
        {
            isLoading = true;
            providers = await ProviderService.GetProvidersForTenantAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to load LLM providers: {ex.Message}");
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

        canAddProvider = roles.Contains("Owner") || roles.Contains("Admin");
        canEditProvider = roles.Contains("Owner") || roles.Contains("Admin");
    }

    private void ApplyFilters()
    {
        filteredProviders = providers
            .Where(p => string.IsNullOrEmpty(searchTerm) ||
                       p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(p => selectedType == null || p.ProviderType == selectedType)
            .Where(p => !showActiveOnly || p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToList();
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        ApplyFilters();
    }

    private void OnTypeFilterChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        selectedType = string.IsNullOrEmpty(value) ? null : value;
        ApplyFilters();
    }

    private void OnShowActiveOnlyChanged(bool value)
    {
        showActiveOnly = value;
        ApplyFilters();
    }

    private void NavigateToCreate()
    {
        if (canAddProvider)
            Navigation.NavigateTo("/settings/llm-providers/create");
    }

    private void NavigateToEdit(Guid id)
    {
        if (canEditProvider)
            Navigation.NavigateTo($"/settings/llm-providers/edit/{id}");
    }

    private void NavigateToDetail(Guid id)
    {
        Navigation.NavigateTo($"/settings/llm-providers/{id}");
    }

    private async Task HandleSetDefaultAsync(Guid id)
    {
        if (!canEditProvider)
        {
            ToastService.ShowError("You don't have permission to set default provider");
            return;
        }

        try
        {
            await ProviderService.SetDefaultProviderAsync(id);
            ToastService.ShowSuccess("Default provider updated successfully");
            await LoadProvidersAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to set default provider: {ex.Message}");
        }
    }

    private async Task HandleDeleteAsync(Guid id)
    {
        if (!canEditProvider)
        {
            ToastService.ShowError("You don't have permission to delete providers");
            return;
        }

        // Simple confirmation (using browser's built-in confirm is OK for now)
        // In a real app, you might want a custom confirmation dialog component
        var confirmed = true; // For now, we'll skip JS interop confirmation

        if (!confirmed)
            return;

        try
        {
            await ProviderService.DeleteProviderAsync(id);
            ToastService.ShowSuccess("LLM provider deactivated successfully");
            await LoadProvidersAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to deactivate provider: {ex.Message}");
        }
    }

    private async Task HandleTestConnectionAsync(Guid id)
    {
        try
        {
            var result = await ProviderService.TestProviderConnectionAsync(id);

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
    }
}
