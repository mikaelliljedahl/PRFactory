using Bunit;
using Xunit;
using PRFactory.Web.UI.Display;
using static PRFactory.Web.UI.Display.ReviewerAvatar;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for ReviewerAvatar component
/// </summary>
public class ReviewerAvatarTests : TestContext
{
    [Fact]
    public void Render_WithDisplayName_ShowsInitials()
    {
        // Arrange
        var displayName = "John Doe";

        // Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, displayName));

        // Assert
        Assert.Contains("JD", cut.Markup);
    }

    [Theory]
    [InlineData("John Doe", "JD")]
    [InlineData("Alice Smith", "AS")]
    [InlineData("Bob", "BO")]
    [InlineData("X", "X")]
    public void Render_WithDifferentNames_ShowsCorrectInitials(string name, string expectedInitials)
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, name));

        // Assert
        Assert.Contains(expectedInitials, cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyName_ShowsQuestionMark()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, ""));

        // Assert
        Assert.Contains("?", cut.Markup);
    }

    [Fact]
    public void Render_WithAvatarUrl_ShowsImage()
    {
        // Arrange
        var avatarUrl = "https://example.com/avatar.jpg";
        var displayName = "John Doe";

        // Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, displayName)
            .Add(p => p.AvatarUrl, avatarUrl));

        // Assert
        var img = cut.Find("img");
        Assert.Equal(avatarUrl, img.GetAttribute("src"));
        Assert.Equal(displayName, img.GetAttribute("alt"));
    }

    [Fact]
    public void Render_WithoutAvatarUrl_ShowsInitialsInColoredCircle()
    {
        // Arrange
        var displayName = "Jane Smith";

        // Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, displayName));

        // Assert
        Assert.Contains("rounded-circle", cut.Markup);
        Assert.Contains("bg-primary", cut.Markup); // Default background color
        Assert.DoesNotContain("<img", cut.Markup);
    }

    [Fact]
    public void Render_WithBackgroundColor_AppliesBackgroundColor()
    {
        // Arrange
        var backgroundColor = "success";

        // Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test User")
            .Add(p => p.BackgroundColor, backgroundColor));

        // Assert
        Assert.Contains($"bg-{backgroundColor}", cut.Markup);
    }

    [Fact]
    public void Render_WithStatusBadge_ShowsStatusBadge()
    {
        // Arrange
        var badgeText = "Active";
        var badgeColor = "success";

        // Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test User")
            .Add(p => p.StatusBadgeText, badgeText)
            .Add(p => p.StatusBadgeColor, badgeColor));

        // Assert
        Assert.Contains(badgeText, cut.Markup);
        Assert.Contains($"bg-{badgeColor}", cut.Markup);
        Assert.Contains("badge", cut.Markup);
    }

    [Fact]
    public void Render_WithoutStatusBadge_DoesNotShowBadge()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test User"));

        // Assert
        var badgeCount = cut.Markup.Split("badge").Length - 1;
        Assert.Equal(0, badgeCount);
    }

    [Theory]
    [InlineData(AvatarSize.Small, "avatar-sm")]
    [InlineData(AvatarSize.Medium, "avatar-md")]
    [InlineData(AvatarSize.Large, "avatar-lg")]
    public void Render_WithSize_AppliesCorrectSizeClass(AvatarSize size, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test User")
            .Add(p => p.Size, size));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_SizeIsMedium()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test User"));

        // Assert
        Assert.Contains("avatar-md", cut.Markup);
    }

    [Fact]
    public void Render_HasReviewerAvatarClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test User"));

        // Assert
        Assert.Contains("reviewer-avatar", cut.Markup);
    }
}
