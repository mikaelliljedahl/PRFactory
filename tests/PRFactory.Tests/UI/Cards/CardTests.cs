using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Cards;
using Xunit;

namespace PRFactory.Tests.UI.Cards;

public class CardTests : ComponentTestBase
{
    [Fact]
    public void Render_WithoutParameters_RendersEmptyCard()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>();

        // Assert
        Assert.Contains("class=\"card", cut.Markup);
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Test Card"));

        // Assert
        Assert.Contains("Test Card", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Settings")
            .Add(p => p.Icon, "gear"));

        // Assert
        Assert.Contains("bi-gear", cut.Markup);
    }

    [Fact]
    public void Render_WithoutTitle_DoesNotRenderHeader()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddContent(0, "Body content");
            }));

        // Assert
        Assert.DoesNotContain("card-header", cut.Markup);
    }

    [Fact]
    public void Render_WithChildContent_DisplaysBodyContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<p>Body content</p>");
            }));

        // Assert
        Assert.Contains("card-body", cut.Markup);
        Assert.Contains("Body content", cut.Markup);
    }

    [Fact]
    public void Render_WithFooterContent_DisplaysFooter()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.FooterContent, builder =>
            {
                builder.AddMarkupContent(0, "<p>Footer content</p>");
            }));

        // Assert
        Assert.Contains("card-footer", cut.Markup);
        Assert.Contains("Footer content", cut.Markup);
    }

    [Fact]
    public void Render_WithHeaderContent_RendersCustomHeader()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.HeaderContent, builder =>
            {
                builder.AddMarkupContent(0, "<div id=\"custom-header\">Custom Header</div>");
            }));

        // Assert
        Assert.Contains("card-header", cut.Markup);
        Assert.Contains("custom-header", cut.Markup);
        Assert.Contains("Custom Header", cut.Markup);
    }

    [Fact]
    public void Render_WithHeaderContent_IgnoresTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "This should be ignored")
            .Add(p => p.HeaderContent, builder =>
            {
                builder.AddMarkupContent(0, "<div>Custom Header</div>");
            }));

        // Assert
        Assert.DoesNotContain("This should be ignored", cut.Markup);
        Assert.Contains("Custom Header", cut.Markup);
    }

    [Theory]
    [InlineData(CardVariant.Primary, "bg-primary text-white")]
    [InlineData(CardVariant.Secondary, "bg-secondary text-white")]
    [InlineData(CardVariant.Success, "bg-success text-white")]
    [InlineData(CardVariant.Danger, "bg-danger text-white")]
    [InlineData(CardVariant.Warning, "bg-warning text-dark")]
    [InlineData(CardVariant.Info, "bg-info text-white")]
    public void Render_WithVariant_AppliesCorrectHeaderClass(CardVariant variant, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Variant, variant));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WithDefaultVariant_HasNoHeaderColorClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Variant, CardVariant.Default));

        // Assert
        Assert.DoesNotContain("bg-primary", cut.Markup);
        Assert.DoesNotContain("bg-secondary", cut.Markup);
        Assert.DoesNotContain("bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.AdditionalClass, "custom-class"));

        // Assert
        Assert.Contains("custom-class", cut.Markup);
    }

    [Fact]
    public void Render_DefaultAdditionalClass_IsMb3()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>();

        // Assert
        Assert.Contains("mb-3", cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyAdditionalClass_DoesNotAddMargin()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.AdditionalClass, ""));

        // Assert
        Assert.DoesNotContain("mb-3", cut.Markup);
    }

    [Fact]
    public void Render_TitleInH4Tag()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Test Card"));

        // Assert
        Assert.Contains("<h4 class=\"mb-0\">", cut.Markup);
    }

    [Fact]
    public void Render_WithoutChildContent_DoesNotRenderBody()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.DoesNotContain("card-body", cut.Markup);
    }

    [Fact]
    public void Render_WithoutFooterContent_DoesNotRenderFooter()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.DoesNotContain("card-footer", cut.Markup);
    }

    [Fact]
    public void Render_WithAllSections_DisplaysAllParts()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Card Title")
            .Add(p => p.Icon, "star")
            .Add(p => p.Variant, CardVariant.Primary)
            .Add(p => p.ChildContent, builder =>
            {
                builder.AddMarkupContent(0, "<p>Body</p>");
            })
            .Add(p => p.FooterContent, builder =>
            {
                builder.AddMarkupContent(0, "<p>Footer</p>");
            }));

        // Assert
        Assert.Contains("card-header", cut.Markup);
        Assert.Contains("Card Title", cut.Markup);
        Assert.Contains("bi-star", cut.Markup);
        Assert.Contains("bg-primary", cut.Markup);
        Assert.Contains("card-body", cut.Markup);
        Assert.Contains("Body", cut.Markup);
        Assert.Contains("card-footer", cut.Markup);
        Assert.Contains("Footer", cut.Markup);
    }
}
