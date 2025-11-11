using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Admin;

/// <summary>
/// Code-behind for Agent Configuration page.
/// Allows administrators to configure which LLM provider each agent type uses.
/// </summary>
public partial class AgentConfiguration : ComponentBase
{
    [Inject]
    private IAgentConfigurationService AgentConfigurationService { get; set; } = null!;

    [Inject]
    private ILogger<AgentConfiguration> Logger { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private AgentConfigurationDto? configuration;
    private List<LlmProviderSummaryDto>? providers;
    private bool isLoading = true;
    private bool isSaving;
    private string? errorMessage;
    private string? successMessage;
    private List<string> validationErrors = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Loads configuration and available providers
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            // Load configuration and providers in parallel
            var configTask = AgentConfigurationService.GetConfigurationAsync();
            var providersTask = AgentConfigurationService.GetAvailableProvidersAsync();

            await Task.WhenAll(configTask, providersTask);

            configuration = await configTask;
            providers = await providersTask;

            if (providers.Count == 0)
            {
                errorMessage = "No LLM providers configured for this tenant. Please configure providers first in Tenant Settings.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading agent configuration");
            errorMessage = $"Failed to load configuration: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Handles form submission to save configuration
    /// </summary>
    private async Task HandleSaveConfiguration()
    {
        if (configuration == null)
        {
            return;
        }

        try
        {
            isSaving = true;
            successMessage = null;
            errorMessage = null;
            validationErrors.Clear();

            // Validate configuration
            var (isValid, errors) = await AgentConfigurationService.ValidateConfigurationAsync(configuration);
            if (!isValid)
            {
                validationErrors = errors;
                return;
            }

            // Save configuration
            configuration = await AgentConfigurationService.SaveConfigurationAsync(configuration);

            successMessage = "Agent configuration saved successfully!";
            Logger.LogInformation("Agent configuration saved for tenant {TenantId}", configuration.TenantId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving agent configuration");
            errorMessage = $"Failed to save configuration: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }
}
