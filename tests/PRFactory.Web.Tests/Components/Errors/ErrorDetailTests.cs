using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using PRFactory.Web.Components.Errors;
using PRFactory.Web.Models;
using PRFactory.Web.UI.Display;
using PRFactory.Domain.ValueObjects;
using Radzen.Blazor;

namespace PRFactory.Web.Tests.Components.Errors;

/// <summary>
/// Tests for ErrorDetail component
/// Verifies rendering of error message, stack trace, metadata, and resolution status.
/// </summary>
public class ErrorDetailTests : TestContext
{
    private ErrorDto CreateTestError(
        ErrorSeverity severity = ErrorSeverity.High,
        bool isResolved = false,
        string? stackTrace = null,
        string? contextData = null)
    {
        return new ErrorDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Severity = severity,
            Message = "Test error message",
            StackTrace = stackTrace ?? "at TestClass.TestMethod() in TestFile.cs:line 42",
            EntityType = "Ticket",
            EntityId = Guid.NewGuid(),
            ContextData = contextData,
            IsResolved = isResolved,
            ResolvedAt = isResolved ? DateTime.UtcNow.AddHours(-1) : null,
            ResolvedBy = isResolved ? "test@example.com" : null,
            ResolutionNotes = isResolved ? "Fixed by updating configuration" : null,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
    }

