using Microsoft.Extensions.Logging;
using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.Web.Services;

public class TicketServiceValidationTests
{
    [Fact]
    public async Task ValidatePlanAsync_CallsPlanValidationService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        const string checkType = "security";

        var mockPlanValidationService = new Mock<IPlanValidationService>();
        var expectedResult = new PlanValidationResult
        {
            TicketId = ticketId,
            CheckType = checkType,
            Score = 85,
            RiskLevel = "Medium",
            Findings = new List<string> { "Finding 1", "Finding 2" },
            Recommendations = new List<string> { "Recommendation 1" },
            ValidatedAt = DateTime.UtcNow
        };

        mockPlanValidationService
            .Setup(x => x.ValidatePlanAsync(ticketId, checkType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var service = CreateTicketService(planValidationService: mockPlanValidationService.Object);

        // Act
        var result = await service.ValidatePlanAsync(ticketId, checkType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticketId, result.TicketId);
        Assert.Equal(checkType, result.CheckType);
        Assert.Equal(85, result.Score);
        Assert.Equal("Medium", result.RiskLevel);
        Assert.Equal(2, result.Findings.Count);
        Assert.Single(result.Recommendations);

        mockPlanValidationService.Verify(
            x => x.ValidatePlanAsync(ticketId, checkType, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidatePlanAsync_ReturnsValidationResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        const string checkType = "completeness";

        var mockPlanValidationService = new Mock<IPlanValidationService>();
        var expectedResult = new PlanValidationResult
        {
            TicketId = ticketId,
            CheckType = checkType,
            Score = 95,
            Findings = new List<string>(),
            Recommendations = new List<string> { "Great plan!" },
            ValidatedAt = DateTime.UtcNow
        };

        mockPlanValidationService
            .Setup(x => x.ValidatePlanAsync(ticketId, checkType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var service = CreateTicketService(planValidationService: mockPlanValidationService.Object);

        // Act
        var result = await service.ValidatePlanAsync(ticketId, checkType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(95, result.Score);
        Assert.Empty(result.Findings);
        Assert.Single(result.Recommendations);
    }

    [Fact]
    public async Task ValidatePlanWithCustomPromptAsync_CallsPlanValidationService()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        const string customPrompt = "Check for accessibility issues";

        var mockPlanValidationService = new Mock<IPlanValidationService>();
        var expectedResult = new PlanValidationResult
        {
            TicketId = ticketId,
            CheckType = "custom",
            Score = 78,
            Findings = new List<string> { "Missing alt text" },
            Recommendations = new List<string> { "Add ARIA labels" },
            ValidatedAt = DateTime.UtcNow
        };

        mockPlanValidationService
            .Setup(x => x.ValidatePlanWithPromptAsync(ticketId, customPrompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var service = CreateTicketService(planValidationService: mockPlanValidationService.Object);

        // Act
        var result = await service.ValidatePlanWithCustomPromptAsync(ticketId, customPrompt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(78, result.Score);
        Assert.Single(result.Findings);
        Assert.Single(result.Recommendations);

        mockPlanValidationService.Verify(
            x => x.ValidatePlanWithPromptAsync(ticketId, customPrompt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithException_PropagatesException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        const string checkType = "security";

        var mockPlanValidationService = new Mock<IPlanValidationService>();
        mockPlanValidationService
            .Setup(x => x.ValidatePlanAsync(ticketId, checkType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Plan not found"));

        var service = CreateTicketService(planValidationService: mockPlanValidationService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ValidatePlanAsync(ticketId, checkType));
    }

    [Fact]
    public void TicketService_InjectsPlanValidationService()
    {
        // Arrange
        var mockPlanValidationService = new Mock<IPlanValidationService>();

        // Act
        var service = CreateTicketService(planValidationService: mockPlanValidationService.Object);

        // Assert
        Assert.NotNull(service);
    }

    private static TicketService CreateTicketService(
        ITicketApplicationService? ticketAppService = null,
        ITicketUpdateService? ticketUpdateService = null,
        IQuestionApplicationService? questionService = null,
        IWorkflowEventApplicationService? eventService = null,
        IPlanService? planService = null,
        ITenantContext? tenantContext = null,
        ITicketRepository? ticketRepo = null,
        IPlanReviewService? planReviewService = null,
        ICurrentUserService? currentUserService = null,
        IPlanValidationService? planValidationService = null)
    {
        var mockLogger = new Mock<ILogger<TicketService>>();

        return new TicketService(
            mockLogger.Object,
            ticketAppService ?? new Mock<ITicketApplicationService>().Object,
            ticketUpdateService ?? new Mock<ITicketUpdateService>().Object,
            questionService ?? new Mock<IQuestionApplicationService>().Object,
            eventService ?? new Mock<IWorkflowEventApplicationService>().Object,
            planService ?? new Mock<IPlanService>().Object,
            tenantContext ?? new Mock<ITenantContext>().Object,
            ticketRepo ?? new Mock<ITicketRepository>().Object,
            planReviewService ?? new Mock<IPlanReviewService>().Object,
            currentUserService ?? new Mock<ICurrentUserService>().Object,
            planValidationService ?? new Mock<IPlanValidationService>().Object);
    }
}
