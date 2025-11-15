using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;

namespace PRFactory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for AgentConfiguration entity operations.
/// Manages agent configurations with proper tenant filtering and timestamp management.
/// </summary>
public class AgentConfigurationRepository : IAgentConfigurationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgentConfigurationRepository> _logger;

    public AgentConfigurationRepository(
        ApplicationDbContext context,
        ILogger<AgentConfigurationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentConfiguration?> GetByTenantAndNameAsync(
        Guid tenantId,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        return await _context.AgentConfigurations
            .FirstOrDefaultAsync(
                ac => ac.TenantId == tenantId && ac.AgentName == agentName,
                cancellationToken);
    }

    public async Task<List<AgentConfiguration>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AgentConfigurations
            .Where(ac => ac.TenantId == tenantId)
            .OrderBy(ac => ac.AgentName)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentConfiguration> CreateAsync(
        AgentConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Set timestamps
        configuration.CreatedAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;

        _context.AgentConfigurations.Add(configuration);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created agent configuration {AgentName} for tenant {TenantId}",
            configuration.AgentName,
            configuration.TenantId);

        return configuration;
    }

    public async Task UpdateAsync(
        AgentConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Update timestamp
        configuration.UpdatedAt = DateTime.UtcNow;

        _context.AgentConfigurations.Update(configuration);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Updated agent configuration {AgentName} for tenant {TenantId}",
            configuration.AgentName,
            configuration.TenantId);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _context.AgentConfigurations
            .FirstOrDefaultAsync(ac => ac.Id == id, cancellationToken);

        if (configuration == null)
        {
            _logger.LogWarning("Attempted to delete non-existent agent configuration {Id}", id);
            return;
        }

        _context.AgentConfigurations.Remove(configuration);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Deleted agent configuration {AgentName} for tenant {TenantId}",
            configuration.AgentName,
            configuration.TenantId);
    }
}
