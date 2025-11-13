using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Repositories;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Components.Repositories;

public class RepositoryFormTests : ComponentTestBase
{
    // Note: IRepositoryService is already registered by TestContextBase

    [Fact]
    public void Render_WithCreateModel_PopulatesForm()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo.git",
            DefaultBranch = "main",
            AccessToken = "token123",
            TenantId = Guid.NewGuid()
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("test-repo", cut.Markup);
        Assert.Contains("GitHub", cut.Markup);
        // FormTextField uses id="field-repository-name" (based on label)
        var inputName = cut.Find("input[id*='repository-name']");
        Assert.NotNull(inputName);
    }

    [Fact]
    public void Render_WithUpdateModel_ShowsIsActiveCheckbox()
    {
        // Arrange
        var model = new UpdateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo.git",
            DefaultBranch = "main",
            IsActive = true
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        Assert.NotNull(checkbox);
        Assert.Contains("Repository is active", cut.Markup);
    }

    [Fact]
    public void Render_WithTenantSelection_ShowsTenantDropdown()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub"
        };

        var tenants = new List<TenantDto>
        {
            new TenantDtoBuilder().WithName("Tenant 1").Build(),
            new TenantDtoBuilder().WithName("Tenant 2").Build()
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ShowTenantSelection, true)
            .Add(p => p.Tenants, tenants));

        // Assert
        Assert.Contains("Tenant 1", cut.Markup);
        Assert.Contains("Tenant 2", cut.Markup);
        Assert.Contains("-- Select Tenant --", cut.Markup);
    }

    [Fact]
    public void Render_WithoutTenantSelection_HidesTenantDropdown()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ShowTenantSelection, false));

        // Assert
        Assert.DoesNotContain("Select Tenant", cut.Markup);
    }

    [Fact]
    public void Render_WithAllGitPlatforms_ShowsAllPlatformOptions()
    {
        // Arrange
        var model = new CreateRepositoryRequest();

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains("GitHub", cut.Markup);
        Assert.Contains("Bitbucket", cut.Markup);
        Assert.Contains("AzureDevOps", cut.Markup);
        Assert.Contains("GitLab", cut.Markup);
    }

    [Fact]
    public async Task Submit_WithValidCreateModel_InvokesOnValidSubmitCallback()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo.git",
            DefaultBranch = "main",
            AccessToken = "token123",
            TenantId = Guid.NewGuid()
        };

        object? submittedModel = null;
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, (m) => { submittedModel = m; }));

        // Act
        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert
        Assert.NotNull(submittedModel);
        Assert.Equal(model, submittedModel);
    }

    [Fact]
    public async Task Submit_WithInvalidModel_DoesNotInvokeCallback()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "", // Invalid - name required
            GitPlatform = "GitHub"
        };

        var callbackInvoked = false;
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnValidSubmit, (m) => { callbackInvoked = true; }));

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
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub"
        };

        var cancelInvoked = false;
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnCancel, () => { cancelInvoked = true; }));

        // Act
        var cancelButton = cut.Find("button:contains('Cancel')");
        await cancelButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.True(cancelInvoked);
    }

    [Fact]
    public void Render_WithIsSubmitting_DisablesSubmitButton()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsSubmitting, true));

        // Assert
        // LoadingButton should be in loading state
        Assert.Contains("spinner", cut.Markup.ToLower());
    }

    [Fact]
    public void Render_WithCustomSubmitButtonText_ShowsCustomText()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.SubmitButtonText, "Create Repository"));

        // Assert
        Assert.Contains("Create Repository", cut.Markup);
    }

    [Fact]
    public void Render_InEditMode_ShowsAccessTokenHelpText()
    {
        // Arrange
        var model = new UpdateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub",
            CloneUrl = "https://github.com/test/repo.git",
            DefaultBranch = "main"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, true));

        // Assert
        Assert.Contains("Leave blank to keep existing token", cut.Markup);
    }

    [Fact]
    public void Render_InCreateMode_ShowsAccessTokenRequiredHelpText()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.IsEditMode, false));

        // Assert
        Assert.Contains("Personal access token for Git operations", cut.Markup);
    }

    [Fact]
    public void Render_WithNoCancelCallback_HidesCancelButton()
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = "GitHub"
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var cancelButtons = cut.FindAll("button:contains('Cancel')");
        Assert.Empty(cancelButtons);
    }

    [Theory]
    [InlineData("GitHub")]
    [InlineData("Bitbucket")]
    [InlineData("AzureDevOps")]
    [InlineData("GitLab")]
    public void Render_WithSpecificGitPlatform_CanSelectPlatform(string platform)
    {
        // Arrange
        var model = new CreateRepositoryRequest
        {
            Name = "test-repo",
            GitPlatform = platform
        };

        // Act
        var cut = RenderComponent<RepositoryForm>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        Assert.Contains(platform, cut.Markup);
    }
}
