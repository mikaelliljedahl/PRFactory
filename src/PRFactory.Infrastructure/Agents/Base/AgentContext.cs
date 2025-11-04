using PRFactory.Core.Domain.Entities;
using System;
using System.Collections.Generic;

namespace PRFactory.Infrastructure.Agents.Base;

/// <summary>
/// Shared context passed between agents in the workflow.
/// Contains ticket information, analysis results, and intermediate data.
/// </summary>
public class AgentContext
{
    /// <summary>
    /// The ticket ID being processed
    /// </summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>
    /// The tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The repository ID
    /// </summary>
    public string RepositoryId { get; set; } = string.Empty;

    /// <summary>
    /// State dictionary for agent framework compatibility
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// The ticket being processed
    /// </summary>
    public Ticket Ticket { get; set; } = null!;

    /// <summary>
    /// Tenant information
    /// </summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Repository information
    /// </summary>
    public Repository Repository { get; set; } = null!;

    /// <summary>
    /// Local path where repository was cloned
    /// </summary>
    public string? RepositoryPath { get; set; }

    /// <summary>
    /// Codebase analysis results from Claude
    /// </summary>
    public CodebaseAnalysis? Analysis { get; set; }

    /// <summary>
    /// Generated implementation plan markdown
    /// </summary>
    public string? ImplementationPlan { get; set; }

    /// <summary>
    /// Feature branch name for plan
    /// </summary>
    public string? PlanBranchName { get; set; }

    /// <summary>
    /// Feature branch name for implementation
    /// </summary>
    public string? ImplementationBranchName { get; set; }

    /// <summary>
    /// Pull request URL after creation
    /// </summary>
    public string? PullRequestUrl { get; set; }

    /// <summary>
    /// Pull request number
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// Execution status
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Running;

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata for passing data between agents
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Checkpoint data for resuming after human-in-the-loop
    /// </summary>
    public CheckpointData? Checkpoint { get; set; }

    /// <summary>
    /// Restores context from a checkpoint
    /// </summary>
    public void RestoreFromCheckpoint(AgentCheckpoint checkpoint)
    {
        if (checkpoint.State != null)
        {
            State = new Dictionary<string, object>(checkpoint.State);
        }
    }
}

/// <summary>
/// Represents the result of a codebase analysis
/// </summary>
public class CodebaseAnalysis
{
    public string Summary { get; set; } = string.Empty;
    public List<string> AffectedFiles { get; set; } = new();
    public List<string> TechnicalConsiderations { get; set; } = new();
    public string Architecture { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Agent execution status
/// </summary>
public enum AgentStatus
{
    Running,
    Suspended,
    Completed,
    Failed
}

/// <summary>
/// Checkpoint data for resuming workflows
/// </summary>
public class CheckpointData
{
    public Guid CheckpointId { get; set; } = Guid.NewGuid();
    public string NextAgentType { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> State { get; set; } = new();
}

/// <summary>
/// Agent checkpoint for saving and restoring execution state
/// </summary>
public class AgentCheckpoint
{
    public string CheckpointId { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement checkpoint persistence (file system or database)
        return Task.CompletedTask;
    }

    public static Task<AgentCheckpoint?> LoadLatestAsync(string ticketId, string agentName, CancellationToken cancellationToken)
    {
        // TODO: Implement checkpoint loading from persistence
        return Task.FromResult<AgentCheckpoint?>(null);
    }
}
