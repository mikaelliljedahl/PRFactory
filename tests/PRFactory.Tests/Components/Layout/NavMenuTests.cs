using Bunit;
using Bunit.TestDoubles;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Layout;
using Xunit;

namespace PRFactory.Tests.Components.Layout;

public class NavMenuTests : ComponentTestBase
{
    private TestAuthorizationContext _authContext = null!;

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Add authorization services before any components are rendered
        _authContext = this.AddTestAuthorization();
    }

    [Fact]
    public void Render_DisplaysNavigationMenu()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_DisplaysMainNavigationItems()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        Assert.Contains("Dashboard", cut.Markup);
        Assert.Contains("Tickets", cut.Markup);
    }

    [Fact]
    public void Render_WithAuthenticatedUser_DisplaysAllMenuItems()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        Assert.Contains("Workflows", cut.Markup);
        Assert.Contains("Repositories", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysMenuIcons()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        Assert.Contains("bi-", cut.Markup); // Bootstrap icons
    }
}
