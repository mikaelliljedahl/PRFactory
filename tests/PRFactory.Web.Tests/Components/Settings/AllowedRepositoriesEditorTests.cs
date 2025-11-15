using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using PRFactory.Web.Components.Settings;

namespace PRFactory.Web.Tests.Components.Settings;

/// <summary>
/// Tests for the AllowedRepositoriesEditor component.
/// Verifies repository list rendering, add/remove functionality, and event callbacks.
/// </summary>
public class AllowedRepositoriesEditorTests : TestContext
{
    [Fact]
    public void Render_WithoutRepositories_DisplaysAllowedMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var message = cut.Find(".text-muted");
        Assert.NotNull(message);
        Assert.Contains("All repositories allowed", message.TextContent);
    }

    [Fact]
    public void Render_WithRepositories_DisplaysRepositoryBadges()
    {
        // Arrange
        var repositories = new[] { "repo1", "repo2", "repo3" };

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repositories));

        // Assert
        var badges = cut.FindAll(".badge.bg-primary");
        Assert.Equal(3, badges.Count);
        Assert.Contains("repo1", badges[0].TextContent);
        Assert.Contains("repo2", badges[1].TextContent);
        Assert.Contains("repo3", badges[2].TextContent);
    }

    [Fact]
    public void Render_WithRepositories_EachBadgeHasRemoveButton()
    {
        // Arrange
        var repositories = new[] { "repo1", "repo2" };

        // Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repositories));

        // Assert
        var closeButtons = cut.FindAll(".btn-close");
        Assert.Equal(2, closeButtons.Count);
    }

    [Fact]
    public void Render_WhenDisabled_HidesAddButton()
    {
        // Arrange & Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, new[] { "repo1" })
            .Add(p => p.Disabled, true));

        // Assert
        var addButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Add"));
        Assert.Null(addButton);
    }

    [Fact]
    public void Render_WhenNotDisabled_ShowsAddButton()
    {
        // Arrange & Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.Disabled, false));

        // Assert
        var addButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Add"));
        Assert.NotNull(addButton);
    }

    [Fact]
    public void AddButton_IsDisabledWhenInputEmpty()
    {
        // Arrange & Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var addButton = cut.Find("button");
        Assert.True(addButton.HasAttribute("disabled"));
    }

    [Fact]
    public void AddButton_IsEnabledWhenInputHasValue()
    {
        // Arrange
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        var input = cut.Find("input[placeholder='repository-name']");
        input.Change("new-repo");

        // Act & Assert
        var addButton = cut.Find("button");
        Assert.False(addButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task AddRepository_WithValidInput_InvokesValueChangedCallback()
    {
        // Arrange
        var valueChangedInvoked = false;
        string[]? newValue = null;

        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var input = cut.Find("input[placeholder='repository-name']");
        input.Change("new-repo");

        var addButton = cut.Find("button");

        // Act
        addButton.Click();

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal(new[] { "new-repo" }, newValue);
    }

    [Fact]
    public async Task AddRepository_WithDuplicateRepository_DoesNotAddDuplicate()
    {
        // Arrange
        var repositories = new[] { "repo1", "repo2" };
        var valueChangedCalls = 0;
        string[]? finalValue = null;

        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repositories)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, value =>
            {
                valueChangedCalls++;
                finalValue = value;
            })));

        var input = cut.Find("input[placeholder='repository-name']");
        input.Change("repo1");

        var addButton = cut.Find("button");

        // Act
        addButton.Click();

        // Assert
        Assert.Equal(0, valueChangedCalls); // Should not invoke callback for duplicate
        Assert.Null(finalValue);
    }

    [Fact]
    public async Task AddRepository_TrimsWhitespace()
    {
        // Arrange
        var valueChangedInvoked = false;
        string[]? newValue = null;

        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var input = cut.Find("input[placeholder='repository-name']");
        input.Change("  repo-with-spaces  ");

        var addButton = cut.Find("button");

        // Act
        addButton.Click();

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal("repo-with-spaces", newValue![0]);
    }

    [Fact]
    public async Task RemoveRepository_InvokesValueChangedCallback()
    {
        // Arrange
        var repositories = new[] { "repo1", "repo2", "repo3" };
        var valueChangedInvoked = false;
        string[]? newValue = null;

        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repositories)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var closeButton = cut.FindAll(".btn-close")[0];

        // Act
        closeButton.Click();

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal(new[] { "repo2", "repo3" }, newValue);
    }

    [Fact]
    public async Task RemoveRepository_WhenLastRepository_SetsValueToNull()
    {
        // Arrange
        var repositories = new[] { "repo1" };
        var valueChangedInvoked = false;
        string[]? newValue = repositories;

        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, repositories)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var closeButton = cut.Find(".btn-close");

        // Act
        closeButton.Click();

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.Null(newValue);
    }

    [Fact]
    public async Task HandleKeyUp_WithEnterKey_AddsRepository()
    {
        // Arrange
        var valueChangedInvoked = false;
        string[]? newValue = null;

        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, value =>
            {
                valueChangedInvoked = true;
                newValue = value;
            })));

        var input = cut.Find("input[placeholder='repository-name']");
        input.Change("new-repo");

        // Act
        input.KeyUp(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs
        {
            Key = "Enter"
        });

        // Assert
        Assert.True(valueChangedInvoked);
        Assert.NotNull(newValue);
        Assert.Equal("new-repo", newValue![0]);
    }

    [Fact]
    public async Task HandleKeyUp_WithOtherKey_DoesNotAddRepository()
    {
        // Arrange
        var valueChangedCalls = 0;

        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, value =>
            {
                valueChangedCalls++;
            })));

        var input = cut.Find("input[placeholder='repository-name']");
        input.Change("new-repo");

        // Act
        input.KeyUp(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs
        {
            Key = "Escape"
        });

        // Assert
        Assert.Equal(0, valueChangedCalls);
    }

    [Fact]
    public void Render_DisplaysHelpText()
    {
        // Arrange & Act
        var cut = RenderComponent<AllowedRepositoriesEditor>(parameters => parameters
            .Add(p => p.Value, null));

        // Assert
        var helpText = cut.Find("small.form-text");
        Assert.NotNull(helpText);
        Assert.Contains("Whitelist of repository names", helpText.TextContent);
    }
}
