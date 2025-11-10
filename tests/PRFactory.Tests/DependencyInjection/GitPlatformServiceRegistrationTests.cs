using Microsoft.Extensions.DependencyInjection;
using PRFactory.Infrastructure;
using PRFactory.Infrastructure.Git;
using PRFactory.Infrastructure.Git.Providers;
using Xunit;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Tests for Git platform integration service registrations via AddGitPlatformIntegration()
/// </summary>
public class GitPlatformServiceRegistrationTests : DIValidationTestBase
{
    /// <summary>
    /// Creates a service collection with Git platform services registered
    /// </summary>
    private IServiceCollection CreateGitPlatformServiceCollection()
    {
        var services = CreateServiceCollection();
        var config = TestConfigurationBuilder.CreateTestConfiguration();

        // Register infrastructure (required for memory cache)
        services.AddInfrastructure(config);

        // Register Git platform integration
        services.AddGitPlatformIntegration();

        return services;
    }

    #region Local Git Service Tests

    [Fact]
    public void AddGitPlatformIntegration_RegistersLocalGitService()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<ILocalGitService>(services);
        AssertServiceResolvable<ILocalGitService>(provider);
    }

    [Fact]
    public void AddGitPlatformIntegration_LocalGitServiceIsScoped()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();

        // Assert
        AssertServiceLifetime<ILocalGitService>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGitPlatformIntegration_LocalGitServiceIsCorrectType()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILocalGitService>();
        Assert.IsType<LocalGitService>(service);
    }

    #endregion

    #region Platform Provider Registration Tests

    [Theory]
    [InlineData(typeof(GitHubProvider))]
    [InlineData(typeof(AzureDevOpsProvider))]
    [InlineData(typeof(BitbucketProvider))]
    public void AddGitPlatformIntegration_RegistersPlatformProvider(Type providerType)
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();

        // Assert
        DIAssertions.AssertImplementationRegistered(services, providerType);
    }

    [Fact]
    public void AddGitPlatformIntegration_RegistersGitHubProvider()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var providers = provider.GetServices<IGitPlatformProvider>().ToList();
        Assert.NotEmpty(providers);
        Assert.Contains(providers, p => p is GitHubProvider);
    }

    [Fact]
    public void AddGitPlatformIntegration_RegistersBitbucketProvider()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var providers = provider.GetServices<IGitPlatformProvider>().ToList();
        Assert.NotEmpty(providers);
        Assert.Contains(providers, p => p is BitbucketProvider);
    }

    [Fact]
    public void AddGitPlatformIntegration_RegistersAzureDevOpsProvider()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        var providers = provider.GetServices<IGitPlatformProvider>().ToList();
        Assert.NotEmpty(providers);
        Assert.Contains(providers, p => p is AzureDevOpsProvider);
    }

    [Fact]
    public void AddGitPlatformIntegration_RegistersAllThreePlatformProviders()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Should have 3 platform providers: GitHub, Bitbucket, Azure DevOps
        var providers = provider.GetServices<IGitPlatformProvider>().ToList();
        Assert.Equal(3, providers.Count);

        DIAssertions.AssertMultipleImplementationsRegistered<IGitPlatformProvider>(
            provider,
            typeof(GitHubProvider),
            typeof(BitbucketProvider),
            typeof(AzureDevOpsProvider));
    }

    [Fact]
    public void AddGitPlatformIntegration_AllPlatformProvidersAreScoped()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();

        // Assert - All platform providers should be Scoped
        var providerDescriptors = services
            .Where(d => d.ServiceType == typeof(IGitPlatformProvider))
            .ToList();

        Assert.NotEmpty(providerDescriptors);
        Assert.All(providerDescriptors, descriptor =>
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
    }

    #endregion

    #region Git Platform Service Facade Tests

    [Fact]
    public void AddGitPlatformIntegration_RegistersGitPlatformService()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        AssertServiceRegistered<IGitPlatformService>(services);
        AssertServiceResolvable<IGitPlatformService>(provider);
    }

    [Fact]
    public void AddGitPlatformIntegration_GitPlatformServiceIsScoped()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();

        // Assert
        AssertServiceLifetime<IGitPlatformService>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGitPlatformIntegration_GitPlatformServiceIsCorrectType()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGitPlatformService>();
        Assert.IsType<GitPlatformService>(service);
    }

    #endregion

    #region Dependency Tests

    [Fact]
    public void AddGitPlatformIntegration_RegistersMemoryCache()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Memory cache is required by GitPlatformService
        var cache = provider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        Assert.NotNull(cache);
    }

    [Fact]
    public void AddGitPlatformIntegration_CanResolveGitPlatformService_WithAllDependencies()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - Should resolve complete dependency chain:
        // IGitPlatformService
        //   ├─ ILocalGitService
        //   ├─ IEnumerable<IGitPlatformProvider>
        //   │  ├─ GitHubProvider
        //   │  ├─ BitbucketProvider
        //   │  └─ AzureDevOpsProvider
        //   ├─ IMemoryCache
        //   └─ ILogger<GitPlatformService>
        DIAssertions.AssertDependencyChainResolvable<IGitPlatformService>(provider);
    }

    [Fact]
    public void AddGitPlatformIntegration_CanResolveLocalGitService_WithDependencies()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        DIAssertions.AssertDependencyChainResolvable<ILocalGitService>(provider);
    }

    [Fact]
    public void AddGitPlatformIntegration_AllPlatformProviders_CanBeResolved()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert - All three providers should resolve independently
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var allProviders = sp.GetServices<IGitPlatformProvider>().ToList();
        Assert.Equal(3, allProviders.Count);

        // Verify each provider can be resolved
        foreach (var provider_ in allProviders)
        {
            Assert.NotNull(provider_);
            Assert.NotNull(provider_.PlatformName);
        }
    }

    #endregion

    #region Platform Provider Properties Tests

    [Fact]
    public void GitHubProvider_HasCorrectPlatformName()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        using var scope = provider.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IGitPlatformProvider>().ToList();
        var githubProvider = providers.FirstOrDefault(p => p is GitHubProvider);

        Assert.NotNull(githubProvider);
        Assert.Equal("GitHub", githubProvider.PlatformName);
    }

    [Fact]
    public void BitbucketProvider_HasCorrectPlatformName()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        using var scope = provider.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IGitPlatformProvider>().ToList();
        var bitbucketProvider = providers.FirstOrDefault(p => p is BitbucketProvider);

        Assert.NotNull(bitbucketProvider);
        Assert.Equal("Bitbucket", bitbucketProvider.PlatformName);
    }

    [Fact]
    public void AzureDevOpsProvider_HasCorrectPlatformName()
    {
        // Arrange & Act
        var services = CreateGitPlatformServiceCollection();
        using var provider = BuildServiceProvider(services);

        // Assert
        using var scope = provider.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IGitPlatformProvider>().ToList();
        var azureProvider = providers.FirstOrDefault(p => p is AzureDevOpsProvider);

        Assert.NotNull(azureProvider);
        Assert.Equal("AzureDevOps", azureProvider.PlatformName);
    }

    #endregion
}
