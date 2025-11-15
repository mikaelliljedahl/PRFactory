using Bunit;
using PRFactory.Core.Application.DTOs;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Web.Tests.Components.Settings;

public class CodeReviewSettingsPanelTests : TestContext
{
    [Fact]
    public void CodeReviewSettingsPanel_DisplaysEnableAutoCodeReviewCheckbox()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableAutoCodeReview = true
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Enable Auto Code Review", cut.Markup);
    }

    [Fact]
    public void CodeReviewSettingsPanel_ShowsMaxIterations_WhenCodeReviewEnabled()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableAutoCodeReview = true,
            MaxCodeReviewIterations = 3
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Max Code Review Iterations", cut.Markup);
    }

    [Fact]
    public void CodeReviewSettingsPanel_HidesMaxIterations_WhenCodeReviewDisabled()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableAutoCodeReview = false
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.DoesNotContain("Max Code Review Iterations", cut.Markup);
        Assert.Contains("Enable auto code review to configure additional settings", cut.Markup);
    }

    [Fact]
    public void CodeReviewSettingsPanel_DisplaysAutoApproveCheckbox_WhenCodeReviewEnabled()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableAutoCodeReview = true,
            AutoApproveIfNoIssues = false
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Auto-Approve If No Issues", cut.Markup);
    }

    [Fact]
    public void CodeReviewSettingsPanel_ShowsWarning_WhenNoProviderAssigned()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableAutoCodeReview = true,
            CodeReviewLlmProviderId = null
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("No Code Review LLM provider assigned", cut.Markup);
    }

    [Fact]
    public void CodeReviewSettingsPanel_HidesWarning_WhenProviderAssigned()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableAutoCodeReview = true,
            CodeReviewLlmProviderId = Guid.NewGuid()
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.DoesNotContain("No Code Review LLM provider assigned", cut.Markup);
    }

    [Fact]
    public void CodeReviewSettingsPanel_DisablesInputs_WhenCannotEdit()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableAutoCodeReview = true
        };

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains("disabled", cut.Markup);
    }

    [Fact]
    public void CodeReviewSettingsPanel_ShowsHumanApprovalCheckbox_AsReadOnly()
    {
        // Arrange
        var config = new TenantConfigurationDto();

        // Act
        var cut = RenderComponent<CodeReviewSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Require Human Approval After Review", cut.Markup);
        Assert.Contains("always enabled", cut.Markup);
    }
}
