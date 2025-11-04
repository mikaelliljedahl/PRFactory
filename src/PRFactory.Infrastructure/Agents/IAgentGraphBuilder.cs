using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Interface for building agent execution graphs.
/// Provides a fluent API for defining workflows with multiple agents, transitions, and conditions.
/// </summary>
public interface IAgentGraphBuilder
{
    /// <summary>
    /// Adds a node (agent) to the graph.
    /// </summary>
    /// <param name="nodeId">Unique identifier for the node.</param>
    /// <param name="agentName">Name of the agent to execute (from registry).</param>
    /// <param name="description">Optional description of what this node does.</param>
    IAgentGraphBuilder AddNode(string nodeId, string agentName, string? description = null);

    /// <summary>
    /// Adds a conditional edge between two nodes.
    /// </summary>
    /// <param name="fromNodeId">Source node ID.</param>
    /// <param name="toNodeId">Target node ID.</param>
    /// <param name="condition">Condition that must be true to follow this edge.</param>
    /// <param name="label">Optional label for the edge.</param>
    IAgentGraphBuilder AddEdge(
        string fromNodeId,
        string toNodeId,
        Func<AgentContext, bool>? condition = null,
        string? label = null);

    /// <summary>
    /// Sets the entry point (start node) for the graph.
    /// </summary>
    IAgentGraphBuilder SetEntryPoint(string nodeId);

    /// <summary>
    /// Sets the exit point (end node) for the graph.
    /// </summary>
    IAgentGraphBuilder SetExitPoint(string nodeId);

    /// <summary>
    /// Adds a decision point that routes to different nodes based on conditions.
    /// </summary>
    /// <param name="nodeId">The decision node ID.</param>
    /// <param name="routes">Dictionary of conditions and target node IDs.</param>
    IAgentGraphBuilder AddDecision(
        string nodeId,
        Dictionary<Func<AgentContext, bool>, string> routes);

    /// <summary>
    /// Adds a parallel execution group where multiple agents run concurrently.
    /// </summary>
    /// <param name="groupId">Unique identifier for the parallel group.</param>
    /// <param name="nodeIds">Node IDs to execute in parallel.</param>
    /// <param name="joinNodeId">Node ID to execute after all parallel nodes complete.</param>
    IAgentGraphBuilder AddParallelGroup(
        string groupId,
        List<string> nodeIds,
        string joinNodeId);

    /// <summary>
    /// Adds a human-in-the-loop checkpoint where execution pauses for approval.
    /// </summary>
    /// <param name="nodeId">The checkpoint node ID.</param>
    /// <param name="description">Description shown to human reviewer.</param>
    /// <param name="nextNodeId">Node to execute after approval.</param>
    IAgentGraphBuilder AddCheckpoint(
        string nodeId,
        string description,
        string nextNodeId);

    /// <summary>
    /// Builds and validates the graph.
    /// </summary>
    IAgentGraph Build();
}

