using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PRFactory.Web.Components.Settings;

public partial class AllowedRepositoriesEditor
{
    [Parameter]
    public string[]? Value { get; set; }

    [Parameter]
    public EventCallback<string[]?> ValueChanged { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private string newRepository = string.Empty;

    private async Task AddRepository()
    {
        if (string.IsNullOrWhiteSpace(newRepository))
            return;

        var trimmedRepo = newRepository.Trim();

        // Avoid duplicates
        if (Value != null && Value.Contains(trimmedRepo))
        {
            newRepository = string.Empty;
            return;
        }

        var updatedRepos = Value == null
            ? new[] { trimmedRepo }
            : Value.Append(trimmedRepo).ToArray();

        Value = updatedRepos;
        await ValueChanged.InvokeAsync(Value);

        newRepository = string.Empty;
    }

    private async Task RemoveRepository(string repo)
    {
        if (Value == null)
            return;

        var updatedRepos = Value.Where(r => r != repo).ToArray();

        Value = updatedRepos.Length > 0 ? updatedRepos : null;
        await ValueChanged.InvokeAsync(Value);
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await AddRepository();
        }
    }
}
