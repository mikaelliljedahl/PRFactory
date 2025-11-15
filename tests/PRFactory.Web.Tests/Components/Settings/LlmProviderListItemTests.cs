using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using PRFactory.Web.Components.Settings;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the LlmProviderListItem component.
/// Verifies provider information rendering, status badges, and action callbacks.
/// </summary>
public class LlmProviderListItemTests : TestContext
{
    private TenantLlmProviderDto CreateTestProvider(
        string name = "OpenAI Production",
        string providerType = "OpenAI",
        string defaultModel = "gpt-4o",
        bool isActive = true,
        bool isDefault = true,
        bool usesOAuth = false)
    {
        return new TenantLlmProviderDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = name,
            ProviderType = providerType,
            DefaultModel = defaultModel,
            IsActive = isActive,
            IsDefault = isDefault,
            UsesOAuth = usesOAuth,
            ApiBaseUrl = "https://api.openai.com/v1"
        };
    }

    [Fact]
    public void Render_DisplaysProviderName()
    {
        // Arrange
        var provider = CreateTestProvider(name: "My Provider");

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var nameCell = cut.FindAll("td")[0];
        Assert.Contains("My Provider", nameCell.TextContent);
    }

    [Fact]
    public void Render_WithDefaultProvider_DisplaysDefaultBadge()
    {
        // Arrange
        var provider = CreateTestProvider(isDefault: true);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var defaultBadge = cut.Find(".badge.bg-warning");
        Assert.NotNull(defaultBadge);
        Assert.Contains("Default", defaultBadge.TextContent);
        Assert.Contains("⭐", defaultBadge.TextContent);
    }

    [Fact]
    public void Render_WithNonDefaultProvider_HidesDefaultBadge()
    {
        // Arrange
        var provider = CreateTestProvider(isDefault: false);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var defaultBadges = cut.FindAll(".badge.bg-warning");
        Assert.Empty(defaultBadges);
    }

    [Fact]
    public void Render_DisplaysProviderTypeBadge()
    {
        // Arrange
        var provider = CreateTestProvider(providerType: "Anthropic");

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var typeBadge = cut.Find(".badge.bg-info");
        Assert.NotNull(typeBadge);
        Assert.Contains("Anthropic", typeBadge.TextContent);
    }

    [Fact]
    public void Render_WithApiKeyAuth_DisplaysApiKeyBadge()
    {
        // Arrange
        var provider = CreateTestProvider(usesOAuth: false);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var apiBadge = cut.Find(".badge.bg-secondary");
        Assert.NotNull(apiBadge);
        Assert.Contains("API Key", apiBadge.TextContent);
        Assert.Contains("bi-key-fill", cut.Markup);
    }

    [Fact]
    public void Render_WithOAuthAuth_DisplaysOAuthBadge()
    {
        // Arrange
        var provider = CreateTestProvider(usesOAuth: true);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var oauthBadge = cut.FindAll(".badge").FirstOrDefault(b => b.TextContent.Contains("OAuth"));
        Assert.NotNull(oauthBadge);
        Assert.Contains("OAuth", oauthBadge.TextContent);
        Assert.Contains("bg-primary", oauthBadge.ClassName);
    }

    [Fact]
    public void Render_DisplaysDefaultModel()
    {
        // Arrange
        var provider = CreateTestProvider(defaultModel: "gpt-4-turbo");

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var codeElement = cut.Find("code");
        Assert.NotNull(codeElement);
        Assert.Contains("gpt-4-turbo", codeElement.TextContent);
    }

    [Fact]
    public void Render_WithDefaultProvider_DisplaysDefaultStar()
    {
        // Arrange
        var provider = CreateTestProvider(isDefault: true);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("bi-star-fill text-warning", markup);
    }

    [Fact]
    public void Render_WithNonDefaultProvider_HidesDefaultStar()
    {
        // Arrange
        var provider = CreateTestProvider(isDefault: false);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("bi-star-fill text-warning", markup);
    }

    [Fact]
    public void Render_WithActiveProvider_DisplaysActiveBadge()
    {
        // Arrange
        var provider = CreateTestProvider(isActive: true);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var activeBadge = cut.FindAll(".badge").FirstOrDefault(b => b.TextContent.Contains("Active") && b.ClassName.Contains("bg-success"));
        Assert.NotNull(activeBadge);
    }

    [Fact]
    public void Render_WithInactiveProvider_DisplaysInactiveBadge()
    {
        // Arrange
        var provider = CreateTestProvider(isActive: false);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var inactiveBadge = cut.FindAll(".badge").FirstOrDefault(b => b.TextContent.Contains("Inactive") && b.ClassName.Contains("bg-secondary"));
        Assert.NotNull(inactiveBadge);
    }

    [Fact]
    public void Render_DisplaysViewDetailsButton()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var viewButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "View Details");
        Assert.NotNull(viewButton);
        Assert.Contains("bi-eye", viewButton.ClassName);
    }

    [Fact]
    public void Render_WhenCanEditTrue_DisplaysEditButton()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true));

        // Assert
        var editButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Edit");
        Assert.NotNull(editButton);
        Assert.Contains("bi-pencil", editButton.ClassName);
    }

    [Fact]
    public void Render_WhenCanEditFalse_HidesEditButton()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, false));

        // Assert
        var editButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Edit");
        Assert.Null(editButton);
    }

    [Fact]
    public void Render_WithNonDefaultProvider_DisplaysSetDefaultButton()
    {
        // Arrange
        var provider = CreateTestProvider(isDefault: false);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true));

        // Assert
        var setDefaultButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Set as Default");
        Assert.NotNull(setDefaultButton);
        Assert.Contains("bi-star", setDefaultButton.ClassName);
    }

    [Fact]
    public void Render_WithDefaultProvider_HidesSetDefaultButton()
    {
        // Arrange
        var provider = CreateTestProvider(isDefault: true);

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true));

        // Assert
        var setDefaultButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Set as Default");
        Assert.Null(setDefaultButton);
    }

    [Fact]
    public void Render_WhenCanEditTrue_DisplaysTestConnectionButton()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true));

        // Assert
        var testButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Test Connection");
        Assert.NotNull(testButton);
        Assert.Contains("bi-plug", testButton.ClassName);
    }

    [Fact]
    public void Render_WhenCanEditTrue_DisplaysDeleteButton()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true));

        // Assert
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Deactivate");
        Assert.NotNull(deleteButton);
        Assert.Contains("bi-trash", deleteButton.ClassName);
    }

    [Fact]
    public void Render_WhenCanEditFalse_HidesActionButtons()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, false));

        // Assert
        var buttons = cut.FindAll("button");
        // Should only have view button
        Assert.Single(buttons);
        Assert.Equal("View Details", buttons[0].GetAttribute("title"));
    }

    [Fact]
    public async Task OnViewDetails_InvokesCallback()
    {
        // Arrange
        var provider = CreateTestProvider();
        var callbackInvoked = false;

        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create(this, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            })));

        var viewButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "View Details");

        // Act
        viewButton!.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task OnEdit_InvokesCallback()
    {
        // Arrange
        var provider = CreateTestProvider();
        var callbackInvoked = false;

        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnEdit, EventCallback.Factory.Create(this, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            })));

        var editButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Edit");

        // Act
        editButton!.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task OnSetDefault_InvokesCallback()
    {
        // Arrange
        var provider = CreateTestProvider(isDefault: false);
        var callbackInvoked = false;

        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnSetDefault, EventCallback.Factory.Create(this, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            })));

        var setDefaultButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Set as Default");

        // Act
        setDefaultButton!.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task OnTestConnection_InvokesCallback()
    {
        // Arrange
        var provider = CreateTestProvider();
        var callbackInvoked = false;

        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnTestConnection, EventCallback.Factory.Create(this, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            })));

        var testButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Test Connection");

        // Act
        testButton!.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task OnDelete_InvokesCallback()
    {
        // Arrange
        var provider = CreateTestProvider();
        var callbackInvoked = false;

        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider)
            .Add(p => p.CanEdit, true)
            .Add(p => p.OnDelete, EventCallback.Factory.Create(this, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            })));

        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Deactivate");

        // Act
        deleteButton!.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Render_IsTableRow()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        var cut = RenderComponent<LlmProviderListItem>(parameters => parameters
            .Add(p => p.Provider, provider));

        // Assert
        var markup = cut.Markup;
        Assert.StartsWith("<tr", markup);
        Assert.EndsWith("</tr>", markup);
    }
}
