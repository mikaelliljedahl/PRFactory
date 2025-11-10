using PRFactory.Domain.Entities;

namespace PRFactory.Tests.Builders;

/// <summary>
/// Fluent builder for creating User entities in tests with sensible defaults
/// </summary>
public class UserBuilder
{
    private Guid _tenantId = Guid.NewGuid();
    private string _email = "test.user@example.com";
    private string _displayName = "Test User";
    private string? _avatarUrl;
    private string? _externalAuthId;
    private bool _hasOAuthTokens;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime? _tokenExpiresAt;
    private string[] _oauthScopes = Array.Empty<string>();

    public UserBuilder()
    {
    }

    public UserBuilder ForTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public UserBuilder WithAvatarUrl(string avatarUrl)
    {
        _avatarUrl = avatarUrl;
        return this;
    }

    public UserBuilder WithExternalAuthId(string externalAuthId)
    {
        _externalAuthId = externalAuthId;
        return this;
    }

    public UserBuilder WithOAuthTokens(string accessToken, string refreshToken, DateTime expiresAt, string[] scopes)
    {
        _hasOAuthTokens = true;
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _tokenExpiresAt = expiresAt;
        _oauthScopes = scopes;
        return this;
    }

    public UserBuilder WithValidOAuthTokens()
    {
        _hasOAuthTokens = true;
        _accessToken = "test-access-token";
        _refreshToken = "test-refresh-token";
        _tokenExpiresAt = DateTime.UtcNow.AddHours(1);
        _oauthScopes = new[] { "read", "write" };
        return this;
    }

    public UserBuilder WithExpiredOAuthTokens()
    {
        _hasOAuthTokens = true;
        _accessToken = "test-access-token";
        _refreshToken = "test-refresh-token";
        _tokenExpiresAt = DateTime.UtcNow.AddHours(-1);
        _oauthScopes = new[] { "read", "write" };
        return this;
    }

    public User Build()
    {
        var user = new User(_tenantId, _email, _displayName, _avatarUrl, _externalAuthId);

        if (_hasOAuthTokens && _accessToken != null && _refreshToken != null && _tokenExpiresAt.HasValue)
        {
            user.SetOAuthTokens(_accessToken, _refreshToken, _tokenExpiresAt.Value, _oauthScopes);
        }

        return user;
    }
}
