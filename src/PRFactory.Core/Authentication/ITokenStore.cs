namespace PRFactory.Core.Authentication;

/// <summary>
/// Interface for token storage operations to enable dependency injection and testing
/// In PRFactory, tokens are stored in the database (User entity) - one OAuth token set per user seat
/// </summary>
public interface ITokenStore
{
    /// <summary>
    /// Saves OAuth tokens for a specific user
    /// </summary>
    /// <param name="userId">The user ID (seat)</param>
    /// <param name="tokens">The tokens to save</param>
    Task SaveTokensAsync(Guid userId, StoredTokens tokens);

    /// <summary>
    /// Loads OAuth tokens for a specific user if they exist
    /// </summary>
    /// <param name="userId">The user ID (seat)</param>
    /// <returns>The stored tokens or null if none exist</returns>
    Task<StoredTokens?> LoadTokensAsync(Guid userId);

    /// <summary>
    /// Clears all stored OAuth tokens for a specific user
    /// </summary>
    /// <param name="userId">The user ID (seat)</param>
    Task ClearTokensAsync(Guid userId);

    /// <summary>
    /// Gets whether the current tokens need to be refreshed for a specific user
    /// </summary>
    /// <param name="userId">The user ID (seat)</param>
    Task<bool> NeedsRefreshAsync(Guid userId);
}

/// <summary>
/// OAuth tokens stored in database
/// </summary>
public class StoredTokens
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public string[]? Scope { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool NeedsRefresh => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}
