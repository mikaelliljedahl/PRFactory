using Microsoft.Extensions.DependencyInjection;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Adapters;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Infrastructure.Execution;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Tests for Infrastructure layer service registrations via AddInfrastructure()
/// </summary>
public class InfrastructureServiceRegistrationTests : DIValidationTestBase
{
    #region Repository Registration Tests

    [Fact]
    public void AddInfrastructure_RegistersTenantRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITenantRepository>(services);
        AssertServiceResolvable<ITenantRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersRepositoryRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IRepositoryRepository>(services);
        AssertServiceResolvable<IRepositoryRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersTicketRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITicketRepository>(services);
        AssertServiceResolvable<ITicketRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersTicketUpdateRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITicketUpdateRepository>(services);
        AssertServiceResolvable<ITicketUpdateRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersWorkflowEventRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IWorkflowEventRepository>(services);
        AssertServiceResolvable<IWorkflowEventRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersCheckpointRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ICheckpointRepository>(services);
        AssertServiceResolvable<ICheckpointRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersAgentPromptTemplateRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IAgentPromptTemplateRepository>(services);
        AssertServiceResolvable<IAgentPromptTemplateRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersErrorRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IErrorRepository>(services);
        AssertServiceResolvable<IErrorRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersUserRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IUserRepository>(services);
        AssertServiceResolvable<IUserRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersPlanReviewRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IPlanReviewRepository>(services);
        AssertServiceResolvable<IPlanReviewRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersReviewCommentRepository()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IReviewCommentRepository>(services);
        AssertServiceResolvable<IReviewCommentRepository>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersAllRepositories()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert - Should have 11 repositories total
        var repoCount = DIAssertions.CountServices(services, d =>
            d.ServiceType.Name.EndsWith("Repository") &&
            d.ServiceType.Namespace != null &&
            d.ServiceType.Namespace.StartsWith("PRFactory.") &&
            d.Lifetime == ServiceLifetime.Scoped);

