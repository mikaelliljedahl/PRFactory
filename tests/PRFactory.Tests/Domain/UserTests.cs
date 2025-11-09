using PRFactory.Domain.Entities;
using Xunit;

namespace PRFactory.Tests.Domain;

public class UserTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string ValidEmail = "test@example.com";
    private const string ValidDisplayName = "Test User";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidInputs_CreatesUserWithCorrectProperties()
    {
        // Arrange
        const string avatarUrl = "https://example.com/avatar.jpg";
        const string externalAuthId = "auth0|123456";

        // Act
        var user = new User(_tenantId, ValidEmail, ValidDisplayName, avatarUrl, externalAuthId);

        // Assert
        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(_tenantId, user.TenantId);
        Assert.Equal(ValidEmail, user.Email);
        Assert.Equal(ValidDisplayName, user.DisplayName);
        Assert.Equal(avatarUrl, user.AvatarUrl);
        Assert.Equal(externalAuthId, user.ExternalAuthId);
        Assert.True(Math.Abs((user.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Null(user.LastSeenAt);
    }

    [Fact]
    public void Constructor_WithEmailContainingWhitespace_TrimsAndLowercasesEmail()
    {
        // Arrange
        const string emailWithWhitespace = "  Test@Example.COM  ";
        const string expectedEmail = "test@example.com";

        // Act
        var user = new User(_tenantId, emailWithWhitespace, ValidDisplayName);

        // Assert
        Assert.Equal(expectedEmail, user.Email);
    }

    [Fact]
    public void Constructor_WithDisplayNameContainingWhitespace_TrimsDisplayName()
    {
        // Arrange
        const string displayNameWithWhitespace = "  Test User  ";
        const string expectedDisplayName = "Test User";

        // Act
        var user = new User(_tenantId, ValidEmail, displayNameWithWhitespace);

        // Assert
        Assert.Equal(expectedDisplayName, user.DisplayName);
    }

    [Fact]
    public void Constructor_WithEmptyEmail_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new User(_tenantId, "", ValidDisplayName));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithNullEmail_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new User(_tenantId, null!, ValidDisplayName));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithWhitespaceEmail_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new User(_tenantId, "   ", ValidDisplayName));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithEmptyDisplayName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new User(_tenantId, ValidEmail, ""));
        Assert.Contains("displayName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithNullDisplayName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new User(_tenantId, ValidEmail, null!));
        Assert.Contains("displayName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateLastSeen Tests

    [Fact]
    public void UpdateLastSeen_SetsLastSeenAtToCurrentTime()
    {
        // Arrange
        var user = new User(_tenantId, ValidEmail, ValidDisplayName);
        Assert.Null(user.LastSeenAt);

        // Act
        user.UpdateLastSeen();

        // Assert
        Assert.NotNull(user.LastSeenAt);
        Assert.True(Math.Abs((user.LastSeenAt.Value - DateTime.UtcNow).TotalSeconds) < 1);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public void UpdateProfile_WithValidInputs_UpdatesDisplayNameAndAvatarUrl()
    {
        // Arrange
        var user = new User(_tenantId, ValidEmail, ValidDisplayName);
        const string newDisplayName = "Updated Name";
        const string newAvatarUrl = "https://example.com/new-avatar.jpg";

        // Act
        user.UpdateProfile(newDisplayName, newAvatarUrl);

        // Assert
        Assert.Equal(newDisplayName, user.DisplayName);
        Assert.Equal(newAvatarUrl, user.AvatarUrl);
    }

    [Fact]
    public void UpdateProfile_WithDisplayNameContainingWhitespace_TrimsDisplayName()
    {
        // Arrange
        var user = new User(_tenantId, ValidEmail, ValidDisplayName);
        const string displayNameWithWhitespace = "  Updated Name  ";
        const string expectedDisplayName = "Updated Name";

        // Act
        user.UpdateProfile(displayNameWithWhitespace);

        // Assert
        Assert.Equal(expectedDisplayName, user.DisplayName);
    }

    [Fact]
    public void UpdateProfile_WithEmptyDisplayName_ThrowsArgumentException()
    {
        // Arrange
        var user = new User(_tenantId, ValidEmail, ValidDisplayName);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => user.UpdateProfile(""));
        Assert.Contains("displayName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateProfile_WithNullDisplayName_ThrowsArgumentException()
    {
        // Arrange
        var user = new User(_tenantId, ValidEmail, ValidDisplayName);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => user.UpdateProfile(null!));
        Assert.Contains("displayName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region LinkExternalAuth Tests

    [Fact]
    public void LinkExternalAuth_WithValidId_SetsExternalAuthId()
    {
        // Arrange
        var user = new User(_tenantId, ValidEmail, ValidDisplayName);
        const string externalAuthId = "auth0|123456";

        // Act
        user.LinkExternalAuth(externalAuthId);

        // Assert
        Assert.Equal(externalAuthId, user.ExternalAuthId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LinkExternalAuth_WithEmptyId_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var user = new User(_tenantId, ValidEmail, ValidDisplayName);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => user.LinkExternalAuth(invalidId!));
        Assert.Contains("externalAuthId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
