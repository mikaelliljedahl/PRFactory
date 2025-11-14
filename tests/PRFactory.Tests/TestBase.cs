using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure;
using PRFactory.Infrastructure.Agents.Graphs;
using PRFactory.Infrastructure.Git;
using PRFactory.Infrastructure.Persistence;

namespace PRFactory.Tests;

/// <summary>
/// Base class for all tests providing common setup and utilities.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected ApplicationDbContext DbContext { get; }
    protected IConfiguration Configuration { get; }
    private bool _disposed;

    protected TestBase()
    {
        var services = new ServiceCollection();

        // Configure test configuration
        Configuration = CreateTestConfiguration();
        services.AddSingleton(Configuration);

        // Add logging
        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        // Add HttpClient
        services.AddHttpClient();

        // Configure in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
                .EnableServiceProviderCaching(false) // Disable caching to avoid EF Core warning about too many service providers
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))); // Suppress warning for test scenarios

        // Add infrastructure services
        services.AddInfrastructure(Configuration);

        // Add Git platform integration
        services.AddGitPlatformIntegration();

        // Add agent graphs
        services.AddAgentGraphs();

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Creates a test configuration with required settings
    /// </summary>
    protected virtual IConfiguration CreateTestConfiguration()
    {
        var configDict = new Dictionary<string, string?>
        {
            { "Encryption:Key", "dGVzdC1lbmNyeXB0aW9uLWtleS12YWxpZC1iYXNlNjQ=" },
            { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
            { "Logging:EnableSensitiveDataLogging", "false" },
            { "Logging:EnableDetailedErrors", "false" },
            { "AgentHost:Enabled", "true" },
            { "ClaudeCodeCli:Path", "/usr/local/bin/claude" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DbContext?.Dispose();
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
