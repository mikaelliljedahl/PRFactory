using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Web.UI.Display;
using Xunit;

namespace PRFactory.Tests.UI.Display;

public class StatusBadgeTests : ComponentTestBase
{
    [Fact]
    public void Render_WithState_DisplaysFriendlyName()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Triggered));

        // Assert
        Assert.Contains("Getting Started", cut.Markup);
    }

    [Fact]
    public void Render_WithUseFriendlyNamesFalse_DisplaysTechnicalName()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Triggered)
            .Add(p => p.UseFriendlyNames, false));

        // Assert
        Assert.Contains("Triggered", cut.Markup);
        Assert.DoesNotContain("Getting Started", cut.Markup);
    }

    [Fact]
    public void Render_WithCustomText_DisplaysCustomText()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Completed)
            .Add(p => p.CustomText, "All Done!"));

        // Assert
        Assert.Contains("All Done!", cut.Markup);
        Assert.DoesNotContain("Completed", cut.Markup);
    }

    [Fact]
    public void Render_WithIcon_DisplaysIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Completed)
            .Add(p => p.Icon, "check-circle"));

        // Assert
        Assert.Contains("bi-check-circle", cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Completed, "bg-success")]
    [InlineData(WorkflowState.Failed, "bg-danger")]
    [InlineData(WorkflowState.ImplementationFailed, "bg-danger")]
    [InlineData(WorkflowState.Cancelled, "bg-secondary")]
    [InlineData(WorkflowState.AwaitingAnswers, "bg-warning")]
    [InlineData(WorkflowState.PlanUnderReview, "bg-warning")]
    [InlineData(WorkflowState.InReview, "bg-warning")]
    [InlineData(WorkflowState.TicketUpdateUnderReview, "bg-warning")]
    [InlineData(WorkflowState.PRCreated, "bg-info")]
    [InlineData(WorkflowState.Triggered, "bg-secondary")]
    [InlineData(WorkflowState.PlanRejected, "bg-danger")]
    [InlineData(WorkflowState.TicketUpdateRejected, "bg-danger")]
    [InlineData(WorkflowState.PlanApproved, "bg-success")]
    [InlineData(WorkflowState.TicketUpdateApproved, "bg-success")]
    public void Render_WithState_ShowsCorrectBadgeColor(WorkflowState state, string expectedColorClass)
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, state));

        // Assert
        Assert.Contains(expectedColorClass, cut.Markup);
    }

    [Fact]
    public void Render_HasBadgeClass()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Completed));

        // Assert
        Assert.Contains("class=\"badge", cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Triggered, "Getting Started")]
    [InlineData(WorkflowState.Analyzing, "Analyzing Code")]
    [InlineData(WorkflowState.TicketUpdateGenerated, "Update Ready")]
    [InlineData(WorkflowState.QuestionsPosted, "Questions Asked")]
    [InlineData(WorkflowState.AnswersReceived, "Answers Received")]
    [InlineData(WorkflowState.Planning, "Creating Plan")]
    [InlineData(WorkflowState.Implementing, "Building Code")]
    [InlineData(WorkflowState.PRCreated, "PR Created")]
    public void Render_FriendlyNames_MatchExpectedText(WorkflowState state, string expectedText)
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.UseFriendlyNames, true));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Analyzing, "Analyzing Repository")]
    [InlineData(WorkflowState.TicketUpdateGenerated, "Ticket Update Generated")]
    [InlineData(WorkflowState.Planning, "Planning")]
    [InlineData(WorkflowState.Implementing, "Implementing")]
    public void Render_TechnicalNames_MatchExpectedText(WorkflowState state, string expectedText)
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.UseFriendlyNames, false));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }

    [Fact]
    public void Render_UnknownState_DisplaysStateToString()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, (WorkflowState)999));

        // Assert
        Assert.Contains("999", cut.Markup);
    }

    [Fact]
    public void Render_DefaultUseFriendlyNames_IsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.State, WorkflowState.Completed));

        // Assert
        Assert.Contains("Completed", cut.Markup);
        Assert.DoesNotContain("Completed", cut.Markup.Replace("Completed", ""));
    }
}
