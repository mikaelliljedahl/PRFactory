using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Tenants;

public partial class Create
{
    [Inject]
    private ITenantService TenantService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private CreateTenantRequest model = new();
    private bool isSubmitting;
    private string? errorMessage;

    private async Task HandleSubmitAsync()
    {
        isSubmitting = true;
        errorMessage = null;

        try
        {
            var tenant = await TenantService.CreateTenantAsync(model);
            Navigation.NavigateTo($"/tenants/{tenant.Id}");
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to create tenant: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private Task HandleCancelAsync()
    {
        Navigation.NavigateTo("/tenants");
        return Task.CompletedTask;
    }
}
