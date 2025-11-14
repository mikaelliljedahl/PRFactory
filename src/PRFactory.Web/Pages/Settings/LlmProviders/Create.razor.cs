using Microsoft.AspNetCore.Components;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.Settings.LlmProviders;

public partial class Create
{
    [Inject] private ITenantLlmProviderService ProviderService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private enum WizardStep
    {
        SelectType = 1,
        Configure = 2
    }

    private WizardStep currentStep = WizardStep.SelectType;
    private string? selectedType = null;

    // Models for different provider types
    private CreateOAuthProviderDto? oauthModel = null;
    private CreateApiKeyProviderDto? apiKeyModel = null;

    private bool isSaving = false;

    private void OnProviderTypeSelected(string type)
    {
        selectedType = type;

        // Initialize appropriate model
        if (type == "AnthropicNative")
        {
            oauthModel = new CreateOAuthProviderDto
            {
                Name = "Anthropic Claude",
                DefaultModel = "claude-sonnet-4-5-20250929"
            };
        }
        else
        {
            apiKeyModel = new CreateApiKeyProviderDto
            {
                ProviderType = type,
                Name = GetDefaultNameForType(type),
                DefaultModel = GetDefaultModelForType(type),
                ApiBaseUrl = GetDefaultApiBaseUrlForType(type),
                TimeoutMs = 300000,
                DisableNonEssentialTraffic = false
            };
        }

        currentStep = WizardStep.Configure;
    }

    private void OnBack()
    {
        currentStep = WizardStep.SelectType;
        selectedType = null;
        oauthModel = null;
        apiKeyModel = null;
    }

    private async Task HandleOAuthSubmitAsync()
    {
        if (oauthModel == null)
            return;

        try
        {
            isSaving = true;

            var provider = await ProviderService.CreateOAuthProviderAsync(oauthModel);

            ToastService.ShowSuccess($"Provider '{provider.Name}' created successfully");

            Navigation.NavigateTo("/settings/llm-providers");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to create provider: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task HandleApiKeySubmitAsync()
    {
        if (apiKeyModel == null)
            return;

        try
        {
            isSaving = true;

            var provider = await ProviderService.CreateApiKeyProviderAsync(apiKeyModel);

            ToastService.ShowSuccess($"Provider '{provider.Name}' created successfully");

            Navigation.NavigateTo("/settings/llm-providers");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to create provider: {ex.Message}");
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

    private string GetWizardTitle() => currentStep switch
    {
        WizardStep.SelectType => "Step 1: Choose Provider Type",
        WizardStep.Configure => $"Step 2: Configure {selectedType}",
        _ => "Add LLM Provider"
    };

    private string GetDefaultNameForType(string type) => type switch
    {
        "ZAi" => "Z.ai Unified API",
        "MinimaxM2" => "Minimax M2",
        "OpenRouter" => "OpenRouter",
        "TogetherAI" => "Together AI",
        "Custom" => "Custom Provider",
        _ => "LLM Provider"
    };

    private string GetDefaultModelForType(string type) => type switch
    {
        "ZAi" => "gpt-4o",
        "MinimaxM2" => "minimax-m2",
        "OpenRouter" => "anthropic/claude-sonnet-4-5",
        "TogetherAI" => "meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo",
        "Custom" => "",
        _ => ""
    };

    private string GetDefaultApiBaseUrlForType(string type) => type switch
    {
        "ZAi" => "https://api.z.ai",
        "MinimaxM2" => "https://api.minimax.chat",
        "OpenRouter" => "https://openrouter.ai/api/v1",
        "TogetherAI" => "https://api.together.xyz",
        "Custom" => "",
        _ => ""
    };
}