    [Fact]
    public void Render_WithNullError_DisplaysErrorNotFoundMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, null));

        // Assert
        Assert.Contains("Error not found", cut.Markup);
        Assert.Contains("alert alert-warning", cut.Markup);
        Assert.Contains("exclamation-triangle", cut.Markup); // Warning icon
    }

    [Fact]
    public void Render_WithError_DisplaysCardHeader()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("card-header", cut.Markup);
        Assert.Contains("Error Details", cut.Markup);
    }

    [Fact]
    public void Render_WithCriticalError_DisplaysDangerHeaderBackground()
    {
        // Arrange
        var error = CreateTestError(severity: ErrorSeverity.Critical);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("bg-danger", cut.Markup);
        Assert.Contains("text-white", cut.Markup);
    }

    [Fact]
    public void Render_WithResolvedError_DisplaysResolvedBadge()
    {
        // Arrange
        var error = CreateTestError(isResolved: true);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("badge bg-success", cut.Markup);
        Assert.Contains("Resolved", cut.Markup);
        Assert.Contains("check-circle", cut.Markup); // Check circle icon
    }

    [Fact]
    public void Render_WithUnresolvedError_DoesNotDisplayResolvedBadge()
    {
        // Arrange
        var error = CreateTestError(isResolved: false);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        var markup = cut.Markup;
        Assert.DoesNotContain("badge bg-success", markup);
    }

    [Fact]
    public void Render_DisplaysOverviewTab()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Overview", cut.Markup);
        var tabs = cut.FindComponents<Radzen.Blazor.RadzenTabsItem>();
        Assert.NotEmpty(tabs);
    }

    [Fact]
    public void Render_DisplaysMessageInOverviewTab()
    {
        // Arrange
        var message = "Critical database connection failure";
        var error = CreateTestError();
        error.Message = message;

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Message", cut.Markup);
        Assert.Contains(message, cut.Markup);
    }

    [Fact]
    public void Render_DisplaysSeverityBadgeInOverviewTab()
    {
        // Arrange
        var error = CreateTestError(severity: ErrorSeverity.Critical);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Severity", cut.Markup);
        Assert.Contains("badge", cut.Markup);
        Assert.Contains("Critical", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysCreatedAtInOverviewTab()
    {
        // Arrange
        var createdTime = new DateTime(2024, 11, 15, 10, 30, 0);
        var error = CreateTestError();
        error.CreatedAt = createdTime;

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Created At", cut.Markup);
    }

    [Fact]
    public void Render_WithEntityType_DisplaysRelatedEntity()
    {
        // Arrange
        var error = CreateTestError();
        error.EntityType = "Repository";
        error.EntityId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Related Entity", cut.Markup);
        Assert.Contains("Repository", cut.Markup);
        Assert.Contains(error.EntityId!.ToString(), cut.Markup);
    }

    [Fact]
    public void Render_WithoutEntityType_DoesNotDisplayRelatedEntity()
    {
        // Arrange
        var error = CreateTestError();
        error.EntityType = null;
        error.EntityId = null;

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.DoesNotContain("Related Entity", cut.Markup);
    }

    [Fact]
    public void Render_WithResolvedError_DisplaysResolvedAtInOverviewTab()
    {
        // Arrange
        var resolvedTime = new DateTime(2024, 11, 15, 14, 30, 0);
        var error = CreateTestError(isResolved: true);
        error.ResolvedAt = resolvedTime;

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Resolved At", cut.Markup);
    }

    [Fact]
    public void Render_WithResolvedError_DisplaysResolvedByInOverviewTab()
    {
        // Arrange
        var error = CreateTestError(isResolved: true);
        error.ResolvedBy = "admin@example.com";

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Resolved By", cut.Markup);
        Assert.Contains("admin@example.com", cut.Markup);
    }

    [Fact]
    public void Render_WithResolvedError_DisplaysResolutionNotesInAlert()
    {
        // Arrange
        var notes = "Fixed by updating the database schema";
        var error = CreateTestError(isResolved: true);
        error.ResolutionNotes = notes;

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Resolution Notes", cut.Markup);
        Assert.Contains(notes, cut.Markup);
        Assert.Contains("alert alert-success", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysStackTraceTab()
    {
        // Arrange
        var error = CreateTestError(stackTrace: "at TestClass.TestMethod() in TestFile.cs:line 42");

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Stack Trace", cut.Markup);
    }

    [Fact]
    public void Render_WithStackTrace_DisplaysStackTraceViewer()
    {
        // Arrange
        var stackTrace = "System.NullReferenceException: Object reference not set to an instance of an object.\n  at MyClass.MyMethod()";
        var error = CreateTestError(stackTrace: stackTrace);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Stack Trace", cut.Markup);
        // StackTraceViewer component should be present
        var stackTraceComponents = cut.FindComponents<StackTraceViewer>();
        Assert.NotEmpty(stackTraceComponents);
    }

    [Fact]
    public void Render_WithContextData_DisplaysContextDataTab()
    {
        // Arrange
        var contextData = """{"userId":"123","operation":"create","status":"failed"}""";
        var error = CreateTestError(contextData: contextData);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Context Data", cut.Markup);
    }

    [Fact]
    public void Render_WithContextData_DisplaysContextInPreTag()
    {
        // Arrange
        var contextData = """{"userId":"123","operation":"create"}""";
        var error = CreateTestError(contextData: contextData);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("Context Data", cut.Markup);
        Assert.Contains("bg-light p-3 rounded", cut.Markup);
    }

    [Fact]
    public void Render_WithoutContextData_DoesNotDisplayContextDataTab()
    {
        // Arrange
        var error = CreateTestError(contextData: null);

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        // Context Data tab should not appear if contextData is null
        Assert.DoesNotContain("Context Data", cut.Markup);
    }

    [Fact]
    public void Render_WithRelatedErrors_DisplaysRelatedErrorsTab()
    {
        // Arrange
        var error = CreateTestError();
        var relatedErrors = new List<ErrorDto>
        {
            CreateTestError(severity: ErrorSeverity.Low),
            CreateTestError(severity: ErrorSeverity.Medium)
        };

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.RelatedErrors, relatedErrors));

        // Assert
        Assert.Contains("Related Errors", cut.Markup);
    }

    [Fact]
    public void Render_WithoutRelatedErrors_DoesNotDisplayRelatedErrorsTab()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.RelatedErrors, null));

        // Assert
        Assert.DoesNotContain("Related Errors", cut.Markup);
    }

    [Fact]
    public void Render_WithEmptyRelatedErrors_DoesNotDisplayRelatedErrorsTab()
    {
        // Arrange
        var error = CreateTestError();
        var relatedErrors = new List<ErrorDto>();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error)
            .Add(p => p.RelatedErrors, relatedErrors));

        // Assert
        Assert.DoesNotContain("Related Errors", cut.Markup);
    }

    [Fact]
    public void Render_AllSeverities_DisplayCorrectBadgeClasses()
    {
        // Test Critical
        var criticalError = CreateTestError(severity: ErrorSeverity.Critical);
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, criticalError));
        Assert.Contains("bg-danger", cut.Markup);

        // Test High
        var highError = CreateTestError(severity: ErrorSeverity.High);
        cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, highError));
        Assert.Contains("bg-warning", cut.Markup);

        // Test Medium
        var mediumError = CreateTestError(severity: ErrorSeverity.Medium);
        cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, mediumError));
        Assert.Contains("bg-info", cut.Markup);

        // Test Low
        var lowError = CreateTestError(severity: ErrorSeverity.Low);
        cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, lowError));
        Assert.Contains("bg-secondary", cut.Markup);
    }

    [Fact]
    public void Render_WithCardBody_ProperlyStructured()
    {
        // Arrange
        var error = CreateTestError();

        // Act
        var cut = RenderComponent<ErrorDetail>(parameters => parameters
            .Add(p => p.Error, error));

        // Assert
        Assert.Contains("card", cut.Markup);
        Assert.Contains("card-body", cut.Markup);
        Assert.Contains("RadzenTabs", cut.Markup);
    }
}
