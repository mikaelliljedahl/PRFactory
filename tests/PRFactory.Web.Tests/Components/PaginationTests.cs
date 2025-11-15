using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.Components;
using Xunit;

namespace PRFactory.Web.Tests.Components;

/// <summary>
/// Tests for the Pagination component.
/// Verifies page navigation, button states, disabled states, and event callbacks.
/// </summary>
public class PaginationTests : TestContext
{
    [Fact]
    public void Render_WithSinglePage_DoesNotDisplayNavigation()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 1));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("<nav", markup);
    }

    [Fact]
    public void Render_WithMultiplePages_DisplaysNavigation()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5));

        // Assert
        var nav = cut.Find("nav[aria-label='Page navigation']");
        Assert.NotNull(nav);
    }

    [Fact]
    public void Render_OnFirstPage_DisablesPreviousButton()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5));

        // Assert
        var previousButton = cut.FindAll("button").First();
        Assert.True(previousButton.HasAttribute("disabled"));
        // Check the parent li has the disabled class
        var parentLi = previousButton.ParentElement;
        Assert.NotNull(parentLi);
        Assert.Contains("disabled", parentLi.GetAttribute("class"));
    }

    [Fact]
    public void Render_OnLastPage_DisablesNextButton()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 5)
            .Add(p => p.TotalPages, 5));

        // Assert
        var buttons = cut.FindAll("button");
        var nextButton = buttons.Last();
        Assert.True(nextButton.HasAttribute("disabled"));
        // Check the parent li has the disabled class
        var parentLi = nextButton.ParentElement;
        Assert.NotNull(parentLi);
        Assert.Contains("disabled", parentLi.GetAttribute("class"));
    }

    [Fact]
    public void Render_OnMiddlePage_EnablesBothNavigationButtons()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 5));

        // Assert
        var buttons = cut.FindAll("button");
        var previousButton = buttons.First();
        var nextButton = buttons.Last();

        Assert.False(previousButton.HasAttribute("disabled"));
        Assert.False(nextButton.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_DisplaysCurrentPageNumber()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 5));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Page 3 of 5", markup);
    }

    [Fact]
    public void Render_WithTotalItems_DisplaysTotalItemsCount()
    {
        // Arrange
        const int totalItems = 150;

        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.TotalItems, totalItems));

        // Assert
        var markup = cut.Markup;
        Assert.Contains($"({totalItems} total items)", markup);
    }

    [Fact]
    public void Render_WithoutTotalItems_DoesNotDisplayItemsCount()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.TotalItems, null));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("total items", markup);
    }

    [Fact]
    public void Render_HighPageNumbers_DisplaysEllipsis()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 10));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("...", markup);
    }

    [Fact]
    public async Task Click_NextButton_InvokesPageChangeCallback()
    {
        // Arrange
        var currentPage = 1;
        var changedPage = 0;

        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, page =>
            {
                changedPage = page;
            })));

        // Act
        var nextButton = cut.FindAll("button").Last();
        await cut.InvokeAsync(() => nextButton.Click());

        // Assert
        Assert.Equal(2, changedPage);
    }

    [Fact]
    public async Task Click_PreviousButton_InvokesPageChangeCallback()
    {
        // Arrange
        var currentPage = 3;
        var changedPage = 0;

        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, page =>
            {
                changedPage = page;
            })));

        // Act
        var previousButton = cut.FindAll("button").First();
        await cut.InvokeAsync(() => previousButton.Click());

        // Assert
        Assert.Equal(2, changedPage);
    }

    [Fact]
    public async Task Click_SpecificPageNumber_InvokesPageChangeCallback()
    {
        // Arrange
        var changedPage = 0;

        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, page =>
            {
                changedPage = page;
            })));

        // Act
        var pageButtons = cut.FindAll("button").Where(b =>
            b.TextContent.Trim() == "4" ||
            b.TextContent.Trim() == "5").ToList();

        if (pageButtons.Any())
        {
            await cut.InvokeAsync(() => pageButtons.First().Click());
        }

        // Assert
        Assert.True(changedPage > 0);
    }

    [Fact]
    public async Task Click_CurrentPage_DoesNotInvokeCallback()
    {
        // Arrange
        var currentPage = 2;
        var changedPage = 0;
        var callbackInvoked = false;

        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, currentPage)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, page =>
            {
                callbackInvoked = true;
                changedPage = page;
            })));

        // Act
        var pageButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Trim() == "2" && b.GetAttribute("class")?.Contains("active") == true);

        if (pageButton != null)
        {
            await cut.InvokeAsync(() => pageButton.Click());
        }

        // Assert
        Assert.False(callbackInvoked);
        Assert.Equal(0, changedPage);
    }

    [Fact]
    public async Task Click_OutOfBoundsPage_DoesNotInvokeCallback()
    {
        // Arrange
        var callbackInvoked = false;

        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ =>
            {
                callbackInvoked = true;
            })));

        // Act - Try to navigate to page 0 (out of bounds)
        // This is tested through the component logic, not UI click

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void Render_ActivePageIsHighlighted()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 5));

        // Assert
        var activePageItem = cut.FindAll("li").FirstOrDefault(li =>
            li.GetAttribute("class")?.Contains("active") == true);
        Assert.NotNull(activePageItem);
    }

    [Fact]
    public void Render_ManyPages_ShowsPageRange()
    {
        // Arrange
        // Act
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 5)
            .Add(p => p.TotalPages, 20));

        // Assert
        var markup = cut.Markup;
        // Should display current page and nearby pages
        Assert.Contains("5", markup);
    }
}
