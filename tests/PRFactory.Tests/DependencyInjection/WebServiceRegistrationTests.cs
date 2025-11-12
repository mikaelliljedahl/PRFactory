using Microsoft.Extensions.DependencyInjection;
using PRFactory.Infrastructure;
using PRFactory.Web.Services;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Tests for Web layer service registrations (Program.cs)
/// </summary>
public class WebServiceRegistrationTests : DIValidationTestBase
{
    /// <summary>
    /// Creates a service collection with Web layer services registered
    /// </summary>
    private IServiceCollection CreateWebServiceCollection()
    {
        var services = CreateServiceCollection();
        var config = TestConfigurationBuilder.CreateTestConfiguration();

        // Register Infrastructure services (required dependency)
        services.AddInfrastructure(config);

        // Register SignalR event broadcaster
        services.AddScoped<PRFactory.Infrastructure.Events.IEventBroadcaster,
            PRFactory.Web.Services.SignalREventBroadcaster>();

        // Register web layer facade services (from Program.cs)
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IRepositoryService, RepositoryService>();
        services.AddScoped<IWorkflowEventService, WorkflowEventService>();
        services.AddScoped<IAgentPromptService, AgentPromptService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IErrorService, ErrorService>();
        services.AddScoped<IToastService, ToastService>();

        return services;
    }

    #region Web Facade Service Tests

    [Theory]
    [InlineData(typeof(ITicketService), typeof(TicketService))]
    [InlineData(typeof(IRepositoryService), typeof(RepositoryService))]
    [InlineData(typeof(IWorkflowEventService), typeof(WorkflowEventService))]
    [InlineData(typeof(IAgentPromptService), typeof(AgentPromptService))]
    [InlineData(typeof(ITenantService), typeof(TenantService))]
    [InlineData(typeof(IErrorService), typeof(ErrorService))]
    [InlineData(typeof(IToastService), typeof(ToastService))]
    public void WebProgram_RegistersWebFacadeService(Type serviceType, Type implementationType)
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.NotNull(descriptor);
        Assert.Equal(implementationType, descriptor!.ImplementationType);

        var service = provider.GetService(serviceType);
        Assert.NotNull(service);
    }

    [Fact]
    public void WebProgram_RegistersTicketService()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITicketService>(services);
        AssertServiceResolvable<ITicketService>(provider);
    }

    [Fact]
    public void WebProgram_RegistersRepositoryService()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IRepositoryService>(services);
        AssertServiceResolvable<IRepositoryService>(provider);
    }

    [Fact]
    public void WebProgram_RegistersWorkflowEventService()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IWorkflowEventService>(services);
        AssertServiceResolvable<IWorkflowEventService>(provider);
    }

    [Fact]
    public void WebProgram_RegistersAgentPromptService()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Web.Services.IAgentPromptService>(services);
        AssertServiceResolvable<PRFactory.Web.Services.IAgentPromptService>(provider);
    }

    [Fact]
    public void WebProgram_RegistersTenantService()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ITenantService>(services);
        AssertServiceResolvable<ITenantService>(provider);
    }

    [Fact]
    public void WebProgram_RegistersErrorService()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IErrorService>(services);
        AssertServiceResolvable<IErrorService>(provider);
    }

    [Fact]
    public void WebProgram_RegistersToastService()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IToastService>(services);
        AssertServiceResolvable<IToastService>(provider);
    }

    [Fact]
    public void WebProgram_AllWebFacadeServicesAreScoped()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();

        // Assert
        AssertServiceLifetime<ITicketService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IRepositoryService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IWorkflowEventService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<PRFactory.Web.Services.IAgentPromptService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ITenantService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IErrorService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IToastService>(services, ServiceLifetime.Scoped);
    }

    #endregion

    #region SignalR Tests

    [Fact]
    public void WebProgram_RegistersSignalREventBroadcaster()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<PRFactory.Infrastructure.Events.IEventBroadcaster>(services);
        AssertServiceResolvable<PRFactory.Infrastructure.Events.IEventBroadcaster>(provider);
    }

    [Fact]
    public void WebProgram_SignalREventBroadcasterIsScoped()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();

        // Assert
        AssertServiceLifetime<PRFactory.Infrastructure.Events.IEventBroadcaster>(services, ServiceLifetime.Scoped);
    }

    #endregion

    #region Database Seeder Tests

    [Fact]
    public void WebProgram_CanResolveDbSeeder()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var seeder = provider.GetService<PRFactory.Infrastructure.Persistence.DbSeeder>();
        Assert.NotNull(seeder);
    }

    #endregion

    #region Dependency Chain Tests

    [Fact]
    public void WebProgram_CanResolveTicketService_WithAllInfrastructureDependencies()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Should resolve complete dependency chain
        DIAssertions.AssertDependencyChainResolvable<ITicketService>(provider);
    }

    [Fact]
    public void WebProgram_CanResolveRepositoryService_WithAllDependencies()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Should resolve complete dependency chain
        DIAssertions.AssertDependencyChainResolvable<IRepositoryService>(provider);
    }

    [Fact]
    public void WebProgram_CanResolveWorkflowEventService_WithAllDependencies()
    {
        // Arrange & Act
        var services = CreateWebServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Should resolve complete dependency chain
        DIAssertions.AssertDependencyChainResolvable<IWorkflowEventService>(provider);
    }

    #endregion
}
