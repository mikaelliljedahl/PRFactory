using Bunit;
using PRFactory.Core.Application.DTOs;
using PRFactory.Web.Components.Repositories;
using Xunit;

namespace PRFactory.Web.Tests.Components.Repositories;

/// <summary>
/// Tests for the RepositoryStatistics component.
/// Verifies rendering of repository statistics including tickets, pull requests, and access timestamps.
/// </summary>
public class RepositoryStatisticsTests : TestContext
{
    [Fact]
    public void Render_WithStatistics_DisplaysTotalTickets()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 42,
            TotalPullRequests = 15,
            LastAccessedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("42", markup);
        Assert.Contains("Total Tickets", markup);
    }

    [Fact]
    public void Render_WithStatistics_DisplaysTotalPullRequests()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 42,
            TotalPullRequests = 15,
            LastAccessedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("15", markup);
        Assert.Contains("Pull Requests", markup);
    }

    [Fact]
    public void Render_WithLastAccessedAt_DisplaysLastAccessedTimestamp()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 10,
            TotalPullRequests = 5,
            LastAccessedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Last Accessed", markup);
    }

    [Fact]
    public void Render_WithoutLastAccessedAt_DisplaysNever()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 10,
            TotalPullRequests = 5,
            LastAccessedAt = null
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Last Accessed", markup);
        Assert.Contains("Never", markup);
    }

    [Fact]
    public void Render_WithZeroStatistics_DisplaysZeroValues()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 0,
            TotalPullRequests = 0,
            LastAccessedAt = null
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("0", markup);
        Assert.Contains("Total Tickets", markup);
        Assert.Contains("Pull Requests", markup);
    }

    [Fact]
    public void Render_DisplaysCardWithTitle()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 5,
            TotalPullRequests = 3
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Repository Statistics", markup);
    }

    [Fact]
    public void Render_WithLargeNumbers_DisplaysCorrectly()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 1234,
            TotalPullRequests = 567,
            LastAccessedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("1234", markup);
        Assert.Contains("567", markup);
    }
}
