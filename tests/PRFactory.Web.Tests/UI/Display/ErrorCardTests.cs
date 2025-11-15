using Bunit;
using Xunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for ErrorCard component
/// </summary>
public class ErrorCardTests : TestContext
{
    private ErrorDto CreateTestError(
        ErrorSeverity severity = ErrorSeverity.Medium,
        bool isResolved = false,
        string? resolvedBy = null)
    {
        return new ErrorDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Severity = severity,
            Message = "Test error message",
            StackTrace = "Stack trace here",
            EntityType = "Ticket",
            EntityId = Guid.NewGuid(),
            IsResolved = isResolved,
            ResolvedAt = isResolved ? DateTime.UtcNow : null,
            ResolvedBy = resolvedBy,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }

    [Fact]
    public void Render_WithError_DisplaysMessage()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Test error message", cut.Markup);
    }

    [Theory]
    [InlineData(ErrorSeverity.Critical, "bg-danger")]
    [InlineData(ErrorSeverity.High, "bg-warning")]
    [InlineData(ErrorSeverity.Medium, "bg-info")]
    [InlineData(ErrorSeverity.Low, "bg-secondary")]
    public void Render_WithSeverity_DisplaysCorrectBadgeColor(ErrorSeverity severity, string expectedClass)
    {
        // Arrange
        var error = CreateTestError(severity: severity);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains(expectedClass, cut.Markup);
    }

    [Fact]
    public void Render_WhenResolved_ShowsResolvedBadge()
    {
        // Arrange
        var error = CreateTestError(isResolved: true, resolvedBy: "John Doe");

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Resolved", cut.Markup);
        Assert.Contains("bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotResolved_DoesNotShowResolvedBadge()
    {
        // Arrange
        var error = CreateTestError(isResolved: false);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        var resolvedBadges = cut.Markup.Split("Resolved").Length - 1;
        Assert.Equal(0, resolvedBadges);
    }

    [Fact]
    public void Render_WithEntityType_DisplaysEntityType()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Ticket", cut.Markup);
    }

    [Fact]
    public void Render_WhenShowActionsTrue_ShowsActionButtons()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, _ => { }));

        // Assert
        Assert.Contains("Details", cut.Markup);
    }

    [Fact]
    public void Render_WhenShowActionsFalse_DoesNotShowActionButtons()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, false));

        // Assert
        Assert.DoesNotContain("Details", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotResolvedWithOnResolve_ShowsResolveButton()
    {
        // Arrange
        var error = CreateTestError(isResolved: false);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnResolve, _ => { }));

        // Assert
        Assert.Contains("Resolve", cut.Markup);
    }

    [Fact]
    public void Render_WhenResolvedWithResolvedBy_DisplaysResolver()
    {
        // Arrange
        var error = CreateTestError(isResolved: true, resolvedBy: "Jane Smith");

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Jane Smith", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysFormattedTimestamp()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains(error.FormattedCreatedAt, cut.Markup);
    }
}
