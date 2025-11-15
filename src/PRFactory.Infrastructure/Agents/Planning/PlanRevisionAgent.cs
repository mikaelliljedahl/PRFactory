using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Infrastructure.Agents.Base;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Regenerates specific plan artifacts based on feedback analysis.
/// Coordinates execution of only the affected artifact agents.
/// </summary>
public class PlanRevisionAgent : BaseAgent
{
    private readonly IServiceProvider _serviceProvider;

    public override string Name => "Plan Revision Agent";
    public override string Description => "Regenerates specific artifacts based on feedback";

    public PlanRevisionAgent(
        ILogger<PlanRevisionAgent> logger,
        IServiceProvider serviceProvider)
        : base(logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var affectedArtifacts = GetRequiredStateValue<List<string>>(context, "AffectedArtifacts");
        var feedback = GetRequiredStateValue<string>(context, "RevisionFeedback");

        Logger.LogInformation(
            "Regenerating artifacts for ticket {TicketKey}: {Artifacts}",
            context.Ticket.TicketKey,
            string.Join(", ", affectedArtifacts));

        // Store feedback in context for agents to use
        context.State["RevisionInstructions"] = feedback;

        // Regenerate each affected artifact
        var regeneratedArtifacts = new List<string>();
        foreach (var artifact in affectedArtifacts)
        {
            await RegenerateArtifactAsync(artifact, context, cancellationToken);
            regeneratedArtifacts.Add(artifact);
        }

        Logger.LogInformation(
            "Successfully regenerated {Count} artifacts for ticket {TicketKey}",
            regeneratedArtifacts.Count,
            context.Ticket.TicketKey);

        return new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["RegeneratedArtifacts"] = regeneratedArtifacts,
                ["ArtifactCount"] = regeneratedArtifacts.Count
            }
        };
    }

    private async Task RegenerateArtifactAsync(
        string artifactName,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Regenerating artifact: {Artifact}", artifactName);

        BaseAgent? agent = artifactName switch
        {
            "UserStories" => _serviceProvider.GetService<PmUserStoriesAgent>(),
            "ApiDesign" => _serviceProvider.GetService<ArchitectApiDesignAgent>(),
            "DatabaseSchema" => _serviceProvider.GetService<ArchitectDbSchemaAgent>(),
            "TestCases" => _serviceProvider.GetService<QaTestCasesAgent>(),
            "ImplementationSteps" => _serviceProvider.GetService<TechLeadImplementationAgent>(),
            _ => null
        };

        if (agent == null)
        {
            Logger.LogWarning("Unknown artifact type: {Artifact}", artifactName);
            return;
        }

        // Execute agent to regenerate artifact
        var result = await agent.ExecuteWithMiddlewareAsync(context, cancellationToken);

        if (result.Status != AgentStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Failed to regenerate {artifactName}: {result.Error}");
        }

        Logger.LogInformation("Successfully regenerated artifact: {Artifact}", artifactName);
    }

    private T GetRequiredStateValue<T>(AgentContext context, string key)
    {
        if (!context.State.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"{key} not found in context");
        }

        if (value is not T typedValue)
        {
            throw new InvalidOperationException($"{key} is not of expected type {typeof(T).Name}");
        }

        return typedValue;
    }
}
