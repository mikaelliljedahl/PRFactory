using Bunit;
using PRFactory.Web.Components.Settings;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the TenantInfoPanel component.
/// Verifies tenant information display, statistics, and read-only behavior.
/// </summary>
public class TenantInfoPanelTests : TestContext
{
    private TenantDto CreateTestTenant(
        string name = "Test Tenant",
        bool isActive = true,
        int repositoryCount = 5,
        int ticketCount = 10,
        string ticketPlatform = "Jira",
        string ticketPlatformUrl = "https://jira.example.com")
    {
        return new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            RepositoryCount = repositoryCount,
            TicketCount = ticketCount,
            TicketPlatform = ticketPlatform,
            TicketPlatformUrl = ticketPlatformUrl,
            AutoImplementAfterPlanApproval = true,
            MaxRetries = 3,
            ClaudeModel = "claude-sonnet-4-5-20250929",
            MaxTokensPerRequest = 8000,
            EnableCodeReview = true,
            HasTicketPlatformApiToken = true,
            HasClaudeApiKey = true
        };
    }

    [Fact]
    public void Render_DisplaysTenantName()
    {
        // Arrange
        var tenant = CreateTestTenant(name: "Acme Corporation");

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Tenant Name", markup);
        Assert.Contains("Acme Corporation", markup);
    }

    [Fact]
    public void Render_DisplaysTicketPlatform()
    {
        // Arrange
        var tenant = CreateTestTenant(ticketPlatform: "Azure DevOps");

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Ticket Platform", markup);
        Assert.Contains("Azure DevOps", markup);
    }

    [Fact]
    public void Render_DisplaysTicketPlatformUrl()
    {
        // Arrange
        var url = "https://dev.azure.com/myorg";
        var tenant = CreateTestTenant(ticketPlatformUrl: url);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains(url, markup);
    }

    [Fact]
    public void Render_WithNoTicketPlatform_ShowsNotConfigured()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.TicketPlatform = string.Empty;
        tenant.TicketPlatformUrl = string.Empty;

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Not configured", markup);
    }

    [Fact]
    public void Render_DisplaysCreatedDate()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Created", markup);
    }

    [Fact]
    public void Render_DisplaysActiveBadge()
    {
        // Arrange
        var tenant = CreateTestTenant(isActive: true);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var badge = cut.Find(".badge.bg-success");
        Assert.NotNull(badge);
        Assert.Contains("Active", badge.TextContent);
    }

    [Fact]
    public void Render_DisplaysInactiveBadge()
    {
        // Arrange
        var tenant = CreateTestTenant(isActive: false);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var badge = cut.Find(".badge.bg-secondary");
        Assert.NotNull(badge);
        Assert.Contains("Inactive", badge.TextContent);
    }

    [Fact]
    public void Render_DisplaysRepositoryCount()
    {
        // Arrange
        var tenant = CreateTestTenant(repositoryCount: 12);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Repositories", markup);
        Assert.Contains("12", markup);
    }

    [Fact]
    public void Render_DisplaysTicketCount()
    {
        // Arrange
        var tenant = CreateTestTenant(ticketCount: 45);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Tickets", markup);
        Assert.Contains("45", markup);
    }

    [Fact]
    public void Render_WithZeroRepositories_DisplaysZero()
    {
        // Arrange
        var tenant = CreateTestTenant(repositoryCount: 0);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Repositories", markup);
        Assert.Contains("0", markup);
    }

    [Fact]
    public void Render_WithZeroTickets_DisplaysZero()
    {
        // Arrange
        var tenant = CreateTestTenant(ticketCount: 0);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Tickets", markup);
        Assert.Contains("0", markup);
    }

    [Fact]
    public void Render_DisplaysStatusLabel()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Status", markup);
    }

    [Fact]
    public void Render_DisplaysDefinitionList()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var dls = cut.FindAll("dl");
        Assert.NotEmpty(dls);

        var dts = cut.FindAll("dt");
        Assert.NotEmpty(dts);

        var dds = cut.FindAll("dd");
        Assert.NotEmpty(dds);
    }

    [Fact]
    public void Render_DisplaysInfoAlert()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var infoAlert = cut.Find(".alert.alert-info");
        Assert.NotNull(infoAlert);
        Assert.Contains("Tenant information is managed by your identity provider", infoAlert.TextContent);
    }

    [Fact]
    public void Render_DisplaysAlertIcon()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var icon = cut.Find("i.bi-info-circle");
        Assert.NotNull(icon);
    }

    [Fact]
    public void Render_DisplaysTwoColumns()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var cols = cut.FindAll(".col-md-6");
        Assert.Equal(2, cols.Count);
    }

    [Fact]
    public void Render_WithDifferentTenant_DisplaysCorrectInfo()
    {
        // Arrange
        var tenant1 = CreateTestTenant(name: "Tenant One", repositoryCount: 5);

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant1));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Tenant One", markup);
        Assert.Contains("5", markup);
    }

    [Fact]
    public void Render_WithMultipleTicketPlatformUrls_DisplaysUrl()
    {
        // Arrange
        var tenant = CreateTestTenant(
            ticketPlatform: "GitHub",
            ticketPlatformUrl: "https://github.com/myorg");

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("GitHub", markup);
        Assert.Contains("https://github.com/myorg", markup);
    }

    [Fact]
    public void Render_DisplaysMutedTextForUrl()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var mutedText = cut.Find(".text-muted");
        Assert.NotNull(mutedText);
        Assert.Contains(tenant.TicketPlatformUrl, mutedText.TextContent);
    }

    [Fact]
    public void Render_WithoutTicketPlatformUrl_DoesNotShowUrl()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.TicketPlatformUrl = string.Empty;

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var mutedTexts = cut.FindAll(".text-muted");
        Assert.DoesNotContain(mutedTexts, m => m.TextContent.Contains("https"));
    }
}
