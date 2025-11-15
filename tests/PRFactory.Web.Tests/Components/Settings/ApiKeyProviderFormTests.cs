using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using PRFactory.Web.Components.Settings;
using PRFactory.Core.Application.DTOs;
using PRFactory.Core.Application.Services;
using PRFactory.Web.Services;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the ApiKeyProviderForm component.
/// Verifies form rendering, validation, test connection, and submission callbacks.
/// </summary>
public class ApiKeyProviderFormTests : TestContext
{
    private readonly Mock<ITenantLlmProviderService> _mockProviderService;
    private readonly Mock<IToastService> _mockToastService;

    public ApiKeyProviderFormTests()
    {
        _mockProviderService = new Mock<ITenantLlmProviderService>();
        _mockToastService = new Mock<IToastService>();

        Services.AddSingleton(_mockProviderService.Object);
        Services.AddSingleton(_mockToastService.Object);
    }

    private CreateApiKeyProviderDto CreateTestModel(string providerType = "OpenAI", string name = "Test Provider")
    {
        return new CreateApiKeyProviderDto
        {
            Name = name,
            ProviderType = providerType,
            ApiKey = "sk-test-key-12345",
            ApiBaseUrl = "https://api.openai.com/v1",
            DefaultModel = "gpt-4o",
            TimeoutMs = 300000,
            DisableNonEssentialTraffic = false,
            ModelOverrides = new Dictionary<string, string> { { "gpt-4", "gpt-4-turbo" } }
        };
    }

    [Fact]
    public void Render_DisplaysFormFields()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var formFields = cut.FindAll("input, textarea, select");
        Assert.True(formFields.Count > 0);

        var providerNameInput = cut.Find("input[placeholder='Production Z.ai']");
        Assert.NotNull(providerNameInput);
    }

    [Fact]
    public void Render_DisplaysProviderNameField()
    {
        // Arrange
        var model = CreateTestModel(name: "My Provider");

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Provider Name"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_DisplaysApiKeyField()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("API Key"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_DisplaysDefaultModelField()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Default Model"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_WithCustomProvider_DisplaysApiBaseUrlInput()
    {
        // Arrange
        var model = CreateTestModel(providerType: "Custom");

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var baseUrlLabel = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("API Base URL"));
        Assert.NotNull(baseUrlLabel);

        var input = cut.Find("input[placeholder='https://api.example.com/v1']");
        Assert.NotNull(input);
        Assert.False(input.HasAttribute("readonly"));
    }

    [Fact]
    public void Render_WithNonCustomProvider_DisplaysReadOnlyApiBaseUrl()
    {
        // Arrange
        var model = CreateTestModel(providerType: "OpenAI");

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var readOnlyInput = cut.FindAll("input").FirstOrDefault(i => i.HasAttribute("readonly"));
        Assert.NotNull(readOnlyInput);
    }

    [Fact]
    public void Render_DisplaysTimeoutField()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Timeout"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_DisplaysDisableNonEssentialTrafficCheckbox()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var checkbox = cut.Find("input[id='disableNonEssentialTraffic']");
        Assert.NotNull(checkbox);
    }

    [Fact]
    public void Render_DisplaysTestConnectionButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var testButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Test Connection"));
        Assert.NotNull(testButton);
    }

    [Fact]
    public void Render_InCreateMode_DisplaysCreateButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false));

        // Assert
        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Create Provider"));
        Assert.NotNull(submitButton);
    }

    [Fact]
    public void Render_InEditMode_DisplaysUpdateButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Update Provider"));
        Assert.NotNull(submitButton);
    }

    [Fact]
    public void Render_DisplaysCancelButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        Assert.NotNull(cancelButton);
    }

    [Fact]
    public void Render_InCreateMode_DisplaysBackButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false));

        // Assert
        var backButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Back"));
        Assert.NotNull(backButton);
    }

    [Fact]
    public void Render_InEditMode_HidesBackButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        var backButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Back"));
        Assert.Null(backButton);
    }

    [Fact]
    public void Render_WithIsSaving_DisablesSubmitButton()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsSaving, true));

        // Assert
        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Create Provider"));
        Assert.NotNull(submitButton);
        Assert.True(submitButton!.HasAttribute("disabled"));
    }

    [Fact]
    public async Task TestConnectionButton_WhenFormInvalid_ShowsErrorToast()
    {
        // Arrange
        var model = new CreateApiKeyProviderDto
        {
            Name = string.Empty, // Invalid - empty name
            ProviderType = "OpenAI",
            ApiKey = string.Empty, // Invalid - empty key
            ApiBaseUrl = "https://api.openai.com/v1",
            DefaultModel = string.Empty, // Invalid - empty model
            TimeoutMs = 300000,
            DisableNonEssentialTraffic = false
        };

        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        var testButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Test Connection"));

        // Act
        testButton!.Click();

        await Task.Delay(100);

        // Assert
        _mockToastService.Verify(s => s.ShowError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task TestConnectionButton_ShowsLoadingState()
    {
        // Arrange
        var model = CreateTestModel();

        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        var testButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Test Connection"));

        // Act
        testButton!.Click();

        // Assert - Button should show loading state temporarily
        var markup = cut.Markup;
        Assert.Contains("Testing", markup);
    }

    [Fact]
    public async Task TestConnectionButton_DisplaysErrorResult()
    {
        // Arrange
        var model = CreateTestModel();

        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        var testButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Test Connection"));

        // Act
        testButton!.Click();

        await Task.Delay(200);

        // Assert - Should show test result message
        var alert = cut.FindAll(".alert").FirstOrDefault();
        Assert.NotNull(alert);
        Assert.Contains("requires creating the provider first", alert.TextContent);
    }

    [Fact]
    public async Task OnValidSubmit_InvokesCallback()
    {
        // Arrange
        var model = CreateTestModel();
        var submitCallbackInvoked = false;

        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, EventCallback.Factory.Create(this, async () =>
            {
                submitCallbackInvoked = true;
            })));

        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Create Provider"));

        // Act
        submitButton!.Click();

        // Assert
        Assert.True(submitCallbackInvoked);
    }

    [Fact]
    public async Task OnCancel_InvokesCallback()
    {
        // Arrange
        var model = CreateTestModel();
        var cancelCallbackInvoked = false;

        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, async () =>
            {
                cancelCallbackInvoked = true;
            })));

        var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));

        // Act
        cancelButton!.Click();

        // Assert
        Assert.True(cancelCallbackInvoked);
    }

    [Fact]
    public async Task OnBack_InvokesCallback()
    {
        // Arrange
        var model = CreateTestModel();
        var backCallbackInvoked = false;

        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false)
            .Add(p => p.OnBack, EventCallback.Factory.Create(this, async () =>
            {
                backCallbackInvoked = true;
            })));

        var backButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Back"));

        // Act
        backButton!.Click();

        // Assert
        Assert.True(backCallbackInvoked);
    }

    [Fact]
    public void ModelOverridesEditor_IsDisplayed()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var cut = RenderComponent<ApiKeyProviderForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        // ModelOverridesEditor should be rendered (check for its label or textarea)
        var markup = cut.Markup;
        Assert.Contains("Model Overrides", markup);
    }
}
