using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.ComponentTests.Components.Tickets;

/// <summary>
/// Tests for the TicketUpdatePreview component.
/// Tests approval/rejection UI and interaction with ITicketService.
/// </summary>
public class TicketUpdatePreviewTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Guid _testTicketId;
    private readonly TicketDto _testTicket;
    private readonly TicketUpdateDto _testTicketUpdate;

    public TicketUpdatePreviewTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _testTicketId = Guid.NewGuid();

        _testTicket = new TicketDto
        {
            Id = _testTicketId,
            TicketKey = "TEST-123",
            Title = "Original Title",
            Description = "Original Description"
        };

        _testTicketUpdate = new TicketUpdateDto
        {
            Id = Guid.NewGuid(),
            TicketId = _testTicketId,
            UpdatedTitle = "Updated Title",
            UpdatedDescription = "Updated Description",
            AcceptanceCriteria = "- Test criteria 1\n- Test criteria 2",
            SuccessCriteria = new List<SuccessCriterionDto>
            {
                new SuccessCriterionDto
                {
                    Description = "Feature must work correctly",
                    Category = SuccessCriterionCategory.Functional,
                    Priority = 0,
                    IsTestable = true
                }
            },
            Version = 1,
            IsDraft = true,
            IsApproved = false,
            GeneratedAt = DateTime.UtcNow
        };

        // Register the mocked service
        Services.AddSingleton(_mockTicketService.Object);
    }

    [Fact]
    public void TicketUpdatePreview_OnInitialized_LoadsTicketUpdate()
    {
        // Arrange
        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Assert
        _mockTicketService.Verify(
            s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Contains("Updated Title", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_NoTicketUpdate_ShowsWarningMessage()
    {
        // Arrange
        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketUpdateDto?)null);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Assert
        Assert.Contains("No ticket update available", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_RendersSuccessCriteria()
    {
        // Arrange
        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Assert
        Assert.Contains("Feature must work correctly", cut.Markup);
        Assert.Contains("Must-Have", cut.Markup);
        Assert.Contains("Testable", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_ApproveButton_CallsServiceAndTriggersCallback()
    {
        // Arrange
        var callbackInvoked = false;

        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        _mockTicketService
            .Setup(s => s.ApproveTicketUpdateAsync(_testTicketUpdate.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket)
            .Add(p => p.OnUpdateApproved, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var approveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Approve"));
        approveButton.Click();

        // Assert
        _mockTicketService.Verify(
            s => s.ApproveTicketUpdateAsync(_testTicketUpdate.Id, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void TicketUpdatePreview_RejectButton_ShowsRejectForm()
    {
        // Arrange
        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Act
        var rejectButton = cut.FindAll("button").First(b => b.TextContent.Contains("Reject Update"));
        rejectButton.Click();

        // Assert
        Assert.Contains("Rejection Reason", cut.Markup);
        Assert.Contains("Confirm Rejection", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_ConfirmReject_WithoutReason_ShowsValidationError()
    {
        // Arrange
        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Show reject form
        var rejectButton = cut.FindAll("button").First(b => b.TextContent.Contains("Reject Update"));
        rejectButton.Click();

        // Act - Try to confirm without entering a reason
        var confirmButton = cut.FindAll("button").First(b => b.TextContent.Contains("Confirm Rejection"));
        confirmButton.Click();

        // Assert - Should show validation error
        Assert.Contains("Please provide a reason", cut.Markup);

        // Verify service was NOT called
        _mockTicketService.Verify(
            s => s.RejectTicketUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void TicketUpdatePreview_ConfirmReject_WithReason_CallsServiceAndTriggersCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var rejectionReason = "Needs more details";

        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        _mockTicketService
            .Setup(s => s.RejectTicketUpdateAsync(_testTicketUpdate.Id, rejectionReason, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket)
            .Add(p => p.OnUpdateRejected, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Show reject form
        var rejectButton = cut.FindAll("button").First(b => b.TextContent.Contains("Reject Update"));
        rejectButton.Click();

        // Enter rejection reason
        var textarea = cut.Find("textarea");
        textarea.Change(rejectionReason);

        // Act - Confirm rejection
        var confirmButton = cut.FindAll("button").First(b => b.TextContent.Contains("Confirm Rejection"));
        confirmButton.Click();

        // Assert
        _mockTicketService.Verify(
            s => s.RejectTicketUpdateAsync(_testTicketUpdate.Id, rejectionReason, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void TicketUpdatePreview_CancelReject_HidesRejectForm()
    {
        // Arrange
        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Show reject form
        var rejectButton = cut.FindAll("button").First(b => b.TextContent.Contains("Reject Update"));
        rejectButton.Click();
        Assert.Contains("Rejection Reason", cut.Markup);

        // Act - Cancel rejection
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel");
        cancelButton.Click();

        // Assert - Reject form should be hidden
        Assert.DoesNotContain("Rejection Reason", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_VersionBadge_DisplaysVersionNumber()
    {
        // Arrange
        _testTicketUpdate.Version = 3;

        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTicketUpdate);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Assert
        Assert.Contains("Version 3", cut.Markup);
    }

    [Fact]
    public void TicketUpdatePreview_LoadingState_ShowsSpinner()
    {
        // Arrange - Simulate a long-running task
        var tcs = new TaskCompletionSource<TicketUpdateDto?>();

        _mockTicketService
            .Setup(s => s.GetLatestTicketUpdateAsync(_testTicketId, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<TicketUpdatePreview>(parameters => parameters
            .Add(p => p.TicketId, _testTicketId)
            .Add(p => p.OriginalTicket, _testTicket));

        // Assert - Should show loading spinner
        Assert.Contains("Loading ticket update", cut.Markup);
        Assert.Contains("spinner-border", cut.Markup);

        // Complete the task
        tcs.SetResult(_testTicketUpdate);
    }
}
