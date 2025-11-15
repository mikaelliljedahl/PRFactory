using Bunit;
using Moq;
using Xunit;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Web.Tests.Components.Tickets;

/// <summary>
/// Tests for TicketUpdatePreview component
/// </summary>
public class TicketUpdatePreviewTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IToastService> _mockToastService;

    public TicketUpdatePreviewTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockToastService = new Mock<IToastService>();
        Services.AddSingleton(_mockTicketService.Object);
        Services.AddSingleton(_mockToastService.Object);
    }

    [Fact]
    public void TicketUpdatePreview_OnInitialized_LoadsTicketUpdate()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            Description = "Original Description",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            Version = 1,
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        _mockTicketService.Verify(
            x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void TicketUpdatePreview_WhileLoading_ShowsLoadingState()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var tcs = new TaskCompletionSource<TicketUpdateDto>();
        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        Assert.Contains("Loading ticket update", cut.Markup);

        // Clean up
        tcs.SetResult(null!);
    }

    [Fact]
    public void TicketUpdatePreview_WithNoUpdate_ShowsNoUpdateMessage()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdateDto?)null);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("No ticket update available"));
        Assert.Contains("No ticket update available", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_WithUpdate_ShowsVersionBadge()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            Version = 2,
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Version 2"));
        Assert.Contains("Version 2", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_ShowsPreviewTab()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Preview"));
        Assert.Contains("Preview", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_ShowsEditTab()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Edit"));
        Assert.Contains("Edit", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_ShowsViewChangesTab()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("View Changes"));
        Assert.Contains("View Changes", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_ShowsApproveButton()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Approve and Post Update"));
        Assert.Contains("Approve and Post Update", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_ShowsRejectButton()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Reject and Request Changes"));
        Assert.Contains("Reject and Request Changes", cut.Markup);
    }

    [Fact]
    public async Task TicketUpdatePreview_ApproveUpdate_CallsService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketService
            .Setup(x => x.ApproveTicketUpdateAsync(ticketUpdate.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var callbackInvoked = false;

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.OnUpdateApproved, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }));

        cut.WaitForState(() => cut.Markup.Contains("Approve and Post Update"));

        var approveButton = cut.Find("button:contains('Approve and Post Update')");
        await approveButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockTicketService.Verify(
            x => x.ApproveTicketUpdateAsync(ticketUpdate.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task TicketUpdatePreview_ClickReject_ShowsRejectionForm()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        cut.WaitForState(() => cut.Markup.Contains("Reject and Request Changes"));

        var rejectButton = cut.Find("button:contains('Reject and Request Changes')");
        await rejectButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Rejection Reason", cut.Markup);
    }

    [Fact]
    public async Task TicketUpdatePreview_RejectWithReason_CallsService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        _mockTicketService
            .Setup(x => x.RejectTicketUpdateAsync(ticketUpdate.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var callbackInvoked = false;

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket)
            .Add(p => p.OnUpdateRejected, () =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }));

        cut.WaitForState(() => cut.Markup.Contains("Reject and Request Changes"));

        // Click reject button
        var rejectButton = cut.Find("button:contains('Reject and Request Changes')");
        await rejectButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Fill in rejection reason
        var textarea = cut.Find("textarea#rejectionReason");
        textarea.Change("Needs more details");

        // Confirm rejection
        var confirmButton = cut.Find("button:contains('Confirm Rejection')");
        await confirmButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockTicketService.Verify(
            x => x.RejectTicketUpdateAsync(ticketUpdate.Id, "Needs more details", It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task TicketUpdatePreview_RejectWithoutReason_ShowsValidationError()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var originalTicket = new TicketDto
        {
            Id = ticketId,
            Title = "Original Title",
            State = WorkflowState.TicketUpdateUnderReview,
            CreatedAt = DateTime.UtcNow
        };

        var ticketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            GeneratedAt = DateTime.UtcNow
        };

        _mockTicketService
            .Setup(x => x.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, ticketId)
            .Add(p => p.OriginalTicket, originalTicket));

        cut.WaitForState(() => cut.Markup.Contains("Reject and Request Changes"));

        // Click reject button
        var rejectButton = cut.Find("button:contains('Reject and Request Changes')");
        await rejectButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Try to confirm without filling in reason
        var confirmButton = cut.Find("button:contains('Confirm Rejection')");
        await confirmButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.Contains("Please provide a reason for rejecting the update", cut.Markup);
        _mockTicketService.Verify(
            x => x.RejectTicketUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
