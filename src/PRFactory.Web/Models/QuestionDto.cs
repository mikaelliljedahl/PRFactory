namespace PRFactory.Web.Models;

/// <summary>
/// Represents a single question with its answer status for display in the UI
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// Unique identifier for the question
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The question text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether this question has been answered
    /// </summary>
    public bool IsAnswered { get; set; }

    /// <summary>
    /// The answer text (if answered)
    /// </summary>
    public string? AnswerText { get; set; }
}

/// <summary>
/// Response containing questions for a ticket
/// </summary>
public class QuestionsResponse
{
    /// <summary>
    /// The ticket ID these questions belong to
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// List of questions
    /// </summary>
    public List<QuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// Indicates whether all questions have been answered
    /// </summary>
    public bool AllAnswered { get; set; }
}
