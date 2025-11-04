using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Base.Middleware;

namespace PRFactory.Infrastructure.Agents.Configuration;

/// <summary>
/// Extension methods for configuring agent framework services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the PRFactory agent framework to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration containing agent settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var agentConfig = configuration
            .GetSection(AgentConfiguration.SectionName)
            .Get<AgentConfiguration>() ?? new AgentConfiguration();

        // Validate configuration
        agentConfig.Validate();

        // Register configuration
        services.Configure<AgentConfiguration>(
            configuration.GetSection(AgentConfiguration.SectionName));

        // Register core services
        services.AddAgentRegistry();
        services.AddSingleton<IAgentGraphBuilder, AgentGraphBuilder>();

        // Register middleware
        if (agentConfig.Middleware.EnableLogging)
        {
            services.AddSingleton<LoggingMiddleware>();
        }

        if (agentConfig.Middleware.EnableTelemetry)
        {
            services.AddSingleton<TelemetryMiddleware>();
        }

        if (agentConfig.Middleware.EnableErrorHandling)
        {
            services.AddSingleton(sp => new ErrorHandlingMiddleware(
                sp.GetRequiredService<ILogger<ErrorHandlingMiddleware>>(),
                agentConfig.ErrorHandling.ToErrorHandlingOptions()));
        }

        if (agentConfig.Middleware.EnableRetry)
        {
            services.AddSingleton(sp => new RetryMiddleware(
                sp.GetRequiredService<ILogger<RetryMiddleware>>(),
                agentConfig.Retry.ToRetryOptions()));
        }

        // Register checkpoint repository (placeholder - implement based on storage choice)
        services.AddSingleton<ICheckpointRepository, InMemoryCheckpointRepository>();

        return services;
    }

    /// <summary>
    /// Adds the PRFactory agent framework with custom configuration.
    /// </summary>
    public static IServiceCollection AddAgentFramework(
        this IServiceCollection services,
        Action<AgentConfiguration> configureOptions)
    {
        var config = new AgentConfiguration();
        configureOptions(config);
        config.Validate();

        services.Configure<AgentConfiguration>(opts =>
        {
            opts.Enabled = config.Enabled;
            opts.DefaultTimeoutSeconds = config.DefaultTimeoutSeconds;
            opts.MaxConcurrentExecutions = config.MaxConcurrentExecutions;
            opts.EnableCheckpoints = config.EnableCheckpoints;
            opts.CheckpointRetentionDays = config.CheckpointRetentionDays;
            opts.Middleware = config.Middleware;
            opts.Retry = config.Retry;
            opts.Telemetry = config.Telemetry;
            opts.ErrorHandling = config.ErrorHandling;
        });

        return AddAgentFramework(services, config);
    }

    private static IServiceCollection AddAgentFramework(
        this IServiceCollection services,
        AgentConfiguration config)
    {
        // Register core services
        services.AddAgentRegistry();
        services.AddSingleton<IAgentGraphBuilder, AgentGraphBuilder>();

        // Register middleware
        if (config.Middleware.EnableLogging)
        {
            services.AddSingleton<LoggingMiddleware>();
        }

        if (config.Middleware.EnableTelemetry)
        {
            services.AddSingleton<TelemetryMiddleware>();
        }

        if (config.Middleware.EnableErrorHandling)
        {
            services.AddSingleton(sp => new ErrorHandlingMiddleware(
                sp.GetRequiredService<ILogger<ErrorHandlingMiddleware>>(),
                config.ErrorHandling.ToErrorHandlingOptions()));
        }

        if (config.Middleware.EnableRetry)
        {
            services.AddSingleton(sp => new RetryMiddleware(
                sp.GetRequiredService<ILogger<RetryMiddleware>>(),
                config.Retry.ToRetryOptions()));
        }

        // Register checkpoint repository
        services.AddSingleton<ICheckpointRepository, InMemoryCheckpointRepository>();

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry for agent framework tracing and metrics.
    /// </summary>
    public static IServiceCollection AddAgentFrameworkTelemetry(
        this IServiceCollection services,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        // Add OpenTelemetry tracing
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource("PRFactory.Agents")
                    .AddSource("PRFactory.Agent.*");

                configureTracing?.Invoke(builder);
            })
            .WithMetrics(builder =>
            {
                builder.AddMeter("PRFactory.Agents");

                configureMetrics?.Invoke(builder);
            });

        return services;
    }

    /// <summary>
    /// Configures the agent framework after all services are registered.
    /// Call this in your Program.cs after building the service provider.
    /// </summary>
    public static IServiceProvider ConfigureAgentFramework(
        this IServiceProvider serviceProvider)
    {
        // Initialize the agent registry with all registered agents
        serviceProvider.ConfigureAgentRegistry();

        return serviceProvider;
    }
}

