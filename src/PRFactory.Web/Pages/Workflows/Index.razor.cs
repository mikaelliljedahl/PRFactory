using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Navigation;

namespace PRFactory.Web.Pages.Workflows;

public partial class Index
{
    private List<TicketDto>? workflows;
    private bool isLoading = true;
    private string? errorMessage;

    // Statistics
    private int activeWorkflowsCount;
    private int awaitingInputCount;
    private int completedTodayCount;
    private int failedCount;

    private List<BreadcrumbItem> breadcrumbItems = new()
    {
        new BreadcrumbItem { Text = "Dashboard", Href = "/", Icon = "house" },
        new BreadcrumbItem { Text = "Workflows", Icon = "diagram-3" }
    };

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Index> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadWorkflows();
    }

    private async Task LoadWorkflows()
    {
        try
        {
            isLoading = true;
            errorMessage = null;
            StateHasChanged();

            var allTickets = await TicketService.GetAllTicketsAsync();

            workflows = allTickets
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            // Calculate statistics
            CalculateStatistics();

            Logger.LogInformation("Loaded {Count} workflows", workflows.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading workflows");
            errorMessage = "Failed to load workflows. Please try again.";
            workflows = new List<TicketDto>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void CalculateStatistics()
    {
        if (workflows == null)
        {
            activeWorkflowsCount = 0;
            awaitingInputCount = 0;
            completedTodayCount = 0;
            failedCount = 0;
            return;
        }

        activeWorkflowsCount = workflows.Count(w => IsActiveState(w.State));
        awaitingInputCount = workflows.Count(w => IsAwaitingState(w.State));

        var today = DateTime.UtcNow.Date;
        completedTodayCount = workflows.Count(w =>
            w.State == WorkflowState.Completed &&
            w.CompletedAt.HasValue &&
            w.CompletedAt.Value.Date == today);

        failedCount = workflows.Count(w =>
            w.State == WorkflowState.Failed ||
            w.State == WorkflowState.ImplementationFailed);
    }

    private bool IsActiveState(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.Triggered => true,
            WorkflowState.Analyzing => true,
            WorkflowState.TicketUpdateGenerated => true,
            WorkflowState.TicketUpdateApproved => true,
            WorkflowState.TicketUpdatePosted => true,
            WorkflowState.AnswersReceived => true,
            WorkflowState.Planning => true,
            WorkflowState.PlanPosted => true,
            WorkflowState.PlanApproved => true,
            WorkflowState.Implementing => true,
            WorkflowState.PRCreated => true,
            _ => false
        };
    }

    private bool IsAwaitingState(WorkflowState state)
    {
        return state switch
        {
            WorkflowState.AwaitingAnswers => true,
            WorkflowState.PlanUnderReview => true,
            WorkflowState.TicketUpdateUnderReview => true,
            WorkflowState.InReview => true,
            _ => false
        };
    }

    private string GetDuration(TicketDto workflow)
    {
        if (workflow.CompletedAt.HasValue)
        {
            var duration = workflow.CompletedAt.Value - workflow.CreatedAt;
            return FormatDuration(duration);
        }

        var currentDuration = DateTime.UtcNow - workflow.CreatedAt;
        return FormatDuration(currentDuration);
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1)
            return "< 1 min";
        if (duration.TotalHours < 1)
            return $"{(int)duration.TotalMinutes} min";
        if (duration.TotalDays < 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        if (duration.TotalDays < 7)
            return $"{(int)duration.TotalDays}d {duration.Hours}h";

        return $"{(int)duration.TotalDays} days";
    }

    private void ViewTicket(Guid ticketId)
    {
        NavigationManager.NavigateTo($"/tickets/{ticketId}");
    }
}
