using Microsoft.Extensions.Logging;
using PRFactory.Core.Authentication;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Authentication;

/// <summary>
/// Database-backed token storage for per-user OAuth (user seats)
/// Tokens are encrypted and stored in the User entity
/// </summary>
public class TokenStore : ITokenStore
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<TokenStore> _logger;

    public TokenStore(IUserRepository userRepository, ILogger<TokenStore> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task SaveTokensAsync(Guid userId, StoredTokens tokens)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User not found: {userId}");
            }

            // Update user with OAuth tokens (SetOAuthTokens method encrypts tokens)
            user.SetOAuthTokens(
                tokens.AccessToken ?? string.Empty,
                tokens.RefreshToken ?? string.Empty,
                tokens.ExpiresAt,
                tokens.Scope ?? Array.Empty<string>()
            );

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("OAuth tokens saved for user {UserId}. Expires at: {ExpiresAt}",
                userId, tokens.ExpiresAt);
        }
        catch
        {
            throw;
        }
    }

    public async Task<StoredTokens?> LoadTokensAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found when loading tokens: {UserId}", userId);
                return null;
            }

            // Check if user has OAuth tokens
            if (string.IsNullOrEmpty(user.OAuthAccessToken))
            {
                return null;
            }

            return new StoredTokens
            {
                AccessToken = user.OAuthAccessToken,
                RefreshToken = user.OAuthRefreshToken,
                ExpiresAt = user.OAuthTokenExpiresAt ?? DateTime.UtcNow,
                CreatedAt = user.CreatedAt,
                TokenType = "Bearer",
                Scope = user.OAuthScopes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load OAuth tokens for user {UserId}", userId);
            return null;
        }
    }

    public async Task ClearTokensAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User not found: {userId}");
            }

            user.ClearOAuthTokens();
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("OAuth tokens cleared for user {UserId}", userId);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> NeedsRefreshAsync(Guid userId)
    {
        var tokens = await LoadTokensAsync(userId);
        return tokens?.NeedsRefresh ?? false;
    }
}
