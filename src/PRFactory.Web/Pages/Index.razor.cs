using Microsoft.AspNetCore.Components;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages;

public partial class Index
{
    [Inject] private ITicketService TicketService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private List<TicketDto> tickets = new();
    private List<TicketDto> recentTickets = new();
    private bool isLoading = true;
    private string? errorMessage;

    private DashboardStatistics statistics = new();
    private List<StateDistribution> stateDistribution = new();
    private WorkflowDurationStats? durationStats;

    private string[] chartColors = new[]
    {
        "#0d6efd", "#6c757d", "#198754", "#dc3545", "#ffc107",
        "#0dcaf0", "#d63384", "#fd7e14", "#6610f2", "#20c997"
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            tickets = await TicketService.GetAllTicketsAsync();

            CalculateStatistics();
            CalculateStateDistribution();
            CalculateDurationStats();

            recentTickets = tickets
                .OrderByDescending(t => t.UpdatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading dashboard: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void CalculateStatistics()
    {
        statistics.TotalTickets = tickets.Count;
        statistics.CompletedTickets = tickets.Count(t => t.State == WorkflowState.Completed);
        statistics.FailedTickets = tickets.Count(t => t.State == WorkflowState.Failed);
        statistics.InProgressTickets = tickets.Count(t =>
            t.State != WorkflowState.Completed &&
            t.State != WorkflowState.Failed &&
            t.State != WorkflowState.Cancelled);

        statistics.AwaitingUserInput = tickets.Count(t =>
            t.State == WorkflowState.AwaitingAnswers ||
            t.State == WorkflowState.PlanUnderReview ||
            t.State == WorkflowState.TicketUpdateUnderReview);

        if (statistics.TotalTickets > 0)
        {
            statistics.CompletionRate = (double)statistics.CompletedTickets / statistics.TotalTickets * 100;
            statistics.FailureRate = (double)statistics.FailedTickets / statistics.TotalTickets * 100;
        }
    }

    private void CalculateStateDistribution()
    {
        stateDistribution = tickets
            .GroupBy(t => t.State)
            .Select(g => new StateDistribution
            {
                State = g.Key.ToString(),
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    private void CalculateDurationStats()
    {
        var completedTickets = tickets
            .Where(t => t.State == WorkflowState.Completed && t.CompletedAt.HasValue)
            .ToList();

        if (!completedTickets.Any())
        {
            durationStats = null;
            return;
        }

        var durations = completedTickets
            .Select(t => t.CompletedAt!.Value - t.CreatedAt)
            .ToList();

        var avgDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
        var fastestDuration = durations.Min();
        var slowestDuration = durations.Max();

        durationStats = new WorkflowDurationStats
        {
            AverageDuration = FormatDuration(avgDuration),
            FastestDuration = FormatDuration(fastestDuration),
            SlowestDuration = FormatDuration(slowestDuration)
        };
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{duration.TotalDays:F1}d";
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1}h";
        if (duration.TotalMinutes >= 1)
            return $"{duration.TotalMinutes:F0}m";
        return $"{duration.TotalSeconds:F0}s";
    }

    private void NavigateToCreateTicket()
    {
        Navigation.NavigateTo("/tickets/create");
    }

    private void NavigateToRepositories()
    {
        Navigation.NavigateTo("/repositories");
    }

    private void NavigateToWorkflows()
    {
        Navigation.NavigateTo("/workflows");
    }

    private void NavigateToTickets()
    {
        Navigation.NavigateTo("/tickets");
    }

    private sealed class DashboardStatistics
    {
        public int TotalTickets { get; set; }
        public int CompletedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int FailedTickets { get; set; }
        public int AwaitingUserInput { get; set; }
        public double CompletionRate { get; set; }
        public double FailureRate { get; set; }
    }

    private sealed class StateDistribution
    {
        public string State { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private sealed class WorkflowDurationStats
    {
        public string AverageDuration { get; set; } = string.Empty;
        public string FastestDuration { get; set; } = string.Empty;
        public string SlowestDuration { get; set; } = string.Empty;
    }
}
