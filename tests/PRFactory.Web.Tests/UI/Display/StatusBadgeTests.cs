using Bunit;
using Xunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.UI.Display;

namespace PRFactory.Web.Tests.UI.Display;

/// <summary>
/// Tests for StatusBadge component
/// </summary>
public class StatusBadgeTests : TestContext
{
    [Fact]
    public void Render_WithState_DisplaysStatusText()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Triggered));

        // Assert
        Assert.Contains("Getting Started", cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Triggered, "Getting Started")]
    [InlineData(WorkflowState.Analyzing, "Analyzing Code")]
    [InlineData(WorkflowState.Planning, "Creating Plan")]
    [InlineData(WorkflowState.Implementing, "Building Code")]
    [InlineData(WorkflowState.Completed, "Completed")]
    [InlineData(WorkflowState.Failed, "Failed")]
    public void Render_WithFriendlyNames_DisplaysFriendlyText(WorkflowState state, string expectedText)
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.UseFriendlyNames, true));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Triggered, "Triggered")]
    [InlineData(WorkflowState.Analyzing, "Analyzing Repository")]
    [InlineData(WorkflowState.Planning, "Planning")]
    [InlineData(WorkflowState.Implementing, "Implementing")]
    public void Render_WithTechnicalNames_DisplaysTechnicalText(WorkflowState state, string expectedText)
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.UseFriendlyNames, false));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }

    [Fact]
    public void Render_WithCustomText_DisplaysCustomText()
    {
        // Arrange
        var customText = "Custom Status";

        // Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Triggered)
            .Add(p => p.CustomText, customText));

        // Assert
        Assert.Contains(customText, cut.Markup);
        Assert.DoesNotContain("Getting Started", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange
        var icon = "check-circle";

        // Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Completed)
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains($"bi-{icon}", cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Completed, "bg-success")]
    [InlineData(WorkflowState.Failed, "bg-danger")]
    [InlineData(WorkflowState.ImplementationFailed, "bg-danger")]
    [InlineData(WorkflowState.Cancelled, "bg-secondary")]
    [InlineData(WorkflowState.AwaitingAnswers, "bg-warning")]
    [InlineData(WorkflowState.PRCreated, "bg-info")]
    public void Render_WithState_AppliesCorrectBadgeColor(WorkflowState state, string expectedClass)
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, state));

        // Assert
        var badge = cut.Find(".badge");
        Assert.Contains(expectedClass, badge.ClassName);
    }

    [Fact]
    public void Render_AlwaysHasBadgeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Triggered));

        // Assert
        var badge = cut.Find(".badge");
        Assert.NotNull(badge);
    }

    [Fact]
    public void Render_ByDefault_UsesFriendlyNames()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Triggered));

        // Assert
        Assert.Contains("Getting Started", cut.Markup);
    }
}
