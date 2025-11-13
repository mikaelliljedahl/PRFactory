using Bunit;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Tenants;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.Tenants;

public class TenantConfigEditorTests : ComponentTestBase
{
    // Note: ITenantService is already registered by TestContextBase

    [Fact]
    public async Task OnInitialized_LoadsTenantConfiguration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder()
            .WithId(tenantId)
            .WithAutoImplementAfterPlanApproval(true)
            .WithCodeReview(true)
            .WithMaxRetries(5)
            .Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("autoImplementAfterPlanApproval"));

        Assert.Contains("Tenant Configuration", cut.Markup);
        MockTenantService.Verify(
            x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnInitialized_WhenTenantNotFound_ShowsErrorMessage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantDto?)null);

        // Act
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("Tenant not found"));
        Assert.Contains("Tenant not found", cut.Markup);
    }

    [Fact]
    public async Task OnInitialized_WhenException_ShowsErrorMessage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("Failed to load configuration"));
        Assert.Contains("Failed to load configuration: Network error", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsJsonTextarea()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder().WithId(tenantId).Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("textarea").Count > 0);

        var textarea = cut.Find("textarea");
        Assert.NotNull(textarea);
        Assert.Contains("Configuration (JSON)", cut.Markup);
    }

    [Fact]
    public async Task Render_ShowsAvailableOptions()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder().WithId(tenantId).Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("Available Options"));

        Assert.Contains("Available Options:", cut.Markup);
        Assert.Contains("AutoImplementAfterPlanApproval", cut.Markup);
        Assert.Contains("MaxRetries", cut.Markup);
        Assert.Contains("ClaudeModel", cut.Markup);
        Assert.Contains("MaxTokensPerRequest", cut.Markup);
        Assert.Contains("ApiTimeoutSeconds", cut.Markup);
        Assert.Contains("EnableVerboseLogging", cut.Markup);
        Assert.Contains("EnableCodeReview", cut.Markup);
        Assert.Contains("AllowedRepositories", cut.Markup);
    }

    [Fact]
    public async Task SaveButton_WithValidJson_SavesConfiguration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        MockTenantService
            .Setup(x => x.UpdateTenantAsync(It.IsAny<UpdateTenantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("textarea").Count > 0);

        // Act
        var saveButton = cut.Find("button:contains('Save Configuration')");
        await saveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        MockTenantService.Verify(
            x => x.UpdateTenantAsync(It.IsAny<UpdateTenantRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveButton_WhenException_ShowsErrorMessage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        MockTenantService
            .Setup(x => x.UpdateTenantAsync(It.IsAny<UpdateTenantRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Save failed"));

        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("button:contains('Save Configuration')").Count > 0);

        // Act
        var saveButton = cut.Find("button:contains('Save Configuration')");
        await saveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("Failed to save configuration"));
        Assert.Contains("Failed to save configuration: Save failed", cut.Markup);
    }

    [Fact]
    public async Task ResetButton_ReloadsConfiguration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder().WithId(tenantId).Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("button:contains('Reset')").Count > 0);

        // Act
        var resetButton = cut.Find("button:contains('Reset')");
        await resetButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        MockTenantService.Verify(
            x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Once on init, once on reset
    }

    [Fact]
    public async Task FormatJsonButton_FormatsJson()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder().WithId(tenantId).Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("button:contains('Format JSON')").Count > 0);

        // Act
        var formatButton = cut.Find("button:contains('Format JSON')");
        await formatButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("JSON formatted successfully"));
        Assert.Contains("JSON formatted successfully", cut.Markup);
    }

    [Fact]
    public async Task SaveConfiguration_InvokesCallback_WhenSuccessful()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        MockTenantService
            .Setup(x => x.UpdateTenantAsync(It.IsAny<UpdateTenantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var callbackInvoked = false;
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId)
            .Add(p => p.OnConfigurationSaved, () => { callbackInvoked = true; }));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("button:contains('Save Configuration')").Count > 0);

        // Act
        var saveButton = cut.Find("button:contains('Save Configuration')");
        await saveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => callbackInvoked);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task Render_WithIsLoading_ShowsSpinner()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<TenantDto?>();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("spinner", cut.Markup.ToLower());
        Assert.Contains("Loading", cut.Markup);

        // Complete loading
        tcs.SetResult(new TenantDtoBuilder().WithId(tenantId).Build());
    }

    [Fact]
    public async Task Render_ShowsActionButtons()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder().WithId(tenantId).Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("Save Configuration"));

        Assert.Contains("Save Configuration", cut.Markup);
        Assert.Contains("Reset", cut.Markup);
        Assert.Contains("Format JSON", cut.Markup);
    }

    [Fact]
    public async Task SaveButton_DisabledWhileSaving()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var saveTcs = new TaskCompletionSource<TenantDto>();
        MockTenantService
            .Setup(x => x.UpdateTenantAsync(It.IsAny<UpdateTenantRequest>(), It.IsAny<CancellationToken>()))
            .Returns(saveTcs.Task);

        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("button:contains('Save Configuration')").Count > 0);

        // Act
        var saveButton = cut.Find("button:contains('Save Configuration')");
        await saveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        Assert.Contains("spinner", cut.Markup.ToLower());

        // Complete save
        saveTcs.SetResult(tenant);
    }

    [Fact]
    public async Task Render_DisablesTextareaWhileSaving()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var saveTcs = new TaskCompletionSource<TenantDto>();
        MockTenantService
            .Setup(x => x.UpdateTenantAsync(It.IsAny<UpdateTenantRequest>(), It.IsAny<CancellationToken>()))
            .Returns(saveTcs.Task);

        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("textarea").Count > 0);

        // Act
        var saveButton = cut.Find("button:contains('Save Configuration')");
        await saveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        var textarea = cut.Find("textarea");
        Assert.True(textarea.HasAttribute("disabled"));

        // Complete save
        saveTcs.SetResult(tenant);
    }

    [Fact]
    public async Task SaveConfiguration_ShowsSuccessMessage_WhenSuccessful()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDtoBuilder()
            .WithId(tenantId)
            .WithName("Test Tenant")
            .Build();

        MockTenantService
            .Setup(x => x.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        MockTenantService
            .Setup(x => x.UpdateTenantAsync(It.IsAny<UpdateTenantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var cut = RenderComponent<TenantConfigEditor>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.FindAll("button:contains('Save Configuration')").Count > 0);

        // Act
        var saveButton = cut.Find("button:contains('Save Configuration')");
        await saveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await cut.InvokeAsync(() => Task.Delay(100));
        cut.WaitForState(() => cut.Markup.Contains("Configuration saved successfully"));
        Assert.Contains("Configuration saved successfully", cut.Markup);
    }
}
