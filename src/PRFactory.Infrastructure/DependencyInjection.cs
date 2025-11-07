using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents;
using PRFactory.Infrastructure.Agents.Stubs;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Infrastructure.Persistence.Repositories;
using GraphCheckpoint = PRFactory.Infrastructure.Agents.Base.ICheckpointStore;

namespace PRFactory.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services with dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register encryption service
        var encryptionKey = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured. Set 'Encryption:Key' in configuration.");

        services.AddSingleton<IEncryptionService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AesEncryptionService>>();
            return new AesEncryptionService(encryptionKey, logger);
        });

        // Register DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=prfactory.db";

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("Logging:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }
        });

        // Register repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();

        // Register stub implementations for agent framework (temporary)
        services.AddScoped<IAgentExecutionQueue, AgentExecutionQueue>();
        services.AddScoped<ICheckpointStore, CheckpointStore>();
        services.AddScoped<GraphCheckpoint, GraphCheckpointStore>();
        services.AddScoped<IAgentGraphExecutor, AgentGraphExecutor>();

        return services;
    }

    /// <summary>
    /// Generates a new encryption key for use in configuration.
    /// </summary>
    /// <returns>A base64-encoded 256-bit encryption key</returns>
    public static string GenerateEncryptionKey()
    {
        return EncryptionKeyGenerator.GenerateKey();
    }
}
