using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PRFactory.Api.Models;

/// <summary>
/// Request to create a new ticket via Web UI
/// </summary>
public class CreateTicketRequest
{
    /// <summary>
    /// Ticket title
    /// </summary>
    [Required]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ticket description
    /// </summary>
    [Required]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID to associate with this ticket
    /// </summary>
    [Required]
    [JsonPropertyName("repositoryId")]
    public Guid RepositoryId { get; set; }

    /// <summary>
    /// Whether to enable synchronization with external ticket systems (e.g., Jira)
    /// </summary>
    [JsonPropertyName("enableExternalSync")]
    public bool EnableExternalSync { get; set; } = false;

    /// <summary>
    /// External system name (e.g., "Jira", "AzureDevOps") - optional
    /// </summary>
    [JsonPropertyName("externalSystem")]
    public string? ExternalSystem { get; set; }
}
