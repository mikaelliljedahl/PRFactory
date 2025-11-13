using Bunit;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using static PRFactory.Web.UI.Display.ReviewerAvatar;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class ReviewerAvatarTests : ComponentTestBase
{
    [Fact]
    public void Render_WithAvatarUrl_DisplaysImage()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.AvatarUrl, "https://example.com/avatar.jpg")
            .Add(p => p.DisplayName, "John Doe"));

        // Assert
        Assert.Contains("<img", cut.Markup);
        Assert.Contains("https://example.com/avatar.jpg", cut.Markup);
        Assert.Contains("alt=\"John Doe\"", cut.Markup);
    }

    [Fact]
    public void Render_WithoutAvatarUrl_DisplaysInitials()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "John Doe"));

        // Assert
        Assert.DoesNotContain("<img", cut.Markup);
        Assert.Contains("JD", cut.Markup);
    }

    [Fact]
    public void Render_WithSingleWordName_UsesTwoCharacters()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Alice"));

        // Assert
        Assert.Contains("AL", cut.Markup);
    }

    [Fact]
    public void Render_WithTwoWordName_UsesFirstLettersOfEachWord()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Alice Smith"));

        // Assert
        Assert.Contains("AS", cut.Markup);
    }

    [Fact]
    public void Render_WithThreeWordName_UsesFirstTwoInitials()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Alice Jane Smith"));

        // Assert
        Assert.Contains("AJ", cut.Markup);
    }

    [Fact]
    public void Render_WithSingleCharacterName_UsesSingleCharacter()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "A"));

        // Assert
        Assert.Contains("A", cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyName_ShowsQuestionMark()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, ""));

        // Assert
        Assert.Contains("?", cut.Markup);
    }

    [Fact]
    public void Render_WithWhitespaceName_ShowsQuestionMark()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "   "));

        // Assert
        Assert.Contains("?", cut.Markup);
    }

    [Theory]
    [InlineData(AvatarSize.Small, "avatar-sm")]
    [InlineData(AvatarSize.Medium, "avatar-md")]
    [InlineData(AvatarSize.Large, "avatar-lg")]
    public void Render_WithSize_AppliesCorrectSizeClass(AvatarSize size, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test")
            .Add(p => p.Size, size));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_DefaultSize_IsMedium()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test"));

        // Assert
        Assert.Contains("avatar-md", cut.Markup);
    }

    [Fact]
    public void Render_WithStatusBadge_DisplaysBadge()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test")
            .Add(p => p.StatusBadgeText, "2")
            .Add(p => p.StatusBadgeColor, "danger"));

        // Assert
        Assert.Contains("badge", cut.Markup);
        Assert.Contains("bg-danger", cut.Markup);
        Assert.Contains("2", cut.Markup);
    }

    [Fact]
    public void Render_WithoutStatusBadge_DoesNotDisplayBadge()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test"));

        // Assert
        var badges = cut.FindAll(".badge");
        Assert.Empty(badges);
    }

    [Fact]
    public void Render_StatusBadgeWithImage_DisplaysBadge()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.AvatarUrl, "https://example.com/avatar.jpg")
            .Add(p => p.DisplayName, "Test")
            .Add(p => p.StatusBadgeText, "Active")
            .Add(p => p.StatusBadgeColor, "success"));

        // Assert
        Assert.Contains("badge", cut.Markup);
        Assert.Contains("bg-success", cut.Markup);
        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public void Render_WithBackgroundColor_AppliesColorClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test")
            .Add(p => p.BackgroundColor, "danger"));

        // Assert
        Assert.Contains("bg-danger", cut.Markup);
    }

    [Fact]
    public void Render_DefaultBackgroundColor_IsPrimary()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test"));

        // Assert
        Assert.Contains("bg-primary", cut.Markup);
    }

    [Fact]
    public void Render_WithInitials_HasRoundedCircle()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test"));

        // Assert
        Assert.Contains("rounded-circle", cut.Markup);
    }

    [Fact]
    public void Render_WithImage_HasRoundedCircle()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.AvatarUrl, "https://example.com/avatar.jpg")
            .Add(p => p.DisplayName, "Test"));

        // Assert
        Assert.Contains("rounded-circle", cut.Markup);
    }

    [Fact]
    public void Render_WithInitials_HasCorrectStyling()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test"));

        // Assert
        Assert.Contains("d-flex", cut.Markup);
        Assert.Contains("align-items-center", cut.Markup);
        Assert.Contains("justify-content-center", cut.Markup);
        Assert.Contains("text-white", cut.Markup);
        Assert.Contains("fw-bold", cut.Markup);
    }

    [Fact]
    public void Render_WithImage_HasObjectFitCover()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.AvatarUrl, "https://example.com/avatar.jpg")
            .Add(p => p.DisplayName, "Test"));

        // Assert
        Assert.Contains("object-fit: cover", cut.Markup);
    }

    [Fact]
    public void Render_InitialsAreUpperCase()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "john doe"));

        // Assert
        Assert.Contains("JD", cut.Markup);
        Assert.DoesNotContain("jd", cut.Markup);
    }

    [Fact]
    public void Render_HandlesNameWithExtraWhitespace()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "  John   Doe  "));

        // Assert
        Assert.Contains("JD", cut.Markup);
    }

    [Fact]
    public void Render_StatusBadgeHasWhiteBorder()
    {
        // Arrange & Act
        var cut = RenderComponent<ReviewerAvatar>(parameters => parameters
            .Add(p => p.DisplayName, "Test")
            .Add(p => p.StatusBadgeText, "1")
            .Add(p => p.StatusBadgeColor, "info"));

        // Assert
        Assert.Contains("border: 2px solid white", cut.Markup);
    }
}
