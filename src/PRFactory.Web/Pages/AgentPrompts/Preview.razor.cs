using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.AgentPrompts;

public partial class Preview
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IAgentPromptService AgentPromptService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private AgentPromptTemplateDto? Template { get; set; }
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private bool UseSampleData { get; set; } = true;

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

    private void OnSampleDataToggled()
    {
        // The component will re-render with new sample data setting
        StateHasChanged();
    }
}
