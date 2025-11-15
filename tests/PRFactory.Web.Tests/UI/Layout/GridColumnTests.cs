using Bunit;
using PRFactory.Web.UI.Layout;
using Xunit;

namespace PRFactory.Web.Tests.UI.Layout;

public class GridColumnTests : TestContext
{
    [Fact]
    public void Render_WithDefaultWidth_AppliesCol12()
    {
        // Arrange & Act
        var cut = RenderComponent<GridColumn>();

        // Assert
        var div = cut.Find("div");
        Assert.Contains("col-md-12", div.ClassName);
    }

    [Theory]
    [InlineData(1, "col-md-1")]
    [InlineData(3, "col-md-3")]
    [InlineData(6, "col-md-6")]
    [InlineData(9, "col-md-9")]
    [InlineData(12, "col-md-12")]
    public void Render_WithWidth_AppliesCorrectClass(int width, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<GridColumn>(parameters => parameters
            .Add(p => p.Width, width));

        // Assert
        var div = cut.Find("div");
        Assert.Contains(expectedClass, div.ClassName);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesBothClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<GridColumn>(parameters => parameters
            .Add(p => p.Width, 6)
            .Add(p => p.AdditionalClass, "col-lg-4 mb-4"));

        // Assert
        var div = cut.Find("div");
        Assert.Contains("col-md-6", div.ClassName);
        Assert.Contains("col-lg-4", div.ClassName);
        Assert.Contains("mb-4", div.ClassName);
    }

    [Fact]
    public void Render_WithChildContent_DisplaysContent()
    {
        // Arrange & Act
        var cut = RenderComponent<GridColumn>(parameters => parameters
            .AddChildContent("<p>Test content</p>"));

        // Assert
        Assert.Contains("Test content", cut.Markup);
    }

    [Fact]
    public void Render_WithComplexChildContent_PreservesStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<GridColumn>(parameters => parameters
            .AddChildContent("<div class=\"card\"><div class=\"card-body\">Card content</div></div>"));

        // Assert
        Assert.Contains("card", cut.Markup);
        Assert.Contains("card-body", cut.Markup);
        Assert.Contains("Card content", cut.Markup);
    }

    [Fact]
    public void Render_WithoutAdditionalClass_OnlyHasWidthClass()
    {
        // Arrange & Act
        var cut = RenderComponent<GridColumn>(parameters => parameters
            .Add(p => p.Width, 4));

        // Assert
        var div = cut.Find("div");
        var classes = div.ClassName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        Assert.Single(classes);
        Assert.Equal("col-md-4", classes[0]);
    }
}
