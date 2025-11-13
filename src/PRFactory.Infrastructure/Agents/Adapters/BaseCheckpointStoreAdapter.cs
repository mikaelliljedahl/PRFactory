using System.Text.Json;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using DomainCheckpoint = PRFactory.Domain.Entities.Checkpoint;
using DomainCheckpointRepository = PRFactory.Domain.Interfaces.ICheckpointRepository;
using BaseCheckpointStore = PRFactory.Infrastructure.Agents.Base.ICheckpointStore;
using BaseCheckpoint = PRFactory.Infrastructure.Agents.Base.Checkpoint;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter that implements Base.ICheckpointStore by delegating to the domain checkpoint repository.
/// This adapter bridges the graph-level checkpoint interface with the domain layer.
/// </summary>
public class BaseCheckpointStoreAdapter : BaseCheckpointStore
{
    private readonly DomainCheckpointRepository _checkpointRepository;
    private readonly ILogger<BaseCheckpointStoreAdapter> _logger;

    public BaseCheckpointStoreAdapter(
        DomainCheckpointRepository checkpointRepository,
        ILogger<BaseCheckpointStoreAdapter> logger)
    {
        _checkpointRepository = checkpointRepository ?? throw new ArgumentNullException(nameof(checkpointRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SaveCheckpointAsync(
        Guid ticketId,
        string graphId,
        string checkpointId,
        Dictionary<string, object> state)
    {
        // Serialize state to JSON
        var stateJson = JsonSerializer.Serialize(state);

        // Check if checkpoint already exists
        var existingCheckpoints = await _checkpointRepository.GetCheckpointsByTicketIdAsync(ticketId);
        var existing = existingCheckpoints.FirstOrDefault(c =>
            c.GraphId == graphId && c.CheckpointId == checkpointId);

        if (existing != null)
        {
            // Update existing checkpoint
            existing.UpdateState(stateJson);
            await _checkpointRepository.SaveCheckpointAsync(existing);
            _logger.LogDebug("Updated checkpoint {CheckpointId} for ticket {TicketId} in graph {GraphId}",
                checkpointId, ticketId, graphId);
        }
        else
        {
            // Get tenant ID from ticket (this assumes a tenant context is available)
            // In a real implementation, you'd inject ITenantContext or similar
            var tenantId = Guid.Empty; // Placeholder - should come from tenant context

            // Create new checkpoint
            state.TryGetValue("currentAgent", out var currentAgentObj);
            state.TryGetValue("nextAgent", out var nextAgentObj);

            var domainCheckpoint = DomainCheckpoint.Create(
                tenantId: tenantId,
                ticketId: ticketId,
                graphId: graphId,
                checkpointId: checkpointId,
                stateJson: stateJson,
                agentName: currentAgentObj?.ToString(),
                nextAgentType: nextAgentObj?.ToString());

            await _checkpointRepository.SaveCheckpointAsync(domainCheckpoint);
            _logger.LogDebug("Created checkpoint {CheckpointId} for ticket {TicketId} in graph {GraphId}",
                checkpointId, ticketId, graphId);
        }
    }

    public async Task<BaseCheckpoint> LoadCheckpointAsync(Guid ticketId, string graphId)
    {
        var checkpoints = await _checkpointRepository.GetCheckpointsByTicketIdAsync(ticketId);
        var latestCheckpoint = checkpoints
            .Where(c => c.GraphId == graphId && c.Status == PRFactory.Domain.ValueObjects.CheckpointStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        if (latestCheckpoint == null)
        {
            _logger.LogDebug("No active checkpoint found for ticket {TicketId} in graph {GraphId}", ticketId, graphId);
            return null!;
        }

        return MapToBaseCheckpoint(latestCheckpoint);
    }

    public async Task<List<BaseCheckpoint>> GetCheckpointHistoryAsync(Guid ticketId, string graphId)
    {
        var checkpoints = await _checkpointRepository.GetCheckpointsByTicketIdAsync(ticketId);
        return checkpoints
            .Where(c => c.GraphId == graphId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(MapToBaseCheckpoint)
            .ToList();
    }

    private static BaseCheckpoint MapToBaseCheckpoint(DomainCheckpoint domainCheckpoint)
    {
        // Deserialize state from JSON
        var state = JsonSerializer.Deserialize<Dictionary<string, object>>(domainCheckpoint.StateJson)
            ?? new Dictionary<string, object>();

        return new BaseCheckpoint
        {
            Id = domainCheckpoint.Id,
            TicketId = domainCheckpoint.TicketId,
            GraphId = domainCheckpoint.GraphId,
            CheckpointId = domainCheckpoint.CheckpointId,
            State = state,
            CreatedAt = domainCheckpoint.CreatedAt
        };
    }
}
