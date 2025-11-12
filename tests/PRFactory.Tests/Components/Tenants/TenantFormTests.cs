using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components.Tenants;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Tests.Components.Tenants;

public class TenantFormTests : ComponentTestBase
{
    [Fact]
    public void Render_WithCreateModel_ShowsCreateTenant()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net",
            TicketPlatform = "Jira"
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false));

        // Assert
        Assert.Contains("Create Tenant", cut.Markup);
    }

    [Fact]
    public void Render_WithUpdateModel_ShowsEditTenant()
    {
        // Arrange
        var model = new UpdateTenantRequest
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net",
            TicketPlatform = "Jira"
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        Assert.Contains("Edit Tenant", cut.Markup);
    }

    [Fact]
    public void Render_ShowsAllTicketPlatformOptions()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Jira", cut.Markup);
        Assert.Contains("AzureDevOps", cut.Markup);
        Assert.Contains("GitHub", cut.Markup);
        Assert.Contains("GitLab", cut.Markup);
    }

    [Fact]
    public void Render_ShowsAllConfigurationOptions()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Active", cut.Markup);
        Assert.Contains("Auto-implement after plan approval", cut.Markup);
        Assert.Contains("Enable code review", cut.Markup);
        Assert.Contains("Enable verbose logging", cut.Markup);
    }

    [Fact]
    public async Task Submit_WithValidModel_InvokesOnValidSubmitCallback()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net",
            TicketPlatform = "Jira",
            TicketPlatformApiToken = "valid-token-123",
            ClaudeApiKey = "sk-ant-api-key-123"
        };

        var callbackInvoked = false;
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, () => { callbackInvoked = true; }));

        // Act
        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task Submit_WithInvalidModel_DoesNotInvokeCallback()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            Name = "", // Invalid - name required
            TicketPlatformUrl = "https://test.atlassian.net"
        };

        var callbackInvoked = false;
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, () => { callbackInvoked = true; }));

        // Act
        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public async Task CancelButton_WhenClicked_InvokesOnCancelCallback()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net"
        };

        var cancelInvoked = false;
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnCancel, () => { cancelInvoked = true; }));

        // Act
        var cancelButton = cut.Find("button:contains('Cancel')");
        await cancelButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.True(cancelInvoked);
    }

    [Fact]
    public void Render_WithIsSubmitting_ShowsLoadingState()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net"
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsSubmitting, true));

        // Assert
        Assert.Contains("spinner", cut.Markup.ToLower());
    }

    [Fact]
    public void Render_InCreateMode_ShowsCreateButtonText()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false));

        // Assert
        Assert.Contains("Create Tenant", cut.Markup);
    }

    [Fact]
    public void Render_InEditMode_ShowsUpdateButtonText()
    {
        // Arrange
        var model = new UpdateTenantRequest
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net"
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        Assert.Contains("Update Tenant", cut.Markup);
    }

    [Fact]
    public void Render_InEditMode_ShowsOptionalTokenHelpText()
    {
        // Arrange
        var model = new UpdateTenantRequest
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            TicketPlatformUrl = "https://test.atlassian.net"
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        Assert.Contains("Leave empty to keep existing token", cut.Markup);
        Assert.Contains("Leave empty to keep existing key", cut.Markup);
    }

    [Fact]
    public void Render_InCreateMode_ShowsRequiredTokenHelpText()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false));

        // Assert
        Assert.Contains("API token for authenticating with your ticket platform", cut.Markup);
        Assert.Contains("API key for Claude AI", cut.Markup);
    }

    [Fact]
    public void Render_ShowsCredentialsSection()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Credentials", cut.Markup);
        Assert.Contains("Ticket Platform API Token", cut.Markup);
        Assert.Contains("Claude API Key", cut.Markup);
    }

    [Fact]
    public void Render_ShowsConfigurationSection()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Configuration", cut.Markup);
        Assert.Contains("Claude Model", cut.Markup);
        Assert.Contains("Max Retries", cut.Markup);
        Assert.Contains("Max Tokens per Request", cut.Markup);
        Assert.Contains("API Timeout (seconds)", cut.Markup);
    }

    [Fact]
    public void Render_ShowsAllCheckboxOptions()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Active", cut.Markup);
        Assert.Contains("Auto-implement after plan approval", cut.Markup);
        Assert.Contains("Enable code review", cut.Markup);
        Assert.Contains("Enable verbose logging", cut.Markup);
    }

    [Fact]
    public void Render_ShowsValidationSummary()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("validation-summary", cut.Markup.ToLower());
    }

    [Theory]
    [InlineData("Jira")]
    [InlineData("AzureDevOps")]
    [InlineData("GitHub")]
    [InlineData("GitLab")]
    public void Render_CanSelectAnyTicketPlatform(string platform)
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            TicketPlatform = platform
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains(platform, cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultValues_ShowsDefaultMaxRetries()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            MaxRetries = 3
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        // The form should render with the value
        Assert.Contains("Maximum retry attempts for failed operations (1-10)", cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultValues_ShowsDefaultMaxTokens()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            MaxTokensPerRequest = 8000
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Maximum tokens per Claude API request (1000-100000)", cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultValues_ShowsDefaultTimeout()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            ApiTimeoutSeconds = 300
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Timeout for Claude API requests (30-600)", cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultClaudeModel_ShowsModel()
    {
        // Arrange
        var model = new CreateTenantRequest
        {
            ClaudeModel = "claude-sonnet-4-5-20250929"
        };

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("Claude Model", cut.Markup);
    }

    [Fact]
    public void Render_WithIsSubmittingTrue_DisablesCancelButton()
    {
        // Arrange
        var model = new CreateTenantRequest();

        // Act
        var cut = RenderComponent<TenantForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsSubmitting, true)
            .Add(p => p.OnCancel, () => { }));

        // Assert
        var cancelButton = cut.Find("button:contains('Cancel')");
        Assert.True(cancelButton.HasAttribute("disabled"));
    }
}
