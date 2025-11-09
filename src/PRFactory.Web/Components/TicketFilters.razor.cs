using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Components;

public partial class TicketFilters
{
    [Parameter]
    public string? SelectedState { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedStateChanged { get; set; }

    [Parameter]
    public string? SelectedSource { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedSourceChanged { get; set; }

    [Parameter]
    public string? SelectedRepositoryId { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedRepositoryIdChanged { get; set; }

    [Parameter]
    public EventCallback OnFilterChanged { get; set; }

    [Parameter]
    public List<RepositoryInfo>? Repositories { get; set; }

    /// <summary>
    /// Synchronous wrapper for @bind:after, which expects Action not EventCallback
    /// </summary>
    private void InvokeFilterChanged()
    {
        _ = OnFilterChanged.InvokeAsync();
    }

    private async Task ClearFilters()
    {
        SelectedState = null;
        SelectedSource = null;
        SelectedRepositoryId = null;

        await SelectedStateChanged.InvokeAsync(null);
        await SelectedSourceChanged.InvokeAsync(null);
        await SelectedRepositoryIdChanged.InvokeAsync(null);
        await OnFilterChanged.InvokeAsync();
    }

    /// <summary>
    /// Simple DTO for repository dropdown
    /// </summary>
    public class RepositoryInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
