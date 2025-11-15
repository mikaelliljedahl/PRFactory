using Bunit;
using Xunit;
using PRFactory.Web.UI.Cards;

namespace PRFactory.Web.Tests.UI.Cards;

/// <summary>
/// Tests for Card component
/// </summary>
public class CardTests : TestContext
{
    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        // Arrange
        var title = "Card Title";

        // Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, title));

        // Assert
        Assert.Contains(title, cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange
        var icon = "gear";

        // Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Settings")
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
    }

    [Fact]
    public void Render_WithoutTitle_DoesNotRenderHeader()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .AddChildContent("<p>Body content</p>"));

        // Assert
        Assert.DoesNotContain("card-header", cut.Markup);
    }

    [Fact]
    public void Render_WithTitleAndNoIcon_ShowsTitleWithoutIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Card Title"));

        // Assert
        Assert.Contains("Card Title", cut.Markup);
        Assert.DoesNotContain("bi-", cut.Markup);
    }

    [Theory]
    [InlineData(CardVariant.Default, "")]
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
            .Add(p => p.Title, "Title")
            .Add(p => p.Variant, variant));

        // Assert
        var header = cut.Find(".card-header");
        if (!string.IsNullOrEmpty(expectedClass))
        {
            Assert.Contains(expectedClass, header.ClassName);
        }
        else
        {
            // Default variant should not have additional classes
            Assert.DoesNotContain("bg-", header.ClassName);
        }
    }

    [Fact]
    public void Render_WithChildContent_DisplaysChildContent()
    {
        // Arrange
        var content = "<p>This is card content</p>";

        // Act
        var cut = RenderComponent<Card>(parameters => parameters
            .AddChildContent(content));

        // Assert
        Assert.Contains("This is card content", cut.Markup);
        var cardBody = cut.Find(".card-body");
        Assert.NotNull(cardBody);
    }

    [Fact]
    public void Render_WithHeaderContent_DisplaysCustomHeader()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.HeaderContent, builder => builder.AddContent(0, "<div>Custom Header</div>")));

        // Assert
        Assert.Contains("Custom Header", cut.Markup);
        var cardHeader = cut.Find(".card-header");
        Assert.NotNull(cardHeader);
    }

    [Fact]
    public void Render_WithHeaderContent_OverridesTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Ignored Title")
            .Add(p => p.HeaderContent, builder => builder.AddContent(0, "<div>Custom Header</div>")));

        // Assert
        Assert.Contains("Custom Header", cut.Markup);
        Assert.DoesNotContain("Ignored Title", cut.Markup);
    }

    [Fact]
    public void Render_WithFooterContent_DisplaysFooter()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.FooterContent, builder => builder.AddContent(0, "<div>Footer Content</div>")));

        // Assert
        Assert.Contains("Footer Content", cut.Markup);
        var cardFooter = cut.Find(".card-footer");
        Assert.NotNull(cardFooter);
    }

    [Fact]
    public void Render_WithAdditionalClass_AppliesAdditionalClass()
    {
        // Arrange
        var additionalClass = "my-custom-card";

        // Act
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.AdditionalClass, additionalClass));

        // Assert
        var card = cut.Find(".card");
        Assert.Contains(additionalClass, card.ClassName);
    }

    [Fact]
    public void Render_ByDefault_HasMb3Class()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>();

        // Assert
        var card = cut.Find(".card");
        Assert.Contains("mb-3", card.ClassName);
    }

    [Fact]
    public void Render_AlwaysHasCardClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Card>();

        // Assert
        var card = cut.Find(".card");
        Assert.NotNull(card);
    }
}
