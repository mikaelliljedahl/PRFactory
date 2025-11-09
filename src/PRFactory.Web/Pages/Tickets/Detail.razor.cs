using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PRFactory.Api.Models;
using PRFactory.Web.Models;

namespace PRFactory.Web.Pages.Tickets;

public partial class Detail : IAsyncDisposable
{
    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    private TicketDto? ticket;
    private List<QuestionDto>? questions;
    private List<WorkflowEventDto>? events;
    private HubConnection? hubConnection;
    private bool isLoading = true;
    private string? errorMessage;

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
                // TODO: Replace with proper API call that returns TicketDto
                // For now, get the Ticket entity and map to TicketDto
                var ticketEntity = await TicketService.GetTicketByIdAsync(ticketId);

                if (ticketEntity != null)
                {
                    ticket = new TicketDto
                    {
                        Id = ticketEntity.Id,
                        TicketKey = ticketEntity.TicketKey,
                        Title = ticketEntity.Title,
                        Description = ticketEntity.Description,
                        State = ticketEntity.State,
                        Source = ticketEntity.Source,
                        RepositoryId = ticketEntity.RepositoryId,
                        RepositoryName = ticketEntity.Repository?.Name,
                        CreatedAt = ticketEntity.CreatedAt,
                        UpdatedAt = ticketEntity.UpdatedAt,
                        CompletedAt = ticketEntity.CompletedAt,
                        PullRequestUrl = ticketEntity.PullRequestUrl,
                        PullRequestNumber = ticketEntity.PullRequestNumber,
                        PlanBranchName = ticketEntity.PlanBranchName,
                        PlanMarkdownPath = ticketEntity.PlanMarkdownPath,
                        LastError = ticketEntity.LastError
                    };
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
            // TODO: Implement API endpoint GET /api/tickets/{id}/questions
            // For now, return empty list
            var client = HttpClientFactory.CreateClient("PRFactoryApi");
            var response = await client.GetFromJsonAsync<QuestionsResponse>($"/api/tickets/{Id}/questions");
            questions = response?.Questions ?? new List<QuestionDto>();
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
            // TODO: Implement API endpoint GET /api/tickets/{id}/events
            // For now, return empty list
            var client = HttpClientFactory.CreateClient("PRFactoryApi");
            events = await client.GetFromJsonAsync<List<WorkflowEventDto>>($"/api/tickets/{Id}/events");
            events ??= new List<WorkflowEventDto>();
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
            // TODO: Set up SignalR connection for real-time ticket updates
            // hubConnection = new HubConnectionBuilder()
            //     .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/tickets"))
            //     .Build();
            //
            // hubConnection.On<TicketDto>("TicketUpdated", async (updatedTicket) =>
            // {
            //     if (updatedTicket.Id == ticket?.Id)
            //     {
            //         ticket = updatedTicket;
            //         await InvokeAsync(StateHasChanged);
            //     }
            // });
            //
            // await hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            // Log but don't fail the page load
            Console.WriteLine($"Error initializing SignalR: {ex.Message}");
        }
    }

    private async Task HandleAnswersSubmitted()
    {
        await LoadTicket();
        await LoadEvents();
    }

    private async Task HandlePlanApproved()
    {
        await LoadTicket();
        await LoadEvents();
    }

    private async Task HandlePlanRejected()
    {
        await LoadTicket();
        await LoadEvents();
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
