using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.DTOs;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PRFactory.Web.Components.TicketFilters;

namespace PRFactory.Web.Pages.Tickets;

public partial class Index : IAsyncDisposable
{
    private PagedResult<TicketDto>? pagedTickets;
    private List<RepositoryInfo>? repositories;
    private HubConnection? hubConnection;

    private PaginationParams paginationParams = new()
    {
        Page = 1,
        PageSize = 12,
        SortBy = "created",
        Descending = true
    };

    private WorkflowState? stateFilter;
    private string? filterSource;
    private string? filterRepositoryId;

    private bool isLoading = true;
    private string? errorMessage;

    // Helper property for UI binding (converts WorkflowState? to string?)
    private string? filterState
    {
        get => stateFilter?.ToString();
        set => stateFilter = string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<WorkflowState>(value);
    }

    private List<BreadcrumbItem> breadcrumbItems = new()
    {
        new BreadcrumbItem { Text = "Dashboard", Href = "/", Icon = "house" },
        new BreadcrumbItem { Text = "Tickets", Icon = "ticket-detailed" }
    };

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IRepositoryService RepositoryService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Index> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadRepositoriesAsync();
        await LoadTicketsAsync();
        await InitializeSignalRAsync();
    }

    private async Task LoadRepositoriesAsync()
    {
        try
        {
            var allRepos = await RepositoryService.GetAllRepositoriesAsync();
            repositories = allRepos.Select(r => new RepositoryInfo
            {
                Id = r.Id,
                Name = r.Name
            }).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading repositories");
            // Non-critical, just log the error
        }
    }

    private async Task LoadTicketsAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = null;
            StateHasChanged();

            // Server-side pagination with filtering and sorting
            pagedTickets = await TicketService.GetTicketsPagedAsync(
                paginationParams,
                stateFilter);

            Logger.LogInformation("Loaded page {Page} of {TotalPages} (Showing {Count} of {TotalCount} tickets)",
                pagedTickets.Page, pagedTickets.TotalPages, pagedTickets.Items.Count, pagedTickets.TotalCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading tickets");
            errorMessage = "Failed to load tickets. Please try again.";
            pagedTickets = new PagedResult<TicketDto>
            {
                Items = new List<TicketDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = paginationParams.PageSize
            };
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnPageChangedAsync(int page)
    {
        paginationParams.Page = page;
        await LoadTicketsAsync();
    }

    private async Task OnStateFilterChanged(string? value)
    {
        filterState = value; // The property setter handles the conversion
        paginationParams.Page = 1; // Reset to first page when filter changes
        await LoadTicketsAsync();
    }

    private async Task OnSourceFilterChanged(string? value)
    {
        filterSource = value;
        paginationParams.Page = 1;
        await LoadTicketsAsync();
    }

    private async Task OnRepositoryFilterChanged(string? value)
    {
        filterRepositoryId = value;
        paginationParams.Page = 1;
        await LoadTicketsAsync();
    }

    private async Task InitializeSignalRAsync()
    {
        try
        {
            // Build the SignalR hub connection
            hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/tickets"))
                .WithAutomaticReconnect()
                .Build();

            // Subscribe to ticket update events
            hubConnection.On<Guid, string>("TicketUpdated", async (ticketId, state) =>
            {
                Logger.LogInformation("Received SignalR update for ticket {TicketId}: {State}", ticketId, state);

                // Reload the tickets to reflect the update
                await LoadTicketsAsync();
            });

            // Subscribe to ticket created events
            hubConnection.On<Guid>("TicketCreated", async (ticketId) =>
            {
                Logger.LogInformation("Received SignalR notification for new ticket {TicketId}", ticketId);

                // Reload the tickets to show the new ticket
                await LoadTicketsAsync();
            });

            // Start the connection
            await hubConnection.StartAsync();
            Logger.LogInformation("SignalR connection established");

            // Subscribe to all ticket updates
            await hubConnection.InvokeAsync("SubscribeToAllTickets");
            Logger.LogInformation("Subscribed to all ticket updates");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing SignalR connection");
            // Non-critical error, continue without real-time updates
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection != null)
        {
            try
            {
                await hubConnection.InvokeAsync("UnsubscribeFromAllTickets");
                await hubConnection.DisposeAsync();
                Logger.LogInformation("SignalR connection disposed");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error disposing SignalR connection");
            }
        }
    }
}
