using PRFactory.Domain.Entities;
using Xunit;

namespace PRFactory.Tests.Domain;

public class ReviewCommentTests
{
    private readonly Guid _ticketId = Guid.NewGuid();
    private readonly Guid _authorId = Guid.NewGuid();
    private const string ValidContent = "This is a valid comment";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidInputs_CreatesReviewComment()
    {
        // Act
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent);

        // Assert
        Assert.NotNull(comment);
        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(_ticketId, comment.TicketId);
        Assert.Equal(_authorId, comment.AuthorId);
        Assert.Equal(ValidContent, comment.Content);
        Assert.NotNull(comment.MentionedUserIds);
        Assert.Empty(comment.MentionedUserIds);
        Assert.True(Math.Abs((comment.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithMentions_StoresProvidedMentions()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var mentions = new List<Guid> { userId1, userId2 };

        // Act
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent, mentions);

        // Assert
        Assert.Equal(2, comment.MentionedUserIds.Count);
        Assert.Contains(userId1, comment.MentionedUserIds);
        Assert.Contains(userId2, comment.MentionedUserIds);
    }

    [Fact]
    public void Constructor_WithoutMentions_InitializesEmptyMentionsList()
    {
        // Act
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent, null);

        // Assert
        Assert.NotNull(comment.MentionedUserIds);
        Assert.Empty(comment.MentionedUserIds);
    }

    [Fact]
    public void Constructor_WithEmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new ReviewComment(_ticketId, _authorId, string.Empty));
        Assert.Contains("content", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithWhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new ReviewComment(_ticketId, _authorId, "   "));
        Assert.Contains("content", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithContentContainingWhitespace_TrimsContent()
    {
        // Arrange
        const string contentWithWhitespace = "  This has whitespace  ";

        // Act
        var comment = new ReviewComment(_ticketId, _authorId, contentWithWhitespace);

        // Assert
        Assert.Equal("This has whitespace", comment.Content);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidContent_UpdatesContentAndTimestamp()
    {
        // Arrange
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent);
        const string newContent = "Updated comment content";
        Thread.Sleep(10); // Ensure time difference

        // Act
        comment.Update(newContent);

        // Assert
        Assert.Equal(newContent, comment.Content);
        Assert.NotNull(comment.UpdatedAt);
        Assert.True(Math.Abs((comment.UpdatedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Update_WithMentions_UpdatesMentionsAndTimestamp()
    {
        // Arrange
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var newMentions = new List<Guid> { userId1, userId2 };
        Thread.Sleep(10); // Ensure time difference

        // Act
        comment.Update("Updated content", newMentions);

        // Assert
        Assert.Equal(2, comment.MentionedUserIds.Count);
        Assert.Contains(userId1, comment.MentionedUserIds);
        Assert.Contains(userId2, comment.MentionedUserIds);
        Assert.NotNull(comment.UpdatedAt);
    }

    [Fact]
    public void Update_WithoutMentions_ClearsExistingMentions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialMentions = new List<Guid> { userId };
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent, initialMentions);

        // Act
        comment.Update("Updated content", null);

        // Assert
        Assert.Empty(comment.MentionedUserIds);
    }

    [Fact]
    public void Update_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => comment.Update(string.Empty));
        Assert.Contains("content", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region MentionsUser Tests

    [Fact]
    public void MentionsUser_WithMentionedUser_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentions = new List<Guid> { userId };
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent, mentions);

        // Act
        var result = comment.MentionsUser(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MentionsUser_WithNonMentionedUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var mentions = new List<Guid> { otherUserId };
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent, mentions);

        // Act
        var result = comment.MentionsUser(userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region AddMention Tests

    [Fact]
    public void AddMention_NewUser_AddsToMentionsAndUpdatesTimestamp()
    {
        // Arrange
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent);
        var userId = Guid.NewGuid();
        Thread.Sleep(10); // Ensure time difference

        // Act
        comment.AddMention(userId);

        // Assert
        var single = Assert.Single(comment.MentionedUserIds);
        Assert.Equal(userId, single);
        Assert.NotNull(comment.UpdatedAt);
        Assert.True(Math.Abs((comment.UpdatedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void AddMention_DuplicateUser_DoesNotAddDuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentions = new List<Guid> { userId };
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent, mentions);
        Thread.Sleep(10); // Ensure time difference
        comment.Update(ValidContent, new List<Guid> { userId }); // Preserve the mention
        var previousUpdatedAt = comment.UpdatedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        comment.AddMention(userId); // Try to add duplicate

        // Assert
        Assert.Single(comment.MentionedUserIds);
        Assert.Equal(previousUpdatedAt, comment.UpdatedAt); // Should not change
    }

    #endregion

    #region RemoveMention Tests

    [Fact]
    public void RemoveMention_ExistingUser_RemovesFromMentionsAndUpdatesTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentions = new List<Guid> { userId };
        var comment = new ReviewComment(_ticketId, _authorId, ValidContent, mentions);
        Thread.Sleep(10); // Ensure time difference

        // Act
        comment.RemoveMention(userId);

        // Assert
        Assert.DoesNotContain(userId, comment.MentionedUserIds);
        Assert.Empty(comment.MentionedUserIds);
        Assert.NotNull(comment.UpdatedAt);
        Assert.True(Math.Abs((comment.UpdatedAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    #endregion
}
