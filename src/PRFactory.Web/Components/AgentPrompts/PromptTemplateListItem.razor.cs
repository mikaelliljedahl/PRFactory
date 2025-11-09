using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.AgentPrompts;

public partial class PromptTemplateListItem
{
    [Parameter, EditorRequired]
    public AgentPromptTemplateDto Template { get; set; } = null!;

    [Parameter]
    public EventCallback<AgentPromptTemplateDto> OnPreview { get; set; }

    [Parameter]
    public EventCallback<AgentPromptTemplateDto> OnEdit { get; set; }

    [Parameter]
    public EventCallback<AgentPromptTemplateDto> OnClone { get; set; }

    [Parameter]
    public EventCallback<AgentPromptTemplateDto> OnDelete { get; set; }
}
