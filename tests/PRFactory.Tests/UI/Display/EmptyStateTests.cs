using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class EmptyStateTests : ComponentTestBase
{
    [Fact]
    public void Render_WithDefaultIcon_DisplaysInboxIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>();

        // Assert
        Assert.Contains("bi-inbox", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomIcon_DisplaysCustomIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, "search"));

        // Assert
        Assert.Contains("bi-search", cut.Markup);
        Assert.DoesNotContain("bi-inbox", cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Title, "No items found"));

        // Assert
        Assert.Contains("No items found", cut.Markup);
        Assert.Contains("<h3", cut.Markup);
    }

    [Fact]
    public void Render_WithMessage_DisplaysMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Message, "Try creating a new item"));

        // Assert
        Assert.Contains("Try creating a new item", cut.Markup);
    }

    [Fact]
    public void Render_WithActionUrl_DisplaysLink()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionUrl, "/create")
            .Add(p => p.ActionText, "Create New"));

        // Assert
        Assert.Contains("href=\"/create\"", cut.Markup);
        Assert.Contains("Create New", cut.Markup);
        Assert.Contains("btn btn-primary", cut.Markup);
    }

    [Fact]
    public void Render_WithActionIcon_DisplaysIconInButton()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionUrl, "/create")
            .Add(p => p.ActionText, "Create")
            .Add(p => p.ActionIcon, "plus"));

        // Assert
        Assert.Contains("bi-plus", cut.Markup);
    }

    [Fact]
    public void Render_WithActionUrlButNoText_DoesNotDisplayAction()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionUrl, "/create"));

        // Assert
        Assert.DoesNotContain("href=\"/create\"", cut.Markup);
    }

    [Fact]
    public void Render_WithOnActionClick_DisplaysButton()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionText, "Try Again")
            .Add(p => p.OnActionClick, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Render_WithOnActionClickAndIcon_DisplaysIconInButton()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionText, "Retry")
            .Add(p => p.ActionIcon, "arrow-clockwise")
            .Add(p => p.OnActionClick, EventCallback.Factory.Create(this, () => { })));

        // Assert
        Assert.Contains("bi-arrow-clockwise", cut.Markup);
    }

    [Fact]
    public void Render_ActionUrlTakesPrecedenceOverOnActionClick()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionUrl, "/create")
            .Add(p => p.ActionText, "Create")
            .Add(p => p.OnActionClick, EventCallback.Factory.Create(this, () => { })));

        // Assert
        // Should render link, not button
        Assert.Contains("<a", cut.Markup);
        Assert.DoesNotContain("<button", cut.Markup);
    }

    [Fact]
    public void Render_CenteredByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>();

        // Assert
        Assert.Contains("text-center", cut.Markup);
    }

    [Fact]
    public void Render_WithCenteredFalse_DoesNotCenterContent()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Centered, false));

        // Assert
        Assert.DoesNotContain("text-center", cut.Markup);
    }

    [Fact]
    public void Render_WithChildContent_DisplaysChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<div id=\"custom-content\">Custom content</div>");
            }));

        // Assert
        Assert.Contains("custom-content", cut.Markup);
        Assert.Contains("Custom content", cut.Markup);
    }

    [Fact]
    public void Render_IconHasCorrectStyling()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, "box"));

        // Assert
        Assert.Contains("font-size: 4rem", cut.Markup);
        Assert.Contains("color: #ccc", cut.Markup);
    }

    [Fact]
    public void Render_TitleHasCorrectStyling()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Empty"));

        // Assert
        Assert.Contains("mt-3", cut.Markup);
        Assert.Contains("text-muted", cut.Markup);
    }

    [Fact]
    public void Render_MessageHasTextMuted()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Message, "No data"));

        // Assert
        Assert.Contains("text-muted", cut.Markup);
    }

    [Fact]
    public void Render_ActionButtonHasCorrectMargin()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionUrl, "/create")
            .Add(p => p.ActionText, "Create"));

        // Assert
        Assert.Contains("mt-3", cut.Markup);
    }

    [Fact]
    public void Render_WithAllParameters_DisplaysAllElements()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, "folder")
            .Add(p => p.Title, "No Files")
            .Add(p => p.Message, "Upload some files to get started")
            .Add(p => p.ActionUrl, "/upload")
            .Add(p => p.ActionText, "Upload Files")
            .Add(p => p.ActionIcon, "upload")
            .Add(p => p.Centered, true));

        // Assert
        Assert.Contains("bi-folder", cut.Markup);
        Assert.Contains("No Files", cut.Markup);
        Assert.Contains("Upload some files to get started", cut.Markup);
        Assert.Contains("href=\"/upload\"", cut.Markup);
        Assert.Contains("Upload Files", cut.Markup);
        Assert.Contains("bi-upload", cut.Markup);
    }

    [Fact]
    public void Render_WithNullIcon_DoesNotDisplayIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, (string?)null)
            .Add(p => p.Title, "Empty"));

        // Assert
        var icons = cut.FindAll("i");
        Assert.Empty(icons);
    }

    [Fact]
    public void Render_WithEmptyIcon_DoesNotDisplayIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, "")
            .Add(p => p.Title, "Empty"));

        // Assert
        var icons = cut.FindAll("i");
        Assert.Empty(icons);
    }
}
