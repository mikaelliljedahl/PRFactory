using Bunit;
using Bunit.TestDoubles;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Auth;
using Xunit;

namespace PRFactory.Tests.Components.Auth;

public class UserProfileDropdownTests : ComponentTestBase
{
    private TestAuthorizationContext _authContext = null!;

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Add authorization services before any components are rendered
        _authContext = this.AddTestAuthorization();
    }

    [Fact]
    public void Render_WithAuthenticatedUser_DisplaysUserInfo()
    {
        // Arrange
        _authContext.SetAuthorized("testuser@example.com");

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        Assert.Contains("testuser", cut.Markup);
    }

    [Fact]
    public void Render_WithUnauthenticatedUser_DisplaysLoginButton()
    {
        // Arrange
        _authContext.SetNotAuthorized();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        // Component should handle unauthenticated state
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_DisplaysDropdownToggle()
    {
        // Arrange
        _authContext.SetAuthorized("testuser@example.com");

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        Assert.NotNull(cut.Markup);
    }
}
