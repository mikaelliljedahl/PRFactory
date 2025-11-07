using System.Text.Json;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using DomainCheckpoint = PRFactory.Domain.Entities.Checkpoint;

namespace PRFactory.Infrastructure.Agents.Adapters;

/// <summary>
/// Adapter that bridges ICheckpointStore (used by AgentGraphBase) and ICheckpointRepository (domain layer).
/// This adapter converts between the DTO Checkpoint class (in AgentGraphBase) and the domain Checkpoint entity.
/// </summary>
public class GraphCheckpointStoreAdapter : ICheckpointStore
{
    private readonly ICheckpointRepository _checkpointRepository;
    private readonly ILogger<GraphCheckpointStoreAdapter> _logger;
    private Guid? _currentTenantId;

    public GraphCheckpointStoreAdapter(
        ICheckpointRepository checkpointRepository,
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

    public async Task SaveCheckpointAsync(
        Guid ticketId,
        string graphId,
        string checkpointId,
        Dictionary<string, object> state)
    {
        if (_currentTenantId == null)
        {
            throw new InvalidOperationException(
                "Tenant context not set. Call SetTenantContext before using the adapter.");
        }

        try
        {
            // Serialize state to JSON
            var stateJson = JsonSerializer.Serialize(state);

            // Extract optional fields from state if available
            string? agentName = null;
            string? nextAgentType = null;

            if (state.TryGetValue("current_agent", out var currentAgent))
            {
                agentName = currentAgent?.ToString();
            }

            if (state.TryGetValue("next_agent_type", out var nextAgent))
            {
                nextAgentType = nextAgent?.ToString();
            }

            // Create domain checkpoint entity
            var checkpoint = DomainCheckpoint.Create(
                tenantId: _currentTenantId.Value,
                ticketId: ticketId,
                graphId: graphId,
                checkpointId: checkpointId,
                stateJson: stateJson,
                agentName: agentName,
                nextAgentType: nextAgentType);

            // Save to repository
            await _checkpointRepository.SaveCheckpointAsync(checkpoint);

            _logger.LogDebug(
                "Saved checkpoint {CheckpointId} for ticket {TicketId} in graph {GraphId}",
                checkpointId, ticketId, graphId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save checkpoint {CheckpointId} for ticket {TicketId} in graph {GraphId}",
                checkpointId, ticketId, graphId);
            throw;
        }
    }

    public async Task<Checkpoint?> LoadCheckpointAsync(Guid ticketId, string graphId)
    {
        try
        {
            var domainCheckpoint = await _checkpointRepository.GetLatestCheckpointAsync(ticketId, graphId);

            if (domainCheckpoint == null)
            {
                _logger.LogDebug(
                    "No checkpoint found for ticket {TicketId} in graph {GraphId}",
                    ticketId, graphId);
                return null;
            }

            // Deserialize state from JSON
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(domainCheckpoint.StateJson)
                ?? new Dictionary<string, object>();

            // Convert domain checkpoint to DTO
            var checkpoint = new Checkpoint
            {
                Id = domainCheckpoint.Id,
                TicketId = domainCheckpoint.TicketId,
                GraphId = domainCheckpoint.GraphId,
                CheckpointId = domainCheckpoint.CheckpointId,
                State = state,
                CreatedAt = domainCheckpoint.CreatedAt
            };

            _logger.LogDebug(
                "Loaded checkpoint {CheckpointId} for ticket {TicketId} in graph {GraphId}",
                checkpoint.CheckpointId, ticketId, graphId);

            return checkpoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to load checkpoint for ticket {TicketId} in graph {GraphId}",
                ticketId, graphId);
            throw;
        }
    }

    public async Task<List<Checkpoint>> GetCheckpointHistoryAsync(Guid ticketId, string graphId)
    {
        try
        {
            var domainCheckpoints = await _checkpointRepository.GetCheckpointHistoryAsync(ticketId, graphId);

            var checkpoints = new List<Checkpoint>();

            foreach (var domainCheckpoint in domainCheckpoints)
            {
                // Deserialize state from JSON
                var state = JsonSerializer.Deserialize<Dictionary<string, object>>(domainCheckpoint.StateJson)
                    ?? new Dictionary<string, object>();

                // Convert domain checkpoint to DTO
                var checkpoint = new Checkpoint
                {
                    Id = domainCheckpoint.Id,
                    TicketId = domainCheckpoint.TicketId,
                    GraphId = domainCheckpoint.GraphId,
                    CheckpointId = domainCheckpoint.CheckpointId,
                    State = state,
                    CreatedAt = domainCheckpoint.CreatedAt
                };

                checkpoints.Add(checkpoint);
            }

            _logger.LogDebug(
                "Retrieved {Count} checkpoints for ticket {TicketId} in graph {GraphId}",
                checkpoints.Count, ticketId, graphId);

            return checkpoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get checkpoint history for ticket {TicketId} in graph {GraphId}",
                ticketId, graphId);
            throw;
        }
    }
}
