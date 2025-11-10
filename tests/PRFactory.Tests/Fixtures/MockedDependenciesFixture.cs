using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Tests.Fixtures;

/// <summary>
/// Reusable fixture providing pre-configured mocks for common service dependencies
/// </summary>
public class MockedDependenciesFixture
{
    public Mock<ITicketRepository> TicketRepository { get; }
    public Mock<ITicketUpdateRepository> TicketUpdateRepository { get; }
    public Mock<ITenantRepository> TenantRepository { get; }
    public Mock<IRepositoryRepository> RepositoryRepository { get; }
    public Mock<IUserRepository> UserRepository { get; }
    public Mock<IPlanReviewRepository> PlanReviewRepository { get; }
    public Mock<IReviewCommentRepository> ReviewCommentRepository { get; }
    public Mock<ICheckpointRepository> CheckpointRepository { get; }
    public Mock<IWorkflowEventRepository> WorkflowEventRepository { get; }
    public Mock<IErrorRepository> ErrorRepository { get; }

    public Mock<IWorkflowOrchestrator> WorkflowOrchestrator { get; }
    public Mock<ILocalGitService> LocalGitService { get; }
    public Mock<IGitPlatformService> GitPlatformService { get; }
    public Mock<ICheckpointStore> CheckpointStore { get; }
    public Mock<IEventPublisher> EventPublisher { get; }

    public Mock<ITicketApplicationService> TicketApplicationService { get; }
    public Mock<ITicketUpdateService> TicketUpdateService { get; }
    public Mock<ITenantApplicationService> TenantApplicationService { get; }
    public Mock<IRepositoryApplicationService> RepositoryApplicationService { get; }
    public Mock<IQuestionApplicationService> QuestionApplicationService { get; }
    public Mock<IWorkflowEventApplicationService> WorkflowEventApplicationService { get; }
    public Mock<IUserService> UserService { get; }
    public Mock<IPlanService> PlanService { get; }
    public Mock<IPlanReviewService> PlanReviewService { get; }
    public Mock<ICurrentUserService> CurrentUserService { get; }
    public Mock<ITenantContext> TenantContext { get; }

    public MockedDependenciesFixture()
    {
        // Repository mocks
        TicketRepository = new Mock<ITicketRepository>();
        TicketUpdateRepository = new Mock<ITicketUpdateRepository>();
        TenantRepository = new Mock<ITenantRepository>();
        RepositoryRepository = new Mock<IRepositoryRepository>();
        UserRepository = new Mock<IUserRepository>();
        PlanReviewRepository = new Mock<IPlanReviewRepository>();
        ReviewCommentRepository = new Mock<IReviewCommentRepository>();
        CheckpointRepository = new Mock<ICheckpointRepository>();
        WorkflowEventRepository = new Mock<IWorkflowEventRepository>();
        ErrorRepository = new Mock<IErrorRepository>();

        // Infrastructure service mocks
        WorkflowOrchestrator = new Mock<IWorkflowOrchestrator>();
        LocalGitService = new Mock<ILocalGitService>();
        GitPlatformService = new Mock<IGitPlatformService>();
        CheckpointStore = new Mock<ICheckpointStore>();
        EventPublisher = new Mock<IEventPublisher>();

        // Application service mocks
        TicketApplicationService = new Mock<ITicketApplicationService>();
        TicketUpdateService = new Mock<ITicketUpdateService>();
        TenantApplicationService = new Mock<ITenantApplicationService>();
        RepositoryApplicationService = new Mock<IRepositoryApplicationService>();
        QuestionApplicationService = new Mock<IQuestionApplicationService>();
        WorkflowEventApplicationService = new Mock<IWorkflowEventApplicationService>();
        UserService = new Mock<IUserService>();
        PlanService = new Mock<IPlanService>();
        PlanReviewService = new Mock<IPlanReviewService>();
        CurrentUserService = new Mock<ICurrentUserService>();
        TenantContext = new Mock<ITenantContext>();
    }

    /// <summary>
    /// Resets all mocks to their initial state
    /// </summary>
    public void ResetAll()
    {
        TicketRepository.Reset();
        TicketUpdateRepository.Reset();
        TenantRepository.Reset();
        RepositoryRepository.Reset();
        UserRepository.Reset();
        PlanReviewRepository.Reset();
        ReviewCommentRepository.Reset();
        CheckpointRepository.Reset();
        WorkflowEventRepository.Reset();
        ErrorRepository.Reset();

        WorkflowOrchestrator.Reset();
        LocalGitService.Reset();
        GitPlatformService.Reset();
        CheckpointStore.Reset();
        EventPublisher.Reset();

        TicketApplicationService.Reset();
        TicketUpdateService.Reset();
        TenantApplicationService.Reset();
        RepositoryApplicationService.Reset();
        QuestionApplicationService.Reset();
        WorkflowEventApplicationService.Reset();
        UserService.Reset();
        PlanService.Reset();
        PlanReviewService.Reset();
        CurrentUserService.Reset();
        TenantContext.Reset();
    }

    /// <summary>
    /// Verifies all mocks (useful for ensuring expected interactions occurred)
    /// </summary>
    public void VerifyAll()
    {
        TicketRepository.VerifyAll();
        TicketUpdateRepository.VerifyAll();
        TenantRepository.VerifyAll();
        RepositoryRepository.VerifyAll();
        UserRepository.VerifyAll();
        PlanReviewRepository.VerifyAll();
        ReviewCommentRepository.VerifyAll();
        CheckpointRepository.VerifyAll();
        WorkflowEventRepository.VerifyAll();
        ErrorRepository.VerifyAll();

        WorkflowOrchestrator.VerifyAll();
        LocalGitService.VerifyAll();
        GitPlatformService.VerifyAll();
        CheckpointStore.VerifyAll();
        EventPublisher.VerifyAll();

        TicketApplicationService.VerifyAll();
        TicketUpdateService.VerifyAll();
        TenantApplicationService.VerifyAll();
        RepositoryApplicationService.VerifyAll();
        QuestionApplicationService.VerifyAll();
        WorkflowEventApplicationService.VerifyAll();
        UserService.VerifyAll();
        PlanService.VerifyAll();
        PlanReviewService.VerifyAll();
        CurrentUserService.VerifyAll();
        TenantContext.VerifyAll();
    }
}
