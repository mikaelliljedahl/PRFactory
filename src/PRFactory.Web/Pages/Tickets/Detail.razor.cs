using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Navigation;

namespace PRFactory.Web.Pages.Tickets;

public partial class Detail : IAsyncDisposable
{
    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<Detail> Logger { get; set; } = null!;

    private TicketDto? ticket;
    private List<QuestionDto>? questions;
    private List<WorkflowEventDto>? events;
    private HubConnection? hubConnection;
    private bool isLoading = true;
    private string? errorMessage;
    private HubConnectionState connectionState = HubConnectionState.Disconnected;
    private List<BreadcrumbItem> breadcrumbItems = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadTicket();

        if (ticket != null)
        {
            await LoadQuestions();
            await LoadEvents();
            await InitializeSignalR();
        }
    }

    private async Task LoadTicket()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            if (Guid.TryParse(Id, out var ticketId))
            {
                ticket = await TicketService.GetTicketDtoByIdAsync(ticketId);

                if (ticket != null)
                {
                    // Build breadcrumbs after ticket is loaded
                    BuildBreadcrumbs();
                }
            }
            else
            {
                errorMessage = "Invalid ticket ID format";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading ticket: {ex.Message}";
            ToastService.ShowError($"Failed to load ticket: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadQuestions()
    {
        try
        {
            if (Guid.TryParse(Id, out var ticketId))
            {
                // Use TicketService directly (Blazor Server architecture - NO HTTP calls)
                questions = await TicketService.GetQuestionsAsync(ticketId);
            }
            else
            {
                questions = new List<QuestionDto>();
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the page load
            Logger.LogWarning(ex, "Failed to load questions for ticket {TicketId}", Id);
            questions = new List<QuestionDto>();
        }
    }

    private async Task LoadEvents()
    {
        try
        {
            if (Guid.TryParse(Id, out var ticketId))
            {
                // Use TicketService directly (Blazor Server architecture - NO HTTP calls)
                events = await TicketService.GetEventsAsync(ticketId);
            }
            else
            {
                events = new List<WorkflowEventDto>();
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the page load
            Logger.LogWarning(ex, "Failed to load events for ticket {TicketId}", Id);
            events = new List<WorkflowEventDto>();
        }
    }

    private async Task InitializeSignalR()
    {
        try
        {
            connectionState = HubConnectionState.Connecting;
            StateHasChanged();

            hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/tickets"))
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<Guid, string>("TicketUpdated", async (ticketId, state) =>
            {
                if (ticketId == ticket?.Id)
                {
                    await LoadTicket();
                    await LoadQuestions();
                    await LoadEvents();
                    await InvokeAsync(StateHasChanged);
                }
            });

            hubConnection.Reconnecting += error =>
            {
                connectionState = HubConnectionState.Reconnecting;
                InvokeAsync(StateHasChanged);
                return Task.CompletedTask;
            };

            hubConnection.Reconnected += connectionId =>
            {
                connectionState = HubConnectionState.Connected;
                InvokeAsync(StateHasChanged);
                return Task.CompletedTask;
            };

            hubConnection.Closed += error =>
            {
                connectionState = HubConnectionState.Disconnected;
                InvokeAsync(StateHasChanged);
                return Task.CompletedTask;
            };

            await hubConnection.StartAsync();
            connectionState = HubConnectionState.Connected;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            connectionState = HubConnectionState.Disconnected;
            Logger.LogWarning(ex, "Failed to initialize SignalR connection for ticket {TicketId}", Id);
            StateHasChanged();
        }
    }

    private async Task HandleAnswersSubmitted()
    {
        ToastService.ShowSuccess("Your answers have been submitted successfully!");
        await LoadTicket();
        await LoadEvents();
    }

    private async Task HandleTicketUpdateApproved()
    {
        ToastService.ShowSuccess("Ticket update has been approved!");
        await LoadTicket();
        await LoadEvents();
    }

    private async Task HandleTicketUpdateRejected()
    {
        ToastService.ShowWarning("Ticket update has been rejected and will be regenerated.");
        await LoadTicket();
        await LoadEvents();
    }

    private async Task HandlePlanApproved()
    {
        ToastService.ShowSuccess("Implementation plan has been approved!");
        await LoadTicket();
        await LoadEvents();
    }

    private async Task HandlePlanRejected()
    {
        ToastService.ShowWarning("Plan has been rejected and will be regenerated.");
        await LoadTicket();
        await LoadEvents();
    }

    private enum HubConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting
    }

    private string GetConnectionAlertClass()
    {
        return connectionState switch
        {
            HubConnectionState.Connected => "alert-success",
            HubConnectionState.Connecting => "alert-info",
            HubConnectionState.Reconnecting => "alert-warning",
            HubConnectionState.Disconnected => "alert-danger",
            _ => "alert-secondary"
        };
    }

    private string GetConnectionIcon()
    {
        return connectionState switch
        {
            HubConnectionState.Connected => "bi-wifi",
            HubConnectionState.Connecting => "bi-wifi",
            HubConnectionState.Reconnecting => "bi-wifi",
            HubConnectionState.Disconnected => "bi-wifi-off",
            _ => "bi-wifi-off"
        };
    }

    private string GetConnectionStatusText()
    {
        return connectionState switch
        {
            HubConnectionState.Connected => "Connected",
            HubConnectionState.Connecting => "Connecting...",
            HubConnectionState.Reconnecting => "Reconnecting...",
            HubConnectionState.Disconnected => "Disconnected",
            _ => "Unknown"
        };
    }

    private void BuildBreadcrumbs()
    {
        breadcrumbItems = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Dashboard", Href = "/", Icon = "house" },
            new BreadcrumbItem { Text = "Tickets", Href = "/tickets", Icon = "ticket-detailed" },
            new BreadcrumbItem
            {
                Text = ticket?.TicketKey ?? Id,
                Icon = "file-text"
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
