namespace PRFactory.Web.Models;

/// <summary>
/// DTO for diff content returned to Blazor components.
/// </summary>
public class DiffContentDto
{
    /// <summary>
    /// The ticket ID this diff belongs to
    /// </summary>
    public required Guid TicketId { get; init; }

    /// <summary>
    /// Raw diff content in unified diff format (git patch)
    /// </summary>
    public required string DiffContent { get; init; }

    /// <summary>
    /// Size of diff in bytes
    /// </summary>
    public int SizeBytes { get; init; }

    /// <summary>
    /// Number of files changed (parsed from diff)
    /// </summary>
    public int FilesChanged { get; init; }

    /// <summary>
    /// Indicates if diff exists and is available
    /// </summary>
    public bool Available { get; init; }
}
