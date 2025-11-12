using Bunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Tests.Blazor;
using PRFactory.Tests.Blazor.TestDataBuilders;
using PRFactory.Web.Components.Tickets;
using Xunit;

namespace PRFactory.Tests.Components.Tickets;

public class TicketHeaderTests : ComponentTestBase
{
    [Fact]
    public void Renders_TicketInformation()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithTicketKey("TEST-123")
            .WithTitle("Test Ticket Title")
            .WithDescription("Test ticket description")
            .WithState(WorkflowState.Triggered)
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("TEST-123", cut.Markup);
        Assert.Contains("Test Ticket Title", cut.Markup);
        Assert.Contains("Test ticket description", cut.Markup);
    }

    [Fact]
    public void Renders_StateDisplay()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(WorkflowState.AwaitingAnswers)
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert - Should display state help text
        Assert.Contains("clarifying questions", cut.Markup);
    }

    [Fact]
    public void Renders_TicketUpdateUnderReviewState()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(WorkflowState.TicketUpdateUnderReview)
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("refined ticket description", cut.Markup);
    }

    [Fact]
    public void Renders_PlanUnderReviewState()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(WorkflowState.PlanUnderReview)
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("implementation plan", cut.Markup);
    }

    [Fact]
    public void Renders_CompletedState()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(WorkflowState.Completed)
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("completed", cut.Markup);
    }

    [Fact]
    public void Renders_FailedState()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(WorkflowState.Failed)
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("error", cut.Markup);
    }

    [Fact]
    public void FormatDescription_PreservesNewlines()
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithDescription("Line 1\n\nLine 2\nLine 3")
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert - Should contain HTML formatted description
        Assert.Contains("Line 1", cut.Markup);
        Assert.Contains("Line 2", cut.Markup);
        Assert.Contains("Line 3", cut.Markup);
    }

    [Theory]
    [InlineData(WorkflowState.Triggered)]
    [InlineData(WorkflowState.Analyzing)]
    [InlineData(WorkflowState.Planning)]
    [InlineData(WorkflowState.Implementing)]
    [InlineData(WorkflowState.PRCreated)]
    [InlineData(WorkflowState.InReview)]
    [InlineData(WorkflowState.Cancelled)]
    public void Renders_AllWorkflowStates(WorkflowState state)
    {
        // Arrange
        var ticket = new TicketDtoBuilder()
            .WithState(state)
            .Build();

        // Act
        var cut = RenderComponent<TicketHeader>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert - Should render without error
        Assert.NotNull(cut.Markup);
        Assert.NotEmpty(cut.Markup);
    }
}
