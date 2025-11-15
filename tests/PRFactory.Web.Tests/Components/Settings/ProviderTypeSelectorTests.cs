using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the ProviderTypeSelector component.
/// Verifies provider type selection, card rendering, and selection callbacks.
/// </summary>
public class ProviderTypeSelectorTests : TestContext
{
    public ProviderTypeSelectorTests()
    {
        // Setup JSInterop for Radzen components
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);
    }

    [Fact]
    public void Render_DisplaysAllProviderCards()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var cards = cut.FindAll(".provider-card");
        Assert.NotEmpty(cards);
        Assert.True(cards.Count >= 6); // At least 6 provider types
    }

    [Fact]
    public void Render_DisplaysAnthropicNativeCard()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Anthropic Native", markup);
        Assert.Contains("Official Anthropic API with OAuth", markup);
        Assert.Contains("OAuth", markup);
    }

    [Fact]
    public void Render_DisplaysZAiCard()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Z.ai Unified API", markup);
        Assert.Contains("Access 100+ models through one API", markup);
        Assert.Contains("API Key", markup);
    }

    [Fact]
    public void Render_DisplaysMinimaxM2Card()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Minimax M2", markup);
        Assert.Contains("Chinese LLM provider", markup);
    }

    [Fact]
    public void Render_DisplaysOpenRouterCard()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("OpenRouter", markup);
        Assert.Contains("Route to 100+ models", markup);
    }

    [Fact]
    public void Render_DisplaysTogetherAICard()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Together AI", markup);
        Assert.Contains("Fast open-source models", markup);
    }

    [Fact]
    public void Render_DisplaysCustomProviderCard()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Custom Provider", markup);
        Assert.Contains("Self-hosted or custom API", markup);
    }

    [Fact]
    public void Render_DisplaysProviderIcons()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var icons = cut.FindAll("i.bi");
        Assert.NotEmpty(icons);
        Assert.True(icons.Count >= 6);
    }

    [Fact]
    public void Render_DisplaysOAuthBadge()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var badges = cut.FindAll(".badge");
        Assert.NotEmpty(badges);
        Assert.True(badges.Any(b => b.TextContent.Contains("OAuth")));
    }

    [Fact]
    public void Render_DisplaysApiKeyBadges()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var badges = cut.FindAll(".badge");
        Assert.NotEmpty(badges);
        Assert.True(badges.Any(b => b.TextContent.Contains("API Key")));
    }

    [Fact]
    public void Click_AnthropicNativeCard_InvokesCallback()
    {
        // Arrange
        string? selectedType = null;

        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add<EventCallback<string>>(p => p.OnProviderTypeSelected, EventCallback.Factory.Create<string>(this, async (type) =>
            {
                selectedType = type;
            })));

        // Act
        var anthropicCard = cut.FindAll(".provider-card").First(c => c.TextContent.Contains("Anthropic Native"));
        anthropicCard.Click();

        // Assert
        Assert.Equal("AnthropicNative", selectedType);
    }

    [Fact]
    public void Click_ZAiCard_InvokesCallback()
    {
        // Arrange
        string? selectedType = null;

        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add<EventCallback<string>>(p => p.OnProviderTypeSelected, EventCallback.Factory.Create<string>(this, async (type) =>
            {
                selectedType = type;
            })));

        // Act
        var zaiCard = cut.FindAll(".provider-card").First(c => c.TextContent.Contains("Z.ai Unified API"));
        zaiCard.Click();

        // Assert
        Assert.Equal("ZAi", selectedType);
    }

    [Fact]
    public void Click_MinimaxM2Card_InvokesCallback()
    {
        // Arrange
        string? selectedType = null;

        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add<EventCallback<string>>(p => p.OnProviderTypeSelected, EventCallback.Factory.Create<string>(this, async (type) =>
            {
                selectedType = type;
            })));

        // Act
        var minimaxCard = cut.FindAll(".provider-card").First(c => c.TextContent.Contains("Minimax M2"));
        minimaxCard.Click();

        // Assert
        Assert.Equal("MinimaxM2", selectedType);
    }

    [Fact]
    public void Click_OpenRouterCard_InvokesCallback()
    {
        // Arrange
        string? selectedType = null;

        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add<EventCallback<string>>(p => p.OnProviderTypeSelected, EventCallback.Factory.Create<string>(this, async (type) =>
            {
                selectedType = type;
            })));

        // Act
        var openrouterCard = cut.FindAll(".provider-card").First(c => c.TextContent.Contains("OpenRouter"));
        openrouterCard.Click();

        // Assert
        Assert.Equal("OpenRouter", selectedType);
    }

    [Fact]
    public void Click_TogetherAICard_InvokesCallback()
    {
        // Arrange
        string? selectedType = null;

        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add<EventCallback<string>>(p => p.OnProviderTypeSelected, EventCallback.Factory.Create<string>(this, async (type) =>
            {
                selectedType = type;
            })));

        // Act
        var togetherCard = cut.FindAll(".provider-card").First(c => c.TextContent.Contains("Together AI"));
        togetherCard.Click();

        // Assert
        Assert.Equal("TogetherAI", selectedType);
    }

    [Fact]
    public void Click_CustomProviderCard_InvokesCallback()
    {
        // Arrange
        string? selectedType = null;

        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add<EventCallback<string>>(p => p.OnProviderTypeSelected, EventCallback.Factory.Create<string>(this, async (type) =>
            {
                selectedType = type;
            })));

        // Act
        var customCard = cut.FindAll(".provider-card").First(c => c.TextContent.Contains("Custom Provider"));
        customCard.Click();

        // Assert
        Assert.Equal("Custom", selectedType);
    }

    [Fact]
    public void Render_DisplaysCancelButton()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var cancelButton = cut.Find("button");
        Assert.NotNull(cancelButton);
        Assert.Contains("Cancel", cancelButton.TextContent);
    }

    [Fact]
    public void Click_CancelButton_InvokesCallback()
    {
        // Arrange
        var cancelCallbackInvoked = false;

        var cut = RenderComponent<ProviderTypeSelector>(parameters => parameters
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, async () =>
            {
                cancelCallbackInvoked = true;
            })));

        // Act
        var cancelButton = cut.Find("button");
        cancelButton.Click();

        // Assert
        Assert.True(cancelCallbackInvoked);
    }

    [Fact]
    public void Render_DisplaysGridLayout()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var grid = cut.Find(".row.g-3");
        Assert.NotNull(grid);

        var cols = cut.FindAll(".col-md-4");
        Assert.NotEmpty(cols);
    }

    [Fact]
    public void Render_DisplaysProviderNames()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var names = cut.FindAll(".provider-name");
        Assert.NotEmpty(names);
        Assert.True(names.Count >= 6);
    }

    [Fact]
    public void Render_DisplaysProviderDescriptions()
    {
        // Act
        var cut = RenderComponent<ProviderTypeSelector>();

        // Assert
        var descriptions = cut.FindAll(".provider-description");
        Assert.NotEmpty(descriptions);
        Assert.True(descriptions.Count >= 6);
    }
}
