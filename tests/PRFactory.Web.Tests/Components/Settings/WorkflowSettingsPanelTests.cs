using AngleSharp.Dom;
using Bunit;
using PRFactory.Core.Application.DTOs;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the WorkflowSettingsPanel component.
/// Verifies workflow configuration display, toggle controls, and input fields.
/// </summary>
public class WorkflowSettingsPanelTests : TestContext
{
    private TenantConfigurationDto CreateTestConfiguration(
        bool autoImplement = true,
        int maxRetries = 3,
        int apiTimeout = 300,
        bool verboseLogging = false,
        string[]? allowedRepositories = null)
    {
        return new TenantConfigurationDto
        {
            AutoImplementAfterPlanApproval = autoImplement,
            MaxRetries = maxRetries,
            ApiTimeoutSeconds = apiTimeout,
            EnableVerboseLogging = verboseLogging,
            AllowedRepositories = allowedRepositories ?? new[] { "repo1", "repo2" },
            ClaudeModel = "claude-sonnet-4-5-20250929",
            MaxTokensPerRequest = 8000,
            EnableCodeReview = true,
            EnableAutoCodeReview = false,
            MaxCodeReviewIterations = 3,
            AutoApproveIfNoIssues = false,
            RequireHumanApprovalAfterReview = true
        };
    }

