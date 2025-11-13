using System.ComponentModel.DataAnnotations;

namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for tenant configuration settings
/// </summary>
public class TenantConfigurationDto
{
    /// <summary>
    /// Whether to automatically start implementation after plan approval
    /// </summary>
    public bool AutoImplementAfterPlanApproval { get; set; }

    /// <summary>
    /// Maximum number of retries for failed operations
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max retries must be between 1 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Claude model to use for this tenant
    /// </summary>
    [Required(ErrorMessage = "Claude model is required")]
    [MaxLength(100, ErrorMessage = "Claude model cannot exceed 100 characters")]
    public string ClaudeModel { get; set; } = "claude-sonnet-4-5-20250929";

    /// <summary>
    /// Maximum tokens per Claude API request
    /// </summary>
    [Range(1000, 200000, ErrorMessage = "Max tokens must be between 1,000 and 200,000")]
    public int MaxTokensPerRequest { get; set; } = 8000;

    /// <summary>
    /// Timeout in seconds for Claude API requests
    /// </summary>
    [Range(30, 600, ErrorMessage = "API timeout must be between 30 and 600 seconds")]
    public int ApiTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to enable verbose logging for this tenant
    /// </summary>
    public bool EnableVerboseLogging { get; set; }

    /// <summary>
    /// Whether to enable code review workflow for this tenant (DEPRECATED - use EnableAutoCodeReview)
    /// </summary>
    public bool EnableCodeReview { get; set; }

    /// <summary>
    /// List of allowed repository names for this tenant (empty means all allowed)
    /// </summary>
    public string[] AllowedRepositories { get; set; } = [];

    /// <summary>
    /// Custom prompt templates for this tenant (optional)
    /// </summary>
    public Dictionary<string, string> CustomPromptTemplates { get; set; } = new();

    /// <summary>
    /// Whether to automatically trigger code review after PR creation
    /// </summary>
    public bool EnableAutoCodeReview { get; set; }

    /// <summary>
    /// LLM provider ID to use for code review (null = use tenant default)
    /// </summary>
    public Guid? CodeReviewLlmProviderId { get; set; }

    /// <summary>
    /// LLM provider ID to use for code implementation (null = use tenant default)
    /// </summary>
    public Guid? ImplementationLlmProviderId { get; set; }

    /// <summary>
    /// LLM provider ID to use for planning (null = use tenant default)
    /// </summary>
    public Guid? PlanningLlmProviderId { get; set; }

    /// <summary>
    /// LLM provider ID to use for ticket analysis and refinement (null = use tenant default)
    /// </summary>
    public Guid? AnalysisLlmProviderId { get; set; }

    /// <summary>
    /// Maximum number of code review iterations before completing with warnings (default: 3)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max code review iterations must be between 1 and 10")]
    public int MaxCodeReviewIterations { get; set; } = 3;

    /// <summary>
    /// Whether to automatically post approval comment if code review finds no issues
    /// </summary>
    public bool AutoApproveIfNoIssues { get; set; }

    /// <summary>
    /// Whether to require human approval after code review before merging (future use)
    /// </summary>
    public bool RequireHumanApprovalAfterReview { get; set; } = true;
}
