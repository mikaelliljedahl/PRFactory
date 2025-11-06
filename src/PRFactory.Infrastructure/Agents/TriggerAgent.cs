using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Initializes the workflow from a Jira webhook payload.
/// Creates the Ticket entity and validates that tenant and repository exist.
/// </summary>
public class TriggerAgent : BaseAgent
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRepositoryRepository _repositoryRepository;

    public override string Name => "TriggerAgent";
    public override string Description => "Initialize workflow from Jira webhook and create ticket entity";

    public TriggerAgent(
        ILogger<TriggerAgent> logger,
        ITicketRepository ticketRepository,
        ITenantRepository tenantRepository,
        IRepositoryRepository repositoryRepository)
        : base(logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _repositoryRepository = repositoryRepository ?? throw new ArgumentNullException(nameof(repositoryRepository));
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing workflow for ticket {TicketId}", context.TicketId);

        // Validate tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(Guid.Parse(context.TenantId), cancellationToken);
        if (tenant == null)
        {
            Logger.LogError("Tenant {TenantId} not found", context.TenantId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Tenant {context.TenantId} not found"
            };
        }

        if (!tenant.IsActive)
        {
            Logger.LogWarning("Tenant {TenantId} is not active", context.TenantId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Tenant {context.TenantId} is not active"
            };
        }

        // Validate repository exists
        var repository = await _repositoryRepository.GetByIdAsync(Guid.Parse(context.RepositoryId), cancellationToken);
        if (repository == null)
        {
            Logger.LogError("Repository {RepositoryId} not found", context.RepositoryId);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Repository {context.RepositoryId} not found"
            };
        }

        // Create ticket entity
        var jiraKey = context.State.ContainsKey("JiraKey") ? context.State["JiraKey"].ToString() : context.TicketId;
        var title = context.State.ContainsKey("Title") ? context.State["Title"].ToString() : "Untitled";
        var description = context.State.ContainsKey("Description") ? context.State["Description"].ToString() : "";

        var ticket = Ticket.Create(
            jiraKey!,
            Guid.Parse(context.TenantId),
            Guid.Parse(context.RepositoryId),
            "Jira"
        );

        // Update ticket info with title and description
        ticket.UpdateTicketInfo(title!, description!);

        // Transition to Analyzing state
        var transitionResult = ticket.TransitionTo(WorkflowState.Analyzing);
        if (!transitionResult.IsSuccess)
        {
            Logger.LogError("Failed to transition ticket to Analyzing state: {Error}", transitionResult.ErrorMessage);
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = transitionResult.ErrorMessage
            };
        }

        // Save ticket
        await _ticketRepository.AddAsync(ticket, cancellationToken);

        // Update context
        context.Ticket = ticket;
        context.Tenant = tenant;
        context.Repository = repository;
        context.TicketId = ticket.Id.ToString();

        Logger.LogInformation("Ticket {JiraKey} created with ID {TicketId}", jiraKey, ticket.Id);

        return new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["TicketId"] = ticket.Id.ToString(),
                ["JiraKey"] = jiraKey!
            }
        };
    }
}
