using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.AgentPrompts;

public partial class PromptTemplateForm
{
    [Parameter, EditorRequired]
    public object Model { get; set; } = null!;

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public bool IsSubmitting { get; set; }

    [Parameter]
    public string SubmitButtonText { get; set; } = "Save Template";

    [Parameter]
    public bool IsNewTemplate { get; set; } = true;

    private async Task HandleValidSubmit()
    {
        await OnValidSubmit.InvokeAsync();
    }
}
