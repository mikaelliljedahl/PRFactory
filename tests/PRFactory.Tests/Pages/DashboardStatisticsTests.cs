using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using Xunit;

namespace PRFactory.Tests.Pages;

public class DashboardStatisticsTests
{
    [Fact]
    public void CalculateStatistics_WithNoTickets_ShouldReturnZeros()
    {
        // Arrange
        var tickets = new List<Ticket>();
        var stats = new DashboardStatistics();

        // Act
        CalculateStatistics(tickets, stats);

        // Assert
        Assert.Equal(0, stats.TotalTickets);
        Assert.Equal(0, stats.CompletedTickets);
        Assert.Equal(0, stats.InProgressTickets);
        Assert.Equal(0, stats.FailedTickets);
        Assert.Equal(0, stats.AwaitingUserInput);
        Assert.Equal(0, stats.CompletionRate);
        Assert.Equal(0, stats.FailureRate);
    }

    [Fact]
    public void CalculateStatistics_WithCompletedTickets_ShouldCalculateCorrectRates()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            CreateTicket(WorkflowState.Completed),
            CreateTicket(WorkflowState.Completed),
            CreateTicket(WorkflowState.Completed),
            CreateTicket(WorkflowState.Failed),
            CreateTicket(WorkflowState.Analyzing)
        };
        var stats = new DashboardStatistics();

        // Act
        CalculateStatistics(tickets, stats);

        // Assert
        Assert.Equal(5, stats.TotalTickets);
        Assert.Equal(3, stats.CompletedTickets);
        Assert.Equal(1, stats.FailedTickets);
        Assert.Equal(1, stats.InProgressTickets);
        Assert.InRange(stats.CompletionRate, 60.0 - 0.1, 60.0 + 0.1);
        Assert.InRange(stats.FailureRate, 20.0 - 0.1, 20.0 + 0.1);
    }

    [Fact]
    public void CalculateStatistics_WithAwaitingUserInputTickets_ShouldCountCorrectly()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            CreateTicket(WorkflowState.AwaitingAnswers),
            CreateTicket(WorkflowState.PlanUnderReview),
            CreateTicket(WorkflowState.TicketUpdateUnderReview),
            CreateTicket(WorkflowState.Analyzing)
        };
        var stats = new DashboardStatistics();

        // Act
        CalculateStatistics(tickets, stats);

        // Assert
        Assert.Equal(3, stats.AwaitingUserInput);
        Assert.Equal(4, stats.InProgressTickets); // All non-completed/failed/cancelled
    }

    [Fact]
    public void CalculateStatistics_WithCancelledTickets_ShouldNotCountAsInProgress()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            CreateTicket(WorkflowState.Cancelled),
            CreateTicket(WorkflowState.Analyzing),
            CreateTicket(WorkflowState.Completed)
        };
        var stats = new DashboardStatistics();

        // Act
        CalculateStatistics(tickets, stats);

        // Assert
        Assert.Equal(1, stats.InProgressTickets);
    }

    [Fact]
    public void FormatDuration_ShouldFormatDaysCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromDays(2.5);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        Assert.Equal("2.5d", formatted);
    }

    [Fact]
    public void FormatDuration_ShouldFormatHoursCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromHours(3.2);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        Assert.Equal("3.2h", formatted);
    }

    [Fact]
    public void FormatDuration_ShouldFormatMinutesCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(45);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        Assert.Equal("45m", formatted);
    }

    [Fact]
    public void FormatDuration_ShouldFormatSecondsCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(30);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        Assert.Equal("30s", formatted);
    }

    // Helper methods that mirror the Dashboard implementation
    private static void CalculateStatistics(List<Ticket> tickets, DashboardStatistics stats)
    {
        stats.TotalTickets = tickets.Count;
        stats.CompletedTickets = tickets.Count(t => t.State == WorkflowState.Completed);
        stats.FailedTickets = tickets.Count(t => t.State == WorkflowState.Failed);
        stats.InProgressTickets = tickets.Count(t =>
            t.State != WorkflowState.Completed &&
            t.State != WorkflowState.Failed &&
            t.State != WorkflowState.Cancelled);

        stats.AwaitingUserInput = tickets.Count(t =>
            t.State == WorkflowState.AwaitingAnswers ||
            t.State == WorkflowState.PlanUnderReview ||
            t.State == WorkflowState.TicketUpdateUnderReview);

        if (stats.TotalTickets > 0)
        {
            stats.CompletionRate = (double)stats.CompletedTickets / stats.TotalTickets * 100;
            stats.FailureRate = (double)stats.FailedTickets / stats.TotalTickets * 100;
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{duration.TotalDays.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}d";
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}h";
        if (duration.TotalMinutes >= 1)
            return $"{duration.TotalMinutes:F0}m";
        return $"{duration.TotalSeconds:F0}s";
    }

    private static Ticket CreateTicket(WorkflowState state)
    {
        var ticket = Ticket.Create(
            $"TICKET-{Guid.NewGuid().ToString().Substring(0, 8)}",
            Guid.NewGuid(),
            Guid.NewGuid());
        ticket.UpdateTicketInfo("Test Ticket", "Test Description");

        // Transition through valid state paths to reach the desired state
        switch (state)
        {
            case WorkflowState.Triggered:
                // Already in this state
                break;

            case WorkflowState.Analyzing:
                ticket.TransitionTo(WorkflowState.Analyzing);
                break;

            case WorkflowState.Failed:
                ticket.TransitionTo(WorkflowState.Failed);
                break;

            case WorkflowState.Cancelled:
                ticket.TransitionTo(WorkflowState.Cancelled);
                break;

            case WorkflowState.TicketUpdateGenerated:
                ticket.TransitionTo(WorkflowState.Analyzing);
                ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
                break;

            case WorkflowState.TicketUpdateUnderReview:
                ticket.TransitionTo(WorkflowState.Analyzing);
                ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
                ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
                break;

            case WorkflowState.AwaitingAnswers:
                ticket.TransitionTo(WorkflowState.Analyzing);
                ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
                ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
                ticket.TransitionTo(WorkflowState.TicketUpdateApproved);
                ticket.TransitionTo(WorkflowState.TicketUpdatePosted);
                ticket.TransitionTo(WorkflowState.QuestionsPosted);
                ticket.TransitionTo(WorkflowState.AwaitingAnswers);
                break;

            case WorkflowState.PlanUnderReview:
                ticket.TransitionTo(WorkflowState.Analyzing);
                ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
                ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
                ticket.TransitionTo(WorkflowState.TicketUpdateApproved);
                ticket.TransitionTo(WorkflowState.TicketUpdatePosted);
                ticket.TransitionTo(WorkflowState.Planning);
                ticket.TransitionTo(WorkflowState.PlanPosted);
                ticket.TransitionTo(WorkflowState.PlanUnderReview);
                break;

            case WorkflowState.Completed:
                ticket.TransitionTo(WorkflowState.Analyzing);
                ticket.TransitionTo(WorkflowState.TicketUpdateGenerated);
                ticket.TransitionTo(WorkflowState.TicketUpdateUnderReview);
                ticket.TransitionTo(WorkflowState.TicketUpdateApproved);
                ticket.TransitionTo(WorkflowState.TicketUpdatePosted);
                ticket.TransitionTo(WorkflowState.Planning);
                ticket.TransitionTo(WorkflowState.PlanPosted);
                ticket.TransitionTo(WorkflowState.PlanUnderReview);
                ticket.TransitionTo(WorkflowState.PlanApproved);
                ticket.TransitionTo(WorkflowState.Completed);
                break;

            default:
                throw new ArgumentException($"Unsupported state transition to {state} in test helper");
        }

        return ticket;
    }

    private class DashboardStatistics
    {
        public int TotalTickets { get; set; }
        public int CompletedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int FailedTickets { get; set; }
        public int AwaitingUserInput { get; set; }
        public double CompletionRate { get; set; }
        public double FailureRate { get; set; }
    }
}
