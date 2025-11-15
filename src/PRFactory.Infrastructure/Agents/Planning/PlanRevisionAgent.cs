using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Messages;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Regenerates only the plan artifacts identified as needing updates.
/// Uses existing plan as context for unchanged artifacts.
/// </summary>
public class PlanRevisionAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IPlanRepository _planRepository;

    public override string Name => "Plan Revision Agent";
    public override string Description => "Regenerates specific plan artifacts based on feedback";

    public PlanRevisionAgent(
        ICliAgent cliAgent,
        IPlanRepository planRepository,
        ILogger<PlanRevisionAgent> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(cliAgent);
        ArgumentNullException.ThrowIfNull(planRepository);

        _cliAgent = cliAgent;
        _planRepository = planRepository;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var ticketId = context.Ticket.Id;

        // Get existing plan
        var existingPlan = await _planRepository.GetByTicketIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException($"No existing plan found for ticket {ticketId}");

        // Get affected artifacts from context
        var affectedArtifacts = context.State.GetValueOrDefault("AffectedArtifacts") as List<string>
            ?? throw new InvalidOperationException("AffectedArtifacts not found in context");

        var feedback = context.State.GetValueOrDefault("RevisionFeedback") as string ?? string.Empty;

        Logger.LogInformation(
            "Regenerating artifacts for ticket {TicketKey}. Affected: {Artifacts}",
            context.Ticket.TicketKey,
            string.Join(", ", affectedArtifacts));

        var updatedArtifacts = new Dictionary<string, object>();

        // Regenerate each affected artifact
        foreach (var artifactType in affectedArtifacts)
        {
            try
            {
                var artifact = artifactType switch
                {
                    "UserStories" => await RegenerateUserStoriesAsync(context, existingPlan, feedback, cancellationToken),
                    "ApiDesign" => await RegenerateApiDesignAsync(context, existingPlan, feedback, cancellationToken),
                    "DatabaseSchema" => await RegenerateDatabaseSchemaAsync(context, existingPlan, feedback, cancellationToken),
                    "TestCases" => await RegenerateTestCasesAsync(context, existingPlan, feedback, cancellationToken),
                    "ImplementationSteps" => await RegenerateImplementationStepsAsync(context, existingPlan, feedback, cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown artifact type: {artifactType}")
                };

                if (artifact == null)
                {
                    Logger.LogWarning("Failed to regenerate {ArtifactType}", artifactType);
                    continue;
                }

                updatedArtifacts[artifactType] = artifact;
                context.State[artifactType] = artifact;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error regenerating {ArtifactType}", artifactType);
                // Continue with other artifacts on error
            }
        }

        if (updatedArtifacts.Count == 0)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = "No artifacts were successfully regenerated"
            };
        }

        Logger.LogInformation(
            "Successfully regenerated {Count} artifacts",
            updatedArtifacts.Count);

        return new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = updatedArtifacts
        };
    }

    private async Task<string?> RegenerateUserStoriesAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Product Manager revising user stories based on feedback.

<existing_user_stories>
{existingPlan.UserStories ?? "None"}
</existing_user_stories>

<feedback>
{feedback}
</feedback>

Revise the user stories to address the feedback. Keep stories that don't need changes, and update/add new ones as needed.

Output only the revised markdown (no preamble).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private async Task<string?> RegenerateApiDesignAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Software Architect revising API design based on feedback.

<existing_api_design>
{existingPlan.ApiDesign ?? "None"}
</existing_api_design>

<feedback>
{feedback}
</feedback>

Revise the OpenAPI specification to address the feedback. Keep endpoints that don't need changes, and update/add new ones as needed.

Output only the revised OpenAPI YAML (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        if (response.Success)
        {
            return ExtractYamlContent(response.Content);
        }

        return null;
    }

    private async Task<string?> RegenerateDatabaseSchemaAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Database Architect revising database schema based on feedback.

<existing_schema>
{existingPlan.DatabaseSchema ?? "None"}
</existing_schema>

<feedback>
{feedback}
</feedback>

Revise the database schema to address the feedback. Keep tables/columns that don't need changes, and update/add new ones as needed.

Output only the revised SQL DDL statements (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private async Task<string?> RegenerateTestCasesAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a QA Engineer revising test cases based on feedback.

<existing_test_cases>
{existingPlan.TestCases ?? "None"}
</existing_test_cases>

<feedback>
{feedback}
</feedback>

Revise the test cases to address the feedback. Keep test cases that are still valid, and update/add new ones as needed.

Output only the revised test cases in markdown format (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private async Task<string?> RegenerateImplementationStepsAsync(
        AgentContext context,
        Domain.Entities.Plan existingPlan,
        string feedback,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a Tech Lead revising implementation steps based on feedback.

<existing_implementation_steps>
{existingPlan.ImplementationSteps ?? "None"}
</existing_implementation_steps>

<feedback>
{feedback}
</feedback>

Revise the implementation steps to address the feedback. Keep steps that are still valid, and update/add new ones as needed.

Output only the revised implementation steps in markdown format (no preamble or explanation).";

        var response = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        return response.Success ? response.Content : null;
    }

    private string ExtractYamlContent(string response)
    {
        if (response.Contains("```yaml") || response.Contains("```yml"))
        {
            var start = response.IndexOf("```", StringComparison.Ordinal);
            if (start >= 0)
            {
                start += 3;
                var newlineIndex = response.IndexOf('\n', start);
                if (newlineIndex >= 0)
                {
                    start = newlineIndex + 1;
                }

                var end = response.IndexOf("```", start, StringComparison.Ordinal);
                if (end > start)
                {
                    return response.Substring(start, end - start).Trim();
                }
            }
        }

        return response.Trim();
    }
}
