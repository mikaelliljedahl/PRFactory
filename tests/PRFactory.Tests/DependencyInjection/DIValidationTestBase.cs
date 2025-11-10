using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Base class for dependency injection validation tests.
/// Provides common setup and helper methods.
/// </summary>
public abstract class DIValidationTestBase
{
    /// <summary>
    /// Creates a service collection with logging configured
    /// </summary>
    protected IServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        // Create and register configuration
        var config = TestConfigurationBuilder.CreateTestConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Add HttpClient (required by many services)
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Creates a service collection with infrastructure services registered
    /// </summary>
    protected IServiceCollection CreateInfrastructureServiceCollection(IConfiguration? configuration = null)
    {
        var services = CreateServiceCollection();
        var config = configuration ?? TestConfigurationBuilder.CreateTestConfiguration();
        services.AddInfrastructure(config);
        return services;
    }

    /// <summary>
    /// Creates a service provider from a service collection
    /// </summary>
    protected ServiceProvider BuildServiceProvider(IServiceCollection services)
    {
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with infrastructure services
    /// </summary>
    protected ServiceProvider CreateInfrastructureServiceProvider(IConfiguration? configuration = null)
    {
        var services = CreateInfrastructureServiceCollection(configuration);
        return BuildServiceProvider(services);
    }

    /// <summary>
    /// Asserts that a service is registered
    /// </summary>
    protected void AssertServiceRegistered<TInterface>(IServiceCollection services)
    {
        DIAssertions.AssertServiceRegistered<TInterface>(services);
    }

    /// <summary>
    /// Asserts that a service can be resolved
    /// </summary>
    protected void AssertServiceResolvable<TInterface>(ServiceProvider provider)
    {
        DIAssertions.AssertServiceResolvable<TInterface>(provider);
    }

    /// <summary>
    /// Asserts service lifetime
    /// </summary>
    protected void AssertServiceLifetime<TInterface>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        DIAssertions.AssertServiceLifetime<TInterface>(services, expectedLifetime);
    }
}
