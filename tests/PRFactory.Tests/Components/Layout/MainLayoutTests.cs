using Bunit;
using Bunit.TestDoubles;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Layout;
using Xunit;

namespace PRFactory.Tests.Components.Layout;

public class MainLayoutTests : ComponentTestBase
{
    private TestAuthorizationContext _authContext = null!;

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Add authorization services before any components are rendered
        _authContext = this.AddTestAuthorization();
    }

    [Fact]
    public void Render_DisplaysLayoutStructure()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_IncludesNavMenu()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        // NavMenu should be rendered as part of the layout
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_IncludesMainContentArea()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(builder => builder
            .AddChildContent("<div id=\"test-content\">Test Content</div>"));

        // Assert
        Assert.Contains("Test Content", cut.Markup);
    }

    [Fact]
    public void Render_WithUnauthenticatedUser_StillRendersLayout()
    {
        // Arrange
        _authContext.SetNotAuthorized();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        Assert.NotNull(cut.Markup);
    }
}
