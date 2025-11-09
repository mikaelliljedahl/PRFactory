using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.AgentPrompts;

public partial class Edit
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IAgentPromptService AgentPromptService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private AgentPromptTemplateDto? Template { get; set; }
    private UpdatePromptTemplateRequest UpdateModel { get; set; } = new();

    private bool IsLoading { get; set; } = true;
    private bool IsSubmitting { get; set; }
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplateAsync();
    }

    private async Task LoadTemplateAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Template = await AgentPromptService.GetTemplateByIdAsync(Id);

            if (Template != null)
            {
                // Populate update model with current values
                UpdateModel = new UpdatePromptTemplateRequest
                {
                    Description = Template.Description,
                    PromptContent = Template.PromptContent,
                    Category = Template.Category,
                    RecommendedModel = Template.RecommendedModel,
                    Color = Template.Color
                };
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading template: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task HandleSubmit()
    {
        if (Template == null || Template.IsSystemTemplate)
        {
            ErrorMessage = "Cannot modify system templates";
            return;
        }

        IsSubmitting = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await AgentPromptService.UpdateTemplateAsync(Id, UpdateModel);
            SuccessMessage = "Template updated successfully";

            // Reload template to show updated values
            await LoadTemplateAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating template: {ex.Message}";
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/agent-prompts");
    }

    private async Task HandleClone()
    {
        if (Template == null)
        {
            return;
        }

        try
        {
            // TODO: Get current tenant ID from auth context
            var tenantId = Guid.NewGuid(); // Placeholder

            var clonedTemplate = await AgentPromptService.CloneTemplateForTenantAsync(Template.Id, tenantId);
            Navigation.NavigateTo($"/agent-prompts/edit/{clonedTemplate.Id}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error cloning template: {ex.Message}";
        }
    }

    private void HandleVariableSelected(string variable)
    {
        // Variable was copied to clipboard by the component
        // No additional action needed here
    }
}
