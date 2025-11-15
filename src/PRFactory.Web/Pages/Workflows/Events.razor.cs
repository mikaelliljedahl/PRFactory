using Microsoft.AspNetCore.Components;
using PRFactory.Domain.DTOs;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using System.Timers;

namespace PRFactory.Web.Pages.Workflows;

public partial class Events : IDisposable
{
    private PagedResult<WorkflowEventDto>? pagedResult;
    private EventStatisticsDto? statistics;
    private List<string> eventTypes = new();
    private bool isLoading = true;
    private string? errorMessage;

    // Filters
    private string? selectedEventType;
    private DateTime? startDate;
    private DateTime? endDate;
    private string? selectedSeverity;
    private string? searchText;

    // Pagination
    private int currentPage = 1;
    private const int pageSize = 50;

    // Selection
    private IList<WorkflowEventDto>? selectedEvents;
    private WorkflowEventDto? selectedEvent;

    // Auto-refresh
    private bool autoRefresh = false;
    private System.Timers.Timer? refreshTimer;
    private bool _disposed;

    [Inject]
    private IWorkflowEventService WorkflowEventService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Events> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            isLoading = true;
            errorMessage = null;
            StateHasChanged();

            // Load event types for filter
            eventTypes = await WorkflowEventService.GetEventTypesAsync();

            // Load events
            await LoadEvents();

            // Load statistics
            await LoadStatistics();

            Logger.LogInformation("Loaded event log data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading event log data");
            errorMessage = "Failed to load event log. Please try again.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadEvents()
    {

        // Filter by severity if selected
        string? eventTypeFilter = selectedEventType;
        if (!string.IsNullOrEmpty(selectedSeverity) && string.IsNullOrEmpty(selectedEventType))
        {
            // If only severity is selected, we need to filter client-side
            // For now, we'll load all and filter later
        }

        pagedResult = await WorkflowEventService.GetEventsAsync(
            currentPage,
            pageSize,
            null, // ticketId
            eventTypeFilter,
            startDate,
            endDate,
            searchText);

        // Filter by severity on client side if needed
        if (!string.IsNullOrEmpty(selectedSeverity) && Enum.TryParse<EventSeverity>(selectedSeverity, out var severity))
        {
            var filteredItems = pagedResult.Items.Where(e => e.Severity == severity).ToList();
            pagedResult = new PagedResult<WorkflowEventDto>(
                filteredItems,
                currentPage,
                pageSize,
                filteredItems.Count);
        }
    }

    private async Task LoadStatistics()
    {
        statistics = await WorkflowEventService.GetStatisticsAsync(startDate, endDate);
    }

    private async Task RefreshData()
    {
        await LoadData();
    }

    private async Task OnFilterChanged()
    {
        currentPage = 1; // Reset to first page when filters change
        await LoadEvents();
        await LoadStatistics();
    }

    private async Task ClearAllFilters()
    {
        selectedEventType = null;
        startDate = null;
        endDate = null;
        selectedSeverity = null;
        searchText = null;
        currentPage = 1;
        await LoadData();
    }

    private async Task PreviousPage()
    {
        if (pagedResult?.HasPreviousPage == true)
        {
            currentPage--;
            await LoadEvents();
        }
    }

    private async Task NextPage()
    {
        if (pagedResult?.HasNextPage == true)
        {
            currentPage++;
            await LoadEvents();
        }
    }

    private async Task GoToPage(int pageNumber)
    {
        currentPage = pageNumber;
        await LoadEvents();
    }

    private void OnRowClick(Radzen.DataGridRowMouseEventArgs<WorkflowEventDto> args)
    {
        ViewEventDetail(args.Data);
    }

    private void ViewEventDetail(WorkflowEventDto evt)
    {
        selectedEvent = evt;
        StateHasChanged();
    }

    private void ToggleAutoRefresh()
    {
        autoRefresh = !autoRefresh;

        if (autoRefresh)
        {
            // Start auto-refresh timer (5 seconds)
            refreshTimer = new System.Timers.Timer(5000);
            refreshTimer.Elapsed += OnAutoRefreshElapsed;
            refreshTimer.AutoReset = true;
            refreshTimer.Start();
            Logger.LogInformation("Auto-refresh enabled");
        }
        else
        {
            // Stop auto-refresh timer
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            refreshTimer = null;
            Logger.LogInformation("Auto-refresh disabled");
        }
    }

    private async void OnAutoRefreshElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await InvokeAsync(async () =>
            {
                await LoadEvents();
                await LoadStatistics();
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during auto-refresh");
        }
    }

    private async Task ExportToCsv()
    {
        try
        {
            var csvData = await WorkflowEventService.ExportToCsvAsync(
                null,
                selectedEventType,
                startDate,
                endDate);

            DownloadFile("workflow-events.csv", "text/csv", csvData);
            Logger.LogInformation("Exported events to CSV");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting to CSV");
            errorMessage = "Failed to export events. Please try again.";
        }
    }

    private async Task ExportToJson()
    {
        try
        {
            var jsonData = await WorkflowEventService.ExportToJsonAsync(
                null,
                selectedEventType,
                startDate,
                endDate);

            DownloadFile("workflow-events.json", "application/json", jsonData);
            Logger.LogInformation("Exported events to JSON");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting to JSON");
            errorMessage = "Failed to export events. Please try again.";
        }
    }

    /// <summary>
    /// Downloads a file using pure Blazor approach with data URI (no JavaScript required).
    /// </summary>
    private void DownloadFile(string fileName, string contentType, byte[] data)
    {
        var base64 = Convert.ToBase64String(data);
        var dataUri = $"data:{contentType};base64,{base64}";

        // Use NavigationManager to trigger download (Blazor-native approach)
        // The 'true' parameter forces the browser to download rather than navigate
        NavigationManager.NavigateTo(dataUri, forceLoad: true);
    }

    private string GetSeverityBadgeClass(EventSeverity severity)
    {
        return severity switch
        {
            EventSeverity.Success => "bg-success",
            EventSeverity.Error => "bg-danger",
            EventSeverity.Warning => "bg-warning text-dark",
            EventSeverity.Info => "bg-info",
            _ => "bg-secondary"
        };
    }

    private string GetEventsTitle()
    {
        if (pagedResult == null) return "Events";

        var count = pagedResult.TotalCount;
        var filtered = selectedEventType != null || startDate != null || endDate != null || selectedSeverity != null || searchText != null;

        return filtered ? $"Events ({count} filtered)" : $"Events ({count})";
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                refreshTimer?.Stop();
                refreshTimer?.Dispose();
            }
            _disposed = true;
        }
    }
}
