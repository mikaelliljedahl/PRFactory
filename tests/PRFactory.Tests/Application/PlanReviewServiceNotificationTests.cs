using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Application;
using PRFactory.Tests.Builders;
using Xunit;

namespace PRFactory.Tests.Application;

/// <summary>
/// Comprehensive tests for PlanReviewService notification integration
/// Tests that notifications are triggered correctly at all integration points
/// </summary>
public class PlanReviewServiceNotificationTests
{
    private readonly Mock<ILogger<PlanReviewService>> _mockLogger;
    private readonly Mock<ITicketRepository> _mockTicketRepo;
    private readonly Mock<IPlanReviewRepository> _mockPlanReviewRepo;
    private readonly Mock<IReviewCommentRepository> _mockReviewCommentRepo;
    private readonly Mock<IInlineCommentAnchorRepository> _mockAnchorRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IChecklistTemplateService> _mockChecklistTemplateService;
    private readonly Mock<INotificationService> _mockNotificationService;

    public PlanReviewServiceNotificationTests()
    {
        _mockLogger = new Mock<ILogger<PlanReviewService>>();
        _mockTicketRepo = new Mock<ITicketRepository>();
        _mockPlanReviewRepo = new Mock<IPlanReviewRepository>();
        _mockReviewCommentRepo = new Mock<IReviewCommentRepository>();
        _mockAnchorRepo = new Mock<IInlineCommentAnchorRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockChecklistTemplateService = new Mock<IChecklistTemplateService>();
        _mockNotificationService = new Mock<INotificationService>();
    }

