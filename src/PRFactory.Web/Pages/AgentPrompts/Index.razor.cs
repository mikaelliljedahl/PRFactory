using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Dialogs;
using Radzen;

namespace PRFactory.Web.Pages.AgentPrompts;

public partial class Index
{
    [Inject]
    private IAgentPromptService AgentPromptService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    private List<AgentPromptTemplateDto> AllTemplates { get; set; } = new();
    private List<AgentPromptTemplateDto> FilteredTemplates { get; set; } = new();

    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }

    private string SelectedCategory { get; set; } = string.Empty;
    private string SelectedType { get; set; } = string.Empty;
    private string SearchQuery { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplatesAsync();
    }

    private async Task LoadTemplatesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            AllTemplates = await AgentPromptService.GetAllTemplatesAsync();
            FilterTemplates();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading templates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterTemplates()
    {
        var query = AllTemplates.AsEnumerable();

        // Filter by category
        if (!string.IsNullOrEmpty(SelectedCategory))
        {
            query = query.Where(t => t.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by type
        if (!string.IsNullOrEmpty(SelectedType))
        {
            if (SelectedType == "system")
            {
                query = query.Where(t => t.IsSystemTemplate);
            }
            else if (SelectedType == "custom")
            {
                query = query.Where(t => !t.IsSystemTemplate);
            }
        }

        // Filter by search query
        if (!string.IsNullOrEmpty(SearchQuery))
        {
            query = query.Where(t =>
                t.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        FilteredTemplates = query.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
    }

    private void NavigateToPreview(Guid id)
    {
        Navigation.NavigateTo($"/agent-prompts/preview/{id}");
    }

    private void NavigateToEdit(Guid id)
    {
        Navigation.NavigateTo($"/agent-prompts/edit/{id}");
    }

    private async Task HandleClone(AgentPromptTemplateDto template)
    {
        try
        {
            // TODO: Get current tenant ID from auth context
            var tenantId = Guid.NewGuid(); // Placeholder

            var clonedTemplate = await AgentPromptService.CloneTemplateForTenantAsync(template.Id, tenantId);
            SuccessMessage = $"Successfully cloned template '{template.Name}' as '{clonedTemplate.Name}'";

            await LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error cloning template: {ex.Message}";
        }
    }

    private async Task HandleDelete(AgentPromptTemplateDto template)
    {
        bool confirmed = await ConfirmDialogHelper.ShowDeletePromptTemplateAsync(
            DialogService,
            template.Name,
            template.IsSystemTemplate);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await AgentPromptService.DeleteTemplateAsync(template.Id);
            SuccessMessage = $"Successfully deleted template '{template.Name}'";

            await LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting template: {ex.Message}";
        }
    }

    private string GetEmptyStateMessage()
    {
        if (!string.IsNullOrEmpty(SearchQuery) || !string.IsNullOrEmpty(SelectedCategory) || !string.IsNullOrEmpty(SelectedType))
        {
            return "No templates match your filters. Try adjusting your search criteria.";
        }

        return "No agent prompt templates have been created yet. Create your first template to get started.";
    }
}
