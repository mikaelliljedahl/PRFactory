using Bunit;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

public class UserStatisticsTests : TestContext
{
    private UserStatisticsDto CreateTestStatistics(
        int totalPlanReviews = 5,
        int totalComments = 10,
        bool hasLastSeenAt = true,
        DateTime? lastSeenAt = null)
    {
        return new UserStatisticsDto
        {
            UserId = Guid.NewGuid(),
            TotalPlanReviews = totalPlanReviews,
            TotalComments = totalComments,
            LastSeenAt = hasLastSeenAt ? (lastSeenAt ?? DateTime.UtcNow.AddHours(-1)) : null
        };
    }

    [Fact]
    public void Render_WithStatistics_DisplaysPlanReviewCount()
    {
        // Arrange
        var statistics = CreateTestStatistics(totalPlanReviews: 15);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("15", cut.Markup);
        Assert.Contains("Plan Reviews", cut.Markup);
    }

    [Fact]
    public void Render_WithStatistics_DisplaysCommentsCount()
    {
        // Arrange
        var statistics = CreateTestStatistics(totalComments: 25);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("25", cut.Markup);
        Assert.Contains("Comments", cut.Markup);
    }

    [Fact]
    public void Render_WithZeroStatistics_DisplaysZeros()
    {
        // Arrange
        var statistics = CreateTestStatistics(
            totalPlanReviews: 0,
            totalComments: 0);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("0", cut.Markup);
    }

    [Fact]
    public void Render_WithLastSeenAt_DisplaysLastActivity()
    {
        // Arrange
        var lastSeen = DateTime.UtcNow.AddHours(-3);
        var statistics = CreateTestStatistics(lastSeenAt: lastSeen);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("Last Activity", cut.Markup);
    }

    [Fact]
    public void Render_WithoutLastSeenAt_HidesLastActivity()
    {
        // Arrange
        var statistics = CreateTestStatistics(hasLastSeenAt: false);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.DoesNotContain("Last Activity", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectTitle()
    {
        // Arrange
        var statistics = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("Activity Statistics", cut.Markup);
    }

    [Fact]
    public void Render_HasCorrectIcon()
    {
        // Arrange
        var statistics = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("bi-bar-chart", cut.Markup);
    }

    [Fact]
    public void Render_AllStatistics_DisplaysAllValues()
    {
        // Arrange
        var statistics = CreateTestStatistics(
            totalPlanReviews: 42,
            totalComments: 108,
            lastSeenAt: DateTime.UtcNow.AddMinutes(-30));

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, statistics));

        // Assert
        Assert.Contains("42", cut.Markup);
        Assert.Contains("108", cut.Markup);
        Assert.Contains("Plan Reviews", cut.Markup);
        Assert.Contains("Comments", cut.Markup);
        Assert.Contains("Last Activity", cut.Markup);
    }
}