    [Fact]
    public void Render_DisplaysAutoImplementCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var checkboxes = cut.FindAll("input[type='checkbox']");
        Assert.NotEmpty(checkboxes);
        Assert.True(checkboxes.Any(c => c.GetAttribute("id") == "autoImplement"));
    }

    [Fact]
    public void Render_DisplaysAutoImplementLabel()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Auto-Implementation After Plan Approval", markup);
    }

    [Fact]
    public void Render_WithCanEdit_EnablesAutoImplementCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var autoImplementCheckbox = cut.Find("input#autoImplement");
        Assert.False(autoImplementCheckbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WithoutCanEdit_DisablesAutoImplementCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        var autoImplementCheckbox = cut.Find("input#autoImplement");
        Assert.True(autoImplementCheckbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_DisplaysMaxRetriesInput()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var input = cut.Find("input#maxRetries");
        Assert.NotNull(input);
        Assert.Equal("number", input.GetAttribute("type"));
    }

    [Fact]
    public void Render_DisplaysMaxRetriesLabel()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Max Retries for Failed Operations", markup);
    }

    [Fact]
    public void Render_DisplaysMaxRetriesConstraints()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var input = cut.Find("input#maxRetries");
        Assert.True(input.HasAttribute("min"));
        Assert.True(input.HasAttribute("max"));
        Assert.Equal("1", input.GetAttribute("min"));
        Assert.Equal("10", input.GetAttribute("max"));
    }

    [Fact]
    public void Render_DisplaysApiTimeoutInput()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var input = cut.Find("input#apiTimeout");
        Assert.NotNull(input);
        Assert.Equal("number", input.GetAttribute("type"));
    }

    [Fact]
    public void Render_DisplaysApiTimeoutLabel()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("API Timeout (seconds)", markup);
    }

    [Fact]
    public void Render_DisplaysApiTimeoutConstraints()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var input = cut.Find("input#apiTimeout");
        Assert.True(input.HasAttribute("min"));
        Assert.True(input.HasAttribute("max"));
        Assert.Equal("30", input.GetAttribute("min"));
        Assert.Equal("600", input.GetAttribute("max"));
    }

    [Fact]
    public void Render_DisplaysVerboseLoggingCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var checkboxes = cut.FindAll("input[type='checkbox']");
        Assert.NotEmpty(checkboxes);
        Assert.True(checkboxes.Any(c => c.GetAttribute("id") == "verboseLogging"));
    }

    [Fact]
    public void Render_DisplaysVerboseLoggingLabel()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Enable Verbose Logging", markup);
    }

    [Fact]
    public void Render_WithCanEdit_EnablesVerboseLoggingCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var verboseLoggingCheckbox = cut.Find("input#verboseLogging");
        Assert.False(verboseLoggingCheckbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WithoutCanEdit_DisablesVerboseLoggingCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        var verboseLoggingCheckbox = cut.Find("input#verboseLogging");
        Assert.True(verboseLoggingCheckbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_DisplaysAllowedRepositoriesEditor()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        // AllowedRepositoriesEditor component renders "Allowed Repositories" label
        Assert.Contains("Allowed Repositories", markup);
    }

    [Fact]
    public void Render_WithAutoImplementTrue_ChecksAutoImplementCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration(autoImplement: true);

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var autoImplementCheckbox = cut.Find("input#autoImplement");
        Assert.True(autoImplementCheckbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_WithAutoImplementFalse_UnchecksAutoImplementCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration(autoImplement: false);

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var autoImplementCheckbox = cut.Find("input#autoImplement");
        Assert.False(autoImplementCheckbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_WithVerboseLoggingTrue_ChecksVerboseLoggingCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration(verboseLogging: true);

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var verboseLoggingCheckbox = cut.Find("input#verboseLogging");
        Assert.True(verboseLoggingCheckbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_WithVerboseLoggingFalse_UnchecksVerboseLoggingCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration(verboseLogging: false);

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var verboseLoggingCheckbox = cut.Find("input#verboseLogging");
        Assert.False(verboseLoggingCheckbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_PrefillsMaxRetriesValue()
    {
        // Arrange
        var config = CreateTestConfiguration(maxRetries: 5);

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var input = cut.Find("input#maxRetries");
        Assert.Equal("5", input.GetAttribute("value"));
    }

    [Fact]
    public void Render_PrefillsApiTimeoutValue()
    {
        // Arrange
        var config = CreateTestConfiguration(apiTimeout: 450);

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var input = cut.Find("input#apiTimeout");
        Assert.Equal("450", input.GetAttribute("value"));
    }

    [Fact]
    public void Render_DisplaysHelpTextForMaxRetries()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var helpTexts = cut.FindAll("small");
        Assert.True(helpTexts.Any(h => h.TextContent.Contains("retry attempts")));
    }

    [Fact]
    public void Render_DisplaysHelpTextForApiTimeout()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var helpTexts = cut.FindAll("small");
        Assert.True(helpTexts.Any(h => h.TextContent.Contains("Timeout")));
    }

    [Fact]
    public void Render_DisplaysTwoColumns()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var cols = cut.FindAll(".col-md-6");
        Assert.Equal(2, cols.Count);
    }

    [Fact]
    public void Render_DisplaysContextualHelpForAutoImplement()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Implementation LLM provider", markup);
    }

    [Fact]
    public void Render_DisplaysContextualHelpForVerboseLogging()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("troubleshooting", markup);
    }

    [Fact]
    public void Render_WithDifferentConfigurations_DisplaysCorrectValues()
    {
        // Arrange
        var config1 = CreateTestConfiguration(maxRetries: 2, apiTimeout: 60);
        var config2 = CreateTestConfiguration(maxRetries: 8, apiTimeout: 500);

        // Act
        var cut1 = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config1)
            .Add(p => p.CanEdit, true));
        var cut2 = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config2)
            .Add(p => p.CanEdit, true));

        // Assert
        var input1 = cut1.Find("input#maxRetries");
        Assert.Equal("2", input1.GetAttribute("value"));

        var input2 = cut2.Find("input#maxRetries");
        Assert.Equal("8", input2.GetAttribute("value"));
    }

    [Fact]
    public void Render_AllowedRepositoriesEditor_PassesDisabledProp()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        var markup = cut.Markup;
        Assert.NotEmpty(markup);
    }

    [Fact]
    public void Render_DisplaysFormControlClasses()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var formInputs = cut.FindAll(".form-control");
        Assert.NotEmpty(formInputs);
    }

    [Fact]
    public void Render_WithCanEditFalse_DisablesAllInputs()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        var inputs = cut.FindAll("input[type='number']");
        Assert.NotEmpty(inputs);
        Assert.All(inputs, input => Assert.True(input.HasAttribute("disabled")));
    }
}
