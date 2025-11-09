namespace PRFactory.Infrastructure.Tickets;

/// <summary>
/// DTOs for ticket operations
/// </summary>
public record TicketInfo(
    string Key,
    string Title,
    string Description,
    string Status,
    string Url
);

public record CommentInfo(
    string Id,
    string Body,
    DateTime CreatedAt
);

/// <summary>
/// Strategy interface for ticket platform-specific operations (posting comments, updating tickets)
/// Each platform (Jira, Azure DevOps, GitHub Issues, GitLab) implements this interface
/// </summary>
public interface ITicketPlatformProvider
{
    /// <summary>
    /// Platform name (Jira, AzureDevOps, GitHub, GitLab)
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Post a comment to a ticket, converting markdown to platform-specific format
    /// </summary>
    Task<CommentInfo> PostCommentAsync(
        Guid tenantId,
        string ticketKey,
        string markdownText,
        CancellationToken ct = default
    );

    /// <summary>
    /// Link a pull request to a ticket
    /// </summary>
    Task LinkPullRequestAsync(
        Guid tenantId,
        string ticketKey,
        string prUrl,
        string prTitle,
        string? repositoryName = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Update a custom field on a ticket
    /// </summary>
    Task UpdateCustomFieldAsync(
        Guid tenantId,
        string ticketKey,
        string fieldKey,
        object value,
        CancellationToken ct = default
    );

    /// <summary>
    /// Transition a ticket to a different status
    /// </summary>
    Task TransitionToStatusAsync(
        Guid tenantId,
        string ticketKey,
        string statusName,
        CancellationToken ct = default
    );

    /// <summary>
    /// Get ticket information
    /// </summary>
    Task<TicketInfo> GetTicketAsync(
        Guid tenantId,
        string ticketKey,
        CancellationToken ct = default
    );

    /// <summary>
    /// Update the title/summary of a ticket
    /// </summary>
    Task UpdateTitleAsync(
        Guid tenantId,
        string ticketKey,
        string title,
        CancellationToken ct = default
    );

    /// <summary>
    /// Add labels/tags to a ticket
    /// </summary>
    Task AddLabelsAsync(
        Guid tenantId,
        string ticketKey,
        string[] labels,
        CancellationToken ct = default
    );
}
