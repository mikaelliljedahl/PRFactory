using Bunit;
using PRFactory.Core.Application.DTOs;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Repositories;
using Xunit;

namespace PRFactory.Tests.Components.Repositories;

public class RepositoryStatisticsTests : ComponentTestBase
{
    [Fact]
    public void Render_WithStatistics_ShowsTotalTickets()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 15,
            TotalPullRequests = 10,
            LastAccessedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("15", cut.Markup);
        Assert.Contains("Total Tickets", cut.Markup);
    }

    [Fact]
    public void Render_WithStatistics_ShowsTotalPullRequests()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 5,
            TotalPullRequests = 8,
            LastAccessedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("8", cut.Markup);
        Assert.Contains("Pull Requests", cut.Markup);
    }

    [Fact]
    public void Render_WithLastAccessedAt_ShowsAccessedTime()
    {
        // Arrange
        var lastAccessedAt = DateTime.UtcNow.AddHours(-2);
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 5,
            TotalPullRequests = 3,
            LastAccessedAt = lastAccessedAt
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("Last Accessed", cut.Markup);
    }

    [Fact]
    public void Render_WithoutLastAccessedAt_ShowsNever()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 5,
            TotalPullRequests = 3,
            LastAccessedAt = null
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("Never", cut.Markup);
    }

    [Fact]
    public void Render_ShowsCardWithIcon()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 0,
            TotalPullRequests = 0
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("Repository Statistics", cut.Markup);
        Assert.Contains("bi-bar-chart", cut.Markup);
    }

    [Fact]
    public void Render_WithZeroStatistics_ShowsZeros()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 0,
            TotalPullRequests = 0
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("0", cut.Markup);
    }

    [Fact]
    public void Render_ShowsStatisticsInGridLayout()
    {
        // Arrange
        var statistics = new RepositoryStatisticsDto
        {
            RepositoryId = Guid.NewGuid(),
            TotalTickets = 10,
            TotalPullRequests = 5
        };

        // Act
        var cut = RenderComponent<RepositoryStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("row", cut.Markup);
        Assert.Contains("col-6", cut.Markup);
    }
}
