using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Web.UI.Display;
using PRFactory.Web.UI.Editors;
using Xunit;

namespace PRFactory.Web.Tests.Components.Tickets;

public class ReviewCommentThreadTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IToastService> _mockToastService;
    private readonly Mock<ILogger<ReviewCommentThread>> _mockLogger;

    public ReviewCommentThreadTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockToastService = new Mock<IToastService>();
        _mockLogger = new Mock<ILogger<ReviewCommentThread>>();

        Services.AddSingleton(_mockTicketService.Object);
        Services.AddSingleton(_mockToastService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    #region Rendering Tests

    [Fact]
    public void Render_WithNoComments_DisplaysEmptyState()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Assert
        Assert.Contains("No comments yet", cut.Markup);
        Assert.Contains("Start the discussion", cut.Markup);
    }

    [Fact]
    public void Render_WithComments_DisplaysCommentCount()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = CreateSampleComments(3);

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var badge = cut.Find(".badge.bg-primary");
        Assert.Contains("3", badge.InnerHtml);
    }

    [Fact]
    public void Render_WithComments_DisplaysAllComments()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = CreateSampleComments(2);

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var commentItems = cut.FindAll(".comment-item");
        Assert.Equal(2, commentItems.Count);
    }

    // Note: Testing IsLoading state requires exposing it as a parameter or testing through integration
    // Skipping this test as it requires testing private implementation details

    [Fact]
    public void Render_DisplaysCardHeader()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Assert
        Assert.Contains("Discussion", cut.Markup);
        var icon = cut.Find(".bi-chat-dots");
        Assert.NotNull(icon);
    }

    #endregion

    #region Comment Display Tests

    [Fact]
    public void CommentDisplay_ShowsAuthorName()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        Assert.Contains("John Doe", cut.Markup);
        // Verify the author name appears in a comment-item context
        var commentItem = cut.Find(".comment-item");
        Assert.Contains("John Doe", commentItem.InnerHtml);
    }

    [Fact]
    public void CommentDisplay_ShowsRelativeTime()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var relativeTime = cut.FindComponent<RelativeTime>();
        Assert.NotNull(relativeTime);
    }

    [Fact]
    public void CommentDisplay_ShowsEditedIndicator_WhenUpdated()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        Assert.Contains("(edited)", cut.Markup);
    }

    [Fact]
    public void CommentDisplay_DoesNotShowEditedIndicator_WhenNotUpdated()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        Assert.DoesNotContain("(edited)", cut.Markup);
    }

    [Fact]
    public void CommentDisplay_ShowsReviewerAvatar()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                AuthorAvatarUrl = "https://example.com/avatar.jpg",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var avatar = cut.FindComponent<ReviewerAvatar>();
        Assert.NotNull(avatar);
        Assert.Equal("John Doe", avatar.Instance.DisplayName);
    }

    [Fact]
    public void CommentDisplay_RendersMarkdownContent()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "# Heading\n\n**Bold text**",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var commentContent = cut.Find(".comment-content");
        Assert.Contains("<h1", commentContent.InnerHtml);
        Assert.Contains("Heading", commentContent.InnerHtml);
        Assert.Contains("<strong>", commentContent.InnerHtml);
        Assert.Contains("Bold text", commentContent.InnerHtml);
    }

    [Fact]
    public void CommentDisplay_ShowsMentionCount_WhenMentionsExist()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        Assert.Contains("Mentioned 2 user(s)", cut.Markup);
        var mentionIcon = cut.Find(".bi-at");
        Assert.NotNull(mentionIcon);
    }

    [Fact]
    public void CommentDisplay_DoesNotShowMentionCount_WhenNoMentions()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        Assert.DoesNotContain("Mentioned", cut.Markup);
    }

    #endregion

    #region New Comment Form Tests

    [Fact]
    public void NewCommentForm_DisplaysMarkdownEditor()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Assert
        var editor = cut.FindComponent<MarkdownEditor>();
        Assert.NotNull(editor);
    }

    [Fact]
    public void NewCommentForm_DisplaysPostButton()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Assert
        var postButton = cut.Find("button.btn-primary");
        Assert.Contains("Post Comment", postButton.InnerHtml);
    }

    [Fact]
    public void NewCommentForm_PostButtonDisabled_WhenContentEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Assert
        var postButton = cut.Find("button.btn-primary");
        Assert.True(postButton.HasAttribute("disabled"));
    }

    [Fact]
    public void NewCommentForm_ClearButtonHidden_WhenContentEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Assert
        var clearButtons = cut.FindAll("button.btn-secondary");
        Assert.Empty(clearButtons);
    }

    [Fact]
    public void NewCommentForm_ClearButtonVisible_WhenContentExists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Set NewCommentContent using reflection
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "Test content");
        cut.Render();

        // Assert
        var clearButton = cut.Find("button.btn-secondary");
        Assert.Contains("Clear", clearButton.InnerHtml);
    }

    [Fact]
    public void NewCommentForm_DisplaysFormText()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Assert
        Assert.Contains("Use Markdown formatting", cut.Markup);
        Assert.Contains("Mentions not yet supported", cut.Markup);
    }

    #endregion

    #region Action Tests

    [Fact]
    public async Task HandlePostComment_CallsTicketService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var newComment = new ReviewCommentDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorName = "Test User",
            Content = "New test comment",
            CreatedAt = DateTime.UtcNow,
            MentionedUserIds = new List<Guid>()
        };

        _mockTicketService
            .Setup(s => s.AddCommentAsync(ticketId, It.IsAny<string>()))
            .ReturnsAsync(newComment);

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Set content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "New test comment");
        cut.Render();

        // Act
        var postButton = cut.Find("button.btn-primary");
        await postButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockTicketService.Verify(s => s.AddCommentAsync(ticketId, "New test comment"), Times.Once);
    }

    [Fact]
    public async Task HandlePostComment_AddsCommentToList()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var existingComments = CreateSampleComments(1);
        var newComment = new ReviewCommentDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorName = "Test User",
            Content = "New test comment",
            CreatedAt = DateTime.UtcNow,
            MentionedUserIds = new List<Guid>()
        };

        _mockTicketService
            .Setup(s => s.AddCommentAsync(ticketId, It.IsAny<string>()))
            .ReturnsAsync(newComment);

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, existingComments));

        // Set content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "New test comment");
        cut.Render();

        // Act
        var postButton = cut.Find("button.btn-primary");
        await postButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(2, existingComments.Count);
        Assert.Contains(existingComments, c => c.Content == "New test comment");
    }

    [Fact]
    public async Task HandlePostComment_ShowsSuccessToast()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var newComment = new ReviewCommentDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorName = "Test User",
            Content = "New test comment",
            CreatedAt = DateTime.UtcNow,
            MentionedUserIds = new List<Guid>()
        };

        _mockTicketService
            .Setup(s => s.AddCommentAsync(ticketId, It.IsAny<string>()))
            .ReturnsAsync(newComment);

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Set content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "New test comment");
        cut.Render();

        // Act
        var postButton = cut.Find("button.btn-primary");
        await postButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockToastService.Verify(s => s.ShowSuccess("Comment posted successfully"), Times.Once);
    }

    [Fact]
    public async Task HandlePostComment_InvokesOnCommentAddedCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var callbackInvoked = false;
        var newComment = new ReviewCommentDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorName = "Test User",
            Content = "New test comment",
            CreatedAt = DateTime.UtcNow,
            MentionedUserIds = new List<Guid>()
        };

        _mockTicketService
            .Setup(s => s.AddCommentAsync(ticketId, It.IsAny<string>()))
            .ReturnsAsync(newComment);

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>())
            .Add(p => p.OnCommentAdded, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Set content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "New test comment");
        cut.Render();

        // Act
        var postButton = cut.Find("button.btn-primary");
        await postButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task HandlePostComment_ShowsWarning_WhenContentEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Set empty content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "   ");

        // Invoke HandlePostComment using reflection
        var method = typeof(ReviewCommentThread).GetMethod("HandlePostComment",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = method?.Invoke(instance, null) as Task;
        if (task != null)
        {
            await task;
        }

        // Assert
        _mockToastService.Verify(s => s.ShowWarning("Please enter a comment"), Times.Once);
        _mockTicketService.Verify(s => s.AddCommentAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandlePostComment_ShowsError_WhenServiceThrows()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var errorMessage = "Service error";

        _mockTicketService
            .Setup(s => s.AddCommentAsync(ticketId, It.IsAny<string>()))
            .ThrowsAsync(new Exception(errorMessage));

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Set content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "Test comment");

        // Invoke HandlePostComment using reflection
        var method = typeof(ReviewCommentThread).GetMethod("HandlePostComment",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = method?.Invoke(instance, null) as Task;
        if (task != null)
        {
            await task;
        }

        // Assert
        _mockToastService.Verify(s => s.ShowError(It.Is<string>(msg => msg.Contains(errorMessage))), Times.Once);
    }

    [Fact]
    public async Task HandlePostComment_ClearsForm_AfterSuccess()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var newComment = new ReviewCommentDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorName = "Test User",
            Content = "New test comment",
            CreatedAt = DateTime.UtcNow,
            MentionedUserIds = new List<Guid>()
        };

        _mockTicketService
            .Setup(s => s.AddCommentAsync(ticketId, It.IsAny<string>()))
            .ReturnsAsync(newComment);

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Set content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "New test comment");

        // Invoke HandlePostComment using reflection
        var method = typeof(ReviewCommentThread).GetMethod("HandlePostComment",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var task = method?.Invoke(instance, null) as Task;
        if (task != null)
        {
            await task;
        }

        // Assert
        var contentValue = contentProperty?.GetValue(instance) as string;
        Assert.Equal(string.Empty, contentValue);
    }

    [Fact]
    public void ClearComment_ClearsContent()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, new List<ReviewCommentDto>()));

        // Set content
        var instance = cut.Instance;
        var contentProperty = typeof(ReviewCommentThread).GetProperty("NewCommentContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contentProperty?.SetValue(instance, "Test content");
        cut.Render();

        // Verify clear button is present
        var clearButton = cut.Find("button.btn-secondary");

        // Act
        clearButton.Click();

        // Assert
        var contentValue = contentProperty?.GetValue(instance) as string;
        Assert.Equal(string.Empty, contentValue);
    }

    #endregion

    #region Markdown Formatting Tests

    [Fact]
    public void FormatMarkdown_ConvertsMarkdownToHtml()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "**Bold** and *italic*",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var commentContent = cut.Find(".comment-content");
        Assert.Contains("<strong>", commentContent.InnerHtml);
        Assert.Contains("Bold", commentContent.InnerHtml);
        Assert.Contains("<em>", commentContent.InnerHtml);
        Assert.Contains("italic", commentContent.InnerHtml);
    }

    [Fact]
    public void FormatMarkdown_HandlesEmptyString()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert - Should not throw
        var commentContent = cut.Find(".comment-content");
        Assert.Equal(string.Empty, commentContent.InnerHtml.Trim());
    }

    [Fact]
    public void FormatMarkdown_HandlesCodeBlocks()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "```csharp\nvar x = 10;\n```",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var commentContent = cut.Find(".comment-content");
        Assert.Contains("<code", commentContent.InnerHtml);
        Assert.Contains("var x = 10;", commentContent.InnerHtml);
    }

    [Fact]
    public void FormatMarkdown_HandlesLists()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "- Item 1\n- Item 2\n- Item 3",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var commentContent = cut.Find(".comment-content");
        Assert.Contains("<ul>", commentContent.InnerHtml);
        Assert.Contains("<li>", commentContent.InnerHtml);
        Assert.Contains("Item 1", commentContent.InnerHtml);
        Assert.Contains("Item 2", commentContent.InnerHtml);
        Assert.Contains("Item 3", commentContent.InnerHtml);
    }

    [Fact]
    public void FormatMarkdown_HandlesLinks()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                AuthorName = "John Doe",
                Content = "[Example](https://example.com)",
                CreatedAt = DateTime.UtcNow,
                MentionedUserIds = new List<Guid>()
            }
        };

        // Act
        var cut = RenderComponent<ReviewCommentThread>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.Comments, comments));

        // Assert
        var commentContent = cut.Find(".comment-content");
        Assert.Contains("<a href=\"https://example.com\"", commentContent.InnerHtml);
        Assert.Contains("Example", commentContent.InnerHtml);
    }

    #endregion

    #region Helper Methods

    private List<ReviewCommentDto> CreateSampleComments(int count)
    {
        var comments = new List<ReviewCommentDto>();
        for (int i = 0; i < count; i++)
        {
            comments.Add(new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                TicketId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                AuthorName = $"User {i + 1}",
                AuthorEmail = $"user{i + 1}@example.com",
                AuthorAvatarUrl = $"https://example.com/avatar{i + 1}.jpg",
                Content = $"Test comment {i + 1}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i * 10),
                MentionedUserIds = new List<Guid>()
            });
        }
        return comments;
    }

    #endregion
}
