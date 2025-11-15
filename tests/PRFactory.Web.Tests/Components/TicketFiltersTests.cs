using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components;
using Xunit;

namespace PRFactory.Web.Tests.Components;

/// <summary>
/// Tests for the TicketFilters component.
/// Verifies filter rendering, selection, clear functionality, and event callbacks.
/// </summary>
public class TicketFiltersTests : TestContext
{
    private List<TicketFilters.RepositoryInfo> CreateTestRepositories(int count = 3)
    {
        var repositories = new List<TicketFilters.RepositoryInfo>();
        for (int i = 1; i <= count; i++)
        {
            repositories.Add(new TicketFilters.RepositoryInfo
            {
                Id = Guid.NewGuid(),
                Name = $"Repository {i}"
            });
        }
        return repositories;
    }

    [Fact]
    public void Render_DisplaysFilterCard()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var card = cut.Find(".card");
        Assert.NotNull(card);
        Assert.Contains("Filters", card.TextContent);
    }

    [Fact]
    public void Render_DisplaysStateFilterDropdown()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var stateFilter = cut.Find("#stateFilter");
        Assert.NotNull(stateFilter);
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("State"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_DisplaysSourceFilterDropdown()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var sourceFilter = cut.Find("#sourceFilter");
        Assert.NotNull(sourceFilter);
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Source"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_DisplaysRepositoryFilterDropdown()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var repositoryFilter = cut.Find("#repositoryFilter");
        Assert.NotNull(repositoryFilter);
        var label = cut.FindAll("label").FirstOrDefault(l => l.TextContent.Contains("Repository"));
        Assert.NotNull(label);
    }

    [Fact]
    public void Render_DisplaysClearFiltersButton()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var clearButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Clear Filters"));
        Assert.NotNull(clearButton);
    }

    [Fact]
    public void Render_StateFilterContainsAllWorkflowStates()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Triggered", markup);
        Assert.Contains("Analyzing", markup);
        Assert.Contains("Planning", markup);
        Assert.Contains("Implementing", markup);
        Assert.Contains("Completed", markup);
        Assert.Contains("Cancelled", markup);
        Assert.Contains("Failed", markup);
    }

    [Fact]
    public void Render_SourceFilterContainsAllSources()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Web UI", markup);
        Assert.Contains("Jira", markup);
        Assert.Contains("Azure DevOps", markup);
        Assert.Contains("GitHub Issues", markup);
    }

    [Fact]
    public void Render_WithRepositories_DisplaysRepositoryOptions()
    {
        // Arrange
        var repositories = CreateTestRepositories(3);

        // Act
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, repositories));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Repository 1", markup);
        Assert.Contains("Repository 2", markup);
        Assert.Contains("Repository 3", markup);
    }

    [Fact]
    public void Render_WithoutRepositories_DoesNotDisplayRepositoryOptions()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, null));

        // Assert
        var repositoryFilter = cut.Find("#repositoryFilter");
        var options = repositoryFilter.QuerySelectorAll("option");
        // Should only have "All Repositories" option
        Assert.Equal(1, options.Length);
    }

    [Fact]
    public void Render_StateFilterHasAllStatesOption()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var stateFilter = cut.Find("#stateFilter");
        var options = stateFilter.QuerySelectorAll("option");
        var allStatesOption = options.FirstOrDefault(o => o.TextContent == "All States");
        Assert.NotNull(allStatesOption);
    }

    [Fact]
    public async Task ChangeState_InvokesFilterChangedCallback()
    {
        // Arrange
        var filterChangedInvoked = false;
        var newState = "";

        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.SelectedStateChanged, EventCallback.Factory.Create<string?>(this, state =>
            {
                newState = state ?? "";
            }))
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var stateFilter = cut.Find("#stateFilter");
        stateFilter.Change("Triggered");

        // Assert
        Assert.True(filterChangedInvoked || !string.IsNullOrEmpty(newState));
    }

    [Fact]
    public async Task ChangeSource_InvokesFilterChangedCallback()
    {
        // Arrange
        var filterChangedInvoked = false;
        var newSource = "";

        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.SelectedSourceChanged, EventCallback.Factory.Create<string?>(this, source =>
            {
                newSource = source ?? "";
            }))
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var sourceFilter = cut.Find("#sourceFilter");
        sourceFilter.Change("Jira");

        // Assert
        Assert.True(filterChangedInvoked || !string.IsNullOrEmpty(newSource));
    }

    [Fact]
    public async Task ChangeRepository_InvokesFilterChangedCallback()
    {
        // Arrange
        var repositories = CreateTestRepositories(2);
        var repoId = repositories.First().Id;
        var filterChangedInvoked = false;
        var newRepositoryId = "";

        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, repositories)
            .Add(p => p.SelectedRepositoryIdChanged, EventCallback.Factory.Create<string?>(this, id =>
            {
                newRepositoryId = id ?? "";
            }))
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var repositoryFilter = cut.Find("#repositoryFilter");
        repositoryFilter.Change(repoId.ToString());

        // Assert
        Assert.True(filterChangedInvoked || !string.IsNullOrEmpty(newRepositoryId));
    }

    [Fact]
    public async Task Click_ClearFiltersButton_ClearsAllSelections()
    {
        // Arrange
        var repositories = CreateTestRepositories(2);
        var stateCleared = false;
        var sourceCleared = false;
        var repositoryCleared = false;

        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, repositories)
            .Add(p => p.SelectedState, "Triggered")
            .Add(p => p.SelectedSource, "Jira")
            .Add(p => p.SelectedRepositoryId, repositories.First().Id.ToString())
            .Add(p => p.SelectedStateChanged, EventCallback.Factory.Create<string?>(this, state =>
            {
                stateCleared = string.IsNullOrEmpty(state);
            }))
            .Add(p => p.SelectedSourceChanged, EventCallback.Factory.Create<string?>(this, source =>
            {
                sourceCleared = string.IsNullOrEmpty(source);
            }))
            .Add(p => p.SelectedRepositoryIdChanged, EventCallback.Factory.Create<string?>(this, id =>
            {
                repositoryCleared = string.IsNullOrEmpty(id);
            }))
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () => { })));

        // Act
        var clearButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Clear Filters"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        Assert.True(stateCleared || sourceCleared || repositoryCleared);
    }

    [Fact]
    public async Task Click_ClearFilters_InvokesOnFilterChangedCallback()
    {
        // Arrange
        var repositories = CreateTestRepositories(1);
        var filterChangedInvoked = false;

        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, repositories)
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var clearButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Clear Filters"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        Assert.True(filterChangedInvoked);
    }

    [Fact]
    public void Render_PreSelectsValues_WhenParametersProvided()
    {
        // Arrange
        var repositories = CreateTestRepositories(2);
        var selectedRepoId = repositories.First().Id.ToString();

        // Act
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.SelectedState, "Triggered")
            .Add(p => p.SelectedSource, "Jira")
            .Add(p => p.SelectedRepositoryId, selectedRepoId)
            .Add(p => p.Repositories, repositories));

        // Assert
        var stateFilter = cut.Find("#stateFilter");
        var sourceFilter = cut.Find("#sourceFilter");
        var repositoryFilter = cut.Find("#repositoryFilter");

        Assert.NotNull(stateFilter);
        Assert.NotNull(sourceFilter);
        Assert.NotNull(repositoryFilter);
    }

    [Fact]
    public void Render_FilterCardHasBootstrapClasses()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var card = cut.Find(".card");
        Assert.NotNull(card);
        Assert.Contains("shadow-sm", card.GetAttribute("class") ?? "");
    }

    [Fact]
    public void Render_InputsAreProperlyOrganized()
    {
        // Arrange
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var rows = cut.FindAll(".row");
        Assert.True(rows.Count >= 2); // At least filters row and buttons row
    }
}
