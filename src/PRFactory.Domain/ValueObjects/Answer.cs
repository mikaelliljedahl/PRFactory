namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Represents an answer provided by a developer to a clarifying question.
/// </summary>
public class Answer
{
    /// <summary>
    /// The ID of the question this answer corresponds to
    /// </summary>
    public string QuestionId { get; init; }

    /// <summary>
    /// The answer text provided by the developer
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// When the answer was provided
    /// </summary>
    public DateTime AnsweredAt { get; init; }

    /// <summary>
    /// Creates a new answer for the specified question
    /// </summary>
    public Answer(string questionId, string text, DateTime answeredAt)
    {
        if (string.IsNullOrWhiteSpace(questionId))
            throw new ArgumentException("Question ID cannot be empty", nameof(questionId));

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Answer text cannot be empty", nameof(text));

        QuestionId = questionId;
        Text = text;
        AnsweredAt = answeredAt;
    }
}
