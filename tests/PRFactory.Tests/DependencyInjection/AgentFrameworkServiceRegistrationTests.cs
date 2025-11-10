using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PRFactory.Infrastructure;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Base.Middleware;
using PRFactory.Infrastructure.Agents.Configuration;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Tests for Agent Framework service registrations via AddAgentFramework()
/// </summary>
public class AgentFrameworkServiceRegistrationTests : DIValidationTestBase
{
    /// <summary>
    /// Creates a service collection with Agent Framework services registered
    /// </summary>
    private IServiceCollection CreateAgentFrameworkServiceCollection(
        bool enableLogging = true,
        bool enableErrorHandling = true,
        bool enableRetry = true)
    {
        var services = CreateServiceCollection();

        // Create configuration with agent framework settings
        var configDict = new Dictionary<string, string?>
        {
            { "Agent:Enabled", "true" },
            { "Agent:DefaultTimeoutSeconds", "300" },
            { "Agent:MaxConcurrentExecutions", "5" },
            { "Agent:EnableCheckpoints", "true" },
            { "Agent:CheckpointRetentionDays", "7" },
            { "Agent:Middleware:EnableLogging", enableLogging.ToString() },
            { "Agent:Middleware:EnableErrorHandling", enableErrorHandling.ToString() },
            { "Agent:Middleware:EnableRetry", enableRetry.ToString() },
            { "Agent:Retry:MaxRetryAttempts", "3" },
            { "Agent:Retry:InitialDelaySeconds", "1" },
            { "Agent:Retry:MaxDelaySeconds", "60" },
            { "Agent:ErrorHandling:EnableDetailedErrors", "true" },
            { "Agent:ErrorHandling:LogErrors", "true" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Register Agent Framework
        services.AddAgentFramework(config);

        return services;
    }

    #region Core Framework Tests

    [Fact]
    public void AddAgentFramework_RegistersAgentConfiguration()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Configuration should be registered via IOptions pattern
        var config = provider.GetService<Microsoft.Extensions.Options.IOptions<AgentConfiguration>>();
        Assert.NotNull(config);
        Assert.NotNull(config.Value);
    }

    [Fact]
    public void AddAgentFramework_ConfigurationIsValid()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var config = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentConfiguration>>().Value;
        Assert.True(config.Enabled);
        Assert.Equal(300, config.DefaultTimeoutSeconds);
        Assert.Equal(5, config.MaxConcurrentExecutions);
        Assert.True(config.EnableCheckpoints);
        Assert.Equal(7, config.CheckpointRetentionDays);
    }

