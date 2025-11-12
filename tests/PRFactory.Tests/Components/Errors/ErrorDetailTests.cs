using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Errors;
using Xunit;

namespace PRFactory.Tests.Components.Errors;

public class ErrorDetailTests : ComponentTestBase
{
    [Fact]
    public void Render_WithNullError_DisplaysEmptyState()
    {
        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, null));

        // Assert
        Assert.Contains("Select an error to view details", cut.Markup);
    }

    [Fact]
    public void Render_WithError_DisplaysErrorDetails()
    {
        // Arrange
        var error = new ErrorDtoBuilder()
            .WithMessage("Test error message")
            .WithSeverity(ErrorSeverity.Critical)
            .Build();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Test error message", cut.Markup);
        Assert.Contains("Critical", cut.Markup);
    }

    [Theory]
    [InlineData(ErrorSeverity.Critical, "danger")]
    [InlineData(ErrorSeverity.High, "warning")]
    [InlineData(ErrorSeverity.Medium, "info")]
    [InlineData(ErrorSeverity.Low, "secondary")]
    public void Render_WithSeverity_ShowsCorrectBadgeClass(ErrorSeverity severity, string expectedClass)
    {
        // Arrange
        var error = new ErrorDtoBuilder()
            .WithSeverity(severity)
            .Build();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains($"bg-{expectedClass}", cut.Markup);
    }

    [Fact]
    public void Render_WithStackTrace_DisplaysStackTrace()
    {
        // Arrange
        var stackTrace = "at MyClass.MyMethod() in file.cs:line 42";
        var error = new ErrorDtoBuilder()
            .WithStackTrace(stackTrace)
            .Build();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Stack Trace", cut.Markup);
        Assert.Contains(stackTrace, cut.Markup);
    }

    [Fact]
    public void Render_WithResolvedError_DisplaysResolutionInfo()
    {
        // Arrange
        var error = new ErrorDtoBuilder()
            .Resolved("John Doe", "Fixed by updating configuration")
            .Build();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Resolved", cut.Markup);
        Assert.Contains("John Doe", cut.Markup);
        Assert.Contains("Fixed by updating configuration", cut.Markup);
    }
}
