using Bunit;
using PRFactory.Web.Components.Settings;
using Xunit;

namespace PRFactory.Tests.Web.Components.Settings;

public class AllowedRepositoriesEditorTests : TestContext
{
    [Fact]
    public void AllowedRepositoriesEditor_ShowsAllRepositoriesAllowed_WhenEmpty()
    {
        // Arrange
        string[]? repos = null;

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repos)
            .Add(p => p.Disabled, false));

        // Assert
        Assert.Contains("All repositories allowed", cut.Markup);
    }

    [Fact]
    public void AllowedRepositoriesEditor_DisplaysExistingRepositories()
    {
        // Arrange
        var repos = new[] { "my-app", "backend-api" };

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repos)
            .Add(p => p.Disabled, false));

        // Assert
        Assert.Contains("my-app", cut.Markup);
        Assert.Contains("backend-api", cut.Markup);
    }

    [Fact]
    public void AllowedRepositoriesEditor_ShowsAddButton_WhenNotDisabled()
    {
        // Arrange
        string[]? repos = null;

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repos)
            .Add(p => p.Disabled, false));

        // Assert
        Assert.Contains("Add", cut.Markup);
    }

    [Fact]
    public void AllowedRepositoriesEditor_HidesAddButton_WhenDisabled()
    {
        // Arrange
        string[]? repos = null;

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repos)
            .Add(p => p.Disabled, true));

        // Assert
        Assert.DoesNotContain("Add", cut.Markup);
    }

    [Fact]
    public void AllowedRepositoriesEditor_ShowsRemoveButtons_WhenNotDisabled()
    {
        // Arrange
        var repos = new[] { "my-app" };

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repos)
            .Add(p => p.Disabled, false));

        // Assert
        Assert.Contains("btn-close", cut.Markup);
    }

    [Fact]
    public void AllowedRepositoriesEditor_HidesRemoveButtons_WhenDisabled()
    {
        // Arrange
        var repos = new[] { "my-app" };

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repos)
            .Add(p => p.Disabled, true));

        // Assert
        Assert.DoesNotContain("btn-close", cut.Markup);
    }

    [Fact]
    public void AllowedRepositoriesEditor_DisplaysHelpText()
    {
        // Arrange
        string[]? repos = null;

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repos)
            .Add(p => p.Disabled, false));

        // Assert
        Assert.Contains("Whitelist of repository names", cut.Markup);
    }
}