    [Fact]
    public void AddAgentFramework_RegistersCheckpointRepository()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var checkpointRepo = provider.GetService<ICheckpointRepository>();
        Assert.NotNull(checkpointRepo);
    }

    [Fact]
    public void AddAgentFramework_CheckpointRepositoryIsSingleton()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICheckpointRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    #endregion

    #region Middleware Registration Tests

    [Fact]
    public void AddAgentFramework_RegistersLoggingMiddleware_WhenEnabled()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableLogging: true);
        using var provider = BuildServiceProvider(services);

        // Assert
        var middleware = provider.GetService<LoggingMiddleware>();
        Assert.NotNull(middleware);
    }

    [Fact]
    public void AddAgentFramework_DoesNotRegisterLoggingMiddleware_WhenDisabled()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableLogging: false);
        using var provider = BuildServiceProvider(services);

        // Assert
        var middleware = provider.GetService<LoggingMiddleware>();
        Assert.Null(middleware);
    }

    [Fact]
    public void AddAgentFramework_LoggingMiddlewareIsSingleton()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableLogging: true);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(LoggingMiddleware));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    [Fact]
    public void AddAgentFramework_RegistersErrorHandlingMiddleware_WhenEnabled()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableErrorHandling: true);
        using var provider = BuildServiceProvider(services);

        // Assert
        var middleware = provider.GetService<ErrorHandlingMiddleware>();
        Assert.NotNull(middleware);
    }

    [Fact]
    public void AddAgentFramework_DoesNotRegisterErrorHandlingMiddleware_WhenDisabled()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableErrorHandling: false);
        using var provider = BuildServiceProvider(services);

        // Assert
        var middleware = provider.GetService<ErrorHandlingMiddleware>();
        Assert.Null(middleware);
    }

    [Fact]
    public void AddAgentFramework_ErrorHandlingMiddlewareIsSingleton()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableErrorHandling: true);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(ErrorHandlingMiddleware));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    [Fact]
    public void AddAgentFramework_RegistersRetryMiddleware_WhenEnabled()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableRetry: true);
        using var provider = BuildServiceProvider(services);

        // Assert
        var middleware = provider.GetService<RetryMiddleware>();
        Assert.NotNull(middleware);
    }

    [Fact]
    public void AddAgentFramework_DoesNotRegisterRetryMiddleware_WhenDisabled()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableRetry: false);
        using var provider = BuildServiceProvider(services);

        // Assert
        var middleware = provider.GetService<RetryMiddleware>();
        Assert.Null(middleware);
    }

    [Fact]
    public void AddAgentFramework_RetryMiddlewareIsSingleton()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(enableRetry: true);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(RetryMiddleware));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor!.Lifetime);
    }

    [Fact]
    public void AddAgentFramework_RegistersAllMiddleware_WhenAllEnabled()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(
            enableLogging: true,
            enableErrorHandling: true,
            enableRetry: true);
        using var provider = BuildServiceProvider(services);

        // Assert
        var loggingMiddleware = provider.GetService<LoggingMiddleware>();
        var errorMiddleware = provider.GetService<ErrorHandlingMiddleware>();
        var retryMiddleware = provider.GetService<RetryMiddleware>();

        Assert.NotNull(loggingMiddleware);
        Assert.NotNull(errorMiddleware);
        Assert.NotNull(retryMiddleware);
    }

    #endregion

    #region Middleware Lifetime Tests

    [Fact]
    public void AddAgentFramework_AllMiddlewareAreSingleton()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(
            enableLogging: true,
            enableErrorHandling: true,
            enableRetry: true);

        // Assert - All middleware should be Singleton for performance
        var middlewareDescriptors = services
            .Where(d => d.ImplementationType != null &&
                       (d.ImplementationType == typeof(LoggingMiddleware) ||
                        d.ImplementationType == typeof(ErrorHandlingMiddleware) ||
                        d.ImplementationType == typeof(RetryMiddleware)))
            .ToList();

        Assert.NotEmpty(middlewareDescriptors);
        Assert.All(middlewareDescriptors, descriptor =>
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime));
    }

    [Fact]
    public void AddAgentFramework_Middleware_SameInstanceAcrossScopes()
    {
        // Arrange
        var services = CreateAgentFrameworkServiceCollection(enableLogging: true);
        using var provider = BuildServiceProvider(services);

        // Act
        LoggingMiddleware instance1;
        LoggingMiddleware instance2;

        using (var scope1 = provider.CreateScope())
        {
            instance1 = scope1.ServiceProvider.GetRequiredService<LoggingMiddleware>();
        }

        using (var scope2 = provider.CreateScope())
        {
            instance2 = scope2.ServiceProvider.GetRequiredService<LoggingMiddleware>();
        }

        // Assert - Singleton should return same instance
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public void AddAgentFramework_ValidatesConfiguration()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Create invalid configuration (negative timeout)
        var configDict = new Dictionary<string, string?>
        {
            { "Agent:Enabled", "true" },
            { "Agent:DefaultTimeoutSeconds", "-1" }, // Invalid!
            { "Agent:MaxConcurrentExecutions", "5" },
            { "Agent:Middleware:EnableLogging", "true" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act & Assert - Should throw validation exception
        Assert.Throws<InvalidOperationException>(() =>
            services.AddAgentFramework(config));
    }

    [Fact]
    public void AddAgentFramework_ThrowsException_WhenConfigurationInvalid()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Create invalid configuration (zero max concurrent executions)
        var configDict = new Dictionary<string, string?>
        {
            { "Agent:Enabled", "true" },
            { "Agent:DefaultTimeoutSeconds", "300" },
            { "Agent:MaxConcurrentExecutions", "0" }, // Invalid!
            { "Agent:Middleware:EnableLogging", "true" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddAgentFramework(config));
    }

    #endregion

    #region Dependency Chain Tests

    [Fact]
    public void AddAgentFramework_CanResolveCheckpointRepository_WithDependencies()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Should resolve:
        // ICheckpointRepository (InMemoryCheckpointRepository)
        //   └─ ILogger<InMemoryCheckpointRepository>
        var checkpointRepo = provider.GetRequiredService<ICheckpointRepository>();
        Assert.NotNull(checkpointRepo);
    }

    [Fact]
    public void AddAgentFramework_CanResolveAllMiddleware_WithDependencies()
    {
        // Arrange & Act
        var services = CreateAgentFrameworkServiceCollection(
            enableLogging: true,
            enableErrorHandling: true,
            enableRetry: true);
        using var provider = BuildServiceProvider(services);

        // Assert - All middleware should resolve with their logger dependencies
        var loggingMiddleware = provider.GetRequiredService<LoggingMiddleware>();
        var errorMiddleware = provider.GetRequiredService<ErrorHandlingMiddleware>();
        var retryMiddleware = provider.GetRequiredService<RetryMiddleware>();

        Assert.NotNull(loggingMiddleware);
        Assert.NotNull(errorMiddleware);
        Assert.NotNull(retryMiddleware);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddAgentFramework_CanBeUsedWithInfrastructure()
    {
        // Arrange
        var services = CreateServiceCollection();
        var config = TestConfigurationBuilder.CreateTestConfiguration();

        // Create agent framework config
        var agentConfigDict = new Dictionary<string, string?>
        {
            { "Agent:Enabled", "true" },
            { "Agent:DefaultTimeoutSeconds", "300" },
            { "Agent:MaxConcurrentExecutions", "5" },
            { "Agent:Middleware:EnableLogging", "true" }
        };

        var fullConfigDict = config.AsEnumerable()
            .Concat(agentConfigDict)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var fullConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(fullConfigDict)
            .Build();

        // Act - Register both Infrastructure and Agent Framework
        services.AddInfrastructure(fullConfig);
        services.AddAgentFramework(fullConfig);

        // Assert - Both should be resolvable
        using var provider = BuildServiceProvider(services);

        // Infrastructure services
        var tenantRepo = provider.GetService<PRFactory.Domain.Interfaces.ITenantRepository>();
        Assert.NotNull(tenantRepo);

        // Agent framework services
        var checkpointRepo = provider.GetService<ICheckpointRepository>();
        Assert.NotNull(checkpointRepo);

        var loggingMiddleware = provider.GetService<LoggingMiddleware>();
        Assert.NotNull(loggingMiddleware);
    }

    #endregion
}
