using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using PRFactory.Web.UI.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PRFactory.Web.Pages.Tickets;

public partial class Index : IAsyncDisposable
{
    private List<TicketDto>? tickets;
    private List<RepositoryInfo>? repositories;
    private HubConnection? hubConnection;

    private int currentPage = 1;
    private int totalPages = 1;
    private int? totalItems;
    private int pageSize = 12;

    private string? filterState;
    private string? filterSource;
    private string? filterRepositoryId;

    private bool isLoading = true;
    private string? errorMessage;

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

            // TODO: Replace with actual API call that supports filtering and pagination
            // For now, load all tickets and filter/paginate in memory
            var allTickets = await TicketService.GetAllTicketsAsync();

            // Map to DTOs
            var ticketDtos = allTickets.Select(t => new TicketDto
            {
                Id = t.Id,
                TicketKey = t.TicketKey,
                Title = t.Title,
                Description = t.Description,
                State = t.State,
                Source = t.Source,
                RepositoryId = t.RepositoryId,
                RepositoryName = t.Repository?.Name,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                CompletedAt = t.CompletedAt,
                PullRequestUrl = t.PullRequestUrl,
                PullRequestNumber = t.PullRequestNumber,
                LastError = t.LastError
            }).ToList();

            // Apply filters
            var filteredTickets = ApplyFilters(ticketDtos);

            // Calculate pagination
            totalItems = filteredTickets.Count;
            totalPages = (int)Math.Ceiling((double)totalItems.Value / pageSize);
            currentPage = Math.Max(1, Math.Min(currentPage, totalPages));

            // Apply pagination
            tickets = filteredTickets
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Logger.LogInformation("Loaded {Count} tickets (page {Page} of {Total})",
                tickets.Count, currentPage, totalPages);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading tickets");
            errorMessage = "Failed to load tickets. Please try again.";
            tickets = new List<TicketDto>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private List<TicketDto> ApplyFilters(List<TicketDto> allTickets)
    {
        var filtered = allTickets.AsEnumerable();

        // Filter by state
        if (!string.IsNullOrWhiteSpace(filterState) && Enum.TryParse<WorkflowState>(filterState, out var state))
        {
            filtered = filtered.Where(t => t.State == state);
        }

        // Filter by source
        if (!string.IsNullOrWhiteSpace(filterSource) && Enum.TryParse<TicketSource>(filterSource, out var source))
        {
            filtered = filtered.Where(t => t.Source == source);
        }

        // Filter by repository
        if (!string.IsNullOrWhiteSpace(filterRepositoryId) && Guid.TryParse(filterRepositoryId, out var repoId))
        {
            filtered = filtered.Where(t => t.RepositoryId == repoId);
        }

        return filtered.OrderByDescending(t => t.CreatedAt).ToList();
    }

    private async Task OnPageChangedAsync(int page)
    {
        currentPage = page;
        await LoadTicketsAsync();
    }

    private async Task OnStateFilterChanged(string? value)
    {
        filterState = value;
        currentPage = 1; // Reset to first page when filter changes
        await LoadTicketsAsync();
    }

    private async Task OnSourceFilterChanged(string? value)
    {
        filterSource = value;
        currentPage = 1;
        await LoadTicketsAsync();
    }

    private async Task OnRepositoryFilterChanged(string? value)
    {
        filterRepositoryId = value;
        currentPage = 1;
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
