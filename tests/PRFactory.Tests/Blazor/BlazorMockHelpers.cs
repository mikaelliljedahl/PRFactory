using Moq;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Models;
using PRFactory.Web.Services;

namespace PRFactory.Tests.Blazor;

/// <summary>
/// Helper methods for common mock scenarios in Blazor component tests
/// </summary>
public static class BlazorMockHelpers
{
    /// <summary>
    /// Configures ITicketService mock to return a specific ticket
    /// </summary>
    public static void SetupGetTicketById(Mock<ITicketService> mockService, Guid ticketId, Ticket ticket)
    {
        mockService.Setup(m => m.GetTicketByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
    }

    /// <summary>
    /// Configures ITicketService mock to return a specific ticket DTO
    /// </summary>
    public static void SetupGetTicketDtoById(Mock<ITicketService> mockService, Guid ticketId, TicketDto ticketDto)
    {
        mockService.Setup(m => m.GetTicketDtoByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketDto);
    }

    /// <summary>
    /// Configures ITicketService mock to return a list of tickets
    /// </summary>
    public static void SetupGetAllTickets(Mock<ITicketService> mockService, List<TicketDto> tickets)
    {
        mockService.Setup(m => m.GetAllTicketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickets);
    }

    /// <summary>
    /// Configures ITicketService mock to return a ticket update
    /// </summary>
    public static void SetupGetLatestTicketUpdate(
        Mock<ITicketService> mockService,
        Guid ticketId,
        TicketUpdateDto ticketUpdate)
    {
        mockService.Setup(m => m.GetLatestTicketUpdateAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);
    }

    /// <summary>
    /// Alias for SetupGetLatestTicketUpdate for consistency
    /// </summary>
    public static void SetupGetLatestUpdate(
        Mock<ITicketService> mockService,
        Guid ticketId,
        TicketUpdateDto ticketUpdate)
    {
        SetupGetLatestTicketUpdate(mockService, ticketId, ticketUpdate);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully approve a ticket update
    /// </summary>
    public static void SetupApproveUpdate(Mock<ITicketService> mockService, Guid updateId)
    {
        mockService.Setup(m => m.ApproveTicketUpdateAsync(updateId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully reject a ticket update
    /// </summary>
    public static void SetupRejectUpdate(Mock<ITicketService> mockService, Guid updateId)
    {
        mockService.Setup(m => m.RejectTicketUpdateAsync(
            updateId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully update a ticket update
    /// </summary>
    public static void SetupUpdateTicketUpdate(Mock<ITicketService> mockService, Guid updateId)
    {
        mockService.Setup(m => m.UpdateTicketUpdateAsync(
            updateId,
            It.IsAny<TicketUpdateDto>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures ITicketService mock to return reviewers
    /// </summary>
    public static void SetupGetReviewers(Mock<ITicketService> mockService, Guid ticketId, List<ReviewerDto> reviewers)
    {
        mockService.Setup(m => m.GetReviewersAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewers);
    }

    /// <summary>
    /// Configures ITicketService mock to return comments
    /// </summary>
    public static void SetupGetComments(Mock<ITicketService> mockService, Guid ticketId, List<ReviewCommentDto> comments)
    {
        mockService.Setup(m => m.GetCommentsAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully add a comment
    /// </summary>
    public static void SetupAddComment(Mock<ITicketService> mockService, Guid ticketId, ReviewCommentDto comment)
    {
        mockService.Setup(m => m.AddCommentAsync(
            ticketId,
            It.IsAny<string>(),
            It.IsAny<List<Guid>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully assign reviewers
    /// </summary>
    public static void SetupAssignReviewers(Mock<ITicketService> mockService, Guid ticketId)
    {
        mockService.Setup(m => m.AssignReviewersAsync(
            ticketId,
            It.IsAny<List<Guid>>(),
            It.IsAny<List<Guid>?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures ITicketService mock to return sufficient approvals status
    /// </summary>
    public static void SetupHasSufficientApprovals(Mock<ITicketService> mockService, Guid ticketId, bool hasSufficient)
    {
        mockService.Setup(m => m.HasSufficientApprovalsAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasSufficient);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully refine a plan
    /// </summary>
    public static void SetupRefinePlan(Mock<ITicketService> mockService, Guid ticketId)
    {
        mockService.Setup(m => m.RefinePlanAsync(
            ticketId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures ITicketService mock to return questions
    /// </summary>
    public static void SetupGetQuestions(Mock<ITicketService> mockService, Guid ticketId, List<QuestionDto> questions)
    {
        mockService.Setup(m => m.GetQuestionsAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questions);
    }

    /// <summary>
    /// Configures ITicketService mock to return workflow events
    /// </summary>
    public static void SetupGetEvents(Mock<ITicketService> mockService, Guid ticketId, List<WorkflowEventDto> events)
    {
        mockService.Setup(m => m.GetEventsAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);
    }

    /// <summary>
    /// Configures ITicketService mock to return a plan
    /// </summary>
    public static void SetupGetPlan(Mock<ITicketService> mockService, Guid ticketId, PlanDto plan)
    {
        mockService.Setup(m => m.GetPlanAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully approve a plan
    /// </summary>
    public static void SetupApprovePlan(Mock<ITicketService> mockService, Guid ticketId)
    {
        mockService.Setup(m => m.ApprovePlanAsync(ticketId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully reject a plan
    /// </summary>
    public static void SetupRejectPlan(Mock<ITicketService> mockService, Guid ticketId)
    {
        mockService.Setup(m => m.RejectPlanAsync(
            ticketId,
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures ITicketService mock to successfully submit answers
    /// </summary>
    public static void SetupSubmitAnswers(Mock<ITicketService> mockService, Guid ticketId)
    {
        mockService.Setup(m => m.SubmitAnswersAsync(
            ticketId,
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configures IToastService mock to capture toast notifications
    /// </summary>
    public static void SetupToastService(Mock<IToastService> mockService)
    {
        mockService.Setup(m => m.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();
        mockService.Setup(m => m.ShowError(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();
        mockService.Setup(m => m.ShowWarning(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();
        mockService.Setup(m => m.ShowInfo(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();
    }

    /// <summary>
    /// Verifies that a success toast was shown
    /// </summary>
    public static void VerifySuccessToast(Mock<IToastService> mockService, string expectedMessage)
    {
        mockService.Verify(
            m => m.ShowSuccess(It.Is<string>(msg => msg.Contains(expectedMessage)), It.IsAny<string>()),
            Times.Once());
    }

    /// <summary>
    /// Verifies that any success toast was shown
    /// </summary>
    public static void VerifySuccessToast(Mock<IToastService> mockService)
    {
        mockService.Verify(
            m => m.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Verifies that an error toast was shown
    /// </summary>
    public static void VerifyErrorToast(Mock<IToastService> mockService, string expectedMessage)
    {
        mockService.Verify(
            m => m.ShowError(It.Is<string>(msg => msg.Contains(expectedMessage)), It.IsAny<string>()),
            Times.Once());
    }

    /// <summary>
    /// Verifies that any error toast was shown
    /// </summary>
    public static void VerifyErrorToast(Mock<IToastService> mockService)
    {
        mockService.Verify(
            m => m.ShowError(It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Verifies that a warning toast was shown
    /// </summary>
    public static void VerifyWarningToast(Mock<IToastService> mockService)
    {
        mockService.Verify(
            m => m.ShowWarning(It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Verifies that an info toast was shown
    /// </summary>
    public static void VerifyInfoToast(Mock<IToastService> mockService)
    {
        mockService.Verify(
            m => m.ShowInfo(It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Verifies that NavigationManager.NavigateTo was called with the expected URL
    /// </summary>
    public static void VerifyNavigatedTo(Mock<Microsoft.AspNetCore.Components.NavigationManager> mockNav, string expectedUrl)
    {
        mockNav.Verify(
            m => m.NavigateTo(It.Is<string>(url => url.Contains(expectedUrl)), It.IsAny<bool>()),
            Times.Once());
    }
}