        Assert.True(repoCount >= 11, $"Expected at least 11 repositories, found {repoCount}");
    }

    [Fact]
    public void AddInfrastructure_AllRepositoriesAreScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<ITenantRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IRepositoryRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ITicketRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ITicketUpdateRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IWorkflowEventRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ICheckpointRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IAgentPromptTemplateRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IErrorRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IUserRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IPlanReviewRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IReviewCommentRepository>(services, ServiceLifetime.Scoped);
    }

    #endregion

    #region Application Service Registration Tests

    [Fact]
    public void AddInfrastructure_RegistersTicketUpdateService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITicketUpdateService>(services);
        AssertServiceResolvable<ITicketUpdateService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersTicketApplicationService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITicketApplicationService>(services);
        AssertServiceResolvable<ITicketApplicationService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersRepositoryApplicationService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IRepositoryApplicationService>(services);
        AssertServiceResolvable<IRepositoryApplicationService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersTenantApplicationService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITenantApplicationService>(services);
        AssertServiceResolvable<ITenantApplicationService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersErrorApplicationService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IErrorApplicationService>(services);
        AssertServiceResolvable<IErrorApplicationService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersTenantContext()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITenantContext>(services);
        AssertServiceResolvable<ITenantContext>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersQuestionApplicationService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IQuestionApplicationService>(services);
        AssertServiceResolvable<IQuestionApplicationService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersWorkflowEventApplicationService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IWorkflowEventApplicationService>(services);
        AssertServiceResolvable<IWorkflowEventApplicationService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersPlanService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IPlanService>(services);
        AssertServiceResolvable<IPlanService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersUserService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IUserService>(services);
        AssertServiceResolvable<IUserService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersPlanReviewService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IPlanReviewService>(services);
        AssertServiceResolvable<IPlanReviewService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersCurrentUserService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ICurrentUserService>(services);
        AssertServiceResolvable<ICurrentUserService>(provider);
    }

    [Fact]
    public void AddInfrastructure_AllApplicationServicesAreScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<ITicketUpdateService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ITicketApplicationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IRepositoryApplicationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ITenantApplicationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IErrorApplicationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ITenantContext>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IQuestionApplicationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IWorkflowEventApplicationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IPlanService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IUserService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IPlanReviewService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ICurrentUserService>(services, ServiceLifetime.Scoped);
    }

    #endregion

    #region Agent Registration Tests

    [Theory]
    [InlineData(typeof(TriggerAgent))]
    [InlineData(typeof(RepositoryCloneAgent))]
    [InlineData(typeof(AnalysisAgent))]
    [InlineData(typeof(QuestionGenerationAgent))]
    [InlineData(typeof(JiraPostAgent))]
    [InlineData(typeof(HumanWaitAgent))]
    [InlineData(typeof(AnswerProcessingAgent))]
    [InlineData(typeof(PlanningAgent))]
    [InlineData(typeof(GitPlanAgent))]
    [InlineData(typeof(ImplementationAgent))]
    [InlineData(typeof(GitCommitAgent))]
    [InlineData(typeof(PullRequestAgent))]
    [InlineData(typeof(CompletionAgent))]
    [InlineData(typeof(ApprovalCheckAgent))]
    [InlineData(typeof(ErrorHandlingAgent))]
    public void AddInfrastructure_RegistersAgent(Type agentType)
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        DIAssertions.AssertImplementationRegistered(services, agentType);
    }

    [Theory]
    [InlineData(typeof(TriggerAgent))]
    [InlineData(typeof(RepositoryCloneAgent))]
    [InlineData(typeof(AnalysisAgent))]
    [InlineData(typeof(QuestionGenerationAgent))]
    [InlineData(typeof(JiraPostAgent))]
    [InlineData(typeof(HumanWaitAgent))]
    [InlineData(typeof(AnswerProcessingAgent))]
    [InlineData(typeof(PlanningAgent))]
    [InlineData(typeof(GitPlanAgent))]
    [InlineData(typeof(ImplementationAgent))]
    [InlineData(typeof(GitCommitAgent))]
    [InlineData(typeof(PullRequestAgent))]
    [InlineData(typeof(CompletionAgent))]
    [InlineData(typeof(ApprovalCheckAgent))]
    [InlineData(typeof(ErrorHandlingAgent))]
    public void AddInfrastructure_AgentIsTransient(Type agentType)
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        DIAssertions.AssertImplementationRegistered(services, agentType, ServiceLifetime.Transient);
    }

    #endregion

    #region Infrastructure Core Services Tests

    [Fact]
    public void AddInfrastructure_RegistersEncryptionService_WhenKeyConfigured()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IEncryptionService>(services);
        AssertServiceResolvable<IEncryptionService>(provider);
    }

    [Fact]
    public void AddInfrastructure_EncryptionServiceIsSingleton()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<IEncryptionService>(services, ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddInfrastructure_ThrowsInvalidOperationException_WhenEncryptionKeyMissing()
    {
        // Arrange
        var services = CreateServiceCollection();
        var config = TestConfigurationBuilder.CreateConfigurationWithoutEncryption();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(config));

        Assert.Contains("Encryption key not configured", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_RegistersApplicationDbContext()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ApplicationDbContext>(services);
        AssertServiceResolvable<ApplicationDbContext>(provider);
    }

    [Fact]
    public void AddInfrastructure_DbContextIsScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<ApplicationDbContext>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_RegistersTenantConfigurationService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Core.Application.Services.ITenantConfigurationService>(services);
        AssertServiceResolvable<PRFactory.Core.Application.Services.ITenantConfigurationService>(provider);
        AssertServiceRegistered<PRFactory.Infrastructure.Configuration.ITenantConfigurationService>(services);
        AssertServiceResolvable<PRFactory.Infrastructure.Configuration.ITenantConfigurationService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersMemoryCache()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var cache = provider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        Assert.NotNull(cache);
    }

    [Fact]
    public void AddInfrastructure_RegistersWorkflowStateStore()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Infrastructure.Agents.Graphs.IWorkflowStateStore>(services);
        AssertServiceResolvable<PRFactory.Infrastructure.Agents.Graphs.IWorkflowStateStore>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersEventPublisher()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Infrastructure.Agents.Graphs.IEventPublisher>(services);
        AssertServiceResolvable<PRFactory.Infrastructure.Agents.Graphs.IEventPublisher>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersCheckpointStoreAdapter()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ICheckpointStore>(services);
        AssertServiceResolvable<ICheckpointStore>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersContextBuilder()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Infrastructure.Claude.IContextBuilder>(services);
        AssertServiceResolvable<PRFactory.Infrastructure.Claude.IContextBuilder>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersAgentPromptService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Infrastructure.Agents.Services.IAgentPromptService>(services);
        AssertServiceResolvable<PRFactory.Infrastructure.Agents.Services.IAgentPromptService>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersAgentPromptLoaderService()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var loaderService = provider.GetService<PRFactory.Infrastructure.Agents.Services.AgentPromptLoaderService>();
        Assert.NotNull(loaderService);
    }

    [Fact]
    public void AddInfrastructure_RegistersAgentExecutor()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Infrastructure.Agents.Graphs.IAgentExecutor>(services);
        AssertServiceResolvable<PRFactory.Infrastructure.Agents.Graphs.IAgentExecutor>(provider);
    }

    #endregion

    #region CLI Agent Tests

    [Fact]
    public void AddInfrastructure_RegistersProcessExecutor()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IProcessExecutor>(services);
        AssertServiceResolvable<IProcessExecutor>(provider);
    }

    [Fact]
    public void AddInfrastructure_RegistersClaudeCodeCliAdapter()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var cliAdapter = provider.GetService<ClaudeCodeCliAdapter>();
        Assert.NotNull(cliAdapter);
    }

    [Fact]
    public void AddInfrastructure_RegistersCodexCliAdapter()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var cliAdapter = provider.GetService<CodexCliAdapter>();
        Assert.NotNull(cliAdapter);
    }

    [Fact]
    public void AddInfrastructure_RegistersDefaultCliAgent()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ICliAgent>(services);
        AssertServiceResolvable<ICliAgent>(provider);
    }

    #endregion

    #region Database Seeder Tests

    [Fact]
    public void AddInfrastructure_RegistersDbSeeder()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var seeder = provider.GetService<DbSeeder>();
        Assert.NotNull(seeder);
    }

    #endregion
}
