namespace PRFactory.Infrastructure.Agents.Messages;

using System;
using System.Collections.Generic;

// Base message interface
public interface IAgentMessage
{
    Guid TicketId { get; }
    DateTime Timestamp { get; }
}

// Trigger messages
public record TriggerTicketMessage(
    string TicketKey,
    Guid TenantId,
    Guid RepositoryId,
    string TicketSystem
) : IAgentMessage
{
    public Guid TicketId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record TicketTriggeredMessage(
    Guid TicketId,
    string TicketKey,
    string Title,
    string Description,
    Guid RepositoryId
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Repository messages
public record RepositoryClonedMessage(
    Guid TicketId,
    string LocalPath,
    string DefaultBranch
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Analysis messages
public record CodebaseAnalyzedMessage(
    Guid TicketId,
    List<string> RelevantFiles,
    string Architecture,
    List<string> Patterns,
    Dictionary<string, string> FileContents
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Question messages
public record QuestionsGeneratedMessage(
    Guid TicketId,
    List<Question> Questions
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record Question(
    string Id,
    string Text,
    string Category,
    bool IsRequired
);

// Jira messages
public record MessagePostedMessage(
    Guid TicketId,
    string MessageType,
    DateTime PostedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Human interaction messages
public record AnswersReceivedMessage(
    Guid TicketId,
    Dictionary<string, string> Answers
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Planning messages
public record PlanGeneratedMessage(
    Guid TicketId,
    string MainPlan,
    string AffectedFiles,
    string TestStrategy,
    int EstimatedComplexity
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record PlanCommittedMessage(
    Guid TicketId,
    string BranchName,
    string CommitSha,
    string BranchUrl
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record PlanApprovedMessage(
    Guid TicketId,
    DateTime ApprovedAt,
    string ApprovedBy
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record PlanRejectedMessage(
    Guid TicketId,
    string Reason,
    string? RefinementInstructions = null,
    bool RegenerateCompletely = false
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Implementation messages
public record CodeImplementedMessage(
    Guid TicketId,
    Dictionary<string, string> ModifiedFiles,
    List<string> CreatedFiles,
    string Summary
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Pull request messages
public record PRCreatedMessage(
    Guid TicketId,
    int PullRequestNumber,
    string PullRequestUrl,
    DateTime CreatedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Completion messages
public record WorkflowCompletedMessage(
    Guid TicketId,
    string FinalState,
    TimeSpan TotalDuration,
    Dictionary<string, object> Metrics
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Ticket update messages
public record TicketUpdateGeneratedMessage(
    Guid TicketId,
    Guid TicketUpdateId,
    int Version,
    string UpdatedTitle
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record TicketUpdateApprovedMessage(
    Guid TicketId,
    Guid TicketUpdateId,
    DateTime ApprovedAt,
    string ApprovedBy
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record TicketUpdateRejectedMessage(
    Guid TicketId,
    Guid TicketUpdateId,
    string Reason
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record TicketUpdatePostedMessage(
    Guid TicketId,
    Guid TicketUpdateId,
    DateTime PostedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Event messages for graph transitions
public record RefinementCompleteEvent(
    Guid TicketId,
    DateTime CompletedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record PlanApprovedEvent(
    Guid TicketId,
    DateTime ApprovedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Code review messages
public record ReviewCodeMessage(
    Guid TicketId,
    int PullRequestNumber,
    string PullRequestUrl,
    string BranchName,
    string? PlanPath = null
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record CodeReviewCompleteMessage(
    Guid TicketId,
    Guid CodeReviewResultId,
    bool HasCriticalIssues,
    List<string> CriticalIssues,
    List<string> Suggestions,
    DateTime ReviewedAt
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record FixCodeIssuesMessage(
    Guid TicketId,
    List<string> Issues,
    string ReviewFeedback
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record PostCommentsMessage(
    Guid TicketId,
    Guid CodeReviewResultId,
    List<string> Comments
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record ApprovalMessage(
    Guid TicketId,
    Guid CodeReviewResultId,
    string ApprovalComment
) : IAgentMessage
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
