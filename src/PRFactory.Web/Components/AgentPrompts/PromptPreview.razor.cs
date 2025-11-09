using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Radzen;

namespace PRFactory.Web.Components.AgentPrompts;

public partial class PromptPreview
{
    [Parameter, EditorRequired]
    public AgentPromptTemplateDto? Template { get; set; }

    [Parameter]
    public Dictionary<string, string>? SampleData { get; set; }

    [Inject]
    private IAgentPromptService AgentPromptService { get; set; } = null!;

    private string TemplateContent => Template?.PromptContent ?? string.Empty;
    private string PreviewContent { get; set; } = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        if (Template == null)
        {
            PreviewContent = "No template selected";
            return;
        }

        try
        {
            PreviewContent = await AgentPromptService.PreviewTemplateAsync(Template.Id, SampleData);
        }
        catch (Exception ex)
        {
            PreviewContent = $"Error loading preview: {ex.Message}";
        }
    }
}
