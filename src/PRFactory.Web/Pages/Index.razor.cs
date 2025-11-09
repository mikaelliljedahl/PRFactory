using Microsoft.AspNetCore.Components;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Pages;

public partial class Index
{
    [Inject] private ITicketService TicketService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private List<Ticket> tickets = new();
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadTickets();
    }

    private async Task LoadTickets()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            tickets = await TicketService.GetAllTicketsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading tickets: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ViewTicket(Guid ticketId)
    {
        Navigation.NavigateTo($"/tickets/{ticketId}");
    }

    private string GetStatusBadgeColor(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Triggered => "primary",
            WorkflowState.Analyzing => "primary",
            WorkflowState.QuestionsPosted => "warning",
            WorkflowState.AwaitingAnswers => "warning",
            WorkflowState.AnswersReceived => "info",
            WorkflowState.Planning => "primary",
            WorkflowState.PlanPosted => "info",
            WorkflowState.PlanUnderReview => "info",
            WorkflowState.PlanApproved => "success",
            WorkflowState.PlanRejected => "danger",
            WorkflowState.Implementing => "primary",
            WorkflowState.ImplementationFailed => "danger",
            WorkflowState.PRCreated => "success",
            WorkflowState.InReview => "info",
            WorkflowState.Completed => "success",
            WorkflowState.Cancelled => "secondary",
            WorkflowState.Failed => "danger",
            _ => "primary"
        };
    }
}
