using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PRFactory.Web.Models;

namespace PRFactory.Web.Components.Workflows;

public partial class EventLogFilter
{
    [Parameter]
    public List<string> EventTypes { get; set; } = new();

    [Parameter]
    public EventCallback OnFilterChange { get; set; }

    [Parameter]
    public string? SelectedEventType { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedEventTypeChanged { get; set; }

    [Parameter]
    public DateRange? DateRange { get; set; }

    [Parameter]
    public EventCallback<DateRange?> DateRangeChanged { get; set; }

    [Parameter]
    public string? SelectedSeverity { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedSeverityChanged { get; set; }

    [Parameter]
    public string? SearchText { get; set; }

    [Parameter]
    public EventCallback<string?> SearchTextChanged { get; set; }

    private List<string> SeverityOptions = new()
    {
        "Info",
        "Success",
        "Warning",
        "Error"
    };

    private async Task OnFilterChanged()
    {
        await OnFilterChange.InvokeAsync();
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchTextChanged.InvokeAsync(SearchText);
            await OnFilterChange.InvokeAsync();
        }
    }

    private async Task ClearSearch()
    {
        SearchText = null;
        await SearchTextChanged.InvokeAsync(SearchText);
        await OnFilterChange.InvokeAsync();
    }

    private async Task ClearAllFilters()
    {
        SelectedEventType = null;
        DateRange = null;
        SelectedSeverity = null;
        SearchText = null;

        await SelectedEventTypeChanged.InvokeAsync(SelectedEventType);
        await DateRangeChanged.InvokeAsync(DateRange);
        await SelectedSeverityChanged.InvokeAsync(SelectedSeverity);
        await SearchTextChanged.InvokeAsync(SearchText);
        await OnFilterChange.InvokeAsync();
    }
}
