using System;
using System.ComponentModel.DataAnnotations;

namespace PRFactory.Web.Models;

/// <summary>
/// DTO for configuring which LLM provider each agent type uses.
/// Maps to TenantConfiguration expanded with agent-specific provider assignments.
/// </summary>
public class AgentConfigurationDto
{
    /// <summary>
    /// Tenant ID this configuration belongs to
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// LLM provider ID for analysis agents (ticket refinement, question answering)
    /// Null means use tenant default provider
    /// </summary>
    public Guid? AnalysisAgentProviderId { get; set; }

    /// <summary>
    /// LLM provider ID for planning agents (implementation plan generation)
    /// Null means use tenant default provider
    /// </summary>
    public Guid? PlanningAgentProviderId { get; set; }

    /// <summary>
    /// LLM provider ID for implementation agents (code generation, git operations)
    /// Null means use tenant default provider
    /// </summary>
    public Guid? ImplementationAgentProviderId { get; set; }

    /// <summary>
    /// LLM provider ID for code review agents (different perspective from implementation)
    /// Null means use tenant default provider
    /// </summary>
    public Guid? CodeReviewAgentProviderId { get; set; }

    /// <summary>
    /// Whether to enable automated code review workflow
    /// </summary>
    public bool EnableCodeReview { get; set; }

    /// <summary>
    /// Maximum number of code review iterations before requiring human intervention
    /// </summary>
    [Range(1, 10)]
    public int MaxCodeReviewIterations { get; set; } = 3;

    /// <summary>
    /// Whether to auto-approve code if no issues are found during review
    /// </summary>
    public bool AutoApproveIfNoIssues { get; set; }

    /// <summary>
    /// Whether to require human approval after automated code review completes
    /// </summary>
    public bool RequireHumanApprovalAfterReview { get; set; } = true;

    /// <summary>
    /// Timestamp when configuration was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Summary of an LLM provider for display in dropdowns
/// </summary>
public class LlmProviderSummaryDto
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Provider display name (e.g., "Production Claude", "Dev Minimax")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default model for this provider (e.g., "claude-sonnet-4-5-20250929")
    /// </summary>
    public string DefaultModel { get; set; } = string.Empty;

    /// <summary>
    /// Provider type (e.g., "AnthropicNative", "MinimaxM2", "ZAi")
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this is the default provider for the tenant
    /// </summary>
    public bool IsDefault { get; set; }
}
