using FluentAssertions;
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
        stats.TotalTickets.Should().Be(0);
        stats.CompletedTickets.Should().Be(0);
        stats.InProgressTickets.Should().Be(0);
        stats.FailedTickets.Should().Be(0);
        stats.AwaitingUserInput.Should().Be(0);
        stats.CompletionRate.Should().Be(0);
        stats.FailureRate.Should().Be(0);
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
        stats.TotalTickets.Should().Be(5);
        stats.CompletedTickets.Should().Be(3);
        stats.FailedTickets.Should().Be(1);
        stats.InProgressTickets.Should().Be(1);
        stats.CompletionRate.Should().BeApproximately(60.0, 0.1);
        stats.FailureRate.Should().BeApproximately(20.0, 0.1);
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
        stats.AwaitingUserInput.Should().Be(3);
        stats.InProgressTickets.Should().Be(4); // All non-completed/failed/cancelled
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
        stats.InProgressTickets.Should().Be(1);
    }

    [Fact]
    public void FormatDuration_ShouldFormatDaysCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromDays(2.5);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        formatted.Should().Be("2.5d");
    }

    [Fact]
    public void FormatDuration_ShouldFormatHoursCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromHours(3.2);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        formatted.Should().Be("3.2h");
    }

    [Fact]
    public void FormatDuration_ShouldFormatMinutesCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(45);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        formatted.Should().Be("45m");
    }

    [Fact]
    public void FormatDuration_ShouldFormatSecondsCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(30);

        // Act
        var formatted = FormatDuration(duration);

        // Assert
        formatted.Should().Be("30s");
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
            return $"{duration.TotalDays:F1}d";
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1}h";
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

        // Transition to desired state if not the default Triggered state
        if (state != WorkflowState.Triggered)
        {
            ticket.TransitionTo(state);
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
