using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.DTOs;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components.Layout;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Web.Tests.Components.Layout;

/// <summary>
/// Tests for the NavMenu component.
/// Verifies navigation rendering, error count display, ticket count display, and menu toggle functionality.
/// </summary>
public class NavMenuTests : TestContext
{
    public NavMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);

        // Setup mock NavigationManager
        Services.AddSingleton<NavigationManager>(new MockNavigationManager());
    }

    private void SetupDefaultServices(
        int unresolvedErrorCount = 0,
        int ticketCount = 5)
    {
        // Setup mock IErrorService
        var mockErrorService = new Mock<IErrorService>();
        mockErrorService
            .Setup(s => s.GetUnresolvedCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(unresolvedErrorCount);

        Services.AddSingleton(mockErrorService.Object);

        // Setup mock ITicketService
        var mockTicketService = new Mock<ITicketService>();
        var tickets = CreateTestTickets(ticketCount);
        mockTicketService
            .Setup(s => s.GetAllTicketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickets);

        Services.AddSingleton(mockTicketService.Object);

        // Setup logger
        Services.AddLogging(builder => builder.AddDebug());
    }

    private List<TicketDto> CreateTestTickets(int count)
    {
        var tickets = new List<TicketDto>();
        var tenantId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();

        for (int i = 1; i <= count; i++)
        {
            tickets.Add(new TicketDto
            {
                Id = Guid.NewGuid(),
                TicketKey = $"TICKET-{i:000}",
                Title = $"Test Ticket {i}",
                State = WorkflowState.Triggered,
                Source = TicketSource.WebUI,
                RepositoryId = repositoryId,
                CreatedAt = DateTime.UtcNow
            });
        }
        return tickets;
    }

    [Fact]
    public void Render_DisplaysNavbar()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var navbar = cut.Find(".navbar");
        Assert.NotNull(navbar);
    }

    [Fact]
    public void Render_DisplaysPRFactoryBrand()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var brand = cut.Find(".navbar-brand");
        Assert.NotNull(brand);
        Assert.Contains("PRFactory", brand.TextContent);
    }

    [Fact]
    public void Render_DisplaysRobotIcon()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var icon = cut.Find("i.bi-robot");
        Assert.NotNull(icon);
    }

    [Fact]
    public void Render_InDemoMode_DisplaysDemoBadge()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, true));

        // Assert
        var badge = cut.FindAll("span").FirstOrDefault(s =>
            s.GetAttribute("class")?.Contains("badge") == true &&
            s.TextContent.Contains("Demo"));
        Assert.NotNull(badge);
    }

    [Fact]
    public void Render_NotInDemoMode_DoesNotDisplayDemoBadge()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var markup = cut.Markup;
        var demoBadgeCount = markup.Split("Demo").Length - 1;
        Assert.Equal(0, demoBadgeCount);
    }

    [Fact]
    public void Render_DisplaysDashboardLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var dashboardLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Dashboard") && a.GetAttribute("href") == "");
        Assert.NotNull(dashboardLink);
    }

    [Fact]
    public void Render_DisplaysTicketsLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var ticketsLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Tickets"));
        Assert.NotNull(ticketsLink);
        Assert.Equal("tickets", ticketsLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysRepositoriesLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var repositoriesLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Repositories"));
        Assert.NotNull(repositoriesLink);
        Assert.Equal("repositories", repositoriesLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysWorkflowsLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var workflowsLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Workflows"));
        Assert.NotNull(workflowsLink);
        Assert.Equal("workflows", workflowsLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysEventLogLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var eventLogLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Event Log"));
        Assert.NotNull(eventLogLink);
        Assert.Equal("workflows/events", eventLogLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysErrorsLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var errorsLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Errors"));
        Assert.NotNull(errorsLink);
        Assert.Equal("errors", errorsLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_WithUnresolvedErrors_DisplaysErrorBadge()
    {
        // Arrange
        SetupDefaultServices(unresolvedErrorCount: 5);

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var errorBadge = cut.FindAll("span").FirstOrDefault(s =>
            s.GetAttribute("class")?.Contains("badge") == true &&
            s.GetAttribute("class")?.Contains("bg-danger") == true &&
            s.TextContent.Contains("5"));
        Assert.NotNull(errorBadge);
    }

    [Fact]
    public void Render_WithNoUnresolvedErrors_DoesNotDisplayErrorBadge()
    {
        // Arrange
        SetupDefaultServices(unresolvedErrorCount: 0);

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var errorBadge = cut.FindAll("span").FirstOrDefault(s =>
            s.GetAttribute("class")?.Contains("bg-danger") == true);
        Assert.Null(errorBadge);
    }

    [Fact]
    public void Render_DisplaysAdminSection()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("ADMIN", markup);
    }

    [Fact]
    public void Render_DisplaysTenantsLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var tenantsLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Tenants"));
        Assert.NotNull(tenantsLink);
        Assert.Equal("tenants", tenantsLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysUsersLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var usersLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Users"));
        Assert.NotNull(usersLink);
        Assert.Equal("settings/users", usersLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysAgentPromptsLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var agentPromptsLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Agent Prompts"));
        Assert.NotNull(agentPromptsLink);
        Assert.Equal("agent-prompts", agentPromptsLink?.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysSettingsLink()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var settingsLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Settings") &&
            a.GetAttribute("href") == "settings/general");
        Assert.NotNull(settingsLink);
    }

    [Fact]
    public void Render_WithFewTickets_DisplaysGettingStartedLink()
    {
        // Arrange
        SetupDefaultServices(ticketCount: 1);

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var gettingStartedLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Getting Started"));
        Assert.NotNull(gettingStartedLink);
    }

    [Fact]
    public void Render_InDemoMode_DisplaysGettingStartedLink()
    {
        // Arrange
        SetupDefaultServices(ticketCount: 10);

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, true));

        // Assert
        var gettingStartedLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Getting Started"));
        Assert.NotNull(gettingStartedLink);
    }

    [Fact]
    public void Render_WithManyTickets_DoesNotDisplayGettingStartedLink()
    {
        // Arrange
        SetupDefaultServices(ticketCount: 10);

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var gettingStartedLink = cut.FindAll("a").FirstOrDefault(a =>
            a.TextContent.Contains("Getting Started"));
        Assert.Null(gettingStartedLink);
    }

    [Fact]
    public void Render_DisplaysToggleButton()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var toggleButton = cut.Find(".navbar-toggler");
        Assert.NotNull(toggleButton);
    }

    [Fact]
    public async Task Click_ToggleButton_ToggleNavMenu()
    {
        // Arrange
        SetupDefaultServices();

        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        var toggleButton = cut.Find(".navbar-toggler");

        // Act
        await cut.InvokeAsync(() => toggleButton.Click());

        // Assert
        var nav = cut.Find(".nav-scrollable");
        Assert.NotNull(nav);
    }

    [Fact]
    public void Render_NavMenuStartsCollapsed()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var nav = cut.Find(".nav-scrollable");
        var cssClass = nav.GetAttribute("class") ?? "";
        Assert.Contains("collapse", cssClass);
    }

    [Fact]
    public void Render_AllMainLinksHaveIcons()
    {
        // Arrange
        SetupDefaultServices();

        // Act
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.IsDemoMode, false));

        // Assert
        var navItems = cut.FindAll(".nav-item");
        Assert.True(navItems.Count > 0);

        foreach (var item in navItems.Take(5))
        {
            var icon = item.QuerySelector("i.bi");
            Assert.NotNull(icon);
        }
    }
}
