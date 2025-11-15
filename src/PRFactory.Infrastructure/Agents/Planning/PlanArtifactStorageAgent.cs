using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Stores plan artifacts in the database with versioning support.
/// Handles both initial plan creation and version updates.
/// </summary>
public class PlanArtifactStorageAgent : BaseAgent
{
    private readonly IPlanRepository _planRepository;

    public override string Name => "Plan Artifact Storage Agent";
    public override string Description => "Stores plan artifacts in database with versioning";

    public PlanArtifactStorageAgent(
        ILogger<PlanArtifactStorageAgent> logger,
        IPlanRepository planRepository)
        : base(logger)
    {
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // Validate context
        ValidateContext(context);

        var ticketId = context.Ticket.Id;

        Logger.LogInformation(
            "Storing plan artifacts for ticket {TicketKey}",
            context.Ticket.TicketKey);

        try
        {
            // Get artifacts from context (all are nullable - support partial plans)
            var userStories = GetStateValue<string>(context, "UserStories");
            var apiDesign = GetStateValue<string>(context, "ApiDesign");
            var databaseSchema = GetStateValue<string>(context, "DatabaseSchema");
            var testCases = GetStateValue<string>(context, "TestCases");
            var implementationSteps = GetStateValue<string>(context, "ImplementationSteps");

            // Check if this is an update or initial creation
            var existingPlan = await _planRepository.GetByTicketIdAsync(ticketId, cancellationToken);

            Plan plan;
            int version;

            if (existingPlan != null)
            {
                // Update existing plan with new version
                var revisionReason = GetStateValue<string>(context, "RevisionFeedback");
                var createdBy = GetStateValue<string>(context, "ApprovedBy");

                existingPlan.UpdateArtifacts(
                    userStories: userStories,
                    apiDesign: apiDesign,
                    databaseSchema: databaseSchema,
                    testCases: testCases,
                    implementationSteps: implementationSteps,
                    createdBy: createdBy,
                    revisionReason: revisionReason);

                await _planRepository.UpdateAsync(existingPlan, cancellationToken);

                plan = existingPlan;
                version = existingPlan.Version;

                Logger.LogInformation(
                    "Updated plan for ticket {TicketKey} to version {Version}",
                    context.Ticket.TicketKey,
                    version);
            }
            else
            {
                // Create new plan
                plan = Plan.CreateWithArtifacts(
                    ticketId: ticketId,
                    userStories: userStories,
                    apiDesign: apiDesign,
                    databaseSchema: databaseSchema,
                    testCases: testCases,
                    implementationSteps: implementationSteps);

                await _planRepository.AddAsync(plan, cancellationToken);

                version = plan.Version;

                Logger.LogInformation(
                    "Created plan for ticket {TicketKey} (version {Version})",
                    context.Ticket.TicketKey,
                    version);
            }

            // Store plan ID in context for downstream agents
            context.State["PlanId"] = plan.Id;
            context.State["PlanVersion"] = version;

            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["PlanId"] = plan.Id,
                    ["TicketId"] = ticketId,
                    ["Version"] = version,
                    ["HasMultipleArtifacts"] = plan.HasMultipleArtifacts
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to store plan artifacts for ticket {TicketKey}",
                context.Ticket.TicketKey);
            throw;
        }
    }

    private void ValidateContext(AgentContext context)
    {
        if (context.Ticket == null)
        {
            throw new InvalidOperationException("Ticket is required in context");
        }
    }

    /// <summary>
    /// Safely retrieves a value from state dictionary, returning null if not found.
    /// </summary>
    private T? GetStateValue<T>(AgentContext context, string key) where T : class
    {
        if (context.State.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return null;
    }
}
