using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.Components.Tenants;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Web.Tests.Components.Tenants;

/// <summary>
/// Tests for the TenantListItem component.
/// Verifies rendering of tenant information, status badges, feature flags, and event callbacks.
/// </summary>
public class TenantListItemTests : TestContext
{
    private TenantDto CreateTestTenant(bool isActive = true, bool autoImplement = true, bool codeReview = true)
    {
        return new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net",
            TicketPlatform = "Jira",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            AutoImplementAfterPlanApproval = autoImplement,
            MaxRetries = 3,
            ClaudeModel = "claude-sonnet-4-5",
            MaxTokensPerRequest = 4096,
            EnableCodeReview = codeReview,
            RepositoryCount = 5,
            TicketCount = 12,
            HasTicketPlatformApiToken = true,
            HasClaudeApiKey = true
        };
    }

    [Fact]
    public void Render_WithActiveTenant_DisplaysActiveStatusBadge()
    {
        // Arrange
        var tenant = CreateTestTenant(isActive: true);

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var badge = cut.Find(".badge.bg-success");
        Assert.NotNull(badge);
        Assert.Contains("Active", badge.TextContent);
    }

    [Fact]
    public void Render_WithInactiveTenant_DisplaysInactiveStatusBadge()
    {
        // Arrange
        var tenant = CreateTestTenant(isActive: false);

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var badge = cut.Find(".badge.bg-secondary");
        Assert.NotNull(badge);
        Assert.Contains("Inactive", badge.TextContent);
    }

    [Fact]
    public void Render_DisplaysTenantName()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains(tenant.Name, markup);
    }

    [Fact]
    public void Render_DisplaysTicketPlatformUrl()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var link = cut.Find("a[target='_blank']");
        Assert.NotNull(link);
        Assert.Contains(tenant.TicketPlatformUrl, link.GetAttribute("href"));
    }

    [Fact]
    public void Render_DisplaysRepositoryCount()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("5 repositories", markup);
    }

    [Fact]
    public void Render_DisplaysTicketCount()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("12 tickets", markup);
    }

    [Fact]
    public void Render_WithAutoImplementEnabled_ShowsAutoImplementBadge()
    {
        // Arrange
        var tenant = CreateTestTenant(autoImplement: true);

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Auto-implement", markup);
    }

    [Fact]
    public void Render_WithCodeReviewEnabled_ShowsCodeReviewBadge()
    {
        // Arrange
        var tenant = CreateTestTenant(codeReview: true);

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Code review", markup);
    }

    [Fact]
    public void Click_EditButton_InvokesOnEditCallback()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var editCallbackInvoked = false;
        Guid? editedTenantId = null;

        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant)
            .Add(p => p.OnEdit, EventCallback.Factory.Create<Guid>(this, id =>
            {
                editCallbackInvoked = true;
                editedTenantId = id;
            })));

        // Act
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
        editButton.Click();

        // Assert
        Assert.True(editCallbackInvoked);
        Assert.Equal(tenant.Id, editedTenantId);
    }

    [Fact]
    public void Click_DeleteButton_InvokesOnDeleteCallback()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var deleteCallbackInvoked = false;
        Guid? deletedTenantId = null;

        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant)
            .Add(p => p.OnDelete, EventCallback.Factory.Create<Guid>(this, id =>
            {
                deleteCallbackInvoked = true;
                deletedTenantId = id;
            })));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
        deleteButton.Click();

        // Assert
        Assert.True(deleteCallbackInvoked);
        Assert.Equal(tenant.Id, deletedTenantId);
    }
}
