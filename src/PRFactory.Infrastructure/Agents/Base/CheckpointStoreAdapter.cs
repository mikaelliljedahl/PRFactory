using System.Text.Json;
using DomainCheckpointRepository = PRFactory.Domain.Interfaces.ICheckpointRepository;

namespace PRFactory.Infrastructure.Agents.Base;

/// <summary>
/// Adapter that bridges Base.ICheckpointStore to ICheckpointRepository.
/// This adapter is specifically for AgentGraphBase and derived graph classes.
/// </summary>
public class CheckpointStoreAdapter : ICheckpointStore
{
    private readonly DomainCheckpointRepository _checkpointRepository;

    public CheckpointStoreAdapter(DomainCheckpointRepository checkpointRepository)
    {
        _checkpointRepository = checkpointRepository;
    }

    public async Task SaveCheckpointAsync(
        Guid ticketId,
        string graphId,
        string checkpointId,
        Dictionary<string, object> state)
    {
        // Get tenant ID from state if available, otherwise throw
        if (!state.TryGetValue("tenant_id", out var tenantIdObj) || tenantIdObj is not Guid tenantId)
            throw new InvalidOperationException("Checkpoint state must contain 'tenant_id' as Guid");

        var stateJson = SerializeState(state);

        var checkpoint = Domain.Entities.Checkpoint.Create(
            tenantId: tenantId,
            ticketId: ticketId,
            graphId: graphId,
            checkpointId: checkpointId,
            stateJson: stateJson
        );

        await _checkpointRepository.SaveCheckpointAsync(checkpoint, CancellationToken.None);
    }

    public async Task<Checkpoint> LoadCheckpointAsync(Guid ticketId, string graphId)
    {
        var domainCheckpoint = await _checkpointRepository.GetLatestCheckpointAsync(ticketId, graphId, CancellationToken.None);

        if (domainCheckpoint == null)
        {
            // Return empty checkpoint if none exists
            return new Checkpoint
            {
                Id = Guid.Empty,
                TicketId = ticketId,
                GraphId = graphId,
                CheckpointId = string.Empty,
                State = new Dictionary<string, object>(),
                CreatedAt = DateTime.MinValue
            };
        }

        return new Checkpoint
        {
            Id = domainCheckpoint.Id,
            TicketId = domainCheckpoint.TicketId,
            GraphId = domainCheckpoint.GraphId,
            CheckpointId = domainCheckpoint.CheckpointId,
            State = DeserializeState(domainCheckpoint.StateJson),
            CreatedAt = domainCheckpoint.CreatedAt
        };
    }

    public async Task<List<Checkpoint>> GetCheckpointHistoryAsync(Guid ticketId, string graphId)
    {
        var domainCheckpoints = await _checkpointRepository.GetCheckpointHistoryAsync(ticketId, graphId, CancellationToken.None);

        return domainCheckpoints.Select(dc => new Checkpoint
        {
            Id = dc.Id,
            TicketId = dc.TicketId,
            GraphId = dc.GraphId,
            CheckpointId = dc.CheckpointId,
            State = DeserializeState(dc.StateJson),
            CreatedAt = dc.CreatedAt
        }).ToList();
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
