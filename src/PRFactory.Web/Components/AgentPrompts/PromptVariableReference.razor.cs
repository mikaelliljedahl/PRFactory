using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PRFactory.Web.Components.AgentPrompts;

public partial class PromptVariableReference
{
    [Parameter]
    public EventCallback<string> OnVariableSelected { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    private string? CopiedVariable { get; set; }

    private Dictionary<string, string> TicketVariables { get; } = new()
    {
        { "{{TicketKey}}", "Ticket identifier (e.g., PROJ-123)" },
        { "{{TicketTitle}}", "Original ticket title" },
        { "{{TicketDescription}}", "Original ticket description" },
        { "{{UpdatedTitle}}", "AI-refined ticket title" },
        { "{{UpdatedDescription}}", "AI-refined description" },
        { "{{AcceptanceCriteria}}", "Acceptance criteria list" }
    };

    private Dictionary<string, string> RepositoryVariables { get; } = new()
    {
        { "{{RepositoryName}}", "Repository name" },
        { "{{RepositoryUrl}}", "Full repository URL" },
        { "{{BranchName}}", "Target branch name" }
    };

    private Dictionary<string, string> PlanVariables { get; } = new()
    {
        { "{{PlanContent}}", "Implementation plan markdown" },
        { "{{UserName}}", "Current user name" }
    };

    private async Task OnVariableClick(string variable)
    {
        try
        {
            // Copy to clipboard
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", variable);

            // Show success message
            CopiedVariable = variable;

            // Notify parent component
            await OnVariableSelected.InvokeAsync(variable);

            // Clear success message after 2 seconds
            _ = Task.Delay(2000).ContinueWith(_ =>
            {
                CopiedVariable = null;
                StateHasChanged();
            });
        }
        catch (Exception)
        {
            // Clipboard API might not be available in all contexts
            // Just notify parent component
            await OnVariableSelected.InvokeAsync(variable);
        }
    }
}
