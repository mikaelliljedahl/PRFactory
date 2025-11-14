using Bunit;
using PRFactory.Core.Application.DTOs;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Tests.Web.Components.Settings;

public class WorkflowSettingsPanelTests : TestContext
{
    [Fact]
    public void WorkflowSettingsPanel_DisplaysAutoImplementationCheckbox()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            AutoImplementAfterPlanApproval = true
        };

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Auto-Implementation After Plan Approval", cut.Markup);
    }

    [Fact]
    public void WorkflowSettingsPanel_DisplaysMaxRetries()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            MaxRetries = 5
        };

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Max Retries for Failed Operations", cut.Markup);
    }

    [Fact]
    public void WorkflowSettingsPanel_DisplaysApiTimeout()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            ApiTimeoutSeconds = 300
        };

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("API Timeout (seconds)", cut.Markup);
    }

    [Fact]
    public void WorkflowSettingsPanel_DisplaysVerboseLoggingCheckbox()
    {
        // Arrange
        var config = new TenantConfigurationDto
        {
            EnableVerboseLogging = true
        };

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, true));

        // Assert
        Assert.Contains("Enable Verbose Logging", cut.Markup);
    }

    [Fact]
    public void WorkflowSettingsPanel_DisablesInputs_WhenCannotEdit()
    {
        // Arrange
        var config = new TenantConfigurationDto();

        // Act
        var cut = RenderComponent<WorkflowSettingsPanel>(parameters => parameters
            .Add(p => p.Configuration, config)
            .Add(p => p.CanEdit, false));

        // Assert
        Assert.Contains("disabled", cut.Markup);
    }
}
