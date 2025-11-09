using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Tenants;

public partial class Edit
{
    [Parameter]
    public Guid TenantId { get; set; }

    [Inject]
    private ITenantService TenantService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private TenantDto? tenant;
    private UpdateTenantRequest? model;
    private bool isLoading = true;
    private bool isSubmitting;
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
            tenant = await TenantService.GetTenantByIdAsync(TenantId);

            if (tenant == null)
            {
                errorMessage = "Tenant not found";
                return;
            }

            // Map to update request
            model = new UpdateTenantRequest
            {
                Id = tenant.Id,
                Name = tenant.Name,
                JiraUrl = tenant.JiraUrl,
                IsActive = tenant.IsActive,
                AutoImplementAfterPlanApproval = tenant.AutoImplementAfterPlanApproval,
                MaxRetries = tenant.MaxRetries,
                ClaudeModel = tenant.ClaudeModel,
                MaxTokensPerRequest = tenant.MaxTokensPerRequest,
                EnableCodeReview = tenant.EnableCodeReview
            };
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

    private async Task HandleSubmitAsync()
    {
        if (model == null) return;

        isSubmitting = true;
        errorMessage = null;

        try
        {
            var updatedTenant = await TenantService.UpdateTenantAsync(model);
            Navigation.NavigateTo($"/tenants/{updatedTenant.Id}");
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to update tenant: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private Task HandleCancelAsync()
    {
        Navigation.NavigateTo($"/tenants/{TenantId}");
        return Task.CompletedTask;
    }
}
