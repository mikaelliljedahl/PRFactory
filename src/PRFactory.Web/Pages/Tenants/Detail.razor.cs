using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Tenants;

public partial class Detail
{
    [Parameter]
    public Guid TenantId { get; set; }

    [Inject]
    private ITenantService TenantService { get; set; } = null!;

    private TenantDto? tenant;
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadTenantAsync();
    }

    private async Task LoadTenantAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            tenant = await TenantService.GetTenantWithDetailsAsync(TenantId);

            if (tenant == null)
            {
                errorMessage = "Tenant not found";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load tenant: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleConfigSavedAsync()
    {
        // Reload tenant to show updated configuration
        await LoadTenantAsync();
        StateHasChanged();
    }
}
