using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Helper assertions for dependency injection validation tests
/// </summary>
public static class DIAssertions
{
    /// <summary>
    /// Asserts that a service is registered in the service collection
    /// </summary>
    public static void AssertServiceRegistered<TInterface>(
        IServiceCollection services,
        Type? expectedImplementation = null)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        Assert.NotNull(descriptor);

        if (expectedImplementation != null && descriptor!.ImplementationType != null)
        {
            Assert.Equal(expectedImplementation, descriptor.ImplementationType);
        }
    }

    /// <summary>
    /// Asserts that a service can be resolved from the service provider
    /// </summary>
    public static void AssertServiceResolvable<TInterface>(IServiceProvider provider)
    {
        var service = provider.GetService<TInterface>();
        Assert.NotNull(service);
    }

    /// <summary>
    /// Asserts that a service is registered with the expected lifetime
    /// </summary>
    public static void AssertServiceLifetime<TInterface>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor!.Lifetime);
    }

    /// <summary>
    /// Asserts that a service can be resolved without throwing (validates dependency chain)
    /// </summary>
    public static void AssertDependencyChainResolvable<TInterface>(IServiceProvider provider)
    {
        // Use scoped provider to properly resolve scoped services
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TInterface>();
        Assert.NotNull(service);
    }

    /// <summary>
    /// Asserts that multiple implementations of the same interface are registered
    /// </summary>
    public static void AssertMultipleImplementationsRegistered<TInterface>(
        IServiceProvider provider,
        params Type[] expectedTypes)
    {
        var implementations = provider.GetServices<TInterface>().ToList();
        Assert.NotEmpty(implementations);

        foreach (var expectedType in expectedTypes)
        {
            Assert.True(
                implementations.Any(impl => impl!.GetType() == expectedType),
                $"Expected implementation {expectedType.Name} not found");
        }
    }

    /// <summary>
    /// Asserts that a service with a specific implementation type is registered
    /// </summary>
    public static void AssertImplementationRegistered(
        IServiceCollection services,
        Type implementationType,
        ServiceLifetime? expectedLifetime = null)
    {
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == implementationType);
        Assert.NotNull(descriptor);

        if (expectedLifetime.HasValue)
        {
            Assert.Equal(expectedLifetime.Value, descriptor!.Lifetime);
        }
    }

    /// <summary>
    /// Counts services matching a predicate
    /// </summary>
    public static int CountServices(IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
    {
        return services.Count(predicate);
    }
}
