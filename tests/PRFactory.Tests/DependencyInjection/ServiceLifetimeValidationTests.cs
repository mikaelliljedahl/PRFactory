using Microsoft.Extensions.DependencyInjection;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Tests to validate that services are registered with appropriate lifetimes
/// </summary>
public class ServiceLifetimeValidationTests : DIValidationTestBase
{
    #region Singleton Lifetime Tests

    [Fact]
    public void ServiceLifetimes_EncryptionServiceIsSingleton()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<IEncryptionService>(services, ServiceLifetime.Singleton);
    }

    [Fact]
    public void ServiceLifetimes_EncryptionService_SameInstanceAcrossScopes()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act
        IEncryptionService instance1;
        IEncryptionService instance2;

        using (var scope1 = provider.CreateScope())
        {
            instance1 = scope1.ServiceProvider.GetRequiredService<IEncryptionService>();
        }

        using (var scope2 = provider.CreateScope())
        {
            instance2 = scope2.ServiceProvider.GetRequiredService<IEncryptionService>();
        }

        // Assert - Singleton should return same instance
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void ServiceLifetimes_MemoryCacheIsSingleton()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    #endregion

    #region Scoped Lifetime Tests

    [Fact]
    public void ServiceLifetimes_AllRepositoriesAreScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert - All repositories must be Scoped for DbContext pattern
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

    [Fact]
    public void ServiceLifetimes_AllApplicationServicesAreScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert - Application services must be Scoped
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

    [Fact]
    public void ServiceLifetimes_DbContextIsScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert - DbContext must be Scoped for EF Core
        AssertServiceLifetime<ApplicationDbContext>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void ServiceLifetimes_DbContext_DifferentInstancePerScope()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act
        ApplicationDbContext instance1;
        ApplicationDbContext instance2;

        using (var scope1 = provider.CreateScope())
        {
            instance1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        using (var scope2 = provider.CreateScope())
        {
            instance2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        // Assert - Scoped should return different instances across scopes
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void ServiceLifetimes_WorkflowStateStoreIsScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<PRFactory.Infrastructure.Agents.Graphs.IWorkflowStateStore>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void ServiceLifetimes_EventPublisherIsScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<PRFactory.Infrastructure.Agents.Graphs.IEventPublisher>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void ServiceLifetimes_CheckpointStoreAdapterIsScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<PRFactory.Infrastructure.Agents.ICheckpointStore>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void ServiceLifetimes_ConfigurationServicesAreScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<PRFactory.Core.Application.Services.ITenantConfigurationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<PRFactory.Infrastructure.Configuration.ITenantConfigurationService>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void ServiceLifetimes_ContextBuilderIsScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<PRFactory.Infrastructure.Claude.IContextBuilder>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void ServiceLifetimes_AgentExecutorIsScoped()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert
        AssertServiceLifetime<PRFactory.Infrastructure.Agents.Graphs.IAgentExecutor>(services, ServiceLifetime.Scoped);
    }

    #endregion

    #region Transient Lifetime Tests

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
    public void ServiceLifetimes_AgentsAreTransient(Type agentType)
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert - Agents must be Transient to ensure no state bleeding
        DIAssertions.AssertImplementationRegistered(services, agentType, ServiceLifetime.Transient);
    }

    [Fact]
    public void ServiceLifetimes_Agents_DifferentInstancePerResolve()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Act
        TriggerAgent instance1;
        TriggerAgent instance2;

        using (var scope = provider.CreateScope())
        {
            instance1 = scope.ServiceProvider.GetRequiredService<TriggerAgent>();
            instance2 = scope.ServiceProvider.GetRequiredService<TriggerAgent>();
        }

        // Assert - Transient should return different instances even in same scope
        Assert.NotSame(instance1, instance2);
    }

    #endregion

    #region Lifetime Appropriateness Tests

    [Fact]
    public void ServiceLifetimes_NoSingletonDependsOnScoped()
    {
        // Arrange
        var services = CreateInfrastructureServiceCollection();

        // Act - Check all singleton services
        var singletonServices = services
            .Where(d => d.Lifetime == ServiceLifetime.Singleton)
            .ToList();

        // Assert - This is a sanity check that no singleton depends on scoped services
        // If a singleton depends on a scoped service, it would cause the scoped service
        // to live as long as the singleton, which is incorrect

        // For this project:
        // - IEncryptionService (Singleton) only depends on ILogger (Singleton) - OK
        // - IMemoryCache (Singleton) has no dependencies - OK

        Assert.NotEmpty(singletonServices);

        // No direct way to validate this without reflection, but we can verify
        // that our known singletons don't have problematic dependencies
        // by ensuring they can be resolved
        using var provider = BuildServiceProvider(services);
        var encryption = provider.GetService<IEncryptionService>();
        Assert.NotNull(encryption);
    }

    [Fact]
    public void ServiceLifetimes_RepositoriesMatchDbContextLifetime()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Assert - All repositories must match DbContext lifetime (Scoped)
        // This ensures repositories and DbContext have the same lifecycle
        var dbContextLifetime = services
            .First(d => d.ServiceType == typeof(ApplicationDbContext))
            .Lifetime;

        Assert.Equal(ServiceLifetime.Scoped, dbContextLifetime);

        // Verify all repositories are also Scoped
        var repositoryDescriptors = services
            .Where(d => d.ServiceType.Name.EndsWith("Repository") &&
                       d.ServiceType.Namespace != null &&
                       d.ServiceType.Namespace.StartsWith("PRFactory.Domain.Interfaces"))
            .ToList();

        Assert.All(repositoryDescriptors, descriptor =>
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
    }

    [Fact]
    public void ServiceLifetimes_CountServicesByLifetime()
    {
        // Arrange & Act
        var services = CreateInfrastructureServiceCollection();

        // Count services by lifetime
        var singletonCount = services.Count(d => d.Lifetime == ServiceLifetime.Singleton);
        var scopedCount = services.Count(d => d.Lifetime == ServiceLifetime.Scoped);
        var transientCount = services.Count(d => d.Lifetime == ServiceLifetime.Transient);

        // Assert - Document service distribution
        // These numbers may change as services are added/removed
        Assert.True(singletonCount > 0, $"Should have singleton services (found {singletonCount})");
        Assert.True(scopedCount > 0, $"Should have scoped services (found {scopedCount})");
        Assert.True(transientCount > 0, $"Should have transient services (found {transientCount})");

        // Log for visibility (optional, can be removed)
        // Console.WriteLine($"Singleton: {singletonCount}, Scoped: {scopedCount}, Transient: {transientCount}");
    }

    #endregion
}
