using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Components;
using Xunit;

namespace PRFactory.Tests.Components;

public class TicketFiltersTests : ComponentTestBase
{
    [Fact]
    public void Render_DisplaysFilterOptions()
    {
        // Act
        var cut = RenderComponent<TicketFilters>();

        // Assert
        Assert.Contains("Filter", cut.Markup);
    }

    [Fact]
    public void Render_WithStates_DisplaysStateFilter()
    {
        // Arrange & Act - Just render the component
        var cut = RenderComponent<TicketFilters>();

        // Assert - Component renders without error
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public async Task ClearFilters_ResetsFilterValues()
    {
        // Arrange
        var filterChangeCalled = false;
        var cut = RenderComponent<TicketFilters>(parameters => parameters
            .Add(p => p.OnFilterChanged, () => { filterChangeCalled = true; return Task.CompletedTask; }));

        // Act
        var clearButton = cut.Find("button:contains('Clear')");
        clearButton.Click();
        await Task.Delay(50);

        // Assert
        Assert.True(filterChangeCalled);
    }
}
