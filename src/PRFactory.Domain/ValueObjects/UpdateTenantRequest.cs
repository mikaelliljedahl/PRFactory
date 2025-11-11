using PRFactory.Domain.Entities;

namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Request parameters for updating a tenant
/// </summary>
public class UpdateTenantRequest
{
    /// <summary>
    /// Tenant ID to update
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Tenant name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Ticket platform URL
    /// </summary>
    public string TicketPlatformUrl { get; }

    /// <summary>
    /// Ticket platform API token (optional, only if changing)
    /// </summary>
    public string? TicketPlatformApiToken { get; }

    /// <summary>
    /// Claude API key (optional, only if changing)
    /// </summary>
    public string? ClaudeApiKey { get; }

    /// <summary>
    /// Ticket platform type (optional)
    /// </summary>
    public string? TicketPlatform { get; }

    /// <summary>
    /// Tenant configuration (optional)
    /// </summary>
    public TenantConfiguration? Configuration { get; }

    public UpdateTenantRequest(
        Guid id,
        string name,
        string ticketPlatformUrl,
        string? ticketPlatformApiToken = null,
        string? claudeApiKey = null,
        string? ticketPlatform = null,
        TenantConfiguration? configuration = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(ticketPlatformUrl))
            throw new ArgumentException("Ticket platform URL cannot be empty", nameof(ticketPlatformUrl));

        Id = id;
        Name = name;
        TicketPlatformUrl = ticketPlatformUrl;
        TicketPlatformApiToken = ticketPlatformApiToken;
        ClaudeApiKey = claudeApiKey;
        TicketPlatform = ticketPlatform;
        Configuration = configuration;
    }
}
