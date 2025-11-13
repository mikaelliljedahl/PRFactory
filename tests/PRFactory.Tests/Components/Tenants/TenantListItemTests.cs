using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Tenants;
using Xunit;

namespace PRFactory.Tests.Components.Tenants;

public class TenantListItemTests : ComponentTestBase
{
    [Fact]
    public void Render_WithActiveTenant_ShowsActiveBadge()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithIsActive(true)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Active", cut.Markup);
        Assert.Contains("badge bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WithInactiveTenant_ShowsInactiveBadge()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithIsActive(false)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Inactive", cut.Markup);
        Assert.Contains("badge bg-secondary", cut.Markup);
    }

    [Fact]
    public void Render_WithTenantName_ShowsName()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithName("ACME Corporation")
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("ACME Corporation", cut.Markup);
    }

    [Fact]
    public void Render_WithTicketPlatformUrl_ShowsUrlAsLink()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithTicketPlatformUrl("https://acme.atlassian.net")
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("https://acme.atlassian.net", cut.Markup);
        Assert.Contains("target=\"_blank\"", cut.Markup);
    }

    [Fact]
    public void Render_WithTicketPlatform_ShowsPlatformBadge()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithTicketPlatform("Jira")
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Jira", cut.Markup);
        Assert.Contains("badge bg-secondary", cut.Markup);
    }

    [Fact]
    public void Render_WithRepositoryCount_ShowsRepositoryCount()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithRepositoryCount(5)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("5 repositories", cut.Markup);
    }

    [Fact]
    public void Render_WithTicketCount_ShowsTicketCount()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithTicketCount(25)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("25 tickets", cut.Markup);
    }

    [Fact]
    public void Render_WithClaudeModel_ShowsClaudeModel()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithClaudeModel("claude-sonnet-4-5-20250929")
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("claude-sonnet-4-5-20250929", cut.Markup);
    }

    [Fact]
    public void Render_WithAutoImplementEnabled_ShowsAutoImplementBadge()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithAutoImplementAfterPlanApproval(true)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Auto-implement", cut.Markup);
        Assert.Contains("badge bg-info", cut.Markup);
    }

    [Fact]
    public void Render_WithCodeReviewEnabled_ShowsCodeReviewBadge()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithCodeReview(true)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Code review", cut.Markup);
        Assert.Contains("badge bg-primary", cut.Markup);
    }

    [Fact]
    public void Render_WithTicketPlatformApiToken_ShowsConfiguredBadge()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithCredentials(hasTicketPlatformToken: true, hasClaudeKey: false)
            .WithTicketPlatform("Jira")
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Jira configured", cut.Markup);
        Assert.Contains("badge bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WithClaudeApiKey_ShowsClaudeConfiguredBadge()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithCredentials(hasTicketPlatformToken: false, hasClaudeKey: true)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Claude configured", cut.Markup);
        Assert.Contains("badge bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WithCreatedAt_ShowsCreatedTimestamp()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var tenant = new TenantDtoBuilder()
            .WithCreatedAt(createdAt)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Created", cut.Markup);
    }

    [Fact]
    public void Render_WithUpdatedAt_ShowsUpdatedTimestamp()
    {
        // Arrange
        var updatedAt = DateTime.UtcNow.AddHours(-5);
        var tenant = new TenantDtoBuilder()
            .WithUpdatedAt(updatedAt)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Updated", cut.Markup);
    }

    [Fact]
    public async Task ViewButton_WhenClicked_InvokesOnViewCallback()
    {
        // Arrange
        var tenant = new TenantDtoBuilder().Build();
        Guid? viewedId = null;

        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant)
            .Add(p => p.OnView, (id) => { viewedId = id; }));

        // Act
        var viewButton = cut.Find("button:contains('View')");
        await viewButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(tenant.Id, viewedId);
    }

    [Fact]
    public async Task EditButton_WhenClicked_InvokesOnEditCallback()
    {
        // Arrange
        var tenant = new TenantDtoBuilder().Build();
        Guid? editedId = null;

        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant)
            .Add(p => p.OnEdit, (id) => { editedId = id; }));

        // Act
        var editButton = cut.Find("button:contains('Edit')");
        await editButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(tenant.Id, editedId);
    }

    [Fact]
    public async Task DeleteButton_WhenClicked_InvokesOnDeleteCallback()
    {
        // Arrange
        var tenant = new TenantDtoBuilder().Build();
        Guid? deletedId = null;

        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant)
            .Add(p => p.OnDelete, (id) => { deletedId = id; }));

        // Act
        var deleteButton = cut.Find("button:contains('Delete')");
        await deleteButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(tenant.Id, deletedId);
    }

    [Fact]
    public async Task ActivateButton_WhenInactiveTenant_InvokesOnActivateCallback()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithIsActive(false)
            .Build();
        Guid? activatedId = null;

        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant)
            .Add(p => p.OnActivate, (id) => { activatedId = id; }));

        // Act
        var activateButton = cut.Find("button:contains('Activate')");
        await activateButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(tenant.Id, activatedId);
    }

    [Fact]
    public async Task DeactivateButton_WhenActiveTenant_InvokesOnDeactivateCallback()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithIsActive(true)
            .Build();
        Guid? deactivatedId = null;

        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant)
            .Add(p => p.OnDeactivate, (id) => { deactivatedId = id; }));

        // Act
        var deactivateButton = cut.Find("button:contains('Deactivate')");
        await deactivateButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Equal(tenant.Id, deactivatedId);
    }

    [Fact]
    public void Render_WithActiveTenant_ShowsDeactivateButton()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithIsActive(true)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Deactivate", cut.Markup);
        Assert.DoesNotContain("Activate", cut.Markup);
    }

    [Fact]
    public void Render_WithInactiveTenant_ShowsActivateButton()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithIsActive(false)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Activate", cut.Markup);
        Assert.DoesNotContain("Deactivate", cut.Markup);
    }

    [Fact]
    public void Render_ShowsAllActionButtons()
    {
        // Arrange
        var tenant = new TenantDtoBuilder()
            .WithIsActive(true)
            .Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("View", cut.Markup);
        Assert.Contains("Edit", cut.Markup);
        Assert.Contains("Deactivate", cut.Markup);
        Assert.Contains("Delete", cut.Markup);
    }

    [Fact]
    public void Render_WithFullyConfiguredTenant_ShowsAllBadges()
    {
        // Arrange
        var tenant = TenantDtoBuilder.FullyConfigured().Build();

        // Act
        var cut = RenderComponent<TenantListItem>(parameters => parameters
            .Add(p => p.Tenant, tenant));

        // Assert
        Assert.Contains("Auto-implement", cut.Markup);
        Assert.Contains("Code review", cut.Markup);
        Assert.Contains("configured", cut.Markup);
    }
}
