using Microsoft.Extensions.DependencyInjection;
using PRFactory.Core.Application.Services;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Configuration;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Tests to validate that critical service dependency chains can be fully resolved
/// </summary>
public class CriticalDependencyChainTests : DIValidationTestBase
{
    #region Ticket Workflow Dependency Chain

    [Fact]
    public void DependencyChain_TicketApplicationService_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert - Should resolve complete chain:
        // ITicketApplicationService
        //   ├─ ITicketRepository
        //   ├─ ITicketUpdateService
        //   │  └─ ITicketUpdateRepository
        //   ├─ IQuestionApplicationService
        //   ├─ IWorkflowEventApplicationService
        //   │  └─ IWorkflowEventRepository
        //   ├─ IPlanService
        //   └─ ITenantContext

        DIAssertions.AssertDependencyChainResolvable<ITicketApplicationService>(provider);
    }

    [Fact]
    public void DependencyChain_TicketUpdateService_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert - Should resolve:
        // ITicketUpdateService
        //   └─ ITicketUpdateRepository
        //      └─ ApplicationDbContext

        DIAssertions.AssertDependencyChainResolvable<ITicketUpdateService>(provider);
    }

    [Fact]
    public void DependencyChain_QuestionApplicationService_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<IQuestionApplicationService>(provider);
    }

    #endregion

    #region Agent Dependency Chains

    [Fact]
    public void DependencyChain_TriggerAgent_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert - Should resolve with all dependencies
        using var scope = provider.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<TriggerAgent>();
        Assert.NotNull(agent);
    }

    [Fact]
    public void DependencyChain_PlanningAgent_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert - Should resolve:
        // PlanningAgent
        //   ├─ IRepositoryRepository
        //   ├─ ITicketRepository
        //   ├─ IPlanService
        //   ├─ Claude.IContextBuilder
        //   └─ IAgentPromptService

        using var scope = provider.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<PlanningAgent>();
        Assert.NotNull(agent);
    }

    [Fact]
    public void DependencyChain_AnalysisAgent_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        using var scope = provider.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<AnalysisAgent>();
        Assert.NotNull(agent);
    }

    [Fact]
    public void DependencyChain_QuestionGenerationAgent_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        using var scope = provider.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<QuestionGenerationAgent>();
        Assert.NotNull(agent);
    }

    [Fact]
    public void DependencyChain_ImplementationAgent_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        using var scope = provider.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<ImplementationAgent>();
        Assert.NotNull(agent);
    }

    #endregion

    #region Repository Dependency Chains

    [Fact]
    public void DependencyChain_AllRepositories_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert - All repositories should resolve with DbContext
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.ITenantRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.IRepositoryRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.ITicketRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.ITicketUpdateRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.IWorkflowEventRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.ICheckpointRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.IAgentPromptTemplateRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.IErrorRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.IUserRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.IPlanReviewRepository>(provider);
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Domain.Interfaces.IReviewCommentRepository>(provider);
    }

    #endregion

    #region Workflow State Management Chains

    [Fact]
    public void DependencyChain_WorkflowStateStore_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Infrastructure.Agents.Graphs.IWorkflowStateStore>(provider);
    }

    [Fact]
    public void DependencyChain_EventPublisher_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Infrastructure.Agents.Graphs.IEventPublisher>(provider);
    }

    [Fact]
    public void DependencyChain_CheckpointStoreAdapter_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Infrastructure.Agents.ICheckpointStore>(provider);
    }

    [Fact]
    public void DependencyChain_AgentExecutor_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Infrastructure.Agents.Graphs.IAgentExecutor>(provider);
    }

    #endregion

    #region Configuration & Context Chains

    [Fact]
    public void DependencyChain_TenantContext_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<ITenantContext>(provider);
    }

    [Fact]
    public void DependencyChain_TenantConfigurationService_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<ITenantConfigurationService>(provider);
    }

    [Fact]
    public void DependencyChain_ContextBuilder_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Infrastructure.Claude.IContextBuilder>(provider);
    }

    #endregion

    #region Team Review Dependency Chains

    [Fact]
    public void DependencyChain_PlanReviewService_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert - Should resolve:
        // IPlanReviewService
        //   ├─ IPlanReviewRepository
        //   ├─ IReviewCommentRepository
        //   ├─ IUserRepository
        //   ├─ ICurrentUserService
        //   └─ ApplicationDbContext (via repositories)

        DIAssertions.AssertDependencyChainResolvable<IPlanReviewService>(provider);
    }

    [Fact]
    public void DependencyChain_UserService_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<IUserService>(provider);
    }

    [Fact]
    public void DependencyChain_CurrentUserService_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<ICurrentUserService>(provider);
    }

    #endregion

    #region CLI Agent Chains

    [Fact]
    public void DependencyChain_ProcessExecutor_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<PRFactory.Infrastructure.Execution.IProcessExecutor>(provider);
    }

    [Fact]
    public void DependencyChain_CliAgent_CanBeResolved()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert
        DIAssertions.AssertDependencyChainResolvable<ICliAgent>(provider);
    }

    #endregion

    #region Complex Multi-Service Chains

    [Fact]
    public void DependencyChain_AllCriticalServices_CanBeResolvedTogether()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act & Assert - Resolve multiple critical services in same scope
        // This validates no conflicts or circular dependencies
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var ticketService = sp.GetRequiredService<ITicketApplicationService>();
        var planService = sp.GetRequiredService<IPlanService>();
        var tenantContext = sp.GetRequiredService<ITenantContext>();
        var workflowStateStore = sp.GetRequiredService<PRFactory.Infrastructure.Agents.Graphs.IWorkflowStateStore>();
        var agentExecutor = sp.GetRequiredService<PRFactory.Infrastructure.Agents.Graphs.IAgentExecutor>();
        var planningAgent = sp.GetRequiredService<PlanningAgent>();

        Assert.NotNull(ticketService);
        Assert.NotNull(planService);
        Assert.NotNull(tenantContext);
        Assert.NotNull(workflowStateStore);
        Assert.NotNull(agentExecutor);
        Assert.NotNull(planningAgent);
    }

    #endregion
}
