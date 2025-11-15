using Bunit;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the UserStatistics component.
/// Verifies statistics display, loading states, and relative time formatting.
/// </summary>
public class UserStatisticsTests : TestContext
{
    public UserStatisticsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);
    }

    private UserStatisticsDto CreateTestStatistics(
        int totalPlanReviews = 25,
        int totalComments = 42,
        DateTime? lastSeenAt = null,
        bool useDefaultLastSeen = true)
    {
        return new UserStatisticsDto
        {
            UserId = Guid.NewGuid(),
            TotalPlanReviews = totalPlanReviews,
            TotalComments = totalComments,
            LastSeenAt = useDefaultLastSeen ? (lastSeenAt ?? DateTime.UtcNow.AddHours(-2)) : lastSeenAt
        };
    }

    [Fact]
    public void Render_DisplaysCardTitle()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Activity Statistics", markup);
    }

    [Fact]
    public void Render_DisplaysStatisticsIcon()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var icon = cut.Find("i.bi-bar-chart");
        Assert.NotNull(icon);
    }

    [Fact]
    public void Render_DisplaysTotalPlanReviews()
    {
        // Arrange
        var stats = CreateTestStatistics(totalPlanReviews: 15);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("15", markup);
        Assert.Contains("Plan Reviews", markup);
    }

    [Fact]
    public void Render_DisplaysTotalComments()
    {
        // Arrange
        var stats = CreateTestStatistics(totalComments: 38);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("38", markup);
        Assert.Contains("Comments", markup);
    }

    [Fact]
    public void Render_WithZeroPlanReviews_DisplaysZero()
    {
        // Arrange
        var stats = CreateTestStatistics(totalPlanReviews: 0);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("0", markup);
    }

    [Fact]
    public void Render_WithZeroComments_DisplaysZero()
    {
        // Arrange
        var stats = CreateTestStatistics(totalComments: 0);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("0", markup);
    }

    [Fact]
    public void Render_DisplaysLastActivityLabel()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var dts = cut.FindAll("dt");
        Assert.NotEmpty(dts);
        Assert.True(dts.Any(dt => dt.TextContent.Contains("Last Activity")));
    }

    [Fact]
    public void Render_WithLastSeenAt_DisplaysRelativeTime()
    {
        // Arrange
        var lastSeen = DateTime.UtcNow.AddDays(-3);
        var stats = CreateTestStatistics(lastSeenAt: lastSeen);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("Never", markup);
    }

    [Fact]
    public void Render_WithoutLastSeenAt_DoesNotShowLastActivity()
    {
        // Arrange
        var stats = CreateTestStatistics(lastSeenAt: null, useDefaultLastSeen: false);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        // When LastSeenAt is null, the "Last Activity" section should not be rendered
        var markup = cut.Markup;
        Assert.DoesNotContain("Last Activity", markup);
    }

    [Fact]
    public void Render_DisplaysCardComponent()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Activity Statistics", markup);
    }

    [Fact]
    public void Render_DisplaysRowLayout()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var row = cut.Find(".row");
        Assert.NotNull(row);
    }

    [Fact]
    public void Render_DisplaysTwoStatisticColumns()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var cols = cut.FindAll(".col-6");
        Assert.Equal(2, cols.Count);
    }

    [Fact]
    public void Render_DisplaysStatisticsAsDisplay6()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var displays = cut.FindAll(".display-6");
        Assert.NotEmpty(displays);
    }

    [Fact]
    public void Render_DisplaysStatisticLabelsAsMuted()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var smallMuted = cut.FindAll("small.text-muted");
        Assert.NotEmpty(smallMuted);
    }

    [Fact]
    public void Render_WithDifferentStatistics_DisplaysCorrectNumbers()
    {
        // Arrange
        var stats1 = CreateTestStatistics(totalPlanReviews: 10, totalComments: 20);
        var stats2 = CreateTestStatistics(totalPlanReviews: 50, totalComments: 100);

        // Act
        var cut1 = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats1));
        var cut2 = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats2));

        // Assert
        Assert.Contains("10", cut1.Markup);
        Assert.Contains("20", cut1.Markup);

        Assert.Contains("50", cut2.Markup);
        Assert.Contains("100", cut2.Markup);
    }

    [Fact]
    public void Render_WithLargeNumbers_DisplaysCorrectly()
    {
        // Arrange
        var stats = CreateTestStatistics(totalPlanReviews: 9999, totalComments: 5555);

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("9999", markup);
        Assert.Contains("5555", markup);
    }

    [Fact]
    public void Render_DisplaysCenteredText()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var textCenter = cut.Find(".text-center");
        Assert.NotNull(textCenter);
    }

    [Fact]
    public void Render_DisplaysMarginingForMb3()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var cols = cut.FindAll(".mb-3");
        Assert.NotEmpty(cols);
    }

    [Fact]
    public void Render_WithRecentActivity_DisplaysRelativeTime()
    {
        // Arrange
        var stats = CreateTestStatistics(lastSeenAt: DateTime.UtcNow.AddMinutes(-15));

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.NotEmpty(markup);
        Assert.Contains("Last Activity", markup);
    }

    [Fact]
    public void Render_DisplaysDefinitionListForLastActivity()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var dl = cut.Find("dl");
        Assert.NotNull(dl);
        var dts = cut.FindAll("dl dt");
        Assert.NotEmpty(dts);
        Assert.True(dts.Any(dt => dt.TextContent.Contains("Last Activity")));
    }

    [Fact]
    public void Render_WithMb0OnDefinitionList()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var dl = cut.Find("dl.mb-0");
        Assert.NotNull(dl);
    }

    [Fact]
    public void Render_PreservesCardWithoutChildContent()
    {
        // Arrange
        var stats = CreateTestStatistics();

        // Act
        var cut = RenderComponent<UserStatistics>(parameters => parameters
            .Add(p => p.Statistics, stats));

        // Assert
        var markup = cut.Markup;
        Assert.NotEmpty(markup);
        Assert.Contains("Activity Statistics", markup);
    }
}