    private PlanReviewService CreateService()
    {
        return new PlanReviewService(
            _mockTicketRepo.Object,
            _mockPlanReviewRepo.Object,
            _mockReviewCommentRepo.Object,
            _mockAnchorRepo.Object,
            _mockUserRepo.Object,
            _mockChecklistTemplateService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    #region AssignReviewersAsync Tests

    [Fact]
    public async Task AssignReviewers_CreatesNotificationsForAllReviewers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewer1Id = Guid.NewGuid();
        var reviewer2Id = Guid.NewGuid();
        var reviewer3Id = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var user1 = new UserBuilder().WithEmail("reviewer1@test.com").Build();
        var user2 = new UserBuilder().WithEmail("reviewer2@test.com").Build();
        var user3 = new UserBuilder().WithEmail("reviewer3@test.com").Build();

        var review1 = new PlanReview(ticketId, reviewer1Id, isRequired: true);
        var review2 = new PlanReview(ticketId, reviewer2Id, isRequired: true);
        var review3 = new PlanReview(ticketId, reviewer3Id, isRequired: false);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user1, user2, user3 });

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlanReview> { review1, review2, review3 });

        var service = CreateService();

        // Act
        await service.AssignReviewersAsync(
            ticketId,
            new List<Guid> { reviewer1Id, reviewer2Id },
            new List<Guid> { reviewer3Id });

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyReviewerAssignedAsync(reviewer1Id, ticketId, true),
            Times.Once);

        _mockNotificationService.Verify(
            x => x.NotifyReviewerAssignedAsync(reviewer2Id, ticketId, true),
            Times.Once);

        _mockNotificationService.Verify(
            x => x.NotifyReviewerAssignedAsync(reviewer3Id, ticketId, false),
            Times.Once);
    }

    [Fact]
    public async Task AssignReviewers_WithRequiredReviewer_SendsRequiredNotification()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var user = new UserBuilder().WithEmail("reviewer@test.com").Build();
        var review = new PlanReview(ticketId, reviewerId, isRequired: true);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlanReview> { review });

        var service = CreateService();

        // Act
        await service.AssignReviewersAsync(
            ticketId,
            new List<Guid> { reviewerId },
            null);

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyReviewerAssignedAsync(reviewerId, ticketId, true),
            Times.Once);
    }

    [Fact]
    public async Task AssignReviewers_WithOptionalReviewer_SendsOptionalNotification()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var requiredReviewerId = Guid.NewGuid();
        var optionalReviewerId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var user1 = new UserBuilder().WithEmail("required@test.com").Build();
        var user2 = new UserBuilder().WithEmail("optional@test.com").Build();

        var review1 = new PlanReview(ticketId, requiredReviewerId, isRequired: true);
        var review2 = new PlanReview(ticketId, optionalReviewerId, isRequired: false);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user1, user2 });

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlanReview> { review1, review2 });

        var service = CreateService();

        // Act
        await service.AssignReviewersAsync(
            ticketId,
            new List<Guid> { requiredReviewerId },
            new List<Guid> { optionalReviewerId });

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyReviewerAssignedAsync(optionalReviewerId, ticketId, false),
            Times.Once);
    }

    [Fact]
    public async Task AssignReviewers_WhenNotificationFails_DoesNotThrowException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var user = new UserBuilder().WithEmail("reviewer@test.com").Build();
        var review = new PlanReview(ticketId, reviewerId, isRequired: true);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlanReview> { review });

        _mockNotificationService
            .Setup(x => x.NotifyReviewerAssignedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Notification service error"));

        var service = CreateService();

        // Act & Assert - Should not throw
        await service.AssignReviewersAsync(
            ticketId,
            new List<Guid> { reviewerId },
            null);

        // Verify notification was attempted
        _mockNotificationService.Verify(
            x => x.NotifyReviewerAssignedAsync(reviewerId, ticketId, true),
            Times.Once);
    }

    #endregion

    #region AddCommentAsync Tests

    [Fact]
    public async Task AddComment_WithMentions_CreatesNotifications()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var mentionedUser1Id = Guid.NewGuid();
        var mentionedUser2Id = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var author = new UserBuilder()
            .WithEmail("author@test.com")
            .WithDisplayName("John Doe")
            .Build();

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var service = CreateService();

        // Act
        var comment = await service.AddCommentAsync(
            ticketId,
            authorId,
            "This is a comment @user1 @user2",
            new List<Guid> { mentionedUser1Id, mentionedUser2Id });

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyMentionedInCommentAsync(
                It.Is<List<Guid>>(ids => ids.Contains(mentionedUser1Id) && ids.Contains(mentionedUser2Id)),
                ticketId,
                comment.Id,
                "John Doe"),
            Times.Once);
    }

    [Fact]
    public async Task AddComment_WithoutMentions_DoesNotCreateNotifications()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var author = new UserBuilder()
            .WithEmail("author@test.com")
            .WithDisplayName("John Doe")
            .Build();

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var service = CreateService();

        // Act
        await service.AddCommentAsync(
            ticketId,
            authorId,
            "This is a comment without mentions",
            null);

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyMentionedInCommentAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AddComment_WithNullDisplayName_UsesFallbackName()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var mentionedUserId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        // Ensure DisplayName is null by creating a new User without setting it
        var authorWithoutDisplayName = new User(
            Guid.NewGuid(),
            "author@test.com",
            null!, // null DisplayName
            null,
            null);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorWithoutDisplayName);

        var service = CreateService();

        // Act
        var comment = await service.AddCommentAsync(
            ticketId,
            authorId,
            "This is a comment @user",
            new List<Guid> { mentionedUserId });

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyMentionedInCommentAsync(
                It.IsAny<List<Guid>>(),
                ticketId,
                comment.Id,
                "Someone"),
            Times.Once);
    }

    [Fact]
    public async Task AddComment_WhenNotificationFails_DoesNotThrowException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var mentionedUserId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        var author = new UserBuilder()
            .WithEmail("author@test.com")
            .WithDisplayName("John Doe")
            .Build();

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockUserRepo
            .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        _mockNotificationService
            .Setup(x => x.NotifyMentionedInCommentAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Notification service error"));

        var service = CreateService();

        // Act & Assert - Should not throw
        var comment = await service.AddCommentAsync(
            ticketId,
            authorId,
            "This is a comment @user",
            new List<Guid> { mentionedUserId });

        Assert.NotNull(comment);

        // Verify notification was attempted
        _mockNotificationService.Verify(
            x => x.NotifyMentionedInCommentAsync(
                It.IsAny<List<Guid>>(),
                ticketId,
                comment.Id,
                "John Doe"),
            Times.Once);
    }

    #endregion

    #region ApproveReviewAsync Tests

    [Fact]
    public async Task ApprovePlan_WithSufficientApprovals_NotifiesAllReviewers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewer1Id = Guid.NewGuid();
        var reviewer2Id = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        // Assign reviewers to the ticket
        ticket.AssignReviewers(new List<Guid> { reviewer1Id, reviewer2Id });

        var review = new PlanReview(ticketId, reviewer1Id, isRequired: true);

        var allReviews = new List<PlanReview>
        {
            review,
            new PlanReview(ticketId, reviewer2Id, isRequired: true)
        };

        // Approve the second review to meet sufficient approvals
        allReviews[1].Approve("LGTM");

        _mockPlanReviewRepo
            .Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allReviews);

        var service = CreateService();

        // Act
        await service.ApproveReviewAsync(reviewId, "Looks good!");

        // Assert - Notification should be sent after sufficient approvals
        _mockNotificationService.Verify(
            x => x.NotifyPlanApprovedAsync(
                ticketId,
                It.Is<List<Guid>>(ids => ids.Contains(reviewer1Id) && ids.Contains(reviewer2Id))),
            Times.Once);
    }

    [Fact]
    public async Task ApprovePlan_WithoutSufficientApprovals_DoesNotNotify()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewer1Id = Guid.NewGuid();
        var reviewer2Id = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        // Assign reviewers to the ticket
        ticket.AssignReviewers(new List<Guid> { reviewer1Id, reviewer2Id });

        var review = new PlanReview(ticketId, reviewer1Id, isRequired: true);

        var allReviews = new List<PlanReview>
        {
            review,
            new PlanReview(ticketId, reviewer2Id, isRequired: true) // Still pending
        };

        _mockPlanReviewRepo
            .Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allReviews);

        var service = CreateService();

        // Act
        await service.ApproveReviewAsync(reviewId, "Looks good!");

        // Assert - No notification should be sent without sufficient approvals
        _mockNotificationService.Verify(
            x => x.NotifyPlanApprovedAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()),
            Times.Never);
    }

    [Fact]
    public async Task ApprovePlan_WhenNotificationFails_DoesNotThrowException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewer1Id = Guid.NewGuid();
        var reviewer2Id = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var ticket = new TicketBuilder()
            .WithTenantId(Guid.NewGuid())
            .WithRepositoryId(Guid.NewGuid())
            .WithTicketKey("TEST-123")
            .Build();

        ticket.AssignReviewers(new List<Guid> { reviewer1Id, reviewer2Id });

        var review = new PlanReview(ticketId, reviewer1Id, isRequired: true);

        var allReviews = new List<PlanReview>
        {
            review,
            new PlanReview(ticketId, reviewer2Id, isRequired: true)
        };

        allReviews[1].Approve("LGTM");

        _mockPlanReviewRepo
            .Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        _mockTicketRepo
            .Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allReviews);

        _mockNotificationService
            .Setup(x => x.NotifyPlanApprovedAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
            .ThrowsAsync(new Exception("Notification service error"));

        var service = CreateService();

        // Act & Assert - Should not throw
        await service.ApproveReviewAsync(reviewId, "Looks good!");

        // Verify notification was attempted
        _mockNotificationService.Verify(
            x => x.NotifyPlanApprovedAsync(ticketId, It.IsAny<List<Guid>>()),
            Times.Once);
    }

    #endregion

    #region RejectReviewAsync Tests

    [Fact]
    public async Task RejectPlan_NotifiesAllReviewers()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewer1Id = Guid.NewGuid();
        var reviewer2Id = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new PlanReview(ticketId, reviewer1Id, isRequired: true);

        var allReviews = new List<PlanReview>
        {
            review,
            new PlanReview(ticketId, reviewer2Id, isRequired: true)
        };

        _mockPlanReviewRepo
            .Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allReviews);

        var service = CreateService();
        var reason = "Missing edge case handling";

        // Act
        await service.RejectReviewAsync(reviewId, reason, regenerateCompletely: false);

        // Assert
        _mockNotificationService.Verify(
            x => x.NotifyPlanRejectedAsync(
                ticketId,
                It.Is<List<Guid>>(ids => ids.Contains(reviewer1Id) && ids.Contains(reviewer2Id)),
                reason),
            Times.Once);
    }

    [Fact]
    public async Task RejectPlan_WhenNotificationFails_DoesNotThrowException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new PlanReview(ticketId, reviewerId, isRequired: true);

        var allReviews = new List<PlanReview> { review };

        _mockPlanReviewRepo
            .Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        _mockPlanReviewRepo
            .Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allReviews);

        _mockNotificationService
            .Setup(x => x.NotifyPlanRejectedAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Notification service error"));

        var service = CreateService();
        var reason = "Missing edge case handling";

        // Act & Assert - Should not throw
        await service.RejectReviewAsync(reviewId, reason, regenerateCompletely: true);

        // Verify notification was attempted
        _mockNotificationService.Verify(
            x => x.NotifyPlanRejectedAsync(ticketId, It.IsAny<List<Guid>>(), reason),
            Times.Once);
    }

    #endregion
}
