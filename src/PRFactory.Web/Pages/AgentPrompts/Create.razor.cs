using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.AgentPrompts;

public partial class Create
{
    [Inject]
    private IAgentPromptService AgentPromptService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private CreatePromptTemplateRequest Model { get; set; } = new();
    private bool IsSubmitting { get; set; }
    private string? ErrorMessage { get; set; }

    private async Task HandleSubmit()
    {
        IsSubmitting = true;
        ErrorMessage = null;

        try
        {
            // TODO: Get tenant ID from auth context if this should be a tenant template
            // For now, creating as system template (TenantId = null)
            Model.TenantId = null;

            var createdTemplate = await AgentPromptService.CreateTemplateAsync(Model);

            // Navigate to the created template's edit page
            Navigation.NavigateTo($"/agent-prompts/edit/{createdTemplate.Id}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating template: {ex.Message}";
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

    private void HandleVariableSelected(string variable)
    {
        // Variable was copied to clipboard by the component
        // No additional action needed here
    }
}
