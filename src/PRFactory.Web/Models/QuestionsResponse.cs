using System.Text.Json.Serialization;

namespace PRFactory.Web.Models;

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
