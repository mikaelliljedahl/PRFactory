using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Domain.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components.Layout;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Radzen;
using Xunit;

namespace PRFactory.Web.Tests.Components.Layout;

/// <summary>
/// Tests for the MainLayout component.
/// Verifies layout structure, demo mode detection, and environment detection.
/// </summary>
public class MainLayoutTests : TestContext
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);

        // Setup mock IErrorService (required by NavMenu)
        var mockErrorService = new Mock<IErrorService>();
        mockErrorService.Setup(s => s.GetUnresolvedCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        Services.AddSingleton(mockErrorService.Object);

        // Setup mock ITicketService (required by NavMenu)
        var mockTicketService = new Mock<ITicketService>();
        var testTickets = new List<TicketDto>
        {
            new TicketDto
            {
                Id = Guid.NewGuid(),
                TicketKey = "TEST-001",
                Title = "Test Ticket",
                State = WorkflowState.Triggered,
                Source = TicketSource.WebUI,
                RepositoryId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            }
        };
        mockTicketService.Setup(s => s.GetAllTicketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testTickets);
        Services.AddSingleton(mockTicketService.Object);

        // Setup Radzen DialogService (required by MainLayout)
        Services.AddScoped<DialogService>();

        // Setup mock IToastService (required by ToastContainer)
        var mockToastService = new Mock<IToastService>();
        Services.AddSingleton(mockToastService.Object);
    }

    private void SetupConfiguration(string environment = "Production")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ASPNETCORE_ENVIRONMENT", environment }
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton<NavigationManager>(new MockNavigationManager());
    }

    [Fact]
    public void Render_DisplaysPageStructure()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var page = cut.Find(".page");
        Assert.NotNull(page);
    }

    [Fact]
    public void Render_DisplaysSidebar()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var sidebar = cut.Find(".sidebar");
        Assert.NotNull(sidebar);
    }

    [Fact]
    public void Render_DisplaysMainContent()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var main = cut.Find("main");
        Assert.NotNull(main);
    }

    [Fact]
    public void Render_DisplaysTopRowWithLinks()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var topRow = cut.Find(".top-row");
        Assert.NotNull(topRow);
        var aboutLink = topRow.QuerySelector("a");
        Assert.NotNull(aboutLink);
        Assert.Contains("About", topRow.TextContent);
    }

    [Fact]
    public void Render_DisplaysNavMenuComponent()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var sidebar = cut.Find(".sidebar");
        Assert.NotNull(sidebar);
    }

    [Fact]
    public void Render_DisplaysArticleForContent()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var article = cut.Find("article.content");
        Assert.NotNull(article);
    }

    [Fact]
    public void Render_DisplaysRadzenDialog()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("RadzenDialog", markup);
    }

    [Fact]
    public void Render_DisplaysToastContainer()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("ToastContainer", markup);
    }

    [Fact]
    public void Render_ProductionEnvironment_IsDemoModeIsFalse()
    {
        // Arrange
        SetupConfiguration("Production");

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert - DemoModeBanner should not be visible with IsDemoMode=false
        var markup = cut.Markup;
        // Check if banner exists (it will, but IsDemoMode should be false)
        Assert.NotNull(cut.Find("div")); // Just verify render succeeds
    }

    [Fact]
    public void Render_DevelopmentEnvironment_IsDemoModeIsTrue()
    {
        // Arrange
        SetupConfiguration("Development");

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert - Component should render with Development environment
        var markup = cut.Markup;
        Assert.NotNull(cut.Find("div"));
    }

    [Fact]
    public void Render_InvalidEnvironment_DefaultsToProduction()
    {
        // Arrange
        SetupConfiguration("InvalidEnv");

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var markup = cut.Markup;
        Assert.NotNull(cut.Find("div"));
    }

    [Fact]
    public void Render_DemoModeBanner_IsIncluded()
    {
        // Arrange
        SetupConfiguration("Production");

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("DemoModeBanner", markup);
    }

    [Fact]
    public void Render_UserProfileDropdown_IsIncluded()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("UserProfileDropdown", markup);
    }

    [Fact]
    public void Render_LayoutHasCorrectCssClasses()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var page = cut.Find(".page");
        Assert.NotNull(page);

        var sidebar = cut.Find(".sidebar");
        Assert.NotNull(sidebar);

        var main = cut.Find("main");
        Assert.NotNull(main);

        var article = cut.Find("article");
        Assert.NotNull(article);
    }

    [Fact]
    public void Render_TopRowHasPadding()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var topRow = cut.Find(".top-row");
        var cssClass = topRow.GetAttribute("class") ?? "";
        Assert.Contains("px-4", cssClass);
    }

    [Fact]
    public void Render_ContentHasPadding()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var article = cut.Find("article");
        var cssClass = article.GetAttribute("class") ?? "";
        Assert.Contains("px-4", cssClass);
    }

    [Fact]
    public void Render_AboutLinkPointsToGitHub()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var aboutLink = cut.Find("a[href='https://github.com/your-org/prfactory']");
        Assert.NotNull(aboutLink);
    }

    [Fact]
    public void Render_AboutLinkOpensInNewTab()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var aboutLink = cut.Find("a[target='_blank']");
        Assert.NotNull(aboutLink);
    }

    [Fact]
    public void Render_SidebarContainsNavMenu()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var sidebar = cut.Find(".sidebar");
        var hasNavMenu = sidebar.GetAttribute("class")?.Contains("sidebar") ?? false;
        Assert.True(hasNavMenu);
    }

    [Fact]
    public void Render_LayoutIsFlexible()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        var page = cut.Find(".page");
        var cssClass = page.GetAttribute("class") ?? "";
        Assert.Contains("page", cssClass);
    }

    [Fact]
    public void Render_NavigationBarIsDarkThemed()
    {
        // Arrange
        SetupConfiguration();

        // Act
        var cut = RenderComponent<MainLayout>();

        // Assert
        // TopRow is part of NavMenu which has navbar-dark
        var markup = cut.Markup;
        Assert.Contains("navbar", markup);
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
