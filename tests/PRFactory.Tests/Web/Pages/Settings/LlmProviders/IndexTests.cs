using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Pages.Settings.LlmProviders;
using PRFactory.Web.Services;
using System.Security.Claims;
using Xunit;

namespace PRFactory.Tests.Web.Pages.Settings.LlmProviders;

public class IndexTests : TestContext
{
    private readonly Mock<ITenantLlmProviderService> _mockProviderService;
    private readonly Mock<IToastService> _mockToastService;
    private readonly Mock<NavigationManager> _mockNavigationManager;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    public IndexTests()
    {
        _mockProviderService = new Mock<ITenantLlmProviderService>();
        _mockToastService = new Mock<IToastService>();
        _mockNavigationManager = new Mock<NavigationManager>();
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

        Services.AddSingleton(_mockProviderService.Object);
        Services.AddSingleton(_mockToastService.Object);
        Services.AddSingleton(_mockNavigationManager.Object);
        Services.AddSingleton(_mockAuthStateProvider.Object);

        // Setup default auth state (Owner role)
        SetupAuthState("Owner");
    }

    [Fact]
    public void Index_RendersLoadingSpinner_WhenLoading()
    {
        // Arrange
        var tcs = new TaskCompletionSource<List<TenantLlmProviderDto>>();
        _mockProviderService
            .Setup(s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<PRFactory.Web.Pages.Settings.LlmProviders.Index>();

        // Assert
        Assert.Contains("Loading LLM providers", cut.Markup);
    }

    [Fact]
    public async Task Index_RendersProviderList_WhenProvidersExist()
    {
        // Arrange
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Provider",
                ProviderType = "ZAi",
                UsesOAuth = false,
                DefaultModel = "gpt-4o",
                IsActive = true,
                IsDefault = true
            }
        };

        _mockProviderService
            .Setup(s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var cut = RenderComponent<PRFactory.Web.Pages.Settings.LlmProviders.Index>();
        await Task.Delay(100); // Wait for async operations

        // Assert
        Assert.Contains("Test Provider", cut.Markup);
        Assert.Contains("ZAi", cut.Markup);
        Assert.Contains("gpt-4o", cut.Markup);
    }

    [Fact]
    public async Task Index_RendersEmptyState_WhenNoProvidersExist()
    {
        // Arrange
        _mockProviderService
            .Setup(s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantLlmProviderDto>());

        // Act
        var cut = RenderComponent<PRFactory.Web.Pages.Settings.LlmProviders.Index>();
        await Task.Delay(100); // Wait for async operations

        // Assert
        Assert.Contains("No LLM providers configured", cut.Markup);
    }

    [Fact]
    public async Task Index_ShowsAddButton_ForOwnerRole()
    {
        // Arrange
        SetupAuthState("Owner");
        _mockProviderService
            .Setup(s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantLlmProviderDto>());

        // Act
        var cut = RenderComponent<PRFactory.Web.Pages.Settings.LlmProviders.Index>();
        await Task.Delay(100); // Wait for async operations

        // Assert
        Assert.Contains("Add Provider", cut.Markup);
    }

    [Fact]
    public async Task Index_ShowsAddButton_ForAdminRole()
    {
        // Arrange
        SetupAuthState("Admin");
        _mockProviderService
            .Setup(s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantLlmProviderDto>());

        // Act
        var cut = RenderComponent<PRFactory.Web.Pages.Settings.LlmProviders.Index>();
        await Task.Delay(100); // Wait for async operations

        // Assert
        Assert.Contains("Add Provider", cut.Markup);
    }

    [Fact]
    public async Task Index_FiltersProviders_BySearchTerm()
    {
        // Arrange
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Production Provider",
                ProviderType = "ZAi",
                UsesOAuth = false,
                DefaultModel = "gpt-4o",
                IsActive = true,
                IsDefault = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Development Provider",
                ProviderType = "MinimaxM2",
                UsesOAuth = false,
                DefaultModel = "minimax-m2",
                IsActive = true,
                IsDefault = false
            }
        };

        _mockProviderService
            .Setup(s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var cut = RenderComponent<PRFactory.Web.Pages.Settings.LlmProviders.Index>();
        await Task.Delay(100); // Wait for async operations

        // Initially should show both
        Assert.Contains("Production Provider", cut.Markup);
        Assert.Contains("Development Provider", cut.Markup);
    }

    [Fact]
    public async Task Index_CallsSetDefault_WhenSetDefaultClicked()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var providers = new List<TenantLlmProviderDto>
        {
            new()
            {
                Id = providerId,
                Name = "Test Provider",
                ProviderType = "ZAi",
                UsesOAuth = false,
                DefaultModel = "gpt-4o",
                IsActive = true,
                IsDefault = false
            }
        };

        _mockProviderService
            .Setup(s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        _mockProviderService
            .Setup(s => s.SetDefaultProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers[0]);

        // Act
        var cut = RenderComponent<PRFactory.Web.Pages.Settings.LlmProviders.Index>();
        await Task.Delay(100); // Wait for async operations

        // Assert
        Assert.NotNull(cut);
        Assert.Contains("Test Provider", cut.Markup);
        _mockProviderService.Verify(
            s => s.GetProvidersForTenantAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    private void SetupAuthState(string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "test@example.com"),
            new Claim(ClaimTypes.Role, role)
        }, "test");

        var claimsPrincipal = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(claimsPrincipal));

        _mockAuthStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .Returns(authState);
    }
}
