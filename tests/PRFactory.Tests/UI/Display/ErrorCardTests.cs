using Bunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Web.Models;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class ErrorCardTests : ComponentTestBase
{
    private ErrorDto CreateTestError(ErrorSeverity severity = ErrorSeverity.Medium, bool isResolved = false)
    {
        return new ErrorDto
        {
            Id = Guid.NewGuid(),
            Severity = severity,
            Message = "Test error message",
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0),
            IsResolved = isResolved,
            EntityType = "TestEntity"
        };
    }

    [Fact]
    public void Render_DisplaysErrorMessage()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Test error message", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSeverityBadge()
    {
        // Arrange
        var error = CreateTestError(ErrorSeverity.High);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("badge", cut.Markup);
        Assert.Contains("High", cut.Markup);
    }

    [Theory]
    [InlineData(ErrorSeverity.Critical, "danger")]
    [InlineData(ErrorSeverity.High, "warning")]
    [InlineData(ErrorSeverity.Medium, "info")]
    [InlineData(ErrorSeverity.Low, "secondary")]
    public void Render_WithSeverity_AppliesCorrectBorderClass(ErrorSeverity severity, string expectedClass)
    {
        // Arrange
        var error = CreateTestError(severity);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains($"border-{expectedClass}", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSeverityIcon()
    {
        // Arrange
        var error = CreateTestError(ErrorSeverity.Critical);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("bi-exclamation-triangle-fill", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysCreatedAtTime()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("2024-01-15 10:30:00", cut.Markup);
        Assert.Contains("bi-clock", cut.Markup);
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
        Assert.Contains("TestEntity", cut.Markup);
        Assert.Contains("bi-tag", cut.Markup);
    }

    [Fact]
    public void Render_WithoutEntityType_DoesNotDisplayEntityTag()
    {
        // Arrange
        var error = CreateTestError();
        error.EntityType = null;

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.DoesNotContain("bi-tag", cut.Markup);
    }

    [Fact]
    public void Render_WhenResolved_DisplaysResolvedBadge()
    {
        // Arrange
        var error = CreateTestError(isResolved: true);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Resolved", cut.Markup);
        Assert.Contains("bi-check-circle", cut.Markup);
        Assert.Contains("badge bg-success", cut.Markup);
    }

    [Fact]
    public void Render_WhenNotResolved_DoesNotDisplayResolvedBadge()
    {
        // Arrange
        var error = CreateTestError(isResolved: false);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        var resolvedBadges = cut.FindAll(".badge.bg-success");
        Assert.Empty(resolvedBadges);
    }

    [Fact]
    public void Render_WhenResolvedWithResolverInfo_DisplaysResolverInfo()
    {
        // Arrange
        var error = CreateTestError(isResolved: true);
        error.ResolvedBy = "admin@example.com";
        error.ResolvedAt = new DateTime(2024, 1, 16, 14, 30, 0);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Resolved by admin@example.com", cut.Markup);
        Assert.Contains("2024-01-16 14:30:00", cut.Markup);
        Assert.Contains("bi-person-check", cut.Markup);
    }

    [Fact]
    public void Render_WithShowActionsTrue_DisplaysActionButtons()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create<Guid>(this, _ => { }))
            .Add(p => p.OnResolve, EventCallback.Factory.Create<Guid>(this, _ => { })));

        // Assert
        Assert.Contains("Details", cut.Markup);
        Assert.Contains("Resolve", cut.Markup);
    }

    [Fact]
    public void Render_WithShowActionsFalse_DoesNotDisplayActionButtons()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, false));

        // Assert
        Assert.DoesNotContain("Details", cut.Markup);
        Assert.DoesNotContain("Resolve", cut.Markup);
    }

    [Fact]
    public void Render_WhenResolved_DoesNotDisplayResolveButton()
    {
        // Arrange
        var error = CreateTestError(isResolved: true);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create<Guid>(this, _ => { }))
            .Add(p => p.OnResolve, EventCallback.Factory.Create<Guid>(this, _ => { })));

        // Assert
        Assert.DoesNotContain("Resolve", cut.Markup);
    }

    [Fact]
    public void DetailsButton_WhenClicked_InvokesOnViewDetailsCallback()
    {
        // Arrange
        Guid? clickedId = null;
        var error = CreateTestError();

        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create<Guid>(this, id => clickedId = id))
            .Add(p => p.OnResolve, EventCallback.Factory.Create<Guid>(this, _ => { })));

        // Act
        var buttons = cut.FindAll("button");
        var detailsButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Details"));
        Assert.NotNull(detailsButton);
        detailsButton.Click();

        // Assert
        Assert.NotNull(clickedId);
        Assert.Equal(error.Id, clickedId.Value);
    }

    [Fact]
    public void ResolveButton_WhenClicked_InvokesOnResolveCallback()
    {
        // Arrange
        Guid? resolvedId = null;
        var error = CreateTestError(isResolved: false);

        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create<Guid>(this, _ => { }))
            .Add(p => p.OnResolve, EventCallback.Factory.Create<Guid>(this, id => resolvedId = id)));

        // Act
        var buttons = cut.FindAll("button");
        var resolveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Resolve"));
        Assert.NotNull(resolveButton);
        resolveButton.Click();

        // Assert
        Assert.NotNull(resolvedId);
        Assert.Equal(error.Id, resolvedId.Value);
    }

    [Fact]
    public void Render_DetailsButtonHasCorrectIcon()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create<Guid>(this, _ => { })));

        // Assert
        Assert.Contains("bi-eye", cut.Markup);
    }

    [Fact]
    public void Render_ResolveButtonHasCorrectIcon()
    {
        // Arrange
        var error = CreateTestError(isResolved: false);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnResolve, EventCallback.Factory.Create<Guid>(this, _ => { })));

        // Assert
        Assert.Contains("bi-check-circle", cut.Markup);
    }

    [Fact]
    public void Render_HasCardStructure()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("class=\"card", cut.Markup);
        Assert.Contains("card-body", cut.Markup);
    }

    [Fact]
    public void Render_HasFlexLayout()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("d-flex", cut.Markup);
        Assert.Contains("justify-content-between", cut.Markup);
        Assert.Contains("align-items-start", cut.Markup);
    }

    [Fact]
    public void Render_HasMarginBottom()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("mb-3", cut.Markup);
    }

    [Fact]
    public void Render_WithOnlyOnViewDetails_ShowsOnlyDetailsButton()
    {
        // Arrange
        var error = CreateTestError(isResolved: false);

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create<Guid>(this, _ => { })));

        // Assert
        Assert.Contains("Details", cut.Markup);
        Assert.DoesNotContain("Resolve", cut.Markup);
    }

    [Fact]
    public void Render_WithAllParameters_DisplaysAllElements()
    {
        // Arrange
        var error = new ErrorDto
        {
            Id = Guid.NewGuid(),
            Severity = ErrorSeverity.Critical,
            Message = "Critical error occurred",
            CreatedAt = DateTime.Now,
            EntityType = "Ticket",
            IsResolved = true,
            ResolvedBy = "admin",
            ResolvedAt = DateTime.Now
        };

        // Act
        var cut = RenderComponent<ErrorCard>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.ShowActions, true)
            .Add(p => p.OnViewDetails, EventCallback.Factory.Create<Guid>(this, _ => { })));

        // Assert
        Assert.Contains("Critical error occurred", cut.Markup);
        Assert.Contains("Critical", cut.Markup);
        Assert.Contains("Resolved", cut.Markup);
        Assert.Contains("Ticket", cut.Markup);
        Assert.Contains("Details", cut.Markup);
    }
}
