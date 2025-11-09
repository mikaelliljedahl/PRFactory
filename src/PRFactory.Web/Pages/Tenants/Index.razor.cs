using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Dialogs;
using Radzen;

namespace PRFactory.Web.Pages.Tenants;

public partial class Index
{
    [Inject]
    private ITenantService TenantService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    private List<TenantDto>? tenants;
    private List<TenantDto>? filteredTenants;
    private (int Active, int Inactive)? stats;
    private bool isLoading = true;
    private string? errorMessage;
    private string? successMessage;
    private string searchTerm = string.Empty;
    private string filterStatus = "all";

    protected override async Task OnInitializedAsync()
    {
        await LoadTenantsAsync();
        await LoadStatsAsync();
    }

    private async Task LoadTenantsAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            tenants = filterStatus switch
            {
                "active" => await TenantService.GetActiveTenantsAsync(),
                "inactive" => (await TenantService.GetAllTenantsAsync()).Where(t => !t.IsActive).ToList(),
                _ => await TenantService.GetAllTenantsAsync()
            };

            ApplyFilters();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load tenants: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            stats = await TenantService.GetTenantStatsAsync();
        }
        catch (Exception ex)
        {
            // Non-critical, just log
            Console.WriteLine($"Failed to load stats: {ex.Message}");
        }
    }

    private void OnSearchChanged()
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (tenants == null)
        {
            filteredTenants = null;
            return;
        }

        filteredTenants = tenants;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            filteredTenants = filteredTenants
                .Where(t => t.Name.ToLower().Contains(search) ||
                           t.JiraUrl.ToLower().Contains(search))
                .ToList();
        }
    }

    private async Task HandleViewAsync(Guid tenantId)
    {
        Navigation.NavigateTo($"/tenants/{tenantId}");
        await Task.CompletedTask;
    }

    private async Task HandleEditAsync(Guid tenantId)
    {
        Navigation.NavigateTo($"/tenants/{tenantId}/edit");
        await Task.CompletedTask;
    }

    private async Task HandleActivateAsync(Guid tenantId)
    {
        var tenant = tenants?.FirstOrDefault(t => t.Id == tenantId);
        if (tenant == null) return;

        var confirmed = await ConfirmDialogHelper.ShowActivateAsync(DialogService, tenant.Name);
        if (!confirmed) return;

        try
        {
            await TenantService.ActivateTenantAsync(tenantId);
            successMessage = $"Tenant '{tenant.Name}' activated successfully";
            await LoadTenantsAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to activate tenant: {ex.Message}";
        }
    }

    private async Task HandleDeactivateAsync(Guid tenantId)
    {
        var tenant = tenants?.FirstOrDefault(t => t.Id == tenantId);
        if (tenant == null) return;

        var confirmed = await ConfirmDialogHelper.ShowDeactivateAsync(DialogService, tenant.Name);
        if (!confirmed) return;

        try
        {
            await TenantService.DeactivateTenantAsync(tenantId);
            successMessage = $"Tenant '{tenant.Name}' deactivated successfully";
            await LoadTenantsAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to deactivate tenant: {ex.Message}";
        }
    }

    private async Task HandleDeleteAsync(Guid tenantId)
    {
        var tenant = tenants?.FirstOrDefault(t => t.Id == tenantId);
        if (tenant == null) return;

        var confirmed = await ConfirmDialogHelper.ShowDeleteAsync(
            DialogService,
            tenant.Name,
            "This will permanently delete all associated data including repositories and tickets.");

        if (!confirmed) return;

        try
        {
            await TenantService.DeleteTenantAsync(tenantId);
            successMessage = $"Tenant '{tenant.Name}' deleted successfully";
            await LoadTenantsAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to delete tenant: {ex.Message}";
        }
    }
}