/// <summary>
/// Concrete implementation of IAgentGraphBuilder.
/// </summary>
public class AgentGraphBuilder : IAgentGraphBuilder
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly List<GraphNode> _nodes = new();
    private readonly List<GraphEdge> _edges = new();
    private string? _entryPointId;
    private string? _exitPointId;

    public AgentGraphBuilder(IAgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
    }

    public IAgentGraphBuilder AddNode(string nodeId, string agentName, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node ID cannot be empty", nameof(nodeId));

        if (_nodes.Any(n => n.Id == nodeId))
            throw new InvalidOperationException($"Node '{nodeId}' already exists in the graph");

        if (!_agentRegistry.IsRegistered(agentName))
            throw new InvalidOperationException($"Agent '{agentName}' is not registered");

        _nodes.Add(new GraphNode
        {
            Id = nodeId,
            AgentName = agentName,
            Description = description ?? agentName,
            Type = GraphNodeType.Agent
        });

        return this;
    }

    public IAgentGraphBuilder AddEdge(
        string fromNodeId,
        string toNodeId,
        Func<AgentContext, bool>? condition = null,
        string? label = null)
    {
        ValidateNodeExists(fromNodeId);
        ValidateNodeExists(toNodeId);

        _edges.Add(new GraphEdge
        {
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            Condition = condition,
            Label = label
        });

        return this;
    }

    public IAgentGraphBuilder SetEntryPoint(string nodeId)
    {
        ValidateNodeExists(nodeId);
        _entryPointId = nodeId;
        return this;
    }

    public IAgentGraphBuilder SetExitPoint(string nodeId)
    {
        ValidateNodeExists(nodeId);
        _exitPointId = nodeId;
        return this;
    }

    public IAgentGraphBuilder AddDecision(
        string nodeId,
        Dictionary<Func<AgentContext, bool>, string> routes)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node ID cannot be empty", nameof(nodeId));

        _nodes.Add(new GraphNode
        {
            Id = nodeId,
            Type = GraphNodeType.Decision,
            Description = "Decision point"
        });

        foreach (var (condition, targetNodeId) in routes)
        {
            ValidateNodeExists(targetNodeId);
            _edges.Add(new GraphEdge
            {
                FromNodeId = nodeId,
                ToNodeId = targetNodeId,
                Condition = condition
            });
        }

        return this;
    }

    public IAgentGraphBuilder AddParallelGroup(
        string groupId,
        List<string> nodeIds,
        string joinNodeId)
    {
        foreach (var nodeId in nodeIds)
        {
            ValidateNodeExists(nodeId);
        }

        ValidateNodeExists(joinNodeId);

        _nodes.Add(new GraphNode
        {
            Id = groupId,
            Type = GraphNodeType.ParallelGroup,
            Description = "Parallel execution group",
            Metadata = new Dictionary<string, object>
            {
                { "parallelNodes", nodeIds },
                { "joinNode", joinNodeId }
            }
        });

        return this;
    }

    public IAgentGraphBuilder AddCheckpoint(
        string nodeId,
        string description,
        string nextNodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node ID cannot be empty", nameof(nodeId));

        ValidateNodeExists(nextNodeId);

        _nodes.Add(new GraphNode
        {
            Id = nodeId,
            Type = GraphNodeType.Checkpoint,
            Description = description,
            Metadata = new Dictionary<string, object>
            {
                { "nextNode", nextNodeId }
            }
        });

        _edges.Add(new GraphEdge
        {
            FromNodeId = nodeId,
            ToNodeId = nextNodeId,
            Label = "on_approval"
        });

        return this;
    }

    public IAgentGraph Build()
    {
        Validate();

        return new AgentGraph(
            _agentRegistry,
            _nodes,
            _edges,
            _entryPointId!,
            _exitPointId!);
    }

    private void ValidateNodeExists(string nodeId)
    {
        if (!_nodes.Any(n => n.Id == nodeId))
        {
            throw new InvalidOperationException($"Node '{nodeId}' does not exist in the graph");
        }
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(_entryPointId))
            throw new InvalidOperationException("Entry point must be set");

        if (string.IsNullOrWhiteSpace(_exitPointId))
            throw new InvalidOperationException("Exit point must be set");

        if (_nodes.Count == 0)
            throw new InvalidOperationException("Graph must have at least one node");

        // Validate all nodes are reachable from entry point
        // (Optional: Add graph traversal validation here)
    }
}

/// <summary>
/// Represents a node in the agent graph.
/// </summary>
public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string? AgentName { get; set; }
    public GraphNodeType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents an edge (transition) between nodes.
/// </summary>
public class GraphEdge
{
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public Func<AgentContext, bool>? Condition { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Types of nodes in the graph.
/// </summary>
public enum GraphNodeType
{
    Agent,
    Decision,
    ParallelGroup,
    Checkpoint,
    Start,
    End
}

/// <summary>
/// Implementation of IAgentGraph that executes the defined workflow.
/// </summary>
public class AgentGraph : IAgentGraph
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly List<GraphNode> _nodes;
    private readonly List<GraphEdge> _edges;
    private readonly string _entryPointId;
    private readonly string _exitPointId;

    public string GraphId => "custom_graph";

    public AgentGraph(
        IAgentRegistry agentRegistry,
        List<GraphNode> nodes,
        List<GraphEdge> edges,
        string entryPointId,
        string exitPointId)
    {
        _agentRegistry = agentRegistry;
        _nodes = nodes;
        _edges = edges;
        _entryPointId = entryPointId;
        _exitPointId = exitPointId;
    }

    public Task<GraphExecutionResult> ExecuteAsync(
        IAgentMessage inputMessage,
        CancellationToken cancellationToken = default)
    {
        // Implementation would execute the graph starting from entry point
        // Following edges based on conditions until reaching exit point
        throw new NotImplementedException("Graph execution logic to be implemented");
    }

    public Task<GraphExecutionResult> ResumeAsync(
        Guid ticketId,
        IAgentMessage resumeMessage,
        CancellationToken cancellationToken = default)
    {
        // Implementation would resume from last checkpoint
        throw new NotImplementedException("Graph resume logic to be implemented");
    }

    public Task<GraphStatus> GetStatusAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        // Implementation would return current graph execution status
        throw new NotImplementedException("Graph status logic to be implemented");
    }
}
