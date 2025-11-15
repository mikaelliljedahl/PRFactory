using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Xunit;
using PRFactory.Web.Components.Auth;

namespace PRFactory.Web.Tests.Components.Auth;

/// <summary>
/// Tests for the UserProfileDropdown component.
/// Verifies rendering of user profile information, dropdown menu, and logout functionality.
/// </summary>
public class UserProfileDropdownTests : TestContext
{
    private const string TestUserName = "testuser@example.com";

    private TestAuthorizationContext _authContext;

    public UserProfileDropdownTests()
    {
        // Setup mock NavigationManager
        Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Add bUnit test authorization (provides cascading AuthenticationState parameter)
        _authContext = this.AddTestAuthorization();

        // Setup JSInterop for Radzen components
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);
    }

    private void SetupAuthorizedUser(string userName = TestUserName)
    {
        _authContext.SetAuthorized(userName);
        _authContext.SetClaims(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, userName)
        );
    }

    private void SetupUnauthorizedUser()
    {
        _authContext.SetNotAuthorized();
    }

    [Fact]
    public void UserProfileDropdown_WhenAuthorized_DisplaysUserName()
    {
        // Arrange
        SetupAuthorizedUser("john.doe@example.com");

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        Assert.Contains("john.doe@example.com", cut.Markup);
    }

    [Fact]
    public void UserProfileDropdown_WhenAuthorized_DisplaysPersonIcon()
    {
        // Arrange
        SetupAuthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var icon = cut.Find("i.bi-person-circle");
        Assert.NotNull(icon);
    }

    [Fact]
    public void UserProfileDropdown_WhenAuthorized_DisplaysProfileMenuItem()
    {
        // Arrange
        SetupAuthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var profileLink = cut.Find("a[href='/settings/profile']");
        Assert.NotNull(profileLink);
        Assert.Contains("Profile", profileLink.TextContent);
    }

    [Fact]
    public void UserProfileDropdown_WhenAuthorized_DisplaysSettingsMenuItem()
    {
        // Arrange
        SetupAuthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var settingsLink = cut.Find("a[href='/settings/integrations']");
        Assert.NotNull(settingsLink);
        Assert.Contains("Settings", settingsLink.TextContent);
    }

    [Fact]
    public void UserProfileDropdown_WhenAuthorized_DisplaysLogoutButton()
    {
        // Arrange
        SetupAuthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var logoutButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Logout"));
        Assert.NotNull(logoutButton);
    }

    [Fact]
    public void UserProfileDropdown_ClickLogoutButton_NavigatesToLogoutEndpoint()
    {
        // Arrange
        SetupAuthorizedUser();
        var navigationManager = Services.GetService<NavigationManager>() as MockNavigationManager;

        var cut = RenderComponent<UserProfileDropdown>();

        // Act
        var logoutButton = cut.FindAll("button").First(b => b.TextContent.Contains("Logout"));
        logoutButton.Click();

        // Assert
        Assert.NotNull(navigationManager);
        Assert.Contains("/api/auth/logout", navigationManager.Uri);
    }

    [Fact]
    public void UserProfileDropdown_WhenUnauthorized_DisplaysSignInButton()
    {
        // Arrange
        SetupUnauthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var signInButton = cut.Find("a.btn-primary");
        Assert.NotNull(signInButton);
        Assert.Contains("Sign In", signInButton.TextContent);
    }

    [Fact]
    public void UserProfileDropdown_WhenUnauthorized_SignInButtonLinksToLogin()
    {
        // Arrange
        SetupUnauthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var signInButton = cut.Find("a[href='/auth/login']");
        Assert.NotNull(signInButton);
    }

    [Fact]
    public void UserProfileDropdown_WhenAuthorized_DoesNotShowSignInButton()
    {
        // Arrange
        SetupAuthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var markup = cut.Markup;
        // Should have dropdown button, not sign in button
        Assert.Contains("dropdown-toggle", markup);
        Assert.DoesNotContain("Sign In", markup);
    }

    [Fact]
    public void UserProfileDropdown_DisplaysPersonIconForAuthorized()
    {
        // Arrange
        SetupAuthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var personIcon = cut.Find("i.bi-person-circle");
        Assert.NotNull(personIcon);
    }

    [Fact]
    public void UserProfileDropdown_DisplaysBoxArrowIconForLogout()
    {
        // Arrange
        SetupAuthorizedUser();

        // Act
        var cut = RenderComponent<UserProfileDropdown>();

        // Assert
        var icons = cut.FindAll("i");
        var logoutIcon = icons.FirstOrDefault(i => i.ClassList.Contains("bi-box-arrow-right"));
        Assert.NotNull(logoutIcon);
    }
}
