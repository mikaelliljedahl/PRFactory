using Bunit;
using PRFactory.Web.UI.Layout;
using Xunit;

namespace PRFactory.Web.Tests.UI.Layout;

public class GridLayoutTests : TestContext
{
    [Fact]
    public void Render_AppliesRowClass()
    {
        // Arrange & Act
        var cut = RenderComponent<GridLayout>();

        // Assert
        var div = cut.Find("div");
        Assert.Contains("row", div.ClassName);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesBothClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<GridLayout>(parameters => parameters
            .Add(p => p.AdditionalClass, "mb-4"));

        // Assert
        var div = cut.Find("div");
        Assert.Contains("row", div.ClassName);
        Assert.Contains("mb-4", div.ClassName);
    }

    [Fact]
    public void Render_WithChildContent_DisplaysContent()
    {
        // Arrange & Act
        var cut = RenderComponent<GridLayout>(parameters => parameters
            .AddChildContent("<div class=\"test-content\">Test</div>"));

        // Assert
        Assert.Contains("test-content", cut.Markup);
        Assert.Contains("Test", cut.Markup);
    }

    [Fact]
    public void Render_WithMultipleChildren_DisplaysAllChildren()
    {
        // Arrange & Act
        var cut = RenderComponent<GridLayout>(parameters => parameters
            .AddChildContent("<div>Child 1</div><div>Child 2</div>"));

        // Assert
        Assert.Contains("Child 1", cut.Markup);
        Assert.Contains("Child 2", cut.Markup);
    }

    [Fact]
    public void Render_WithoutAdditionalClass_OnlyHasRowClass()
    {
        // Arrange & Act
        var cut = RenderComponent<GridLayout>();

        // Assert
        var div = cut.Find("div");
        var classes = div.ClassName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(classes);
        Assert.Equal("row", classes[0]);
    }
}
