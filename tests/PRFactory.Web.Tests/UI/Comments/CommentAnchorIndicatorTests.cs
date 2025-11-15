using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.UI.Comments;
using PRFactory.Web.Models;

namespace PRFactory.Web.Tests.UI.Comments;

/// <summary>
/// Tests for CommentAnchorIndicator component
/// </summary>
public class CommentAnchorIndicatorTests : TestContext
{
    [Fact]
    public void CommentAnchorIndicator_RendersAnchorIndicatorIcon()
    {
        // Arrange
        var anchor = new InlineCommentAnchorDto
        {
            Id = Guid.NewGuid(),
            StartLine = 5,
            EndLine = 5,
            TextSnippet = "Sample code",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<CommentAnchorIndicator>(parameters => parameters
            .Add(p => p.Anchor, anchor));

        // Assert
        Assert.Contains("comment-anchor-indicator", cut.Markup);
        Assert.Contains("bi-chat-dots-fill", cut.Markup);
        Assert.Contains("text-primary", cut.Markup);
    }

    [Fact]
    public void CommentAnchorIndicator_ShowsLineRange()
    {
        // Arrange
        var anchor = new InlineCommentAnchorDto
        {
            Id = Guid.NewGuid(),
            StartLine = 10,
            EndLine = 15,
            TextSnippet = "Multi-line code",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<CommentAnchorIndicator>(parameters => parameters
            .Add(p => p.Anchor, anchor)
            .Add(p => p.ShowLineRange, true));

        // Assert
        Assert.Contains("10-15", cut.Markup);
    }

    [Fact]
    public void CommentAnchorIndicator_HidesLineRangeWhenDisabled()
    {
        // Arrange
        var anchor = new InlineCommentAnchorDto
        {
            Id = Guid.NewGuid(),
            StartLine = 10,
            EndLine = 15,
            TextSnippet = "Multi-line code",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<CommentAnchorIndicator>(parameters => parameters
            .Add(p => p.Anchor, anchor)
            .Add(p => p.ShowLineRange, false));

        // Assert - Line range should not be displayed in the visible text (only in tooltip)
        var indicatorDiv = cut.Find(".comment-anchor-indicator");
        var innerText = indicatorDiv.TextContent;
        Assert.DoesNotContain("10-15", innerText);
    }

    [Fact]
    public void CommentAnchorIndicator_OnClickCallbackInvokedWhenClicked()
    {
        // Arrange
        var anchor = new InlineCommentAnchorDto
        {
            Id = Guid.NewGuid(),
            StartLine = 5,
            EndLine = 5,
            TextSnippet = "Sample code",
            CreatedAt = DateTime.UtcNow
        };

        InlineCommentAnchorDto? clickedAnchor = null;
        var cut = RenderComponent<CommentAnchorIndicator>(parameters => parameters
            .Add(p => p.Anchor, anchor)
            .Add(p => p.OnAnchorClick, EventCallback.Factory.Create<InlineCommentAnchorDto>(
                this, (InlineCommentAnchorDto a) => clickedAnchor = a)));

        // Act
        var indicator = cut.Find(".comment-anchor-indicator");
        indicator.Click();

        // Assert
        Assert.NotNull(clickedAnchor);
        Assert.Equal(anchor.Id, clickedAnchor.Id);
    }

    [Fact]
    public void CommentAnchorIndicator_ShowsTooltipWithCommentInfo()
    {
        // Arrange
        var anchor = new InlineCommentAnchorDto
        {
            Id = Guid.NewGuid(),
            StartLine = 5,
            EndLine = 8,
            TextSnippet = "Sample code",
            CreatedAt = DateTime.UtcNow,
            ReviewComment = new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "This needs improvement",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var cut = RenderComponent<CommentAnchorIndicator>(parameters => parameters
            .Add(p => p.Anchor, anchor));

        // Assert
        var indicatorDiv = cut.Find(".comment-anchor-indicator");
        var title = indicatorDiv.GetAttribute("title");
        Assert.Contains("John Doe", title);
        Assert.Contains("5-8", title);
    }

    [Fact]
    public void CommentAnchorIndicator_ShowsSingleLineRange()
    {
        // Arrange
        var anchor = new InlineCommentAnchorDto
        {
            Id = Guid.NewGuid(),
            StartLine = 42,
            EndLine = 42,
            TextSnippet = "Single line",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<CommentAnchorIndicator>(parameters => parameters
            .Add(p => p.Anchor, anchor)
            .Add(p => p.ShowLineRange, true));

        // Assert - Should show just "42" not "42-42"
        Assert.Contains("42", cut.Markup);
    }
}
