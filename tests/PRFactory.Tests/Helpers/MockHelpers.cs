using Moq;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Infrastructure.Git;

namespace PRFactory.Tests.Helpers;

/// <summary>
/// Helper methods for setting up common mock configurations
/// </summary>
public static class MockHelpers
{
    /// <summary>
    /// Sets up a mock ticket repository to return a specific ticket by ID
    /// </summary>
    public static void SetupTicketRepositoryGetById(
        Mock<ITicketRepository> mockRepo,
        Guid ticketId,
        Ticket ticket)
    {
        mockRepo.Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
    }

    /// <summary>
    /// Sets up a mock ticket repository to return null (ticket not found)
    /// </summary>
    public static void SetupTicketRepositoryGetByIdNotFound(
        Mock<ITicketRepository> mockRepo,
        Guid ticketId)
    {
        mockRepo.Setup(x => x.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);
    }

    /// <summary>
    /// Sets up a mock tenant repository to return a specific tenant by ID
    /// </summary>
    public static void SetupTenantRepositoryGetById(
        Mock<ITenantRepository> mockRepo,
        Guid tenantId,
        Tenant tenant)
    {
        mockRepo.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
    }

    /// <summary>
    /// Sets up a mock repository repository to return a specific repository by ID
    /// </summary>
    public static void SetupRepositoryRepositoryGetById(
        Mock<IRepositoryRepository> mockRepo,
        Guid repositoryId,
        Repository repository)
    {
        mockRepo.Setup(x => x.GetByIdAsync(repositoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repository);
    }

    /// <summary>
    /// Sets up a mock ticket update repository to return the latest draft update for a ticket
    /// </summary>
    public static void SetupTicketUpdateRepositoryGetLatestDraft(
        Mock<ITicketUpdateRepository> mockRepo,
        Guid ticketId,
        TicketUpdate? ticketUpdate)
    {
        mockRepo.Setup(x => x.GetLatestDraftByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketUpdate);
    }

    /// <summary>
    /// Sets up a mock user repository to return a specific user by ID
    /// </summary>
    public static void SetupUserRepositoryGetById(
        Mock<IUserRepository> mockRepo,
        Guid userId,
        User user)
    {
        mockRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    /// <summary>
    /// Sets up a mock user repository to return a user by email
    /// </summary>
    public static void SetupUserRepositoryGetByEmail(
        Mock<IUserRepository> mockRepo,
        Guid tenantId,
        string email,
        User? user)
    {
        mockRepo.Setup(x => x.GetByEmailAsync(tenantId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    /// <summary>
    /// Sets up a mock workflow orchestrator to successfully start a workflow
    /// </summary>
    public static void SetupWorkflowOrchestratorStart(
        Mock<IWorkflowOrchestrator> mockOrchestrator,
        TriggerTicketMessage triggerMessage,
        Guid workflowId)
    {
        mockOrchestrator.Setup(x => x.StartWorkflowAsync(triggerMessage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowId);
    }

    /// <summary>
    /// Sets up a mock workflow orchestrator to successfully resume a workflow
    /// </summary>
    public static void SetupWorkflowOrchestratorResume<TMessage>(
        Mock<IWorkflowOrchestrator> mockOrchestrator,
        Guid ticketId,
        TMessage message)
        where TMessage : IAgentMessage
    {
        mockOrchestrator.Setup(x => x.ResumeWorkflowAsync(ticketId, message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Sets up a mock git platform service to successfully create a pull request
    /// </summary>
    public static void SetupGitPlatformCreatePullRequest(
        Mock<IGitPlatformService> mockGitService,
        string prUrl,
        int prNumber)
    {
        mockGitService.Setup(x => x.CreatePullRequestAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreatePullRequestRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PullRequestInfo(prNumber, prUrl, prUrl));
    }

    /// <summary>
    /// Sets up a mock local git service to successfully clone a repository
    /// </summary>
    public static void SetupLocalGitClone(
        Mock<ILocalGitService> mockGitService,
        string localPath)
    {
        mockGitService.Setup(x => x.CloneAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(localPath);
    }

    /// <summary>
    /// Sets up a mock local git service to successfully create a branch
    /// </summary>
    public static void SetupLocalGitCreateBranch(
        Mock<ILocalGitService> mockGitService,
        string branchName)
    {
        mockGitService.Setup(x => x.CreateBranchAsync(
                It.IsAny<string>(),
                branchName,
                It.IsAny<string>()))
            .ReturnsAsync(branchName);
    }

    /// <summary>
    /// Sets up a mock event publisher to track published events
    /// </summary>
    public static void SetupEventPublisher<TEvent>(
        Mock<IEventPublisher> mockPublisher)
        where TEvent : class
    {
        mockPublisher.Setup(x => x.PublishAsync(It.IsAny<TEvent>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Sets up a mock tenant context to return a specific tenant ID
    /// </summary>
    public static void SetupTenantContext(
        Mock<ITenantContext> mockContext,
        Guid tenantId)
    {
        mockContext.Setup(x => x.GetCurrentTenantId()).Returns(tenantId);
    }

    /// <summary>
    /// Sets up a mock current user service to return a specific user ID
    /// </summary>
    public static void SetupCurrentUserService(
        Mock<ICurrentUserService> mockService,
        Guid userId)
    {
        mockService.Setup(x => x.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
    }

    /// <summary>
    /// Verifies that a repository method was called to add an entity
    /// </summary>
    public static void VerifyRepositoryAdd<TEntity>(
        Mock<ITicketRepository> mockRepo,
        Times? times = null)
        where TEntity : class
    {
        mockRepo.Verify(
            x => x.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that a repository method was called to update an entity
    /// </summary>
    public static void VerifyRepositoryUpdate<TEntity>(
        Mock<ITicketRepository> mockRepo,
        Times? times = null)
        where TEntity : class
    {
        mockRepo.Verify(
            x => x.UpdateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that an event was published
    /// </summary>
    public static void VerifyEventPublished<TEvent>(
        Mock<IEventPublisher> mockPublisher,
        Times? times = null)
        where TEvent : class
    {
        mockPublisher.Verify(
            x => x.PublishAsync(It.IsAny<TEvent>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that an event was published with a specific condition
    /// </summary>
    public static void VerifyEventPublishedWith<TEvent>(
        Mock<IEventPublisher> mockPublisher,
        Func<TEvent, bool> condition,
        Times? times = null)
        where TEvent : class
    {
        mockPublisher.Verify(
            x => x.PublishAsync(It.Is<TEvent>(e => condition(e))),
            times ?? Times.Once());
    }
}
