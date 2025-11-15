using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.UI.Editors;

namespace PRFactory.Web.Tests.UI.Editors;

/// <summary>
/// Tests for MarkdownToolbar component
/// </summary>
public class MarkdownToolbarTests : TestContext
{
    [Fact]
    public void MarkdownToolbar_RendersToolbarButtons()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownToolbar>();

        // Assert
        Assert.Contains("bi-type-bold", cut.Markup);
        Assert.Contains("bi-type-italic", cut.Markup);
        Assert.Contains("bi-type-strikethrough", cut.Markup);
        Assert.Contains("bi-link-45deg", cut.Markup);
        Assert.Contains("bi-code-square", cut.Markup);
    }

    [Fact]
    public void MarkdownToolbar_OnBoldClickCallbackInvoked()
    {
        // Arrange
        ToolbarCommand? invokedCommand = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnCommand, EventCallback.Factory.Create<ToolbarCommand>(
                this, (ToolbarCommand cmd) => invokedCommand = cmd)));

        // Act
        var boldButton = cut.FindAll("button").First(b => b.InnerHtml.Contains("bi-type-bold"));
        boldButton.Click();

        // Assert
        Assert.Equal(ToolbarCommand.Bold, invokedCommand);
    }

    [Fact]
    public void MarkdownToolbar_OnItalicClickCallbackInvoked()
    {
        // Arrange
        ToolbarCommand? invokedCommand = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnCommand, EventCallback.Factory.Create<ToolbarCommand>(
                this, (ToolbarCommand cmd) => invokedCommand = cmd)));

        // Act
        var italicButton = cut.FindAll("button").First(b => b.InnerHtml.Contains("bi-type-italic"));
        italicButton.Click();

        // Assert
        Assert.Equal(ToolbarCommand.Italic, invokedCommand);
    }

    [Fact]
    public void MarkdownToolbar_OnLinkClickCallbackInvoked()
    {
        // Arrange
        ToolbarCommand? invokedCommand = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnCommand, EventCallback.Factory.Create<ToolbarCommand>(
                this, (ToolbarCommand cmd) => invokedCommand = cmd)));

        // Act
        var linkButton = cut.FindAll("button").First(b => b.InnerHtml.Contains("bi-link-45deg"));
        linkButton.Click();

        // Assert
        Assert.Equal(ToolbarCommand.Link, invokedCommand);
    }

    [Fact]
    public void MarkdownToolbar_OnCodeClickCallbackInvoked()
    {
        // Arrange
        ToolbarCommand? invokedCommand = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnCommand, EventCallback.Factory.Create<ToolbarCommand>(
                this, (ToolbarCommand cmd) => invokedCommand = cmd)));

        // Act
        var codeButton = cut.FindAll("button").First(b => b.InnerHtml.Contains("bi-code-square"));
        codeButton.Click();

        // Assert
        Assert.Equal(ToolbarCommand.CodeBlock, invokedCommand);
    }

    [Fact]
    public void MarkdownToolbar_OnListClickCallbackInvoked()
    {
        // Arrange
        ToolbarCommand? invokedCommand = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnCommand, EventCallback.Factory.Create<ToolbarCommand>(
                this, (ToolbarCommand cmd) => invokedCommand = cmd)));

        // Act
        var listButton = cut.FindAll("button").First(b => b.InnerHtml.Contains("bi-list-ul"));
        listButton.Click();

        // Assert
        Assert.Equal(ToolbarCommand.UnorderedList, invokedCommand);
    }

    [Fact]
    public void MarkdownToolbar_ShowsTooltipsOnButtons()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownToolbar>();

        // Assert
        Assert.Contains("title=\"Bold (Ctrl+B)\"", cut.Markup);
        Assert.Contains("title=\"Italic (Ctrl+I)\"", cut.Markup);
        Assert.Contains("title=\"Insert Link (Ctrl+K)\"", cut.Markup);
    }

    [Fact]
    public void MarkdownToolbar_HeadingDropdownInvokesCallback()
    {
        // Arrange
        ToolbarCommand? invokedCommand = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnCommand, EventCallback.Factory.Create<ToolbarCommand>(
                this, (ToolbarCommand cmd) => invokedCommand = cmd)));

        // Act
        var headingSelect = cut.FindAll("select").First();
        headingSelect.Change(ToolbarCommand.Heading2.ToString());

        // Assert
        Assert.Equal(ToolbarCommand.Heading2, invokedCommand);
    }

    [Fact]
    public void MarkdownToolbar_ViewModeDropdownInvokesCallback()
    {
        // Arrange
        ViewMode? invokedMode = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.CurrentViewMode, ViewMode.Split)
            .Add(p => p.OnViewModeChanged, EventCallback.Factory.Create<ViewMode>(
                this, (ViewMode mode) => invokedMode = mode)));

        // Act
        var viewModeSelect = cut.FindAll("select").Last();
        viewModeSelect.Change(ViewMode.PreviewOnly.ToString());

        // Assert
        Assert.Equal(ViewMode.PreviewOnly, invokedMode);
    }

    [Fact]
    public void MarkdownToolbar_RendersAllFormattingButtons()
    {
        // Arrange & Act
        var cut = RenderComponent<MarkdownToolbar>();

        // Assert - Check for all major formatting buttons
        Assert.Contains("bi-type-bold", cut.Markup);
        Assert.Contains("bi-type-italic", cut.Markup);
        Assert.Contains("bi-type-strikethrough", cut.Markup);
        Assert.Contains("bi-list-ul", cut.Markup);
        Assert.Contains("bi-list-ol", cut.Markup);
        Assert.Contains("bi-check2-square", cut.Markup);
        Assert.Contains("bi-link-45deg", cut.Markup);
        Assert.Contains("bi-image", cut.Markup);
        Assert.Contains("bi-code-square", cut.Markup);
        Assert.Contains("bi-code", cut.Markup);
        Assert.Contains("bi-table", cut.Markup);
        Assert.Contains("bi-quote", cut.Markup);
        Assert.Contains("bi-dash-lg", cut.Markup);
    }

    [Fact]
    public void MarkdownToolbar_OnStrikethroughClickCallbackInvoked()
    {
        // Arrange
        ToolbarCommand? invokedCommand = null;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnCommand, EventCallback.Factory.Create<ToolbarCommand>(
                this, (ToolbarCommand cmd) => invokedCommand = cmd)));

        // Act
        var strikethroughButton = cut.FindAll("button").First(b => b.InnerHtml.Contains("bi-type-strikethrough"));
        strikethroughButton.Click();

        // Assert
        Assert.Equal(ToolbarCommand.Strikethrough, invokedCommand);
    }
}
