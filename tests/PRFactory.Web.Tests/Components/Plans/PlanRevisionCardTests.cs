using Bunit;
using Xunit;
using Moq;
using PRFactory.Web.Components.Plans;
using PRFactory.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PRFactory.Web.Tests.Components.Plans;

/// <summary>
/// Tests for PlanRevisionCard component
/// </summary>
public class PlanRevisionCardTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<ILogger<PlanRevisionCard>> _mockLogger;

    public PlanRevisionCardTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockLogger = new Mock<ILogger<PlanRevisionCard>>();

        Services.AddSingleton(_mockTicketService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }
    [Fact]
    public void PlanRevisionCard_Renders_WithTicketId()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters =>
            parameters.Add(p => p.TicketId, ticketId));

        // Assert
        Assert.Contains("Revise Plan", cut.Markup);
        Assert.Contains("Revision Instructions", cut.Markup);
        Assert.Contains("Revise Plan", cut.Markup);
        Assert.Contains("Approve Plan", cut.Markup);
    }

    [Fact]
    public void PlanRevisionCard_ReviseButton_InitiallyDisabled()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters =>
            parameters.Add(p => p.TicketId, ticketId));

        // Assert - Revise button should be disabled when textarea is empty
        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);
        Assert.True(reviseButton.HasAttribute("disabled"));
    }

    [Fact]
    public void PlanRevisionCard_ApproveButton_AlwaysEnabled()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters =>
            parameters.Add(p => p.TicketId, ticketId));

        // Assert - Approve button should always be enabled
        var buttons = cut.FindAll("button");
        var approveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Approve Plan"));
        Assert.NotNull(approveButton);
        Assert.False(approveButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task PlanRevisionCard_ApproveButton_CallsServiceAndInvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var approvedCalled = false;

        _mockTicketService
            .Setup(x => x.ApprovePlanAsync(ticketId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnApproved, () =>
            {
                approvedCalled = true;
                return Task.CompletedTask;
            }));

        var buttons = cut.FindAll("button");
        var approveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Approve Plan"));
        Assert.NotNull(approveButton);

        await approveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockTicketService.Verify(x => x.ApprovePlanAsync(ticketId, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(approvedCalled);
    }

    [Fact]
    public async Task PlanRevisionCard_ApproveButton_ShowsSuccessMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        _mockTicketService
            .Setup(x => x.ApprovePlanAsync(ticketId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnApproved, () => Task.CompletedTask));

        var buttons = cut.FindAll("button");
        var approveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Approve Plan"));
        Assert.NotNull(approveButton);

        await approveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Plan approved", cut.Markup);
    }

    [Fact]
    public void PlanRevisionCard_ReviseButton_EnabledWhenFeedbackProvided()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters =>
            parameters.Add(p => p.TicketId, ticketId));

        var textarea = cut.Find("textarea");
        textarea.Input("Add caching strategy");

        // Assert - Revise button should now be enabled
        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);
        Assert.False(reviseButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task PlanRevisionCard_ReviseButton_CallsServiceAndInvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var revisionStartedCalled = false;
        var feedback = "Add caching strategy";

        _mockTicketService
            .Setup(x => x.RefinePlanAsync(ticketId, feedback, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () =>
            {
                revisionStartedCalled = true;
                return Task.CompletedTask;
            }));

        var textarea = cut.Find("textarea");
        textarea.Input(feedback);

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockTicketService.Verify(x => x.RefinePlanAsync(ticketId, feedback, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(revisionStartedCalled);
    }

    [Fact]
    public async Task PlanRevisionCard_ReviseButton_ShowsSuccessMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Add caching strategy";

        _mockTicketService
            .Setup(x => x.RefinePlanAsync(ticketId, feedback, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () => Task.CompletedTask));

        var textarea = cut.Find("textarea");
        textarea.Input(feedback);

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Plan revision started", cut.Markup);
    }

    [Fact]
    public async Task PlanRevisionCard_ReviseButton_ClearsFeedbackAfterSuccess()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Add caching strategy";

        _mockTicketService
            .Setup(x => x.RefinePlanAsync(ticketId, feedback, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () => Task.CompletedTask));

        var textarea = cut.Find("textarea");
        textarea.Input(feedback);

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Feedback should be cleared
        var textareaAfter = cut.Find("textarea");
        Assert.Empty(textareaAfter.TextContent.Trim());
    }

    [Fact]
    public async Task PlanRevisionCard_ReviseButton_ShowsErrorWhenServiceThrows()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var feedback = "Add caching strategy";

        _mockTicketService
            .Setup(x => x.RefinePlanAsync(ticketId, feedback, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () => Task.CompletedTask));

        var textarea = cut.Find("textarea");
        textarea.Input(feedback);

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Failed to start revision", cut.Markup);
        Assert.Contains("Service error", cut.Markup);
    }

    [Fact]
    public async Task PlanRevisionCard_ApproveButton_ShowsErrorWhenServiceThrows()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        _mockTicketService
            .Setup(x => x.ApprovePlanAsync(ticketId, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Approval service error"));

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnApproved, () => Task.CompletedTask));

        var buttons = cut.FindAll("button");
        var approveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Approve Plan"));
        Assert.NotNull(approveButton);

        await approveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Failed to approve plan", cut.Markup);
        Assert.Contains("Approval service error", cut.Markup);
    }

    [Fact]
    public async Task PlanRevisionCard_ReviseButton_ValidatesFeedbackNotEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () => Task.CompletedTask));

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        // Try clicking without entering feedback
        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Service should NOT be called
        _mockTicketService.Verify(
            x => x.RefinePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
