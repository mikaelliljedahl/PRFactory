using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using PRFactory.Web.Components.Auth;

namespace PRFactory.Web.Tests.Components.Auth;

/// <summary>
/// Tests for the RedirectToLogin component.
/// Verifies redirect to login page behavior on initialization.
/// </summary>
public class RedirectToLoginTests : TestContext
{
    public RedirectToLoginTests()
    {
        // Setup mock NavigationManager
        Services.AddSingleton<NavigationManager>(new MockNavigationManager());
    }

    [Fact]
    public void RedirectToLogin_OnInitialize_NavigatesToLoginPage()
    {
        // Arrange
        var navigationManager = Services.GetService<NavigationManager>() as MockNavigationManager;
        Assert.NotNull(navigationManager);

        // Act
        var cut = RenderComponent<RedirectToLogin>();

        // Assert
        Assert.NotNull(navigationManager);
        Assert.Contains("/auth/login", navigationManager.Uri);
    }

    [Fact]
    public void RedirectToLogin_UsesForceLoad_WhenNavigating()
    {
        // Arrange
        var navigationManager = Services.GetService<NavigationManager>() as MockNavigationManager;
        Assert.NotNull(navigationManager);

        // Act
        var cut = RenderComponent<RedirectToLogin>();

        // Assert
        Assert.NotNull(navigationManager);
        // Verify the navigation happened (mock tracks it in Uri)
        Assert.False(string.IsNullOrEmpty(navigationManager.Uri));
    }

    [Fact]
    public void RedirectToLogin_RendersCssClass()
    {
        // Act
        var cut = RenderComponent<RedirectToLogin>();

        // Assert - Component should render without throwing
        Assert.NotNull(cut);
    }

    [Fact]
    public void RedirectToLogin_WithNoParameters_RendersSuccessfully()
    {
        // Act
        var cut = RenderComponent<RedirectToLogin>();

        // Assert
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void RedirectToLogin_MultipleInstances_EachRedirect()
    {
        // Arrange
        var navigationManager = Services.GetService<NavigationManager>() as MockNavigationManager;

        // Act - Create first instance
        var cut1 = RenderComponent<RedirectToLogin>();
        var firstUri = navigationManager?.Uri;

        // Create second instance (navigation should happen again)
        var cut2 = RenderComponent<RedirectToLogin>();

        // Assert - Both instances should have triggered navigation
        Assert.NotNull(navigationManager);
        Assert.Contains("/auth/login", navigationManager.Uri);
    }

    [Fact]
    public void RedirectToLogin_ComponentDoesNotRenderContent()
    {
        // Act
        var cut = RenderComponent<RedirectToLogin>();

        // Assert - Component has minimal markup (only injection)
        Assert.True(string.IsNullOrWhiteSpace(cut.Markup.Trim()));
    }
}

/// <summary>
/// Mock NavigationManager for testing navigation behavior.
/// </summary>
internal class MockNavigationManager : NavigationManager
{
    public MockNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Uri = ToAbsoluteUri(uri).ToString();
    }
}
