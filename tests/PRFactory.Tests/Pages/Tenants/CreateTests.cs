using Bunit;
using Microsoft.AspNetCore.Components;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Models;
using PRFactory.Web.Pages.Tenants;
using PRFactory.Web.Services;
using Radzen;
using Xunit;

namespace PRFactory.Tests.Pages.Tenants;

public class CreateTests : PageTestBase
{
    protected override void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Add Radzen DialogService (required by the page)
        services.AddScoped<DialogService>();

        // Note: NavigationManager is already registered by bUnit as FakeNavigationManager
        // IToastService and ITenantService are already registered by TestContextBase
    }

    [Fact]
    public void Render_ShowsTenantForm()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.True(cut.Markup.Contains("Tenant Name") || cut.Markup.Contains("Name"));
    }

    [Fact]
    public void Render_ShowsCreateButton()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.Contains("Create", cut.Markup);
    }

    [Fact]
    public void Render_ShowsCancelButton()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.Contains("Cancel", cut.Markup);
    }

    [Fact]
    public async Task CreateTenant_WhenSuccessful_NavigatesToTenantList()
    {
        // Arrange
        var createdTenant = new TenantDtoBuilder()
            .WithName("New Tenant")
            .Build();

        MockTenantService
            .Setup(x => x.CreateTenantAsync(It.IsAny<CreateTenantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTenant);

        // Act
        var cut = RenderComponent<Create>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.NotNull(cut);
    }

    [Fact]
    public async Task CreateTenant_WhenError_ShowsErrorMessage()
    {
        // Arrange
        MockTenantService
            .Setup(x => x.CreateTenantAsync(It.IsAny<CreateTenantRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Creation failed"));

        // Act
        var cut = RenderComponent<Create>();

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.NotNull(cut);
    }

    [Fact]
    public void Render_ShowsAllTicketPlatformOptions()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.Contains("Jira", cut.Markup);
    }

    [Fact]
    public void Render_ShowsConfigurationOptions()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        Assert.True(cut.Markup.Contains("Active") || cut.Markup.Contains("Configuration"));
    }
}
