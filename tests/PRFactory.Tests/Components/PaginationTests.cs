using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components;
using Xunit;

namespace PRFactory.Tests.Components;

/// <summary>
/// Comprehensive bUnit tests for Pagination component
/// Tests page navigation, button states, pagination display, and callbacks
/// </summary>
public class PaginationTests : ComponentTestBase
{
    [Fact]
    public void Render_WithMultiplePages_DisplaysPaginationControls()
    {
        // Arrange
        var currentPage = 1;
        var totalPages = 5;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        Assert.Contains("Previous", cut.Markup);
        Assert.Contains("Next", cut.Markup);
        Assert.Contains("pagination", cut.Markup);
    }

    [Fact]
    public void Render_WithSinglePage_HidesNavigation()
    {
        // Arrange
        var currentPage = 1;
        var totalPages = 1;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void Render_OnFirstPage_DisablesPreviousButton()
    {
        // Arrange
        var currentPage = 1;
        var totalPages = 5;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        var prevButton = cut.FindAll("button")[0];
        Assert.True(prevButton.HasAttribute("disabled"));
        Assert.Contains("disabled", prevButton.ClassName);
    }

    [Fact]
    public void Render_OnLastPage_DisablesNextButton()
    {
        // Arrange
        var currentPage = 5;
        var totalPages = 5;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        var buttons = cut.FindAll("button");
        var nextButton = buttons[buttons.Count - 1];
        Assert.True(nextButton.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_OnMiddlePage_EnablesBothButtons()
    {
        // Arrange
        var currentPage = 3;
        var totalPages = 5;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        var buttons = cut.FindAll("button");
        var prevButton = buttons[0];
        var nextButton = buttons[buttons.Count - 1];
        Assert.False(prevButton.HasAttribute("disabled"));
        Assert.False(nextButton.HasAttribute("disabled"));
    }

    [Fact]
    public void NextButton_Click_InvokesOnPageChanged()
    {
        // Arrange
        var newPage = 0;
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int page) =>
            {
                newPage = page;
                return Task.CompletedTask;
            })));

        // Act
        var buttons = cut.FindAll("button");
        var nextButton = buttons[buttons.Count - 1];
        nextButton.Click();

        // Assert
        Assert.Equal(2, newPage);
    }

    [Fact]
    public void PreviousButton_Click_InvokesOnPageChanged()
    {
        // Arrange
        var newPage = 0;
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int page) =>
            {
                newPage = page;
                return Task.CompletedTask;
            })));

        // Act
        var buttons = cut.FindAll("button");
        var prevButton = buttons[0];
        prevButton.Click();

        // Assert
        Assert.Equal(2, newPage);
    }

    [Fact]
    public void Render_WithTotalItems_DisplaysItemCount()
    {
        // Arrange
        var currentPage = 1;
        var totalPages = 3;
        var totalItems = 75;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.TotalItems, totalItems)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        Assert.Contains("75 total items", cut.Markup);
        Assert.Contains("Page 1 of 3", cut.Markup);
    }

    [Fact]
    public void Render_WithoutTotalItems_DisplaysPageNumbers()
    {
        // Arrange
        var currentPage = 2;
        var totalPages = 4;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        Assert.Contains("Page 2 of 4", cut.Markup);
        Assert.DoesNotContain("total items", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysPageNumberButtons()
    {
        // Arrange
        var currentPage = 3;
        var totalPages = 5;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        // Current page and neighboring pages should be visible
        Assert.Contains(">3<", cut.Markup); // Page 3 is current
        var markup = cut.Markup;
        // Should have page numbers around current page
        Assert.Contains("page-item active", markup); // Current page has active class
    }

    [Fact]
    public void Render_WithManyPages_DisplaysEllipsis()
    {
        // Arrange
        var currentPage = 1;
        var totalPages = 10;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        // When there are many pages, ellipsis should be displayed
        Assert.Contains("...", cut.Markup);
    }

    [Fact]
    public void Render_CurrentPageHighlighted_WithActiveClass()
    {
        // Arrange
        var currentPage = 2;
        var totalPages = 5;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("page-item active", markup);
    }

    [Fact]
    public void Render_WithLargePageNumber_DisplaysFirstAndLastPages()
    {
        // Arrange
        var currentPage = 8;
        var totalPages = 10;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, totalPages)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, (int _) => { })));

        // Assert
        // Should show first page (1) and last page (10)
        Assert.Contains(">1<", cut.Markup);
        Assert.Contains(">10<", cut.Markup);
    }
}
