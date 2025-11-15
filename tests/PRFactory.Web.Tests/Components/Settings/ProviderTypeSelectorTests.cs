using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

public class ProviderTypeSelectorTests : TestContext
{
    [Fact]
    public void ProviderTypeSelector_RendersAllProviderTypes()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        Assert.Contains("Anthropic Native", cut.Markup);
        Assert.Contains("Z.ai Unified API", cut.Markup);
        Assert.Contains("Minimax M2", cut.Markup);
        Assert.Contains("OpenRouter", cut.Markup);
        Assert.Contains("Together AI", cut.Markup);
        Assert.Contains("Custom Provider", cut.Markup);
    }

    [Fact]
    public void ProviderTypeSelector_ShowsOAuthBadge_ForAnthropicNative()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        Assert.Contains("OAuth", cut.Markup);
    }

    [Fact]
    public void ProviderTypeSelector_ShowsApiKeyBadge_ForOtherProviders()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        // Count API Key badges (should be 5: Z.ai, Minimax, OpenRouter, Together, Custom)
        var apiKeyCount = System.Text.RegularExpressions.Regex.Matches(cut.Markup, "API Key").Count;
        Assert.True(apiKeyCount >= 5, $"Expected at least 5 'API Key' badges, found {apiKeyCount}");
    }

    [Fact]
    public void ProviderTypeSelector_RendersCancelButton()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        Assert.Contains("Cancel", cut.Markup);
    }

    [Fact]
    public void ProviderTypeSelector_InvokesCallback_WhenProviderTypeSelected()
    {
        // Arrange
        var selectedType = string.Empty;
        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add(p => p.OnProviderTypeSelected, EventCallback.Factory.Create<string>(
                this,
                (type) => selectedType = type)));

        // Act
        var anthropicCard = cut.FindAll(".provider-card").FirstOrDefault();
        Assert.NotNull(anthropicCard);
        anthropicCard.Click();

        // Assert
        Assert.Equal("AnthropicNative", selectedType);
    }
}
