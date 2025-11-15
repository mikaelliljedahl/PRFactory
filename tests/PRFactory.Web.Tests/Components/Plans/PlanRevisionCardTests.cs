using Bunit;
using Xunit;
using PRFactory.Web.Components.Plans;
using Microsoft.AspNetCore.Components;

namespace PRFactory.Web.Tests.Components.Plans;

/// <summary>
/// Tests for PlanRevisionCard component
/// </summary>
public class PlanRevisionCardTests : TestContext
{
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
    public async Task PlanRevisionCard_ApproveButton_InvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var approvedCalled = false;

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
        Assert.True(approvedCalled);
    }

    [Fact]
    public async Task PlanRevisionCard_ApproveButton_ShowsSuccessMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

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
    public async Task PlanRevisionCard_ReviseButton_InvokesCallback()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var revisionStartedCalled = false;

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () =>
            {
                revisionStartedCalled = true;
                return Task.CompletedTask;
            }));

        var textarea = cut.Find("textarea");
        textarea.Input("Add caching strategy");

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.True(revisionStartedCalled);
    }

    [Fact]
    public async Task PlanRevisionCard_ReviseButton_ShowsSuccessMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () => Task.CompletedTask));

        var textarea = cut.Find("textarea");
        textarea.Input("Add caching strategy");

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

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () => Task.CompletedTask));

        var textarea = cut.Find("textarea");
        textarea.Input("Add caching strategy");

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Feedback should be cleared
        var textareaAfter = cut.Find("textarea");
        Assert.Empty(textareaAfter.TextContent.Trim());
    }

    [Fact]
    public async Task PlanRevisionCard_ReviseButton_ShowsErrorWhenCallbackThrows()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnRevisionStarted, () => throw new Exception("Test error")));

        var textarea = cut.Find("textarea");
        textarea.Input("Add caching strategy");

        var buttons = cut.FindAll("button");
        var reviseButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Revise Plan"));
        Assert.NotNull(reviseButton);

        await reviseButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Failed to start revision", cut.Markup);
        Assert.Contains("Test error", cut.Markup);
    }

    [Fact]
    public async Task PlanRevisionCard_ApproveButton_ShowsErrorWhenCallbackThrows()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var cut = RenderComponent<PlanRevisionCard>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OnApproved, () => throw new Exception("Approval failed")));

        var buttons = cut.FindAll("button");
        var approveButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Approve Plan"));
        Assert.NotNull(approveButton);

        await approveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Failed to approve plan", cut.Markup);
        Assert.Contains("Approval failed", cut.Markup);
    }
}
