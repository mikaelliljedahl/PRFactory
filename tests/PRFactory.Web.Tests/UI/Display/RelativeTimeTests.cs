using Bunit;
using Xunit;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for RelativeTime component
/// </summary>
public class RelativeTimeTests : TestContext
{
    [Fact]
    public void Render_WithVeryRecentTime_ShowsJustNow()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddSeconds(-30);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("Just now", cut.Markup);
    }

    [Fact]
    public void Render_WithMinutesAgo_ShowsMinutesAgo()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("5 minutes ago", cut.Markup);
    }

    [Fact]
    public void Render_WithHoursAgo_ShowsHoursAgo()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddHours(-2);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("2 hours ago", cut.Markup);
    }

    [Fact]
    public void Render_WithDaysAgo_ShowsDaysAgo()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddDays(-3);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("3 days ago", cut.Markup);
    }

    [Fact]
    public void Render_WithOldDate_ShowsFullDate()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddDays(-10);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        var expectedMonth = timestamp.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains(expectedMonth, cut.Markup);
        Assert.Contains("at", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_ShowsIcon()
    {
        // Arrange
        var icon = "clock";
        var timestamp = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp)
            .Add(p => p.ShowIcon, true)
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
    }

    [Fact]
    public void Render_WhenShowIconFalse_DoesNotShowIcon()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp)
            .Add(p => p.ShowIcon, false));

        // Assert
        Assert.DoesNotContain("bi-", cut.Markup);
    }

    [Fact]
    public void Render_WithPrefix_ShowsPrefix()
    {
        // Arrange
        var prefix = "Updated";
        var timestamp = DateTime.UtcNow.AddMinutes(-10);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp)
            .Add(p => p.Prefix, prefix));

        // Assert
        Assert.Contains($"{prefix} 10 minutes ago", cut.Markup);
    }

    [Fact]
    public void Render_HasTooltipWithFullDateTime()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        var span = cut.Find("span");
        Assert.True(span.HasAttribute("title"));
    }

    [Fact]
    public void Render_ByDefault_IconIsClock()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp)
            .Add(p => p.ShowIcon, true));

        // Assert
        Assert.Contains("bi-clock", cut.Markup);
    }
}
