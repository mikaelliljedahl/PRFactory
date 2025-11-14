using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Services;

public class TicketServiceTests
{
    [Fact]
    public async Task GetTicketDtoByIdAsync_WithValidId_ShouldReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();

        var ticket = Ticket.Create("TICKET-123", tenantId, repositoryId);
        ticket.UpdateTicketInfo("Test Ticket", "Description");
        ticket.TransitionTo(WorkflowState.Analyzing);

        var mockTicketAppService = new Mock<ITicketApplicationService>();
        mockTicketAppService
            .Setup(x => x.GetTicketByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var service = CreateTicketService(mockTicketAppService.Object);

        // Act
        var result = await service.GetTicketDtoByIdAsync(ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticket.Id, result!.Id);
        Assert.Equal("TICKET-123", result.TicketKey);
        Assert.Equal("Test Ticket", result.Title);
        Assert.Equal("Description", result.Description);
        Assert.Equal(WorkflowState.Analyzing, result.State);
    }

    [Fact]
    public async Task GetTicketDtoByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        var mockTicketAppService = new Mock<ITicketApplicationService>();
        mockTicketAppService
            .Setup(x => x.GetTicketByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var service = CreateTicketService(mockTicketAppService.Object);

        // Act
        var result = await service.GetTicketDtoByIdAsync(ticketId);

        // Assert
        Assert.Null(result);
    }

    private static TicketService CreateTicketService(ITicketApplicationService? ticketAppService = null)
    {
        var mockLogger = new Mock<ILogger<TicketService>>();
        var mockTicketUpdateService = new Mock<ITicketUpdateService>();
        var mockQuestionService = new Mock<IQuestionApplicationService>();
        var mockEventService = new Mock<IWorkflowEventApplicationService>();
        var mockPlanService = new Mock<IPlanService>();
        var mockTenantContext = new Mock<ITenantContext>();
        var mockTicketRepo = new Mock<ITicketRepository>();
        var mockPlanReviewService = new Mock<IPlanReviewService>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var mockMapper = new Mock<MapsterMapper.IMapper>();

        return new TicketService(
            mockLogger.Object,
            ticketAppService ?? new Mock<ITicketApplicationService>().Object,
            mockTicketUpdateService.Object,
            mockQuestionService.Object,
            mockEventService.Object,
            mockPlanService.Object,
            mockTenantContext.Object,
            mockTicketRepo.Object,
            mockPlanReviewService.Object,
            mockCurrentUserService.Object,
            mockMapper.Object);
    }
}
