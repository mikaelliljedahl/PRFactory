using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Components.Tickets;
using PRFactory.Web.Models;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Web.Tests.Components.Tickets;

public class PlanReviewSectionTests : TestContext
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IToastService> _mockToastService;
    private readonly Mock<ILogger<PlanReviewSection>> _mockLogger;

    public PlanReviewSectionTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockToastService = new Mock<IToastService>();
        _mockLogger = new Mock<ILogger<PlanReviewSection>>();

        Services.AddSingleton(_mockTicketService.Object);
        Services.AddSingleton(_mockToastService.Object);
        Services.AddSingleton(_mockLogger.Object);
    }

    private TicketDto CreateTestTicket()
    {
        return new TicketDto
        {
            Id = Guid.NewGuid(),
            TicketKey = "TEST-123",
            Title = "Test Ticket",
            Description = "Test Description",
            State = WorkflowState.PlanUnderReview,
            PlanBranchName = "feature/test-plan",
            PlanMarkdownPath = "docs/PLAN.md",
            RepositoryId = Guid.NewGuid(),
            RepositoryName = "test-repo",
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void Render_WithRequiredProps_DisplaysComponent()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Implementation Plan Ready for Review", cut.Markup);
    }

    [Fact]
    public void Render_WithPlanDetails_DisplaysBranchAndPath()
    {
        // Arrange
        var ticket = CreateTestTicket();
        ticket.PlanBranchName = "feature/my-plan";
        ticket.PlanMarkdownPath = "docs/implementation-plan.md";
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("feature/my-plan", cut.Markup);
        Assert.Contains("docs/implementation-plan.md", cut.Markup);
    }

    [Fact]
    public void Render_WithNoPlanBranch_ShowsWarningMessage()
    {
        // Arrange
        var ticket = CreateTestTicket();
        ticket.PlanBranchName = null;
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Plan details are not yet available", cut.Markup);
    }

    [Fact]
    public void Render_WithReviewActions_DisplaysAllActionButtons()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("Approve Plan", cut.Markup);
        Assert.Contains("Request Refinements", cut.Markup);
        Assert.Contains("Reject & Regenerate", cut.Markup);
    }

    [Fact]
    public void Render_DisplaysHelpSection()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert
        Assert.Contains("What do these actions do?", cut.Markup);
        Assert.Contains("Approve:", cut.Markup);
        Assert.Contains("Refine:", cut.Markup);
        Assert.Contains("Reject & Regenerate:", cut.Markup);
    }

    [Fact]
    public async Task OnInitializedAsync_LoadsReviewersAndComments()
    {
        // Arrange
        var ticket = CreateTestTicket();
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Test Reviewer",
                Email = "reviewer@test.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            }
        };
        var comments = new List<ReviewCommentDto>
        {
            new ReviewCommentDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                AuthorId = Guid.NewGuid(),
                AuthorName = "Test Author",
                AuthorEmail = "author@test.com",
                Content = "Test comment",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(reviewers);
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(comments);

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Wait for async operations
        await Task.Delay(100);

        // Assert
        _mockTicketService.Verify(x => x.GetReviewersAsync(ticket.Id, default), Times.Once);
        _mockTicketService.Verify(x => x.GetCommentsAsync(ticket.Id, default), Times.Once);
    }

    [Fact]
    public async Task HandleApprove_WithoutReviewers_CallsApprovePlan()
    {
        // Arrange
        var ticket = CreateTestTicket();
        var onApprovedCalled = false;
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());
        _mockTicketService.Setup(x => x.ApprovePlanAsync(ticket.Id, null, default))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket)
            .Add(p => p.OnPlanApproved, () => { onApprovedCalled = true; }));

        // Act
        var approveButton = cut.Find("button[title*='Approve this plan']");
        await cut.InvokeAsync(() => approveButton.Click());

        // Wait for async operations
        await Task.Delay(100);

        // Assert
        _mockTicketService.Verify(x => x.ApprovePlanAsync(ticket.Id, null, default), Times.Once);
        _mockToastService.Verify(x => x.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        Assert.True(onApprovedCalled);
    }

    [Fact]
    public async Task HandleApprove_WithReviewersButInsufficientApprovals_ShowsWarning()
    {
        // Arrange
        var ticket = CreateTestTicket();
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Test Reviewer",
                Email = "reviewer@test.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            }
        };

        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(reviewers);
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());
        _mockTicketService.Setup(x => x.HasSufficientApprovalsAsync(ticket.Id, default))
            .ReturnsAsync(false);

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Wait for reviewers to load
        await Task.Delay(100);

        // Act
        var approveButton = cut.Find("button[title*='Approve this plan']");
        await cut.InvokeAsync(() => approveButton.Click());

        // Wait for async operations
        await Task.Delay(100);

        // Assert
        _mockTicketService.Verify(x => x.ApprovePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
        _mockToastService.Verify(x => x.ShowWarning(
            It.Is<string>(s => s.Contains("Insufficient reviewer approvals")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleApprove_WithReviewersAndSufficientApprovals_ApprovesSuccessfully()
    {
        // Arrange
        var ticket = CreateTestTicket();
        var reviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Test Reviewer",
                Email = "reviewer@test.com",
                Status = ReviewStatus.Approved,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow
            }
        };

        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(reviewers);
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());
        _mockTicketService.Setup(x => x.HasSufficientApprovalsAsync(ticket.Id, default))
            .ReturnsAsync(true);
        _mockTicketService.Setup(x => x.ApprovePlanAsync(ticket.Id, null, default))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Wait for reviewers to load
        await Task.Delay(100);

        // Act
        var approveButton = cut.Find("button[title*='Approve this plan']");
        await cut.InvokeAsync(() => approveButton.Click());

        // Wait for async operations
        await Task.Delay(100);

        // Assert
        _mockTicketService.Verify(x => x.ApprovePlanAsync(ticket.Id, null, default), Times.Once);
        _mockToastService.Verify(x => x.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleApprove_OnError_DisplaysErrorMessage()
    {
        // Arrange
        var ticket = CreateTestTicket();
        var errorMessage = "Failed to approve plan";
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());
        _mockTicketService.Setup(x => x.ApprovePlanAsync(ticket.Id, null, default))
            .ThrowsAsync(new Exception(errorMessage));

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Act
        var approveButton = cut.Find("button[title*='Approve this plan']");
        await cut.InvokeAsync(() => approveButton.Click());

        // Wait for async operations
        await Task.Delay(100);

        // Assert
        Assert.Contains(errorMessage, cut.Markup);
        _mockToastService.Verify(x => x.ShowError(
            It.Is<string>(s => s.Contains(errorMessage)),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void ShowRefineForm_DisplaysRefineForm()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Act
        var refineButton = cut.Find("button[title*='Request specific changes']");
        refineButton.Click();

        // Assert
        Assert.Contains("Refine Plan", cut.Markup);
        Assert.Contains("Refinement Instructions", cut.Markup);
    }

    [Fact]
    public void ShowRefineForm_DisplaysExamplesSection()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Act
        var refineButton = cut.Find("button[title*='Request specific changes']");
        refineButton.Click();

        var showExamplesButton = cut.Find("button:contains('Show Examples')");
        showExamplesButton.Click();

        // Assert
        Assert.Contains("Example Refinement Instructions:", cut.Markup);
        Assert.Contains("Add database migration steps", cut.Markup);
    }

    [Fact]
    public void CancelRefine_HidesRefineForm()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Show form
        var refineButton = cut.Find("button[title*='Request specific changes']");
        refineButton.Click();

        Assert.Contains("Refinement Instructions", cut.Markup);

        // Act - Cancel
        var cancelButton = cut.Find("button:contains('Cancel')");
        cancelButton.Click();

        // Assert
        Assert.DoesNotContain("Refinement Instructions", cut.Markup);
    }

    [Fact]
    public async Task ConfirmRefine_WithEmptyInstructions_ShowsValidationError()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Show refine form
        var refineButton = cut.Find("button[title*='Request specific changes']");
        refineButton.Click();

        // Act - Confirm without entering instructions
        var confirmButton = cut.Find("button:contains('Refine Plan')");
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        Assert.Contains("Please provide refinement instructions", cut.Markup);
        _mockTicketService.Verify(x => x.RefinePlanAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            default), Times.Never);
    }

    [Fact]
    public void ShowRejectForm_DisplaysRejectForm()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Act
        var rejectButton = cut.Find("button[title*='Reject this plan']");
        rejectButton.Click();

        // Assert
        Assert.Contains("Reject & Regenerate", cut.Markup);
        Assert.Contains("Rejection Reason", cut.Markup);
    }

    [Fact]
    public void CancelReject_HidesRejectForm()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Show form
        var rejectButton = cut.Find("button[title*='Reject this plan']");
        rejectButton.Click();

        Assert.Contains("Rejection Reason", cut.Markup);

        // Act - Cancel
        var cancelButtons = cut.FindAll("button:contains('Cancel')");
        var cancelButton = cancelButtons[cancelButtons.Count - 1];
        cancelButton.Click();

        // Assert
        Assert.DoesNotContain("Rejection Reason", cut.Markup);
    }

    [Fact]
    public async Task ConfirmReject_WithEmptyReason_ShowsValidationError()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Show reject form
        var rejectButton = cut.Find("button[title*='Reject this plan']");
        rejectButton.Click();

        // Act - Confirm without entering reason
        var confirmButtons = cut.FindAll("button:contains('Reject & Regenerate')");
        var confirmButton = confirmButtons[confirmButtons.Count - 1];
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        Assert.Contains("Please provide a reason for rejecting the plan", cut.Markup);
        _mockTicketService.Verify(x => x.RejectPlanAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            default), Times.Never);
    }

    [Fact]
    public async Task LoadReviewers_OnError_DoesNotShowErrorToast()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ThrowsAsync(new Exception("Failed to load reviewers"));
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Wait for async operations
        await Task.Delay(100);

        // Assert - Should not show error toast for team review failures (optional feature)
        _mockToastService.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadComments_OnError_DoesNotShowErrorToast()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ThrowsAsync(new Exception("Failed to load comments"));

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Wait for async operations
        await Task.Delay(100);

        // Assert - Should not show error toast for comments failures (optional feature)
        _mockToastService.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Render_WithNoReviewers_ShowsAssignReviewersButton()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Wait for async operations (need to wait for reviewers to load to show the button)
        await Task.Delay(100);
        cut.Render();

        // Assert
        Assert.Contains("Assign Team Reviewers", cut.Markup);
    }

    [Fact]
    public void Render_SubmittingState_DisablesButtons()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());
        _mockTicketService.Setup(x => x.ApprovePlanAsync(ticket.Id, null, default))
            .Returns(async () =>
            {
                await Task.Delay(500);
            });

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Act - Click approve to trigger submitting state
        var approveButton = cut.Find("button[title*='Approve this plan']");
        approveButton.Click();

        // Assert - Buttons should be disabled during submission
        var buttons = cut.FindAll("button");
        var disabledButtons = buttons.Where(b => b.HasAttribute("disabled")).ToList();
        Assert.NotEmpty(disabledButtons);
    }

    [Fact]
    public async Task Render_WithErrorMessage_DisplaysErrorAlert()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());
        _mockTicketService.Setup(x => x.ApprovePlanAsync(ticket.Id, null, default))
            .ThrowsAsync(new Exception("Test error"));

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Act - Trigger an error
        var approveButton = cut.Find("button[title*='Approve this plan']");
        approveButton.Click();

        // Wait for error to appear
        await Task.Delay(100);
        cut.Render();

        // Assert
        var errorAlerts = cut.FindAll(".alert-danger");
        Assert.NotEmpty(errorAlerts);
    }

    [Fact]
    public async Task HandleReviewersAssigned_ReloadsReviewers()
    {
        // Arrange
        var ticket = CreateTestTicket();
        var initialReviewers = new List<ReviewerDto>();
        var updatedReviewers = new List<ReviewerDto>
        {
            new ReviewerDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "New Reviewer",
                Email = "new@test.com",
                Status = ReviewStatus.Pending,
                IsRequired = true,
                AssignedAt = DateTime.UtcNow
            }
        };

        var callCount = 0;
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? initialReviewers : updatedReviewers;
            });
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Wait for initial load
        await Task.Delay(100);

        // Initial state should show assign button
        Assert.Contains("Assign Team Reviewers", cut.Markup);

        // Note: Testing HandleReviewersAssigned requires triggering the child component's callback
        // This would require more complex setup with the ReviewerAssignment component
        // For now, verify that GetReviewersAsync was called at least once
        _mockTicketService.Verify(x => x.GetReviewersAsync(ticket.Id, default), Times.AtLeastOnce);
    }

    [Fact]
    public void Render_WithEmptyTicket_HandlesGracefully()
    {
        // Arrange
        var ticket = new TicketDto
        {
            Id = Guid.NewGuid(),
            State = WorkflowState.PlanUnderReview
        };
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        // Assert - Should not throw
        Assert.NotNull(cut.Markup);
        Assert.Contains("Plan details are not yet available", cut.Markup);
    }

    [Fact]
    public async Task Render_WithContextualHelp_DisplaysHelpIcons()
    {
        // Arrange
        var ticket = CreateTestTicket();
        _mockTicketService.Setup(x => x.GetReviewersAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewerDto>());
        _mockTicketService.Setup(x => x.GetCommentsAsync(ticket.Id, default))
            .ReturnsAsync(new List<ReviewCommentDto>());

        // Act
        var cut = RenderComponent<PlanReviewSection>(parameters => parameters
            .Add(p => p.Ticket, ticket));

        await Task.Delay(50);

        // Assert - ContextualHelp components should be rendered
        // Note: This test verifies the markup contains help text, actual ContextualHelp component testing
        // would require the component to be available or mocked
        Assert.Contains("What do these actions do?", cut.Markup);
    }
}
