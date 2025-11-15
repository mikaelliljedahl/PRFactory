using Bunit;
using PRFactory.Web.Components.Settings;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

public class TenantInfoPanelTests : TestContext
{
    [Fact]
    public void TenantInfoPanel_DisplaysTenantName()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Acme Corporation",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Acme Corporation", cut.Markup);
    }

    [Fact]
    public void TenantInfoPanel_DisplaysTicketPlatform()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            TicketPlatform = "Jira",
            TicketPlatformUrl = "https://acme.atlassian.net",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Jira", cut.Markup);
        Assert.Contains("https://acme.atlassian.net", cut.Markup);
    }

    [Fact]
    public void TenantInfoPanel_DisplaysActiveStatus()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public void TenantInfoPanel_DisplaysInactiveStatus()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Inactive", cut.Markup);
    }

    [Fact]
    public void TenantInfoPanel_DisplaysRepositoryCount()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RepositoryCount = 5
        };

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("5", cut.Markup);
    }

    [Fact]
    public void TenantInfoPanel_DisplaysTicketCount()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TicketCount = 12
        };

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("12", cut.Markup);
    }

    [Fact]
    public void TenantInfoPanel_ShowsNotConfigured_WhenNoTicketPlatform()
    {
        // Arrange
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TicketPlatform = string.Empty
        };

        // Act
        var cut = RenderComponent<TenantInfoPanel>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Not configured", cut.Markup);
    }
}
