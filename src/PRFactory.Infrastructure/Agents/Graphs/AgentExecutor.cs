using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;

namespace PRFactory.Infrastructure.Agents.Graphs
{
    /// <summary>
    /// Executes agents by resolving type markers to real implementations.
    /// Bridges the gap between graph-based execution and agent-based execution.
    /// </summary>
    public class AgentExecutor : IAgentExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentExecutor> _logger;
        private readonly ITicketRepository _ticketRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IRepositoryRepository _repositoryRepository;

        /// <summary>
        /// Maps agent type markers to real agent implementation types
        /// </summary>
        private readonly Dictionary<string, Type> _agentTypeMap;

        public AgentExecutor(
            IServiceProvider serviceProvider,
            ILogger<AgentExecutor> logger,
            ITicketRepository ticketRepository,
            ITenantRepository tenantRepository,
            IRepositoryRepository repositoryRepository)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
            _repositoryRepository = repositoryRepository ?? throw new ArgumentNullException(nameof(repositoryRepository));

            // Initialize agent type mapping
            _agentTypeMap = new Dictionary<string, Type>
            {
                // RefinementGraph agents
                ["TriggerAgent"] = typeof(TriggerAgent),
                ["RepositoryCloneAgent"] = typeof(RepositoryCloneAgent),
                ["AnalysisAgent"] = typeof(AnalysisAgent),
                ["QuestionGenerationAgent"] = typeof(QuestionGenerationAgent),
                ["JiraPostAgent"] = typeof(JiraPostAgent),
                ["HumanWaitAgent"] = typeof(HumanWaitAgent),
                ["AnswerProcessingAgent"] = typeof(AnswerProcessingAgent),

                // PlanningGraph agents
                ["PlanningAgent"] = typeof(PlanningAgent),
                ["GitPlanAgent"] = typeof(GitPlanAgent),

                // ImplementationGraph agents
                ["ImplementationAgent"] = typeof(ImplementationAgent),
                ["GitCommitAgent"] = typeof(GitCommitAgent),
                ["PullRequestAgent"] = typeof(PullRequestAgent),
                ["CompletionAgent"] = typeof(CompletionAgent)
            };
        }

        /// <summary>
        /// Execute an agent by resolving the type marker to a real implementation
        /// </summary>
        public async Task<IAgentMessage> ExecuteAsync<TAgent>(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            var agentTypeName = typeof(TAgent).Name;

            _logger.LogInformation(
                "Executing agent {AgentType} for ticket {TicketId}",
                agentTypeName, context.TicketId);

            try
            {
                // Resolve agent type marker to real implementation
                if (!_agentTypeMap.TryGetValue(agentTypeName, out var agentType))
                {
                    throw new AgentExecutionException(
                        $"Agent type '{agentTypeName}' is not registered in the agent type map");
                }

                // Resolve agent instance from DI
                var agent = _serviceProvider.GetService(agentType) as BaseAgent;
                if (agent == null)
                {
                    throw new AgentExecutionException(
                        $"Failed to resolve agent '{agentTypeName}' from service provider. " +
                        $"Ensure the agent is registered in DI.");
                }

                // Build AgentContext from GraphContext and IAgentMessage
                var agentContext = await BuildAgentContextAsync(inputMessage, context, cancellationToken);

                // Execute agent with middleware (includes retry, telemetry, checkpointing)
                var result = await agent.ExecuteWithMiddlewareAsync(agentContext, cancellationToken);

                // Check if execution was successful
                if (result.Status == AgentStatus.Failed)
                {
                    throw new AgentExecutionException(
                        $"Agent '{agentTypeName}' failed: {result.Error}",
                        result.ErrorDetails);
                }

                // Convert AgentResult to IAgentMessage
                var outputMessage = ConvertResultToMessage(
                    agentTypeName,
                    result,
                    agentContext,
                    context.TicketId);

                _logger.LogInformation(
                    "Agent {AgentType} completed successfully for ticket {TicketId}",
                    agentTypeName, context.TicketId);

                return outputMessage;
            }
            catch (AgentExecutionException)
            {
                // Re-throw agent execution exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error executing agent {AgentType} for ticket {TicketId}",
                    agentTypeName, context.TicketId);

                throw new AgentExecutionException(
                    $"Unexpected error executing agent '{agentTypeName}': {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Build AgentContext from GraphContext and input message
        /// </summary>
        private async Task<AgentContext> BuildAgentContextAsync(
            IAgentMessage inputMessage,
            GraphContext context,
            CancellationToken cancellationToken)
        {
            // Load ticket, tenant, and repository entities if not already in state
            Ticket? ticket = null;
            Tenant? tenant = null;
            Repository? repository = null;

            // Try to get ticket
            if (context.TicketId != Guid.Empty)
            {
                ticket = await _ticketRepository.GetByIdAsync(context.TicketId, cancellationToken);

                if (ticket != null)
                {
                    tenant = await _tenantRepository.GetByIdAsync(ticket.TenantId, cancellationToken);
                    repository = await _repositoryRepository.GetByIdAsync(ticket.RepositoryId, cancellationToken);
                }
            }

            // Build AgentContext
            var agentContext = new AgentContext
            {
                TicketId = context.TicketId.ToString(),
                TenantId = tenant?.Id.ToString() ?? string.Empty,
                RepositoryId = repository?.Id.ToString() ?? string.Empty,
                State = new Dictionary<string, object>(context.State),
                Ticket = ticket!,
                Tenant = tenant!,
                Repository = repository!
            };

            // Copy relevant data from GraphContext state to AgentContext
            if (context.State.TryGetValue("RepositoryPath", out var repoPath))
            {
                agentContext.RepositoryPath = repoPath?.ToString();
            }

            if (context.State.TryGetValue("Analysis", out var analysis) && analysis is CodebaseAnalysis codebaseAnalysis)
            {
                agentContext.Analysis = codebaseAnalysis;
            }

            if (context.State.TryGetValue("ImplementationPlan", out var plan))
            {
                agentContext.ImplementationPlan = plan?.ToString();
            }

            if (context.State.TryGetValue("PlanBranchName", out var planBranch))
            {
                agentContext.PlanBranchName = planBranch?.ToString();
            }

            if (context.State.TryGetValue("ImplementationBranchName", out var implBranch))
            {
                agentContext.ImplementationBranchName = implBranch?.ToString();
            }

            // Extract data from input message and add to State/Metadata
            ExtractMessageDataToContext(inputMessage, agentContext);

            return agentContext;
        }

        /// <summary>
        /// Extract data from input message and populate AgentContext state/metadata
        /// </summary>
        private void ExtractMessageDataToContext(IAgentMessage message, AgentContext context)
        {
            switch (message)
            {
                case TriggerTicketMessage trigger:
                    context.State["JiraKey"] = trigger.TicketKey;
                    context.State["TicketSystem"] = trigger.TicketSystem;
                    context.TenantId = trigger.TenantId.ToString();
                    context.RepositoryId = trigger.RepositoryId.ToString();
                    break;

                case TicketTriggeredMessage ticketTriggered:
                    context.State["Title"] = ticketTriggered.Title;
                    context.State["Description"] = ticketTriggered.Description;
                    break;

                case RepositoryClonedMessage repoCloned:
                    context.RepositoryPath = repoCloned.LocalPath;
                    context.State["DefaultBranch"] = repoCloned.DefaultBranch;
                    break;

                case CodebaseAnalyzedMessage analyzed:
                    context.State["RelevantFiles"] = analyzed.RelevantFiles;
                    context.State["Architecture"] = analyzed.Architecture;
                    context.State["Patterns"] = analyzed.Patterns;
                    break;

                case QuestionsGeneratedMessage questions:
                    context.State["Questions"] = questions.Questions;
                    break;

                case AnswersReceivedMessage answers:
                    context.Metadata["AnswerText"] = string.Join("\n",
                        answers.Answers.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                    context.State["Answers"] = answers.Answers;
                    break;

                case PlanGeneratedMessage planGen:
                    context.ImplementationPlan = planGen.MainPlan;
                    context.State["ImplementationPlan"] = planGen.MainPlan;
                    context.State["AffectedFiles"] = planGen.AffectedFiles;
                    context.State["TestStrategy"] = planGen.TestStrategy;
                    break;

                case PlanCommittedMessage planCommitted:
                    context.PlanBranchName = planCommitted.BranchName;
                    context.State["CommitSha"] = planCommitted.CommitSha;
                    break;

                case PlanApprovedMessage planApproved:
                    context.State["ApprovedBy"] = planApproved.ApprovedBy;
                    context.State["ApprovedAt"] = planApproved.ApprovedAt;
                    break;

                case PlanRejectedMessage planRejected:
                    context.State["RejectionReason"] = planRejected.Reason;
                    break;

                case CodeImplementedMessage codeImpl:
                    context.State["ModifiedFiles"] = codeImpl.ModifiedFiles;
                    context.State["CreatedFiles"] = codeImpl.CreatedFiles;
                    context.State["ImplementationSummary"] = codeImpl.Summary;
                    break;

                case PRCreatedMessage prCreated:
                    context.PullRequestUrl = prCreated.PullRequestUrl;
                    context.PullRequestNumber = prCreated.PullRequestNumber;
                    break;
            }
        }

        /// <summary>
        /// Convert AgentResult to appropriate IAgentMessage type based on agent type
        /// </summary>
        private IAgentMessage ConvertResultToMessage(
            string agentType,
            AgentResult result,
            AgentContext agentContext,
            Guid ticketId)
        {
            return agentType switch
            {
                "TriggerAgent" => new TicketTriggeredMessage(
                    ticketId,
                    result.Output.GetValueOrDefault("JiraKey", "")?.ToString() ?? "",
                    agentContext.Ticket?.Title ?? "",
                    agentContext.Ticket?.Description ?? "",
                    agentContext.Repository?.Id ?? Guid.Empty
                ),

                "RepositoryCloneAgent" => new RepositoryClonedMessage(
                    ticketId,
                    result.Output.GetValueOrDefault("RepositoryPath", "")?.ToString() ?? "",
                    agentContext.Repository?.DefaultBranch ?? "main"
                ),

                "AnalysisAgent" => new CodebaseAnalyzedMessage(
                    ticketId,
                    agentContext.Analysis?.AffectedFiles ?? new List<string>(),
                    agentContext.Analysis?.Architecture ?? "",
                    agentContext.Analysis?.TechnicalConsiderations ?? new List<string>(),
                    new Dictionary<string, string>() // FileContents - can be populated if needed
                ),

                "QuestionGenerationAgent" => new QuestionsGeneratedMessage(
                    ticketId,
                    agentContext.Ticket?.Questions.Select(q => new Question(
                        q.Id,
                        q.Text,
                        q.Category,
                        true // IsRequired - can be adjusted based on question
                    )).ToList() ?? new List<Question>()
                ),

                "JiraPostAgent" => new MessagePostedMessage(
                    ticketId,
                    result.Output.GetValueOrDefault("PostType", "")?.ToString() ?? "unknown",
                    DateTime.UtcNow
                ),

                "HumanWaitAgent" => new MessagePostedMessage(
                    ticketId,
                    "waiting",
                    DateTime.UtcNow
                ),

                "AnswerProcessingAgent" => new AnswersReceivedMessage(
                    ticketId,
                    agentContext.Ticket?.Answers.ToDictionary(
                        a => a.QuestionId,
                        a => a.Text
                    ) ?? new Dictionary<string, string>()
                ),

                "PlanningAgent" => new PlanGeneratedMessage(
                    ticketId,
                    agentContext.ImplementationPlan ?? "",
                    string.Join(", ", agentContext.Analysis?.AffectedFiles ?? new List<string>()),
                    "Unit tests, integration tests, manual testing",
                    agentContext.Analysis?.TechnicalConsiderations?.Count ?? 0
                ),

                "GitPlanAgent" => new PlanCommittedMessage(
                    ticketId,
                    result.Output.GetValueOrDefault("BranchName", "")?.ToString() ?? "",
                    "commit-sha-placeholder", // TODO: Get actual commit SHA from result
                    $"https://github.com/org/repo/tree/{result.Output.GetValueOrDefault("BranchName", "")}"
                ),

                "ImplementationAgent" => new CodeImplementedMessage(
                    ticketId,
                    new Dictionary<string, string>(), // ModifiedFiles - can be extracted from result
                    new List<string>(), // CreatedFiles - can be extracted from result
                    result.Output.GetValueOrDefault("Summary", "")?.ToString() ?? "Code implemented"
                ),

                "GitCommitAgent" => new PlanCommittedMessage(
                    ticketId,
                    result.Output.GetValueOrDefault("BranchName", "")?.ToString() ?? "",
                    result.Output.GetValueOrDefault("CommitSha", "")?.ToString() ?? "",
                    result.Output.GetValueOrDefault("BranchUrl", "")?.ToString() ?? ""
                ),

                "PullRequestAgent" => new PRCreatedMessage(
                    ticketId,
                    agentContext.PullRequestNumber ?? 0,
                    agentContext.PullRequestUrl ?? "",
                    DateTime.UtcNow
                ),

                "CompletionAgent" => new WorkflowCompletedMessage(
                    ticketId,
                    "completed",
                    TimeSpan.Zero, // Can be calculated from context
                    new Dictionary<string, object>()
                ),

                _ => throw new AgentExecutionException(
                    $"Unknown agent type '{agentType}' - cannot convert result to message")
            };
        }
    }
}
