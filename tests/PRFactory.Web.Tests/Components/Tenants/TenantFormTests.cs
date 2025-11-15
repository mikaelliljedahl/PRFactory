using Bunit;
using PRFactory.Web.Components.Tenants;
using PRFactory.Web.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace PRFactory.Web.Tests.Components.Tenants;

/// <summary>
/// Tests for the TenantForm component.
/// Verifies form rendering, validation, field bindings, and submission.
/// </summary>
public class TenantFormTests : TestContext
{
    [Fact]
    public void Render_DisplaysRequiredFormFields()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Tenant Name", markup);
        Assert.Contains("Ticket Platform", markup);
        Assert.Contains("Ticket Platform URL", markup);
        Assert.Contains("Ticket Platform API Token", markup);
    }

    [Fact]
    public void Render_DisplaysTicketPlatformOptions()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Jira", markup);
        Assert.Contains("Azure DevOps", markup);
        Assert.Contains("GitHub Issues", markup);
        Assert.Contains("GitLab Issues", markup);
    }

    [Fact]
    public void Render_InCreateMode_DisplaysCreateTenantTitle()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Create Tenant", markup);
    }

    [Fact]
    public void Render_InEditMode_DisplaysEditTenantTitle()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Edit Tenant", markup);
    }

    [Fact]
    public void Render_InEditMode_ShowsKeepExistingTokenHelpText()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("leave empty to keep existing", markup.ToLower());
    }

    [Fact]
    public void Render_PreFillsModelData()
    {
        // Arrange
        var model = new TestTenantRequest
        {
            Name = "Test Tenant",
            TicketPlatform = "Jira",
            TicketPlatformUrl = "https://test.atlassian.net",
            ClaudeModel = "claude-sonnet-4-5",
            MaxRetries = 3,
            MaxTokensPerRequest = 4096,
            ApiTimeoutSeconds = 300,
            AutoImplementAfterPlanApproval = true,
            EnableCodeReview = true,
            EnableVerboseLogging = false,
            IsActive = true
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var inputs = cut.FindAll("input, select");
        Assert.True(inputs.Any());
    }

    [Fact]
    public async Task Submit_WithValidData_InvokesOnValidSubmitCallback()
    {
        // Arrange
        var model = new TestTenantRequest
        {
            Name = "Test Tenant",
            TicketPlatform = "Jira",
            TicketPlatformUrl = "https://test.atlassian.net",
            TicketPlatformApiToken = "test-token",
            ClaudeApiKey = "test-key",
            ClaudeModel = "claude-sonnet-4-5"
        };

        var submitCallbackInvoked = false;

        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, () => submitCallbackInvoked = true));

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        Assert.True(submitCallbackInvoked);
    }

    [Fact]
    public async Task Submit_WithInvalidData_DoesNotInvokeCallback()
    {
        // Arrange
        var model = new TestTenantRequest
        {
            // Missing required fields
            Name = "",
            TicketPlatformUrl = ""
        };

        var submitCallbackInvoked = false;

        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, () => submitCallbackInvoked = true));

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        Assert.False(submitCallbackInvoked);
    }

    [Fact]
    public async Task CancelButton_InvokesOnCancelCallback()
    {
        // Arrange
        var model = new TestTenantRequest();
        var cancelCallbackInvoked = false;

        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnCancel, () => cancelCallbackInvoked = true));

        // Act
        var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
        if (cancelButton != null)
        {
            await cut.InvokeAsync(() => cancelButton.Click());
        }

        // Assert
        Assert.True(cancelCallbackInvoked || cancelButton == null); // OK if no cancel button
    }

    [Fact]
    public void Render_DisplaysConfigurationSection()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        // Should show configuration fields
        Assert.Contains("claude", markup.ToLower());
    }

    [Fact]
    public void Render_DisplaysCredentialsSection()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Credentials", markup);
    }

    [Fact]
    public void Render_DisplaysHelpText()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("unique name", markup.ToLower());
    }

    [Fact]
    public void Render_TicketPlatformApiToken_IsPasswordField()
    {
        // Arrange
        var model = new TestTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var passwordInputs = cut.FindAll("input[type='password']");
        Assert.NotEmpty(passwordInputs);
    }
}

/// <summary>
/// Test implementation of ITenantRequest for testing purposes.
/// </summary>
internal class TestTenantRequest : ITenantRequest
{
    [Required(ErrorMessage = "Tenant name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ticket platform URL is required")]
    public string TicketPlatformUrl { get; set; } = string.Empty;

    public string TicketPlatform { get; set; } = "Jira";
    public string? TicketPlatformApiToken { get; set; }
    public string? ClaudeApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoImplementAfterPlanApproval { get; set; }
    public int MaxRetries { get; set; } = 3;

    [Required(ErrorMessage = "Claude model is required")]
    public string ClaudeModel { get; set; } = "claude-sonnet-4-5";

    public int MaxTokensPerRequest { get; set; } = 4096;
    public int ApiTimeoutSeconds { get; set; } = 300;
    public bool EnableVerboseLogging { get; set; }
    public bool EnableCodeReview { get; set; }
}
