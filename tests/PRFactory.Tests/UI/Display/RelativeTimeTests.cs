using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class RelativeTimeTests : ComponentTestBase
{
    [Fact]
    public void Render_LessThanOneMinute_ShowsJustNow()
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
    public void Render_MinutesAgo_ShowsMinutes()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-15);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("15 minutes ago", cut.Markup);
    }

    [Fact]
    public void Render_HoursAgo_ShowsHours()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddHours(-3);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("3 hours ago", cut.Markup);
    }

    [Fact]
    public void Render_DaysAgo_ShowsDays()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddDays(-2);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("2 days ago", cut.Markup);
    }

    [Fact]
    public void Render_MoreThanSevenDaysAgo_ShowsFormattedDate()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("Jan 15, 2024", cut.Markup);
    }

    [Fact]
    public void Render_WithPrefix_IncludesPrefix()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-10);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp)
            .Add(p => p.Prefix, "Updated"));

        // Assert
        Assert.Contains("Updated", cut.Markup);
        Assert.Contains("10 minutes ago", cut.Markup);
    }

    [Fact]
    public void Render_WithShowIconTrue_DisplaysIcon()
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

    [Fact]
    public void Render_WithShowIconFalse_HidesIcon()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp)
            .Add(p => p.ShowIcon, false));

        // Assert
        Assert.DoesNotContain("bi-clock", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomIcon_DisplaysCustomIcon()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp)
            .Add(p => p.ShowIcon, true)
            .Add(p => p.Icon, "calendar"));

        // Assert
        Assert.Contains("bi-calendar", cut.Markup);
        Assert.DoesNotContain("bi-clock", cut.Markup);
    }

    [Fact]
    public void Render_HasTooltipWithFullTimestamp()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("title=", cut.Markup);
    }

    [Fact]
    public void Render_WithoutPrefix_DoesNotAddExtraSpace()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("  ", markup);
    }

    [Fact]
    public void Render_OneMinute_ShowsSingularMinutes()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMinutes(-1.5);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("1 minutes ago", cut.Markup);
    }

    [Fact]
    public void Render_OneHour_ShowsSingularHours()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddHours(-1.5);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("1 hours ago", cut.Markup);
    }

    [Fact]
    public void Render_OneDay_ShowsSingularDays()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddDays(-1.5);

        // Act
        var cut = RenderComponent<RelativeTime>(parameters => parameters
            .Add(p => p.Timestamp, timestamp));

        // Assert
        Assert.Contains("1 days ago", cut.Markup);
    }
}
