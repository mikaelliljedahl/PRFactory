using PRFactory.Infrastructure.Agents.Base;
using System.Text.Json;
using DomainCheckpointRepository = PRFactory.Domain.Interfaces.ICheckpointRepository;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Adapter that bridges ICheckpointStore to ICheckpointRepository.
/// Allows graph workflows to use checkpoint storage without changing interfaces.
/// </summary>
public class CheckpointStoreAdapter : ICheckpointStore
{
    private readonly DomainCheckpointRepository _checkpointRepository;

    public CheckpointStoreAdapter(DomainCheckpointRepository checkpointRepository)
    {
        _checkpointRepository = checkpointRepository;
    }

    public async Task<CheckpointData?> LoadCheckpointAsync(
        Guid checkpointId,
        CancellationToken cancellationToken)
    {
        var checkpoint = await _checkpointRepository.GetCheckpointByIdAsync(checkpointId, cancellationToken);

        if (checkpoint == null)
            return null;

        return new CheckpointData
        {
            CheckpointId = checkpoint.Id,
            NextAgentType = checkpoint.NextAgentType ?? string.Empty,
            SavedAt = checkpoint.CreatedAt,
            State = DeserializeState(checkpoint.StateJson)
        };
    }

    public async Task<CheckpointData?> LoadLatestCheckpointAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        // Get all checkpoints for the ticket and find the most recent active one
        var checkpoints = await _checkpointRepository.GetCheckpointsByTicketIdAsync(ticketId, cancellationToken);
        var latestCheckpoint = checkpoints
            .Where(c => c.Status == Domain.ValueObjects.CheckpointStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        if (latestCheckpoint == null)
            return null;

        return new CheckpointData
        {
            CheckpointId = latestCheckpoint.Id,
            NextAgentType = latestCheckpoint.NextAgentType ?? string.Empty,
            SavedAt = latestCheckpoint.CreatedAt,
            State = DeserializeState(latestCheckpoint.StateJson)
        };
    }

    public async Task SaveCheckpointAsync(
        CheckpointData checkpoint,
        CancellationToken cancellationToken)
    {
        // CheckpointData doesn't contain enough information to create a full Checkpoint entity
        // We need ticketId, tenantId, graphId, and checkpointId (string identifier)
        // This method requires these to be in the State dictionary

        if (!checkpoint.State.TryGetValue("ticket_id", out var ticketIdObj) || ticketIdObj is not Guid ticketId)
            throw new InvalidOperationException("CheckpointData State must contain 'ticket_id' as Guid");

        if (!checkpoint.State.TryGetValue("tenant_id", out var tenantIdObj) || tenantIdObj is not Guid tenantId)
            throw new InvalidOperationException("CheckpointData State must contain 'tenant_id' as Guid");

        if (!checkpoint.State.TryGetValue("graph_id", out var graphIdObj) || graphIdObj is not string graphId)
            throw new InvalidOperationException("CheckpointData State must contain 'graph_id' as string");

        if (!checkpoint.State.TryGetValue("checkpoint_id", out var checkpointIdObj) || checkpointIdObj is not string checkpointIdString)
            throw new InvalidOperationException("CheckpointData State must contain 'checkpoint_id' as string");

        var stateJson = SerializeState(checkpoint.State);

        var entity = Domain.Entities.Checkpoint.Create(
            tenantId: tenantId,
            ticketId: ticketId,
            graphId: graphId,
            checkpointId: checkpointIdString,
            stateJson: stateJson,
            nextAgentType: checkpoint.NextAgentType
        );

        await _checkpointRepository.SaveCheckpointAsync(entity, cancellationToken);
    }

    private Dictionary<string, object> DeserializeState(string stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson) || stateJson == "{}")
            return new Dictionary<string, object>();

        try
        {
            var state = JsonSerializer.Deserialize<Dictionary<string, object>>(stateJson);
            return state ?? new Dictionary<string, object>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, object>();
        }
    }

    private string SerializeState(Dictionary<string, object> state)
    {
        if (state == null || state.Count == 0)
            return "{}";

        try
        {
            return JsonSerializer.Serialize(state);
        }
        catch (JsonException)
        {
            return "{}";
        }
    }
}
