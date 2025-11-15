using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PRFactory.Core.Application.DTOs;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the OAuthProviderForm component.
/// Verifies OAuth configuration form rendering, validation, and submission callbacks.
/// </summary>
public class OAuthProviderFormTests : TestContext
{
    public OAuthProviderFormTests()
    {
        // Setup JSInterop for Radzen components
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Radzen.preventArrows", _ => true);
        JSInterop.SetupVoid("Radzen.closeDropdown", _ => true);
        JSInterop.SetupVoid("Radzen.openDropdown", _ => true);
    }

    private CreateOAuthProviderDto CreateTestModel(string name = "Test OAuth Provider", string defaultModel = "claude-sonnet-4-5-20250929")
    {
        return new CreateOAuthProviderDto
        {
            Name = name,
            DefaultModel = defaultModel
        };
    }

    [Fact]
    public void Render_DisplaysProviderNameField()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var providerNameLabel = cut.Find("label");
        Assert.NotNull(providerNameLabel);
        Assert.Contains("Provider Name", providerNameLabel.TextContent);

        var input = cut.Find("input[type='text']");
        Assert.NotNull(input);
    }

    [Fact]
    public void Render_DisplaysDefaultModelDropdown()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var dropdownLabel = cut.FindAll("label").First(l => l.TextContent.Contains("Default Model"));
        Assert.NotNull(dropdownLabel);

        var select = cut.Find("select.form-select");
        Assert.NotNull(select);
    }

    [Fact]
    public void Render_DisplaysOAuthFlowInformation()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var infoAlert = cut.Find(".alert.alert-info");
        Assert.NotNull(infoAlert);
        Assert.Contains("OAuth Authentication Required", infoAlert.TextContent);
    }

    [Fact]
    public void Render_DisplaysOAuthSetupInstructions()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var card = cut.Find(".card");
        Assert.NotNull(card);
        Assert.Contains("OAuth Setup Instructions", card.TextContent);
        Assert.Contains("Anthropic Console", card.TextContent);
        Assert.Contains("redirect URI", card.TextContent);
    }

    [Fact]
    public void Render_DisplaysOAuthFlowPlaceholderWarning()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var warningAlert = cut.Find(".alert.alert-warning");
        Assert.NotNull(warningAlert);
        Assert.Contains("OAuth Flow Placeholder", warningAlert.TextContent);
    }

    [Fact]
    public void Render_DisplaysSubmitButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var submitButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create Provider"));
        Assert.NotNull(submitButton);
        Assert.True(submitButton.HasAttribute("type") && submitButton.GetAttribute("type") == "submit");
    }

    [Fact]
    public void Render_DisplaysBackButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var backButton = cut.FindAll("button").First(b => b.TextContent.Contains("Back"));
        Assert.NotNull(backButton);
    }

    [Fact]
    public void Render_DisplaysCancelButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var cancelButton = cut.FindAll("button").Last(b => b.TextContent.Contains("Cancel"));
        Assert.NotNull(cancelButton);
    }

    [Fact]
    public void Render_WithIsSaving_DisablesSubmitButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsSaving, true));

        // Assert
        var submitButton = cut.FindAll("button").First(b => b.TextContent.Contains("Creating"));
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WithIsSaving_ShowsSpinner()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsSaving, true));

        // Assert
        var spinner = cut.Find(".spinner-border");
        Assert.NotNull(spinner);
        Assert.Contains("Creating", cut.Markup);
    }

    [Fact]
    public void Click_BackButton_InvokesOnBackCallback()
    {
        // Arrange
        var model = CreateTestModel();
        var backCallbackInvoked = false;

        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnBack, EventCallback.Factory.Create(this, async () =>
            {
                backCallbackInvoked = true;
            })));

        // Act
        var backButton = cut.FindAll("button").First(b => b.TextContent.Contains("Back"));
        backButton.Click();

        // Assert
        Assert.True(backCallbackInvoked);
    }

    [Fact]
    public void Click_CancelButton_InvokesOnCancelCallback()
    {
        // Arrange
        var model = CreateTestModel();
        var cancelCallbackInvoked = false;

        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, async () =>
            {
                cancelCallbackInvoked = true;
            })));

        // Act
        var cancelButton = cut.FindAll("button").Last(b => b.TextContent.Contains("Cancel"));
        cancelButton.Click();

        // Assert
        Assert.True(cancelCallbackInvoked);
    }

    [Fact]
    public void Render_PrefillsModelFields()
    {
        // Arrange
        var model = CreateTestModel("Production OAuth", "claude-opus-4-5-20250929");

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var input = cut.Find("input[type='text']");
        Assert.NotNull(input);
        var value = input.GetAttribute("value");
        Assert.Equal("Production OAuth", value);
    }

    [Fact]
    public void Render_DisplaysAllAvailableModels()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var select = cut.Find("select.form-select");
        var markup = select.OuterHtml;
        Assert.Contains("claude-sonnet-4-5-20250929", markup);
        Assert.Contains("claude-opus-4-5-20250929", markup);
        Assert.Contains("claude-3-5-sonnet-20241022", markup);
    }

    [Fact]
    public void Render_ProviderNamePlaceholder()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var input = cut.Find("input[type='text']");
        Assert.True(input.HasAttribute("placeholder"));
        Assert.Contains("Production Anthropic Claude", input.GetAttribute("placeholder"));
    }

    [Fact]
    public void Render_DisplaysHelpText()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<OAuthProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var helpTexts = cut.FindAll("small");
        Assert.NotEmpty(helpTexts);
        Assert.True(helpTexts.Any(h => h.TextContent.Contains("friendly name")));
    }
}
