using Bunit;
using Xunit;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for EmptyState component
/// </summary>
public class EmptyStateTests : TestContext
{
    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        // Arrange
        var message = "No items found";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        Assert.Contains(message, cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange
        var title = "Nothing Here";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Title, title));

        // Assert
        Assert.Contains(title, cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange
        var icon = "inbox";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_ShowsInboxIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>();

        // Assert
        Assert.Contains("bi-inbox", cut.Markup);
    }

    [Fact]
    public void Render_WithActionUrlAndText_ShowsActionButton()
    {
        // Arrange
        var actionUrl = "/create";
        var actionText = "Create New";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionUrl, actionUrl)
            .Add(p => p.ActionText, actionText));

        // Assert
        var link = cut.Find("a.btn");
        Assert.Equal(actionUrl, link.GetAttribute("href"));
        Assert.Contains(actionText, link.TextContent);
    }

    [Fact]
    public void Render_WithActionIcon_ShowsIconInButton()
    {
        // Arrange
        var actionIcon = "plus";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionUrl, "/create")
            .Add(p => p.ActionText, "Create")
            .Add(p => p.ActionIcon, actionIcon));

        // Assert
        Assert.Contains($"bi-{actionIcon}", cut.Markup);
    }

    [Fact]
    public void Render_WithOnActionClick_ShowsClickableButton()
    {
        // Arrange
        var clicked = false;

        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionText, "Create")
            .Add(p => p.OnActionClick, () => clicked = true));

        // Act
        var button = cut.Find("button.btn");
        button.Click();

        // Assert
        Assert.True(clicked);
    }

    [Fact]
    public void Render_WhenCentered_AppliesTextCenterClass()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Centered, true));

        // Assert
        Assert.Contains("text-center", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotCentered_DoesNotApplyTextCenterClass()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Centered, false));

        // Assert
        Assert.DoesNotContain("text-center", cut.Markup);
    }

    [Fact]
    public void Render_ByDefault_IsCentered()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>();

        // Assert
        Assert.Contains("text-center", cut.Markup);
    }

    [Fact]
    public void Render_WithChildContent_DisplaysChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .AddChildContent("<div class=\"custom-content\">Custom Empty State</div>"));

        // Assert
        Assert.Contains("custom-content", cut.Markup);
        Assert.Contains("Custom Empty State", cut.Markup);
    }
}
