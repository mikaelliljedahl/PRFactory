using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PRFactory.Api.Models;

/// <summary>
/// Request to submit answers to clarifying questions
/// </summary>
public class SubmitAnswersRequest
{
    /// <summary>
    /// List of answers to the questions
    /// </summary>
    [Required]
    [JsonPropertyName("answers")]
    public List<QuestionAnswer> Answers { get; set; } = new();
}

/// <summary>
/// Represents an answer to a specific question
/// </summary>
public class QuestionAnswer
{
    /// <summary>
    /// Unique identifier for the question being answered
    /// </summary>
    [Required]
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// The user's answer text
    /// </summary>
    [Required]
    [JsonPropertyName("answerText")]
    public string AnswerText { get; set; } = string.Empty;
}
