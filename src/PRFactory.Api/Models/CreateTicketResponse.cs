using System.Text.Json.Serialization;

namespace PRFactory.Api.Models;

/// <summary>
/// Response for ticket creation
/// </summary>
public class CreateTicketResponse
{
    /// <summary>
    /// Unique identifier for the created ticket
    /// </summary>
    [JsonPropertyName("ticketId")]
    public Guid TicketId { get; set; }

    /// <summary>
    /// Ticket key (e.g., "WEB-001" for internal tickets or Jira issue key)
    /// </summary>
    [JsonPropertyName("ticketKey")]
    public string TicketKey { get; set; } = string.Empty;

    /// <summary>
    /// Current workflow state of the ticket
    /// </summary>
    [JsonPropertyName("currentState")]
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the ticket was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
