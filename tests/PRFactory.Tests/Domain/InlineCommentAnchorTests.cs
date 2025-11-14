using PRFactory.Domain.Entities;
using Xunit;

namespace PRFactory.Tests.Domain;

public class InlineCommentAnchorTests
{
    private readonly Guid _reviewCommentId = Guid.NewGuid();
    private const int ValidStartLine = 10;
    private const int ValidEndLine = 15;
    private const string ValidTextSnippet = "This is a test snippet";

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_CreatesAnchor()
    {
        // Act
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            ValidStartLine,
            ValidEndLine,
            ValidTextSnippet);

        // Assert
        Assert.NotNull(anchor);
        Assert.NotEqual(Guid.Empty, anchor.Id);
        Assert.Equal(_reviewCommentId, anchor.ReviewCommentId);
        Assert.Equal(ValidStartLine, anchor.StartLine);
        Assert.Equal(ValidEndLine, anchor.EndLine);
        Assert.Equal(ValidTextSnippet, anchor.TextSnippet);
        Assert.True(Math.Abs((anchor.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
    }

    [Fact]
    public void Create_WithSameStartAndEndLine_CreatesAnchor()
    {
        // Act
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            ValidStartLine,
            ValidStartLine,
            ValidTextSnippet);

        // Assert
        Assert.Equal(ValidStartLine, anchor.StartLine);
        Assert.Equal(ValidStartLine, anchor.EndLine);
    }

    [Fact]
    public void Create_WithLongTextSnippet_TruncatesTo200Characters()
    {
        // Arrange
        var longSnippet = new string('x', 250);

        // Act
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            ValidStartLine,
            ValidEndLine,
            longSnippet);

        // Assert
        Assert.Equal(200, anchor.TextSnippet.Length);
    }

    [Fact]
    public void Create_WithEmptyReviewCommentId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            InlineCommentAnchor.Create(
                Guid.Empty,
                ValidStartLine,
                ValidEndLine,
                ValidTextSnippet));

        Assert.Contains("ReviewCommentId", exception.Message);
    }

    [Fact]
    public void Create_WithStartLineLessThanOne_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            InlineCommentAnchor.Create(
                _reviewCommentId,
                0,
                ValidEndLine,
                ValidTextSnippet));

        Assert.Contains("Start line", exception.Message);
    }

    [Fact]
    public void Create_WithEndLineBeforeStartLine_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            InlineCommentAnchor.Create(
                _reviewCommentId,
                ValidEndLine,
                ValidStartLine,
                ValidTextSnippet));

        Assert.Contains("End line", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyTextSnippet_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            InlineCommentAnchor.Create(
                _reviewCommentId,
                ValidStartLine,
                ValidEndLine,
                string.Empty));

        Assert.Contains("Text snippet", exception.Message);
    }

    [Fact]
    public void Create_WithWhitespaceTextSnippet_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            InlineCommentAnchor.Create(
                _reviewCommentId,
                ValidStartLine,
                ValidEndLine,
                "   "));

        Assert.Contains("Text snippet", exception.Message);
    }

    #endregion

    #region CoversLine Tests

    [Fact]
    public void CoversLine_WithLineInRange_ReturnsTrue()
    {
        // Arrange
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            10,
            20,
            ValidTextSnippet);

        // Act & Assert
        Assert.True(anchor.CoversLine(10));
        Assert.True(anchor.CoversLine(15));
        Assert.True(anchor.CoversLine(20));
    }

    [Fact]
    public void CoversLine_WithLineOutsideRange_ReturnsFalse()
    {
        // Arrange
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            10,
            20,
            ValidTextSnippet);

        // Act & Assert
        Assert.False(anchor.CoversLine(9));
        Assert.False(anchor.CoversLine(21));
        Assert.False(anchor.CoversLine(1));
        Assert.False(anchor.CoversLine(100));
    }

    [Fact]
    public void CoversLine_WithSingleLine_CoversOnlyThatLine()
    {
        // Arrange
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            15,
            15,
            ValidTextSnippet);

        // Act & Assert
        Assert.True(anchor.CoversLine(15));
        Assert.False(anchor.CoversLine(14));
        Assert.False(anchor.CoversLine(16));
    }

    #endregion

    #region GetLineRangeDisplay Tests

    [Fact]
    public void GetLineRangeDisplay_WithSingleLine_ReturnsLineNumber()
    {
        // Arrange
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            15,
            15,
            ValidTextSnippet);

        // Act
        var display = anchor.GetLineRangeDisplay();

        // Assert
        Assert.Equal("15", display);
    }

    [Fact]
    public void GetLineRangeDisplay_WithMultipleLines_ReturnsRange()
    {
        // Arrange
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            10,
            20,
            ValidTextSnippet);

        // Act
        var display = anchor.GetLineRangeDisplay();

        // Assert
        Assert.Equal("10-20", display);
    }

    [Fact]
    public void GetLineRangeDisplay_WithTwoLines_ReturnsRange()
    {
        // Arrange
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            10,
            11,
            ValidTextSnippet);

        // Act
        var display = anchor.GetLineRangeDisplay();

        // Assert
        Assert.Equal("10-11", display);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithLine1_IsValid()
    {
        // Act
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            1,
            1,
            ValidTextSnippet);

        // Assert
        Assert.Equal(1, anchor.StartLine);
        Assert.Equal(1, anchor.EndLine);
    }

    [Fact]
    public void Create_WithVeryLargeLine_IsValid()
    {
        // Act
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            10000,
            20000,
            ValidTextSnippet);

        // Assert
        Assert.Equal(10000, anchor.StartLine);
        Assert.Equal(20000, anchor.EndLine);
    }

    [Fact]
    public void Create_WithExactly200CharSnippet_DoesNotTruncate()
    {
        // Arrange
        var snippet200 = new string('x', 200);

        // Act
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            ValidStartLine,
            ValidEndLine,
            snippet200);

        // Assert
        Assert.Equal(200, anchor.TextSnippet.Length);
        Assert.Equal(snippet200, anchor.TextSnippet);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        // Act
        var anchor1 = InlineCommentAnchor.Create(
            _reviewCommentId,
            ValidStartLine,
            ValidEndLine,
            ValidTextSnippet);

        var anchor2 = InlineCommentAnchor.Create(
            _reviewCommentId,
            ValidStartLine,
            ValidEndLine,
            ValidTextSnippet);

        // Assert
        Assert.NotEqual(anchor1.Id, anchor2.Id);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var anchor = InlineCommentAnchor.Create(
            _reviewCommentId,
            ValidStartLine,
            ValidEndLine,
            ValidTextSnippet);

        var after = DateTime.UtcNow;

        // Assert
        Assert.True(anchor.CreatedAt >= before);
        Assert.True(anchor.CreatedAt <= after);
        Assert.Equal(DateTimeKind.Utc, anchor.CreatedAt.Kind);
    }

    #endregion
}
