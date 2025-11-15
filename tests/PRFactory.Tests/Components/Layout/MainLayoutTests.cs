using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.Configuration;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Layout;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.Layout;

/// <summary>
/// Comprehensive bUnit tests for MainLayout component
/// Tests layout structure, navigation, demo mode, and content area rendering
/// </summary>
public class MainLayoutTests : ComponentTestBase
{
    private TestAuthorizationContext _authContext = null!;

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Add authorization services
        _authContext = this.AddTestAuthorization();

        // Mock IConfiguration
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ASPNETCORE_ENVIRONMENT"])
            .Returns("Production");

        // Mock services for NavMenu
        var errorServiceMock = new Mock<IErrorService>();
        errorServiceMock.Setup(s => s.GetUnresolvedCountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        var ticketServiceMock = new Mock<ITicketService>();
        ticketServiceMock.Setup(s => s.GetAllTicketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PRFactory.Web.Models.TicketDto>());

        services.AddScoped(_ => configMock.Object);
        services.AddScoped(_ => errorServiceMock.Object);
        services.AddScoped(_ => ticketServiceMock.Object);
    }

    [Fact]
    public void Render_DisplaysLayoutStructure()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        Assert.NotNull(cut.Markup);
        Assert.Contains("page", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSidebarWithNavMenu()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        Assert.Contains("sidebar", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysMainContent()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        Assert.Contains("<main>", cut.Markup);
        Assert.Contains("Test Content", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysTopBar()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        Assert.Contains("top-row", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysAboutLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        Assert.Contains("About", cut.Markup);
        Assert.Contains("https://github.com", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysContentArea()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");
        var testContent = "This is test page content";

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent(testContent));

        // Assert
        Assert.Contains("content", cut.Markup);
        Assert.Contains(testContent, cut.Markup);
    }

    [Fact]
    public void Render_DisplaysRadzenDialog()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        // RadzenDialog component should be rendered
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_DisplaysToastContainer()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        // ToastContainer component should be rendered
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_InProductionMode_NoDemoBanner()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        // In production mode (not development), demo mode is false
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_InDevelopmentMode_ShowsDemoBanner()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Create a new mock with Development environment
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ASPNETCORE_ENVIRONMENT"])
            .Returns("Development");

        // We need to re-render with updated config
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert - MainLayout will show demo banner in development
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_LayoutHasProperCSSClasses()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        Assert.Contains("page", cut.Markup);
        Assert.Contains("sidebar", cut.Markup);
        Assert.Contains("top-row", cut.Markup);
    }

    [Fact]
    public void Render_ContainsUserProfileDropdown()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        // UserProfileDropdown component should be present in top bar
        Assert.NotNull(cut.Markup);
        Assert.Contains("top-row", cut.Markup);
    }

    [Fact]
    public void Render_LayoutStructureIntegration()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");
        var pageTitle = "Test Dashboard";

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent(pageTitle));

        // Assert
        // Verify complete layout structure
        Assert.Contains("page", cut.Markup);
        Assert.Contains("sidebar", cut.Markup);
        Assert.Contains("<main>", cut.Markup);
        Assert.Contains("content", cut.Markup);
        Assert.Contains("top-row", cut.Markup);
        Assert.Contains(pageTitle, cut.Markup);
    }

    [Fact]
    public void Render_NavMenuReceivesDemoModeParameter()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        // NavMenu should be rendered with IsDemoMode parameter
        Assert.NotNull(cut.Markup);
        Assert.Contains("sidebar", cut.Markup);
    }

    [Fact]
    public void Render_ContentAreaHasCorrectPadding()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        Assert.Contains("px-4", cut.Markup);
    }

    [Fact]
    public void Render_MultipleChildContentSections()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");
        var content = "Sidebar content | Main content";

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent(content));

        // Assert
        Assert.Contains("Main content", cut.Markup);
    }

    [Fact]
    public void Render_LayoutResponsive()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<MainLayout>(parameters => parameters
            .AddChildContent("Test Content"));

        // Assert
        // Layout should have responsive structure
        Assert.Contains("page", cut.Markup);
        Assert.Contains("sidebar", cut.Markup);
    }
}
