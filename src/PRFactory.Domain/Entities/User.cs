namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a user in the PRFactory system.
/// Users can be assigned as reviewers for plan reviews and can comment on tickets.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The tenant this user belongs to
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// User's email address (unique within tenant)
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Display name for the user
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Optional URL to user's avatar image
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// External authentication ID (e.g., Auth0 user ID)
    /// Used for integration with authentication providers
    /// </summary>
    public string? ExternalAuthId { get; private set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the user was last active (nullable for users who haven't logged in yet)
    /// </summary>
    public DateTime? LastSeenAt { get; private set; }

    /// <summary>
    /// OAuth access token for Anthropic API (encrypted at rest)
    /// </summary>
    public string? OAuthAccessToken { get; private set; }

    /// <summary>
    /// OAuth refresh token for Anthropic API (encrypted at rest)
    /// </summary>
    public string? OAuthRefreshToken { get; private set; }

    /// <summary>
    /// When the OAuth access token expires
    /// </summary>
    public DateTime? OAuthTokenExpiresAt { get; private set; }

    /// <summary>
    /// OAuth scopes granted for this user
    /// </summary>
    public string[] OAuthScopes { get; private set; } = Array.Empty<string>();

    // Navigation properties
    public Tenant Tenant { get; private set; } = null!;
    public ICollection<PlanReview> PlanReviews { get; private set; } = new List<PlanReview>();
    public ICollection<ReviewComment> Comments { get; private set; } = new List<ReviewComment>();

    // EF Core constructor
    private User() { }

    /// <summary>
    /// Creates a new user
    /// </summary>
    public User(Guid tenantId, string email, string displayName, string? avatarUrl = null, string? externalAuthId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        AvatarUrl = avatarUrl;
        ExternalAuthId = externalAuthId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's last seen timestamp
    /// </summary>
    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's profile information
    /// </summary>
    public void UpdateProfile(string displayName, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        DisplayName = displayName.Trim();
        AvatarUrl = avatarUrl;
    }

    /// <summary>
    /// Links the user to an external authentication provider
    /// </summary>
    public void LinkExternalAuth(string externalAuthId)
    {
        if (string.IsNullOrWhiteSpace(externalAuthId))
            throw new ArgumentException("External auth ID cannot be empty", nameof(externalAuthId));

        ExternalAuthId = externalAuthId.Trim();
    }

    /// <summary>
    /// Sets OAuth tokens for this user (tokens are encrypted by EF Core)
    /// </summary>
    public void SetOAuthTokens(string accessToken, string refreshToken, DateTime expiresAt, string[] scopes)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        OAuthAccessToken = accessToken;
        OAuthRefreshToken = refreshToken;
        OAuthTokenExpiresAt = expiresAt;
        OAuthScopes = scopes ?? Array.Empty<string>();
    }

    /// <summary>
    /// Clears OAuth tokens for this user (logout)
    /// </summary>
    public void ClearOAuthTokens()
    {
        OAuthAccessToken = null;
        OAuthRefreshToken = null;
        OAuthTokenExpiresAt = null;
        OAuthScopes = Array.Empty<string>();
    }

    /// <summary>
    /// Returns true if the user has valid (non-expired) OAuth tokens
    /// </summary>
    public bool HasValidOAuthTokens()
    {
        return !string.IsNullOrEmpty(OAuthAccessToken)
            && OAuthTokenExpiresAt.HasValue
            && OAuthTokenExpiresAt.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the OAuth tokens need to be refreshed (expire within 5 minutes)
    /// </summary>
    public bool OAuthTokensNeedRefresh()
    {
        return !string.IsNullOrEmpty(OAuthAccessToken)
            && OAuthTokenExpiresAt.HasValue
            && OAuthTokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5);
    }
}
