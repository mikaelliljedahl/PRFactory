namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents an anchor that links a review comment to specific lines of text in a plan.
/// Enables inline commenting on specific sections of markdown content.
/// </summary>
public class InlineCommentAnchor
{
    /// <summary>
    /// Unique identifier for this anchor
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The review comment this anchor belongs to
    /// </summary>
    public Guid ReviewCommentId { get; private set; }

    /// <summary>
    /// Starting line number (1-based) of the anchored text
    /// </summary>
    public int StartLine { get; private set; }

    /// <summary>
    /// Ending line number (1-based) of the anchored text
    /// </summary>
    public int EndLine { get; private set; }

    /// <summary>
    /// Text snippet from the anchored location (max 200 chars for display)
    /// </summary>
    public string TextSnippet { get; private set; } = string.Empty;

    /// <summary>
    /// When the anchor was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public ReviewComment ReviewComment { get; private set; } = null!;

    // EF Core constructor
    private InlineCommentAnchor() { }

    /// <summary>
    /// Creates a new inline comment anchor
    /// </summary>
    /// <param name="reviewCommentId">ID of the associated review comment</param>
    /// <param name="startLine">Starting line number (1-based)</param>
    /// <param name="endLine">Ending line number (1-based)</param>
    /// <param name="textSnippet">Snippet of the anchored text</param>
    /// <returns>New InlineCommentAnchor instance</returns>
    public static InlineCommentAnchor Create(
        Guid reviewCommentId,
        int startLine,
        int endLine,
        string textSnippet)
    {
        if (reviewCommentId == Guid.Empty)
            throw new ArgumentException("ReviewCommentId cannot be empty", nameof(reviewCommentId));

        if (startLine < 1)
            throw new ArgumentException("Start line must be at least 1", nameof(startLine));

        if (endLine < startLine)
            throw new ArgumentException("End line must be greater than or equal to start line", nameof(endLine));

        if (string.IsNullOrWhiteSpace(textSnippet))
            throw new ArgumentException("Text snippet cannot be empty", nameof(textSnippet));

        return new InlineCommentAnchor
        {
            Id = Guid.NewGuid(),
            ReviewCommentId = reviewCommentId,
            StartLine = startLine,
            EndLine = endLine,
            TextSnippet = TruncateSnippet(textSnippet, 200),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Checks if this anchor covers a specific line number
    /// </summary>
    public bool CoversLine(int lineNumber)
    {
        return lineNumber >= StartLine && lineNumber <= EndLine;
    }

    /// <summary>
    /// Gets the line range as a string (e.g., "10-15" or "10" for single line)
    /// </summary>
    public string GetLineRangeDisplay()
    {
        return StartLine == EndLine
            ? StartLine.ToString()
            : $"{StartLine}-{EndLine}";
    }

    private static string TruncateSnippet(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength);
    }
}
