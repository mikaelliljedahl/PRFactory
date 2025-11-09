using Microsoft.AspNetCore.Components;
using PRFactory.Web.Models;

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
}
