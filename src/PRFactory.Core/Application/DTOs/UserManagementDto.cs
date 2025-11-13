namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for displaying user management information
/// </summary>
public class UserManagementDto
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The tenant this user belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the user
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL to user's avatar image
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// User's role within the tenant (Owner, Admin, Member, Viewer)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Whether this user is active and can access the system
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user was last active
    /// </summary>
    public DateTime? LastSeenAt { get; set; }
}
