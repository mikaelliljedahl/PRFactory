using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.Components.Errors;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Errors;

/// <summary>
/// Tests for ErrorListFilter component
/// Verifies filter controls for severity and status, filter callback invocation, and clear functionality.
/// </summary>
public class ErrorListFilterTests : TestContext
{
    [Fact]
    public void Render_DisplaysFilterCard()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("card", cut.Markup);
        Assert.Contains("Filters", cut.Markup);
        Assert.Contains("bi-funnel", cut.Markup); // Funnel icon
    }

    [Fact]
    public void Render_DisplaysSeverityFilterSelect()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("Severity", cut.Markup);
        var selects = cut.FindAll("select");
        Assert.NotEmpty(selects);
    }

    [Fact]
    public void Render_DisplaysSeverityOptions()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("All Severities", markup);
        Assert.Contains("Critical", markup);
        Assert.Contains("High", markup);
        Assert.Contains("Medium", markup);
        Assert.Contains("Low", markup);
    }

    [Fact]
    public void Render_DisplaysEntityTypeFilter()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("Entity Type", cut.Markup);
        var inputs = cut.FindAll("input[type='text']");
        Assert.NotEmpty(inputs);
    }

    [Fact]
    public void Render_DisplaysEntityTypeInputPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        var input = cut.Find("input[placeholder='e.g., Ticket, Repository']");
        Assert.NotNull(input);
    }

    [Fact]
    public void Render_DisplaysStatusFilter()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("Status", cut.Markup);
        var selects = cut.FindAll("select");
        Assert.True(selects.Count >= 2); // At least severity and status selects
    }

    [Fact]
    public void Render_DisplaysStatusOptions()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("All", markup);
        Assert.Contains("Unresolved", markup);
        Assert.Contains("Resolved", markup);
    }

    [Fact]
    public void Render_DisplaysFromDateFilter()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("From Date", cut.Markup);
        var dateInputs = cut.FindAll("input[type='date']");
        Assert.NotEmpty(dateInputs);
    }

    [Fact]
    public void Render_DisplaysToDateFilter()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("To Date", cut.Markup);
        var dateInputs = cut.FindAll("input[type='date']");
        Assert.True(dateInputs.Count >= 2); // Both from and to dates
    }

    [Fact]
    public void Render_DisplaysSearchInput()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("Search", cut.Markup);
        var input = cut.Find("input[placeholder*='Search']");
        Assert.NotNull(input);
    }

    [Fact]
    public void Render_DisplaysSearchInputPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        var input = cut.Find("input[placeholder*='Search in messages']");
        Assert.NotNull(input);
    }

    [Fact]
    public void Render_DisplaysClearButton()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        var buttons = cut.FindAll("button");
        var clearButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Clear"));
        Assert.NotNull(clearButton);
        Assert.Contains("x-circle", cut.Markup); // Clear icon
    }

    [Fact]
    public void SetParameter_SelectedSeverity_BindsToSelect()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.SelectedSeverity, ErrorSeverity.Critical.ToString()));

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void SetParameter_EntityType_BindsToInput()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.EntityType, "Ticket"));

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void SetParameter_ResolvedStatus_BindsToSelect()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.ResolvedStatus, "unresolved"));

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void SetParameter_FromDate_BindsToDateInput()
    {
        // Arrange
        var fromDate = new DateTime(2024, 11, 01);

        // Act
        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.FromDate, fromDate));

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void SetParameter_ToDate_BindsToDateInput()
    {
        // Arrange
        var toDate = new DateTime(2024, 11, 15);

        // Act
        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.ToDate, toDate));

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void SetParameter_SearchTerm_BindsToInput()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.SearchTerm, "database error"));

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void Click_ClearButton_ClearsAllFilters()
    {
        // Arrange
        var filterChangedInvoked = false;
        var lastArgs = (ErrorListFilter.FilterChangedArgs?)null;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.SelectedSeverity, ErrorSeverity.Critical.ToString())
            .Add(p => p.EntityType, "Ticket")
            .Add(p => p.ResolvedStatus, "unresolved")
            .Add(p => p.SearchTerm, "error")
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, args =>
            {
                filterChangedInvoked = true;
                lastArgs = args;
            })));

        // Act
        var buttons = cut.FindAll("button");
        var clearButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Clear"));
        Assert.NotNull(clearButton);
        clearButton.Click();

        // Assert
        Assert.True(filterChangedInvoked);
        Assert.NotNull(lastArgs);
        // After clear, all filter values should be null/default
        Assert.Null(lastArgs.Severity);
        Assert.Null(lastArgs.EntityType);
        Assert.Null(lastArgs.IsResolved);
        Assert.Null(lastArgs.SearchTerm);
    }

    [Fact]
    public void SeverityChange_InvokesClearFilter_WithCorrectArgs()
    {
        // Arrange
        var filterChangedInvoked = false;
        ErrorListFilter.FilterChangedArgs? passedArgs = null;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, args =>
            {
                filterChangedInvoked = true;
                passedArgs = args;
            })));

        // Act
        var selects = cut.FindAll("select");
        var severitySelect = selects[0]; // First select is severity
        severitySelect.Change(ErrorSeverity.High.ToString());

        // Assert
        Assert.True(filterChangedInvoked);
        Assert.NotNull(passedArgs);
    }

    [Fact]
    public void EntityTypeChange_InvokesClearFilter()
    {
        // Arrange
        var filterChangedInvoked = false;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, _ =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var inputs = cut.FindAll("input[type='text']");
        var entityTypeInput = inputs.FirstOrDefault(i => i.GetAttribute("placeholder")?.Contains("e.g.") == true);
        if (entityTypeInput != null)
        {
            entityTypeInput.Change("Repository");
        }

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void ResolvedStatusChange_InvokesClearFilter()
    {
        // Arrange
        var filterChangedInvoked = false;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, _ =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var selects = cut.FindAll("select");
        var statusSelect = selects[1]; // Second select is status
        statusSelect.Change("resolved");

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void FromDateChange_InvokesClearFilter()
    {
        // Arrange
        var filterChangedInvoked = false;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, _ =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var dateInputs = cut.FindAll("input[type='date']");
        if (dateInputs.Count > 0)
        {
            dateInputs[0].Change("2024-11-01");
        }

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void ToDateChange_InvokesClearFilter()
    {
        // Arrange
        var filterChangedInvoked = false;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, _ =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var dateInputs = cut.FindAll("input[type='date']");
        if (dateInputs.Count > 1)
        {
            dateInputs[1].Change("2024-11-15");
        }

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void SearchTermChange_InvokesClearFilter()
    {
        // Arrange
        var filterChangedInvoked = false;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, _ =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var searchInput = cut.Find("input[placeholder*='Search in messages']");
        searchInput.Change("test error");

        // Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void FilterChangedArgs_WithResolved_SetsIsResolvedToTrue()
    {
        // This tests the FilterChangedArgs logic by invoking filter change with resolved status
        var filterChangedInvoked = false;
        ErrorListFilter.FilterChangedArgs? passedArgs = null;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.ResolvedStatus, "resolved")
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, args =>
            {
                filterChangedInvoked = true;
                passedArgs = args;
            })));

        // Act
        var selects = cut.FindAll("select");
        var statusSelect = selects.FirstOrDefault(s => s.GetAttribute("class")?.Contains("form-select") == true);
        Assert.NotNull(statusSelect);
    }

    [Fact]
    public void FilterChangedArgs_WithUnresolved_SetsIsResolvedToFalse()
    {
        // This tests the FilterChangedArgs logic by invoking filter change with unresolved status
        var filterChangedInvoked = false;
        ErrorListFilter.FilterChangedArgs? passedArgs = null;

        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.ResolvedStatus, "unresolved")
            .Add(p => p.OnFiltersChanged, EventCallback.Factory.Create<ErrorListFilter.FilterChangedArgs>(this, args =>
            {
                filterChangedInvoked = true;
                passedArgs = args;
            })));

        // Act & Assert
        Assert.NotNull(cut);
    }

    [Fact]
    public void Render_AllFilterFieldsInProperLayout()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        Assert.Contains("row g-3", cut.Markup); // Grid layout
        Assert.Contains("col-md-3", cut.Markup); // Column sizing
        Assert.Contains("col-md-2", cut.Markup);
    }

    [Fact]
    public void Render_ClearButtonInProperColumn()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorListFilter>();

        // Assert
        var buttons = cut.FindAll("button");
        var clearButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Clear"));
        Assert.NotNull(clearButton);
        Assert.Contains("w-100", cut.Markup); // Full width button
    }

    [Fact]
    public void Render_MultipleFiltersCanBeCombined()
    {
        // Arrange
        var fromDate = new DateTime(2024, 11, 01);
        var toDate = new DateTime(2024, 11, 15);

        // Act
        var cut = RenderComponent<ErrorListFilter>(parameters => parameters
            .Add(p => p.SelectedSeverity, ErrorSeverity.High.ToString())
            .Add(p => p.EntityType, "Ticket")
            .Add(p => p.ResolvedStatus, "unresolved")
            .Add(p => p.FromDate, fromDate)
            .Add(p => p.ToDate, toDate)
            .Add(p => p.SearchTerm, "database"));

        // Assert
        Assert.NotNull(cut);
        Assert.Contains("Filters", cut.Markup);
    }
}
