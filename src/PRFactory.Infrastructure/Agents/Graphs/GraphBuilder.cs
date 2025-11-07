using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Graphs
{
    /// <summary>
    /// Fluent builder for constructing agent graphs
    /// Provides methods: AddAgent(), AddConditional(), AddParallel(), Build()
    /// Registers with Agent Framework
    /// </summary>
    public class GraphBuilder
    {
        private readonly string _graphId;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<GraphNode> _nodes = new();
        private GraphNode? _entryNode;
        private GraphNode? _currentNode;

        public GraphBuilder(string graphId, IServiceProvider serviceProvider)
        {
            _graphId = graphId ?? throw new ArgumentNullException(nameof(graphId));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Add an agent to the graph
        /// </summary>
        public GraphBuilder AddAgent<TAgent>(string? agentId = null)
        {
            var nodeId = agentId ?? typeof(TAgent).Name;
            var node = new AgentNode<TAgent>
            {
                NodeId = nodeId,
                NodeType = GraphNodeType.Agent,
                ServiceProvider = _serviceProvider
            };

            _nodes.Add(node);

            // If this is the first node, set it as entry
            if (_entryNode == null)
            {
                _entryNode = node;
            }

            // Link from current node if exists
            if (_currentNode != null)
            {
                _currentNode.NextNodes.Add(node);
            }

            _currentNode = node;
            return this;
        }

        /// <summary>
        /// Add a conditional branch to the graph
        /// </summary>
        public GraphBuilder AddConditional(
            Func<GraphContext, IAgentMessage, bool> condition,
            Action<GraphBuilder> trueBranch,
            Action<GraphBuilder>? falseBranch = null)
        {
            var conditionalNode = new ConditionalNode
            {
                NodeId = $"conditional_{_nodes.Count}",
                NodeType = GraphNodeType.Conditional,
                Condition = condition,
                ServiceProvider = _serviceProvider
            };

            _nodes.Add(conditionalNode);

            // Link from current node
            if (_currentNode != null)
            {
                _currentNode.NextNodes.Add(conditionalNode);
            }

            // Build true branch
            var trueBuilder = new GraphBuilder($"{_graphId}_true", _serviceProvider);
            trueBranch(trueBuilder);
            conditionalNode.TrueBranch = trueBuilder.BuildNodes();

            // Build false branch if provided
            if (falseBranch != null)
            {
                var falseBuilder = new GraphBuilder($"{_graphId}_false", _serviceProvider);
                falseBranch(falseBuilder);
                conditionalNode.FalseBranch = falseBuilder.BuildNodes();
            }

            _currentNode = conditionalNode;
            return this;
        }

        /// <summary>
        /// Add parallel execution of multiple agents
        /// </summary>
        public GraphBuilder AddParallel(params Action<GraphBuilder>[] branches)
        {
            if (branches == null || branches.Length == 0)
            {
                throw new ArgumentException("At least one branch is required", nameof(branches));
            }

            var parallelNode = new ParallelNode
            {
                NodeId = $"parallel_{_nodes.Count}",
                NodeType = GraphNodeType.Parallel,
                ServiceProvider = _serviceProvider
            };

            _nodes.Add(parallelNode);

            // Link from current node
            if (_currentNode != null)
            {
                _currentNode.NextNodes.Add(parallelNode);
            }

            // Build each parallel branch
            foreach (var branch in branches)
            {
                var branchBuilder = new GraphBuilder($"{_graphId}_parallel_{parallelNode.Branches.Count}", _serviceProvider);
                branch(branchBuilder);
                parallelNode.Branches.Add(branchBuilder.BuildNodes());
            }

            _currentNode = parallelNode;
            return this;
        }

        /// <summary>
        /// Add a checkpoint to the graph
        /// </summary>
        public GraphBuilder AddCheckpoint(string checkpointName)
        {
            var checkpointNode = new CheckpointNode
            {
                NodeId = $"checkpoint_{checkpointName}",
                NodeType = GraphNodeType.Checkpoint,
                CheckpointName = checkpointName,
                ServiceProvider = _serviceProvider
            };

            _nodes.Add(checkpointNode);

            if (_currentNode != null)
            {
                _currentNode.NextNodes.Add(checkpointNode);
            }

            _currentNode = checkpointNode;
            return this;
        }

        /// <summary>
        /// Add error handling to the graph
        /// </summary>
        public GraphBuilder WithErrorHandler(Func<Exception, GraphContext, Task<bool>> errorHandler)
        {
            if (_currentNode != null)
            {
                _currentNode.ErrorHandler = errorHandler;
            }
            return this;
        }

        /// <summary>
        /// Add retry logic to the current agent
        /// </summary>
        public GraphBuilder WithRetry(int maxAttempts, TimeSpan? delay = null)
        {
            if (_currentNode != null)
            {
                _currentNode.MaxRetries = maxAttempts;
                _currentNode.RetryDelay = delay ?? TimeSpan.FromSeconds(1);
            }
            return this;
        }

        /// <summary>
        /// Build the graph and return an executable instance
        /// </summary>
        public IAgentGraph Build()
        {
            if (_entryNode == null)
            {
                throw new InvalidOperationException("Graph must have at least one node");
            }

            return new BuiltGraph(_graphId, _entryNode, _nodes, _serviceProvider);
        }

        /// <summary>
        /// Build the nodes without creating a graph (used for branches)
        /// </summary>
        private GraphNode? BuildNodes()
        {
            return _entryNode;
        }

        /// <summary>
        /// Register the graph with the service collection
        /// </summary>
        public static void RegisterGraph<TGraph>(IServiceCollection services)
            where TGraph : class, IAgentGraph
        {
            services.AddScoped<TGraph>();
        }
    }

    /// <summary>
    /// Graph node types
    /// </summary>
    public enum GraphNodeType
    {
        Agent,
        Conditional,
        Parallel,
        Checkpoint
    }

    /// <summary>
    /// Base class for graph nodes
    /// </summary>
    public abstract class GraphNode
    {
        public string NodeId { get; set; } = string.Empty;
        public GraphNodeType NodeType { get; set; }
        public List<GraphNode> NextNodes { get; set; } = new();
        public IServiceProvider? ServiceProvider { get; set; }
        public int MaxRetries { get; set; } = 1;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public Func<Exception, GraphContext, Task<bool>>? ErrorHandler { get; set; }

        public abstract Task<IAgentMessage> ExecuteAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Agent execution node
    /// </summary>
    public class AgentNode<TAgent> : GraphNode
    {
        public override async Task<IAgentMessage> ExecuteAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var agentExecutor = ServiceProvider.GetRequiredService<IAgentExecutor>();

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        await Task.Delay(RetryDelay * attempt, cancellationToken);
                    }

                    return await agentExecutor.ExecuteAsync<TAgent>(inputMessage, context, cancellationToken);
                }
                catch (Exception ex) when (attempt < MaxRetries - 1)
                {
                    if (ErrorHandler != null)
                    {
                        var shouldRetry = await ErrorHandler(ex, context);
                        if (!shouldRetry)
                        {
                            throw;
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Agent {typeof(TAgent).Name} failed after {MaxRetries} attempts");
        }
    }

    /// <summary>
    /// Conditional branch node
    /// </summary>
    public class ConditionalNode : GraphNode
    {
        public Func<GraphContext, IAgentMessage, bool> Condition { get; set; }
        public GraphNode TrueBranch { get; set; }
        public GraphNode FalseBranch { get; set; }

        public override async Task<IAgentMessage> ExecuteAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var result = Condition(context, inputMessage);
            var branch = result ? TrueBranch : FalseBranch;

            if (branch != null)
            {
                return await branch.ExecuteAsync(inputMessage, context, cancellationToken);
            }

            return inputMessage;
        }
    }

    /// <summary>
    /// Parallel execution node
    /// </summary>
    public class ParallelNode : GraphNode
    {
        public List<GraphNode> Branches { get; set; } = new();

        public override async Task<IAgentMessage> ExecuteAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var tasks = Branches.Select(branch =>
                branch.ExecuteAsync(inputMessage, context, cancellationToken)).ToArray();

            var results = await Task.WhenAll(tasks);

            // Return the first result (or you could merge results)
            return results.FirstOrDefault() ?? inputMessage;
        }
    }

    /// <summary>
    /// Checkpoint node
    /// </summary>
    public class CheckpointNode : GraphNode
    {
        public string CheckpointName { get; set; }

        public override async Task<IAgentMessage> ExecuteAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var checkpointStore = ServiceProvider.GetRequiredService<ICheckpointStore>();

            context.State["checkpoint_name"] = CheckpointName;
            context.State["checkpoint_time"] = DateTime.UtcNow;

            var checkpoint = new CheckpointData
            {
                NextAgentType = CheckpointName,
                State = context.State
            };
            await checkpointStore.SaveCheckpointAsync(checkpoint, cancellationToken);

            return inputMessage;
        }
    }

    /// <summary>
    /// Built graph implementation
    /// </summary>
    public class BuiltGraph : AgentGraphBase
    {
        private readonly string _graphId;
        private readonly GraphNode _entryNode;
        private readonly List<GraphNode> _allNodes;

        public override string GraphId => _graphId;

        public BuiltGraph(
            string graphId,
            GraphNode entryNode,
            List<GraphNode> allNodes,
            IServiceProvider serviceProvider)
            : base(
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BuiltGraph>>(),
                serviceProvider.GetRequiredService<Base.ICheckpointStore>())
        {
            _graphId = graphId;
            _entryNode = entryNode;
            _allNodes = allNodes;
        }

        protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var currentMessage = inputMessage;
                var currentNode = _entryNode;

                while (currentNode != null)
                {
                    currentMessage = await currentNode.ExecuteAsync(currentMessage, context, cancellationToken);

                    // Move to next node
                    currentNode = currentNode.NextNodes.FirstOrDefault();
                }

                var duration = DateTime.UtcNow - startTime;
                return GraphExecutionResult.Success("completed", currentMessage, duration);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "BuiltGraph {GraphId} failed", _graphId);
                return GraphExecutionResult.Failure("failed", ex);
            }
        }

        protected override Task<GraphExecutionResult> ResumeCoreAsync(
            IAgentMessage resumeMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            // For built graphs, resume from the checkpoint
            return ExecuteCoreAsync(resumeMessage, context, cancellationToken);
        }
    }

    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class GraphBuilderExtensions
    {
        /// <summary>
        /// Add agent graph services to the service collection
        /// </summary>
        public static IServiceCollection AddAgentGraphs(this IServiceCollection services)
        {
            // Register graph implementations
            services.AddScoped<RefinementGraph>();
            services.AddScoped<PlanningGraph>();
            services.AddScoped<ImplementationGraph>();
            services.AddScoped<WorkflowOrchestrator>();

            // Register as IAgentGraph for dynamic resolution
            services.AddScoped<IAgentGraph, RefinementGraph>(sp => sp.GetRequiredService<RefinementGraph>());
            services.AddScoped<IAgentGraph, PlanningGraph>(sp => sp.GetRequiredService<PlanningGraph>());
            services.AddScoped<IAgentGraph, ImplementationGraph>(sp => sp.GetRequiredService<ImplementationGraph>());

            return services;
        }

        /// <summary>
        /// Create a new graph builder
        /// </summary>
        public static GraphBuilder CreateGraphBuilder(this IServiceProvider serviceProvider, string graphId)
        {
            return new GraphBuilder(graphId, serviceProvider);
        }
    }
}
