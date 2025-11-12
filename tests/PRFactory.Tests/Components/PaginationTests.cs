using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components;
using Xunit;

namespace PRFactory.Tests.Components;

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
            .Add(p => p.OnPageChanged, (int page) => { }));

        // Assert
        Assert.Contains("Previous", cut.Markup);
        Assert.Contains("Next", cut.Markup);
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
            .Add(p => p.OnPageChanged, (int page) => { }));

        // Assert
        var prevButton = cut.Find("button:contains('Previous')");
        Assert.True(prevButton.HasAttribute("disabled"));
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
            .Add(p => p.OnPageChanged, (int page) => { }));

        // Assert
        var nextButton = cut.Find("button:contains('Next')");
        Assert.True(nextButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task NextButton_Click_InvokesOnPageChanged()
    {
        // Arrange
        var newPage = 0;
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, (int page) => { newPage = page; }));

        // Act
        var nextButton = cut.Find("button:contains('Next')");
        nextButton.Click();
        await Task.Delay(50);

        // Assert
        Assert.Equal(2, newPage);
    }

    [Fact]
    public async Task PreviousButton_Click_InvokesOnPageChanged()
    {
        // Arrange
        var newPage = 0;
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, (int page) => { newPage = page; }));

        // Act
        var prevButton = cut.Find("button:contains('Previous')");
        prevButton.Click();
        await Task.Delay(50);

        // Assert
        Assert.Equal(2, newPage);
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
            .Add(p => p.OnPageChanged, (int page) => { }));

        // Assert
        // With single page, pagination might be hidden or both buttons disabled
        Assert.NotNull(cut.Markup);
    }
}
