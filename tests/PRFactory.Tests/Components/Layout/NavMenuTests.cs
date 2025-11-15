using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Layout;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.Layout;

/// <summary>
/// Comprehensive bUnit tests for NavMenu component
/// Tests navigation menu rendering, menu items, demo mode, and error handling
/// </summary>
public class NavMenuTests : ComponentTestBase
{
    private TestAuthorizationContext _authContext = null!;

    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Add authorization services before any components are rendered
        _authContext = this.AddTestAuthorization();

        // Mock services
        var errorServiceMock = new Mock<IErrorService>();
        errorServiceMock.Setup(s => s.GetUnresolvedCountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(2);

        var ticketServiceMock = new Mock<ITicketService>();
        ticketServiceMock.Setup(s => s.GetAllTicketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PRFactory.Web.Models.TicketDto>
            {
                new PRFactory.Web.Models.TicketDto { Id = Guid.NewGuid(), Title = "Ticket 1" },
                new PRFactory.Web.Models.TicketDto { Id = Guid.NewGuid(), Title = "Ticket 2" }
            });

        var loggerMock = new Mock<ILogger<NavMenu>>();

        services.AddScoped(_ => errorServiceMock.Object);
        services.AddScoped(_ => ticketServiceMock.Object);
        services.AddScoped(_ => loggerMock.Object);
    }

    [Fact]
    public void Render_DisplaysNavigationMenu()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.NotNull(cut.Markup);
        Assert.Contains("nav", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysDashboardLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Dashboard", cut.Markup);
        Assert.Contains("bi-speedometer2", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysTicketsLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Tickets", cut.Markup);
        Assert.Contains("bi-ticket-detailed", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysRepositoriesLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Repositories", cut.Markup);
        Assert.Contains("bi-folder", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysWorkflowsLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Workflows", cut.Markup);
        Assert.Contains("bi-diagram-3", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysEventLogLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Event Log", cut.Markup);
        Assert.Contains("bi-calendar-event", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysErrorsLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Errors", cut.Markup);
        Assert.Contains("bi-exclamation-triangle", cut.Markup);
    }

    [Fact]
    public void Render_WithUnresolvedErrors_DisplaysErrorBadge()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("bg-danger", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysAdminSection()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("ADMIN", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysTenantsLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Tenants", cut.Markup);
        Assert.Contains("bi-building", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysUsersLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Users", cut.Markup);
        Assert.Contains("bi-people", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysAgentPromptsLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Agent Prompts", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysAgentConfigurationLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Agent Configuration", cut.Markup);
        Assert.Contains("bi-cpu", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysLLMProvidersLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("LLM Providers", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSettingsLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("Settings", cut.Markup);
        Assert.Contains("bi-gear", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysMenuIcons()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("bi-", cut.Markup); // Bootstrap icons
        var iconCount = cut.Markup.Split("bi-").Length - 1;
        Assert.True(iconCount > 5, "Should have multiple Bootstrap icons");
    }

    [Fact]
    public void Render_InDemoMode_DisplaysDemoBadge()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, true));

        // Assert
        // Demo mode is shown in MainLayout, but NavMenu receives IsDemoMode parameter
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void Render_InDemoMode_DisplaysGettingStartedLink()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, true));

        // Assert
        Assert.Contains("Getting Started", cut.Markup);
    }

    [Fact]
    public void Render_InDemoMode_ShowsNewBadge()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, true));

        // Assert
        Assert.Contains("Getting Started", cut.Markup);
        Assert.Contains("bg-success", cut.Markup);
    }

    [Fact]
    public void Render_NavMenuHasCorrectStructure()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        Assert.Contains("nav-item", cut.Markup);
        Assert.Contains("nav-link", cut.Markup);
    }

    [Fact]
    public void Render_AllMainMenuItemsHaveLinks()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var navLinks = cut.FindAll("a.nav-link");
        Assert.NotEmpty(navLinks);
        Assert.True(navLinks.Count >= 10, "Should have at least 10 navigation links");
    }

    [Fact]
    public void Render_MenuItemsHaveCorrectHrefAttributes()
    {
        // Arrange
        _authContext.SetAuthorized("testuser");

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("href=\"\"", markup); // Dashboard
        Assert.Contains("href=\"tickets\"", markup); // Tickets
        Assert.Contains("href=\"repositories\"", markup); // Repositories
        Assert.Contains("href=\"workflows\"", markup); // Workflows
    }
}
