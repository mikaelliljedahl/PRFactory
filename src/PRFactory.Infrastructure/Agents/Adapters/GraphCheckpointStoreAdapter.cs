using System.Text.Json;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using DomainCheckpoint = PRFactory.Domain.Entities.Checkpoint;
using DomainCheckpointRepository = PRFactory.Domain.Interfaces.ICheckpointRepository;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter that bridges ICheckpointStore (used by IAgentWorkflowInterfaces) and ICheckpointRepository (domain layer).
/// This adapter converts between CheckpointData and the domain Checkpoint entity.
/// </summary>
public class GraphCheckpointStoreAdapter : ICheckpointStore
{
    private readonly DomainCheckpointRepository _checkpointRepository;
    private readonly ILogger<GraphCheckpointStoreAdapter> _logger;
    private Guid? _currentTenantId;

    public GraphCheckpointStoreAdapter(
        DomainCheckpointRepository checkpointRepository,
        ILogger<GraphCheckpointStoreAdapter> logger)
    {
        _checkpointRepository = checkpointRepository ?? throw new ArgumentNullException(nameof(checkpointRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets the current tenant context for checkpoint operations.
    /// This must be called before using the adapter to ensure multi-tenant isolation.
    /// </summary>
    public void SetTenantContext(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }

    public async Task<CheckpointData?> LoadCheckpointAsync(
        Guid checkpointId,
        CancellationToken cancellationToken)
    {
        try
        {
            var domainCheckpoint = await _checkpointRepository.GetCheckpointByIdAsync(checkpointId, cancellationToken);

            if (domainCheckpoint == null)
            {
                _logger.LogDebug("No checkpoint found with ID {CheckpointId}", checkpointId);
                return null;
            }

            return MapToCheckpointData(domainCheckpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load checkpoint {CheckpointId}", checkpointId);
            throw;
        }
    }

    public async Task<CheckpointData?> LoadLatestCheckpointAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get all checkpoints for the ticket, ordered by creation date
            var checkpoints = await _checkpointRepository.GetCheckpointsByTicketIdAsync(ticketId, cancellationToken);

            var latestCheckpoint = checkpoints
                .Where(c => c.Status == PRFactory.Domain.ValueObjects.CheckpointStatus.Active)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            if (latestCheckpoint == null)
            {
                _logger.LogDebug("No active checkpoint found for ticket {TicketId}", ticketId);
                return null;
            }

            return MapToCheckpointData(latestCheckpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load latest checkpoint for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task SaveCheckpointAsync(
        CheckpointData checkpoint,
        CancellationToken cancellationToken)
    {
        if (_currentTenantId == null)
        {
            throw new InvalidOperationException(
                "Tenant context not set. Call SetTenantContext before using the adapter.");
        }

        try
        {
            // Serialize state to JSON
            var stateJson = JsonSerializer.Serialize(checkpoint.State);

            // Create domain checkpoint entity
            var domainCheckpoint = DomainCheckpoint.Create(
                tenantId: _currentTenantId.Value,
                ticketId: checkpoint.CheckpointId, // Using CheckpointId as TicketId for now
                graphId: "WorkflowGraph", // Default graph ID
                checkpointId: checkpoint.CheckpointId.ToString(),
                stateJson: stateJson,
                agentName: checkpoint.NextAgentType,
                nextAgentType: checkpoint.NextAgentType);

            // Save to repository
            await _checkpointRepository.SaveCheckpointAsync(domainCheckpoint, cancellationToken);

            _logger.LogDebug(
                "Saved checkpoint {CheckpointId} for ticket",
                checkpoint.CheckpointId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save checkpoint {CheckpointId}",
                checkpoint.CheckpointId);
            throw;
        }
    }

    private CheckpointData MapToCheckpointData(DomainCheckpoint domainCheckpoint)
    {
        // Deserialize state from JSON
        var state = JsonSerializer.Deserialize<Dictionary<string, object>>(domainCheckpoint.StateJson)
            ?? new Dictionary<string, object>();

        return new CheckpointData
        {
            CheckpointId = domainCheckpoint.Id,
            NextAgentType = domainCheckpoint.NextAgentType ?? string.Empty,
            SavedAt = domainCheckpoint.CreatedAt,
            State = state
        };
    }
}
