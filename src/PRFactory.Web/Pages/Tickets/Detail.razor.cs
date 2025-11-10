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
            Console.WriteLine($"Error loading questions: {ex.Message}");
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
            Console.WriteLine($"Error loading events: {ex.Message}");
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
            Console.WriteLine($"Error initializing SignalR: {ex.Message}");
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

    private string GetWorkflowStateHelpText()
    {
        if (ticket == null) return string.Empty;

        return ticket.State switch
        {
            PRFactory.Domain.ValueObjects.WorkflowState.AwaitingAnswers =>
                "The AI has posted clarifying questions to help better understand your requirements. Please review and answer them to continue the workflow.",

            PRFactory.Domain.ValueObjects.WorkflowState.TicketUpdateUnderReview =>
                "The AI has generated a refined ticket description based on the analysis. Review the changes and approve or reject them.",

            PRFactory.Domain.ValueObjects.WorkflowState.PlanUnderReview =>
                "The AI has created an implementation plan. Review the plan details and decide whether to approve, refine, or reject it.",

            PRFactory.Domain.ValueObjects.WorkflowState.PRCreated =>
                "The AI has implemented the code and created a pull request. Review the changes in your repository and merge when ready.",

            PRFactory.Domain.ValueObjects.WorkflowState.InReview =>
                "The pull request is currently under review. Check your repository for the latest status.",

            PRFactory.Domain.ValueObjects.WorkflowState.Completed =>
                "The ticket has been completed and the pull request has been merged successfully.",

            PRFactory.Domain.ValueObjects.WorkflowState.Failed =>
                "The workflow encountered an error and could not complete. Check the error details below.",

            PRFactory.Domain.ValueObjects.WorkflowState.Cancelled =>
                "This ticket has been cancelled and will not be processed.",

            PRFactory.Domain.ValueObjects.WorkflowState.Triggered =>
                "The workflow has been triggered and is starting the initial analysis.",

            PRFactory.Domain.ValueObjects.WorkflowState.Analyzing =>
                "The AI is analyzing your codebase to understand the context and requirements.",

            PRFactory.Domain.ValueObjects.WorkflowState.Planning =>
                "The AI is creating an implementation plan based on your requirements.",

            PRFactory.Domain.ValueObjects.WorkflowState.Implementing =>
                "The AI is implementing the code based on the approved plan.",

            _ => "The workflow is processing. Current state: " + ticket.StateDisplay
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
