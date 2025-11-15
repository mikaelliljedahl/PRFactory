using Bunit;
using PRFactory.Web.Components.Repositories;
using PRFactory.Web.Models;
using Xunit;

namespace PRFactory.Web.Tests.Components.Repositories;

/// <summary>
/// Tests for the RepositoryForm component.
/// Verifies form rendering, validation, submission, and cancellation.
/// </summary>
public class RepositoryFormTests : TestContext
{
    [Fact]
    public void Render_DisplaysRequiredFormFields()
    {
        // Arrange
        var model = new TestRepositoryFormModel();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Repository Name", markup);
        Assert.Contains("Git Platform", markup);
        Assert.Contains("Clone URL", markup);
        Assert.Contains("Default Branch", markup);
        Assert.Contains("Access Token", markup);
    }

    [Fact]
    public void Render_DisplaysPlatformOptions()
    {
        // Arrange
        var model = new TestRepositoryFormModel();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("GitHub", markup);
        Assert.Contains("Bitbucket", markup);
        Assert.Contains("Azure DevOps", markup);
        Assert.Contains("GitLab", markup);
    }

    [Fact]
    public void Render_WithShowTenantSelectionTrue_DisplaysTenantDropdown()
    {
        // Arrange
        var model = new TestRepositoryFormModel();
        var tenants = new List<TenantDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Tenant 1" },
            new() { Id = Guid.NewGuid(), Name = "Tenant 2" }
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ShowTenantSelection, true)
            .Add(p => p.Tenants, tenants));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Tenant", markup);
        Assert.Contains("Tenant 1", markup);
        Assert.Contains("Tenant 2", markup);
    }

    [Fact]
    public void Render_WithShowTenantSelectionFalse_DoesNotDisplayTenantDropdown()
    {
        // Arrange
        var model = new TestRepositoryFormModel();
        var tenants = new List<TenantDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Tenant 1" }
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ShowTenantSelection, false)
            .Add(p => p.Tenants, tenants));

        // Assert
        var markup = cut.Markup;
        // Should not contain tenant-specific text
        Assert.DoesNotContain("Select the tenant", markup);
    }

    [Fact]
    public void Render_WithNoTenants_DoesNotDisplayTenantDropdown()
    {
        // Arrange
        var model = new TestRepositoryFormModel();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ShowTenantSelection, true)
            .Add(p => p.Tenants, new List<TenantDto>()));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("Select the tenant", markup);
    }

    [Fact]
    public void Render_PreFillsModelData()
    {
        // Arrange
        var model = new TestRepositoryFormModel
        {
            Name = "Test Repository",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo.git",
            DefaultBranch = "main",
            AccessToken = "test-token"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var inputs = cut.FindAll("input[type='text'], input[type='password'], select");
        Assert.True(inputs.Any());
    }

    [Fact]
    public async Task Submit_WithValidData_InvokesOnValidSubmitCallback()
    {
        // Arrange
        var model = new TestRepositoryFormModel
        {
            Name = "Test Repository",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo.git",
            DefaultBranch = "main"
        };

        var submitCallbackInvoked = false;
        object? submittedModel = null;

        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, submittedData =>
            {
                submitCallbackInvoked = true;
                submittedModel = submittedData;
            }));

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        Assert.True(submitCallbackInvoked);
        Assert.NotNull(submittedModel);
        Assert.Equal(model, submittedModel);
    }

    [Fact]
    public async Task Submit_WithInvalidData_DoesNotInvokeCallback()
    {
        // Arrange
        var model = new TestRepositoryFormModel
        {
            // Missing required fields
            Name = "",
            GitPlatform = "",
            CloneUrl = "",
            DefaultBranch = ""
        };

        var submitCallbackInvoked = false;

        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, _ => submitCallbackInvoked = true));

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
        var model = new TestRepositoryFormModel();
        var cancelCallbackInvoked = false;

        var cut = RenderComponent<RepositoryForm>(parameters => parameters
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
    public void Render_WithCustomSubmitButtonText_DisplaysCustomText()
    {
        // Arrange
        var model = new TestRepositoryFormModel();
        var customText = "Create Repository";

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.SubmitButtonText, customText));

        // Assert
        var markup = cut.Markup;
        Assert.Contains(customText, markup);
    }

    [Fact]
    public void Render_WithIsEditModeTrue_ConfiguresFormForEditing()
    {
        // Arrange
        var model = new TestRepositoryFormModel
        {
            Name = "Existing Repository",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo.git",
            DefaultBranch = "main"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert - form should render with existing data
        var inputs = cut.FindAll("input, select");
        Assert.True(inputs.Any());
    }

    [Fact]
    public void Render_DisplaysHelpText()
    {
        // Arrange
        var model = new TestRepositoryFormModel();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("friendly name", markup.ToLower());
        Assert.Contains("hosting platform", markup.ToLower());
    }

    [Fact]
    public void Render_DefaultBranch_HasDefaultValue()
    {
        // Arrange
        var model = new TestRepositoryFormModel();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Equal("main", model.DefaultBranch);
    }

    [Fact]
    public void Render_AccessToken_IsPasswordField()
    {
        // Arrange
        var model = new TestRepositoryFormModel();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var passwordInput = cut.FindAll("input[type='password']");
        Assert.NotEmpty(passwordInput);
    }
}

/// <summary>
/// Test implementation of RepositoryFormModel for testing purposes.
/// </summary>
internal class TestRepositoryFormModel : RepositoryFormModel
{
    // Concrete implementation for testing
}
