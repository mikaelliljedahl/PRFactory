using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components;
using Xunit;
using static PRFactory.Web.Components.TicketFilters;

namespace PRFactory.Tests.Components;

/// <summary>
/// Comprehensive bUnit tests for TicketFilters component
/// Tests filter selection, state changes, and clear functionality
/// </summary>
public class TicketFiltersTests : ComponentTestBase
{
    [Fact]
    public void Render_DisplaysFilterCard()
    {
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        Assert.Contains("Filters", cut.Markup);
        Assert.Contains("form-label", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysAllFilterDropdowns()
    {
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        Assert.Contains("State", cut.Markup);
        Assert.Contains("Source", cut.Markup);
        Assert.Contains("Repository", cut.Markup);
        var selects = cut.FindAll("select");
        Assert.Equal(3, selects.Count);
    }

    [Fact]
    public void Render_DisplaysStateFilterOptions()
    {
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        Assert.Contains("Triggered", cut.Markup);
        Assert.Contains("Planning", cut.Markup);
        Assert.Contains("Completed", cut.Markup);
        Assert.Contains("Failed", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSourceFilterOptions()
    {
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        Assert.Contains("Web UI", cut.Markup);
        Assert.Contains("Jira", cut.Markup);
        Assert.Contains("Azure DevOps", cut.Markup);
        Assert.Contains("GitHub Issues", cut.Markup);
    }

    [Fact]
    public void Render_WithRepositories_DisplaysRepositoryOptions()
    {
        // Arrange
        var repos = new List<RepositoryInfo>
        {
            new RepositoryInfo { Id = Guid.NewGuid(), Name = "TestRepo1" },
            new RepositoryInfo { Id = Guid.NewGuid(), Name = "TestRepo2" }
        };

        // Act
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, repos));

        // Assert
        Assert.Contains("TestRepo1", cut.Markup);
        Assert.Contains("TestRepo2", cut.Markup);
    }

    [Fact]
    public void Render_WithoutRepositories_DoesNotCrash()
    {
        // Act
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, null));

        // Assert
        Assert.NotNull(cut.Markup);
        Assert.Contains("Repository", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysClearFiltersButton()
    {
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        var clearButton = cut.FindAll("button").FirstOrDefault();
        Assert.NotNull(clearButton);
        Assert.Contains("Clear Filters", cut.Markup);
        Assert.Contains("bi-x-circle", cut.Markup);
    }

    [Fact]
    public void StateFilter_Changed_InvokesCallback()
    {
        // Arrange
        var newState = string.Empty;
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.SelectedStateChanged, EventCallback.Factory.Create<string?>(this, (string? state) =>
            {
                newState = state ?? string.Empty;
                return Task.CompletedTask;
            }))
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        // Act
        var stateSelect = cut.FindAll("select")[0];
        stateSelect.Change(WorkflowState.Planning.ToString());

        // Assert
        Assert.Contains(WorkflowState.Planning.ToString(), newState);
    }

    [Fact]
    public void SourceFilter_Changed_InvokesCallback()
    {
        // Arrange
        var newSource = string.Empty;
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.SelectedSourceChanged, EventCallback.Factory.Create<string?>(this, (string? source) =>
            {
                newSource = source ?? string.Empty;
                return Task.CompletedTask;
            }))
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        // Act
        var sourceSelect = cut.FindAll("select")[1];
        sourceSelect.Change(TicketSource.Jira.ToString());

        // Assert
        Assert.Contains(TicketSource.Jira.ToString(), newSource);
    }

    [Fact]
    public void ClearFilters_ResetsAllFilterValues()
    {
        // Arrange
        var filterChangeCalled = false;
        var stateCleared = false;
        var sourceCleared = false;
        var repositoryCleared = false;

        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.SelectedState, WorkflowState.Planning.ToString())
            .Add(p => p.SelectedSource, TicketSource.Jira.ToString())
            .Add(p => p.SelectedRepositoryId, Guid.NewGuid().ToString())
            .Add(p => p.SelectedStateChanged, EventCallback.Factory.Create<string?>(this, (string? state) =>
            {
                if (string.IsNullOrEmpty(state))
                    stateCleared = true;
                return Task.CompletedTask;
            }))
            .Add(p => p.SelectedSourceChanged, EventCallback.Factory.Create<string?>(this, (string? source) =>
            {
                if (string.IsNullOrEmpty(source))
                    sourceCleared = true;
                return Task.CompletedTask;
            }))
            .Add(p => p.SelectedRepositoryIdChanged, EventCallback.Factory.Create<string?>(this, (string? repo) =>
            {
                if (string.IsNullOrEmpty(repo))
                    repositoryCleared = true;
                return Task.CompletedTask;
            }))
            .Add(p => p.OnFilterChanged, EventCallback.Factory.Create(this, () =>
            {
                filterChangeCalled = true;
                return Task.CompletedTask;
            })));

        // Act
        var clearButton = cut.FindAll("button")[0];
        clearButton.Click();

        // Assert
        Assert.True(stateCleared);
        Assert.True(sourceCleared);
        Assert.True(repositoryCleared);
        Assert.True(filterChangeCalled);
    }

    [Fact]
    public void Render_WithMultipleRepositories_DisplaysAllRepositories()
    {
        // Arrange
        var repos = new List<RepositoryInfo>
        {
            new RepositoryInfo { Id = Guid.NewGuid(), Name = "auth-service" },
            new RepositoryInfo { Id = Guid.NewGuid(), Name = "api-gateway" },
            new RepositoryInfo { Id = Guid.NewGuid(), Name = "ui-components" },
            new RepositoryInfo { Id = Guid.NewGuid(), Name = "data-pipeline" }
        };

        // Act
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.Repositories, repos));

        // Assert
        Assert.Contains("auth-service", cut.Markup);
        Assert.Contains("api-gateway", cut.Markup);
        Assert.Contains("ui-components", cut.Markup);
        Assert.Contains("data-pipeline", cut.Markup);
    }

    [Fact]
    public void Render_HasFormStructure()
    {
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        Assert.Contains("row", cut.Markup);
        Assert.Contains("col-md-", cut.Markup);
        Assert.Contains("form-select", cut.Markup);
    }
}
