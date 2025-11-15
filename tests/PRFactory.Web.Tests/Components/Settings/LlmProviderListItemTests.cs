using Bunit;
using PRFactory.Core.Application.DTOs;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

public class LlmProviderListItemTests : TestContext
{
    [Fact]
    public void LlmProviderListItem_RendersProviderName()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Provider",
            ProviderType = "ZAi",
            UsesOAuth = false,
            DefaultModel = "gpt-4o",
            IsActive = true,
            IsDefault = false
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        Assert.Contains("Test Provider", cut.Markup);
    }

    [Fact]
    public void LlmProviderListItem_ShowsDefaultBadge_WhenIsDefault()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Default Provider",
            ProviderType = "ZAi",
            UsesOAuth = false,
            DefaultModel = "gpt-4o",
            IsActive = true,
            IsDefault = true
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        Assert.Contains("Default", cut.Markup);
    }

    [Fact]
    public void LlmProviderListItem_ShowsOAuthBadge_WhenUsesOAuth()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Anthropic Provider",
            ProviderType = "AnthropicNative",
            UsesOAuth = true,
            DefaultModel = "claude-sonnet-4-5",
            IsActive = true,
            IsDefault = false
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        Assert.Contains("OAuth", cut.Markup);
    }

    [Fact]
    public void LlmProviderListItem_ShowsApiKeyBadge_WhenNotUsesOAuth()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Z.ai Provider",
            ProviderType = "ZAi",
            UsesOAuth = false,
            DefaultModel = "gpt-4o",
            IsActive = true,
            IsDefault = false
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        Assert.Contains("API Key", cut.Markup);
    }

    [Fact]
    public void LlmProviderListItem_ShowsActiveBadge_WhenActive()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Active Provider",
            ProviderType = "ZAi",
            UsesOAuth = false,
            DefaultModel = "gpt-4o",
            IsActive = true,
            IsDefault = false
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public void LlmProviderListItem_ShowsInactiveBadge_WhenNotActive()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Provider",
            ProviderType = "ZAi",
            UsesOAuth = false,
            DefaultModel = "gpt-4o",
            IsActive = false,
            IsDefault = false
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        Assert.Contains("Inactive", cut.Markup);
    }

    [Fact]
    public void LlmProviderListItem_ShowsActionButtons_WhenCanEdit()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Provider",
            ProviderType = "ZAi",
            UsesOAuth = false,
            DefaultModel = "gpt-4o",
            IsActive = true,
            IsDefault = false
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count >= 4, $"Expected at least 4 action buttons, found {buttons.Count}");
    }

    [Fact]
    public void LlmProviderListItem_HidesSetDefaultButton_WhenIsDefault()
    {
        // Arrange
        var provider = new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            Name = "Default Provider",
            ProviderType = "ZAi",
            UsesOAuth = false,
            DefaultModel = "gpt-4o",
            IsActive = true,
            IsDefault = true
        };

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true));

        // Assert - Should not have "Set as Default" button since it's already default
        var setDefaultButtons = cut.FindAll("button[title='Set as Default']");
        Assert.Empty(setDefaultButtons);
    }
}
