namespace PRFactory.Web.Models;

/// <summary>
/// DTO representing a question with its answer status
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
    /// Category of the question (e.g., "requirements", "technical", "testing")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// When the question was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the question has been answered
    /// </summary>
    public bool IsAnswered { get; set; }

    /// <summary>
    /// The answer text (if answered)
    /// </summary>
    public string? AnswerText { get; set; }

    /// <summary>
    /// When the question was answered (if answered)
    /// </summary>
    public DateTime? AnsweredAt { get; set; }
}
