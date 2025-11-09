using System.Text.Json;
using Microsoft.AspNetCore.Components;
using PRFactory.Domain.Entities;
using PRFactory.Web.Services;

namespace PRFactory.Web.Components.Tenants;

public partial class TenantConfigEditor
{
    [Inject]
    private ITenantService TenantService { get; set; } = null!;

    [Parameter, EditorRequired]
    public Guid TenantId { get; set; }

    [Parameter]
    public EventCallback OnConfigurationSaved { get; set; }

    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }
    private string? validationError;

    private string configJson = string.Empty;
    private TenantConfiguration? originalConfig;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadConfigurationAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Reload when TenantId changes
        if (TenantId != Guid.Empty)
        {
            await LoadConfigurationAsync();
        }
    }

    private async Task LoadConfigurationAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var tenant = await TenantService.GetTenantByIdAsync(TenantId);
            if (tenant == null)
            {
                ErrorMessage = "Tenant not found";
                return;
            }

            // Create configuration object from tenant DTO
            originalConfig = new TenantConfiguration
            {
                AutoImplementAfterPlanApproval = tenant.AutoImplementAfterPlanApproval,
                MaxRetries = tenant.MaxRetries,
                ClaudeModel = tenant.ClaudeModel,
                MaxTokensPerRequest = tenant.MaxTokensPerRequest,
                EnableCodeReview = tenant.EnableCodeReview
            };

            configJson = JsonSerializer.Serialize(originalConfig, JsonOptions);
            validationError = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load configuration: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task HandleSaveAsync()
    {
        if (!ValidateJson())
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var config = JsonSerializer.Deserialize<TenantConfiguration>(configJson, JsonOptions);
            if (config == null)
            {
                ErrorMessage = "Failed to parse configuration";
                return;
            }

            // Validate configuration values
            if (config.MaxRetries < 1 || config.MaxRetries > 10)
            {
                ErrorMessage = "MaxRetries must be between 1 and 10";
                return;
            }

            if (config.MaxTokensPerRequest < 1000 || config.MaxTokensPerRequest > 100000)
            {
                ErrorMessage = "MaxTokensPerRequest must be between 1,000 and 100,000";
                return;
            }

            if (config.ApiTimeoutSeconds < 30 || config.ApiTimeoutSeconds > 600)
            {
                ErrorMessage = "ApiTimeoutSeconds must be between 30 and 600";
                return;
            }

            // Get current tenant to build update request
            var tenant = await TenantService.GetTenantByIdAsync(TenantId);
            if (tenant == null)
            {
                ErrorMessage = "Tenant not found";
                return;
            }

            // Update via service
            var updateRequest = new PRFactory.Web.Models.UpdateTenantRequest
            {
                Id = TenantId,
                Name = tenant.Name,
                JiraUrl = tenant.JiraUrl,
                IsActive = tenant.IsActive,
                AutoImplementAfterPlanApproval = config.AutoImplementAfterPlanApproval,
                MaxRetries = config.MaxRetries,
                ClaudeModel = config.ClaudeModel,
                MaxTokensPerRequest = config.MaxTokensPerRequest,
                ApiTimeoutSeconds = config.ApiTimeoutSeconds,
                EnableVerboseLogging = config.EnableVerboseLogging,
                EnableCodeReview = config.EnableCodeReview,
                AllowedRepositories = config.AllowedRepositories
            };

            await TenantService.UpdateTenantAsync(updateRequest);

            SuccessMessage = "Configuration saved successfully";
            originalConfig = config;

            // Notify parent
            await OnConfigurationSaved.InvokeAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save configuration: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task HandleResetAsync()
    {
        await LoadConfigurationAsync();
        SuccessMessage = null;
        validationError = null;
    }

    private Task HandleFormatAsync()
    {
        try
        {
            var config = JsonSerializer.Deserialize<TenantConfiguration>(configJson);
            if (config != null)
            {
                configJson = JsonSerializer.Serialize(config, JsonOptions);
                validationError = null;
                SuccessMessage = "JSON formatted successfully";
            }
        }
        catch (JsonException ex)
        {
            validationError = $"Invalid JSON: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private bool ValidateJson()
    {
        try
        {
            var config = JsonSerializer.Deserialize<TenantConfiguration>(configJson, JsonOptions);
            if (config == null)
            {
                validationError = "Invalid JSON configuration";
                return false;
            }

            validationError = null;
            return true;
        }
        catch (JsonException ex)
        {
            validationError = $"Invalid JSON: {ex.Message}";
            return false;
        }
    }
}
