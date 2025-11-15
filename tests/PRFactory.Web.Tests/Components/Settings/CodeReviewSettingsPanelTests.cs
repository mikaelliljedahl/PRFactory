using Bunit;
using Xunit;
using PRFactory.Web.Components.Settings;
using PRFactory.Core.Application.DTOs;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the CodeReviewSettingsPanel component.
/// Verifies settings rendering, toggle functionality, and conditional display.
/// </summary>
public class CodeReviewSettingsPanelTests : TestContext
{
    private TenantConfigurationDto CreateTestConfiguration(bool enableAutoCodeReview = false)
    {
        return new TenantConfigurationDto
        {
            EnableAutoCodeReview = enableAutoCodeReview,
            MaxCodeReviewIterations = 3,
            AutoApproveIfNoIssues = false,
            CodeReviewLlmProviderId = enableAutoCodeReview ? Guid.NewGuid() : null,
            AnalysisLlmProviderId = null,
            PlanningLlmProviderId = null,
            ImplementationLlmProviderId = null
        };
    }

    [Fact]
    public void Render_DisplaysEnableAutoCodeReviewCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var checkbox = cut.Find("input[id='enableCodeReview']");
        Assert.NotNull(checkbox);
        Assert.Equal("checkbox", checkbox.GetAttribute("type"));
    }

    [Fact]
    public void Render_DisplaysEnableAutoCodeReviewLabel()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Enable Auto Code Review"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_WhenAutoCodeReviewDisabled_HidesAdditionalSettings()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: false);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var maxIterationsLabel = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Max Code Review Iterations"));
        Assert.Null(maxIterationsLabel);

        var infoAlert = cut.Find(".alert-info");
        Assert.NotNull(infoAlert);
        Assert.Contains("Enable auto code review to configure additional settings", infoAlert.TextContent);
    }

    [Fact]
    public void Render_WhenAutoCodeReviewEnabled_DisplaysMaxIterationsField()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Max Code Review Iterations"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_WhenAutoCodeReviewEnabled_DisplaysAutoApproveCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var checkbox = cut.Find("input[id='autoApprove']");
        Assert.NotNull(checkbox);
    }

    [Fact]
    public void Render_WhenAutoCodeReviewEnabled_DisplaysWarningIfNoCRProvider()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            TenantId = Guid.NewGuid(),
            EnableAutoCodeReview = true,
            MaxCodeReviewIterations = 3,
            AutoApproveIfNoIssues = false,
            CodeReviewLlmProviderId = null, // No provider assigned
            AnalysisLlmProviderId = null,
            PlanningLlmProviderId = null,
            ImplementationLlmProviderId = null
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var warningAlert = cut.Find(".alert-warning");
        Assert.NotNull(warningAlert);
        Assert.Contains("No Code Review LLM provider assigned", warningAlert.TextContent);
        Assert.Contains("LLM Providers", warningAlert.TextContent);
    }

    [Fact]
    public void Render_WhenAutoCodeReviewEnabledWithProvider_HidesWarning()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var warningAlert = cut.FindAll(".alert-warning").FirstOrDefault();
        Assert.Null(warningAlert);
    }

    [Fact]
    public void Render_DisplaysRequireHumanApprovalCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var checkbox = cut.Find("input[id='requireHumanApproval']");
        Assert.NotNull(checkbox);
        Assert.True(checkbox.HasAttribute("disabled"));
        Assert.True(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void Render_WhenCanEditFalse_DisablesEnableCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        var checkbox = cut.Find("input[id='enableCodeReview']");
        Assert.True(checkbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenCanEditTrue_EnablesEnableCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var checkbox = cut.Find("input[id='enableCodeReview']");
        Assert.False(checkbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_MaxIterationsField_HasCorrectMinMaxValues()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var maxIterationsInput = cut.Find("input[id='maxIterations']");
        Assert.NotNull(maxIterationsInput);
        Assert.Equal("1", maxIterationsInput.GetAttribute("min"));
        Assert.Equal("10", maxIterationsInput.GetAttribute("max"));
    }

    [Fact]
    public void Render_MaxIterationsField_DisplaysHelpText()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var helpText = cut.FindAll(".form-text.text-muted").FirstOrDefault(h => h.TextContent.Contains("Maximum number of review/fix iterations"));
        Assert.NotNull(helpText);
    }

    [Fact]
    public void Render_AutoApproveCheckbox_DisplaysHelpText()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Auto-Approve If No Issues", markup);
        Assert.Contains("Automatically approve PR if no issues found", markup);
    }

    [Fact]
    public void Render_BothColumns_DisplaysProperlyStructured()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var columns = cut.FindAll(".col-md-6");
        Assert.Equal(2, columns.Count);
    }

    [Fact]
    public void Render_WhenCanEditFalse_DisablesMaxIterationsInput()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        var input = cut.Find("input[id='maxIterations']");
        Assert.True(input.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_WhenCanEditFalse_DisablesAutoApproveCheckbox()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        var checkbox = cut.Find("input[id='autoApprove']");
        Assert.True(checkbox.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_DisplaysTipAlert()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("ContextualHelp", markup); // Component is used
    }

    [Fact]
    public void Toggle_AutoCodeReviewCheckbox_UpdatesConfiguration()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: false);

        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        var checkbox = cut.Find("input[id='enableCodeReview']");

        // Act
        checkbox.Change(true);

        // Assert
        Assert.True(config.EnableAutoCodeReview);
    }

    [Fact]
    public void Toggle_AutoApproveCheckbox_UpdatesConfiguration()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        var checkbox = cut.Find("input[id='autoApprove']");

        // Act
        checkbox.Change(true);

        // Assert
        Assert.True(config.AutoApproveIfNoIssues);
    }

    [Fact]
    public void Render_MaxIterationsField_IsNumberType()
    {
        // Arrange
        var config = CreateTestConfiguration(enableAutoCodeReview: true);

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        var input = cut.Find("input[id='maxIterations']");
        Assert.Equal("number", input.GetAttribute("type"));
    }
}
