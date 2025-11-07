using System.Text.Json.Serialization;

namespace PRFactory.Api.Models;

/// <summary>
/// Response containing questions for a ticket
/// </summary>
public class QuestionsResponse
{
    /// <summary>
    /// The ticket ID these questions belong to
    /// </summary>
    [JsonPropertyName("ticketId")]
    public Guid TicketId { get; set; }

    /// <summary>
    /// List of questions
    /// </summary>
    [JsonPropertyName("questions")]
    public List<QuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// Indicates whether all questions have been answered
    /// </summary>
    [JsonPropertyName("allAnswered")]
    public bool AllAnswered { get; set; }
}

/// <summary>
/// Represents a single question with its answer status
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// Unique identifier for the question
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The question text
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether this question has been answered
    /// </summary>
    [JsonPropertyName("isAnswered")]
    public bool IsAnswered { get; set; }

    /// <summary>
    /// The answer text (if answered)
    /// </summary>
    [JsonPropertyName("answerText")]
    public string? AnswerText { get; set; }
}