/// <summary>
/// In-memory implementation of checkpoint repository for development/testing.
/// Replace with database-backed implementation for production.
/// </summary>
internal class InMemoryCheckpointRepository : ICheckpointRepository
{
    private readonly Dictionary<string, AgentCheckpoint> _checkpoints = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryCheckpointRepository> _logger;

    public InMemoryCheckpointRepository(ILogger<InMemoryCheckpointRepository> logger)
    {
        _logger = logger;
    }

    public Task SaveCheckpointAsync(
        AgentCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKey(checkpoint.TicketId, checkpoint.AgentName);
            _checkpoints[key] = checkpoint;

            _logger.LogDebug(
                "Saved checkpoint {CheckpointId} for ticket {TicketId}, agent {AgentName}",
                checkpoint.CheckpointId,
                checkpoint.TicketId,
                checkpoint.AgentName);
        }

        return Task.CompletedTask;
    }

    public Task<AgentCheckpoint?> GetLatestCheckpointAsync(
        string ticketId,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKey(ticketId, agentName);
            _checkpoints.TryGetValue(key, out var checkpoint);
            return Task.FromResult(checkpoint);
        }
    }

    public Task<List<AgentCheckpoint>> GetCheckpointsAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var checkpoints = _checkpoints.Values
                .Where(c => c.TicketId == ticketId)
                .OrderByDescending(c => c.Timestamp)
                .ToList();

            return Task.FromResult(checkpoints);
        }
    }

    public Task<AgentCheckpoint?> GetCheckpointByIdAsync(
        string checkpointId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var checkpoint = _checkpoints.Values
                .FirstOrDefault(c => c.CheckpointId == checkpointId);

            return Task.FromResult(checkpoint);
        }
    }

    public Task DeleteCheckpointAsync(
        string checkpointId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var toRemove = _checkpoints
                .Where(kvp => kvp.Value.CheckpointId == checkpointId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _checkpoints.Remove(key);
            }

            _logger.LogDebug("Deleted checkpoint {CheckpointId}", checkpointId);
        }

        return Task.CompletedTask;
    }

    public Task DeleteTicketCheckpointsAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var toRemove = _checkpoints
                .Where(kvp => kvp.Value.TicketId == ticketId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _checkpoints.Remove(key);
            }

            _logger.LogDebug(
                "Deleted {Count} checkpoints for ticket {TicketId}",
                toRemove.Count,
                ticketId);
        }

        return Task.CompletedTask;
    }

    private static string GetKey(string ticketId, string agentName)
    {
        return $"{ticketId}:{agentName}";
    }
}

/// <summary>
/// Fluent configuration builder for agent framework.
/// </summary>
public class AgentFrameworkBuilder
{
    private readonly IServiceCollection _services;
    private readonly AgentConfiguration _configuration;

    public AgentFrameworkBuilder(IServiceCollection services)
    {
        _services = services;
        _configuration = new AgentConfiguration();
    }

    /// <summary>
    /// Configures retry behavior.
    /// </summary>
    public AgentFrameworkBuilder WithRetry(Action<RetryConfiguration> configure)
    {
        configure(_configuration.Retry);
        return this;
    }

    /// <summary>
    /// Configures telemetry.
    /// </summary>
    public AgentFrameworkBuilder WithTelemetry(Action<TelemetryConfiguration> configure)
    {
        configure(_configuration.Telemetry);
        return this;
    }

    /// <summary>
    /// Configures error handling.
    /// </summary>
    public AgentFrameworkBuilder WithErrorHandling(Action<ErrorHandlingConfiguration> configure)
    {
        configure(_configuration.ErrorHandling);
        return this;
    }

    /// <summary>
    /// Configures middleware.
    /// </summary>
    public AgentFrameworkBuilder WithMiddleware(Action<MiddlewareConfiguration> configure)
    {
        configure(_configuration.Middleware);
        return this;
    }

    /// <summary>
    /// Builds and registers all agent framework services.
    /// </summary>
    public IServiceCollection Build()
    {
        _configuration.Validate();
        _services.AddAgentFramework(_ => { }, _configuration);
        return _services;
    }

    private static IServiceCollection AddAgentFramework(
        IServiceCollection services,
        Action<AgentConfiguration> configureOptions,
        AgentConfiguration config)
    {
        services.Configure(configureOptions);
        return ServiceCollectionExtensions.AddAgentFramework(services, config);
    }
}
