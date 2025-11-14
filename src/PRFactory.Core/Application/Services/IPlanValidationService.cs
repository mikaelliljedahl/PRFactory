namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for validating implementation plans using AI analysis
/// </summary>
public interface IPlanValidationService
{
    /// <summary>
    /// Validate a plan using a predefined check type
    /// </summary>
    /// <param name="ticketId">Ticket ID containing the plan</param>
    /// <param name="checkType">Type of validation: security, completeness, performance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with score and findings</returns>
    Task<PlanValidationResult> ValidatePlanAsync(Guid ticketId, string checkType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a plan using a custom prompt
    /// </summary>
    /// <param name="ticketId">Ticket ID containing the plan</param>
    /// <param name="customPrompt">Custom validation prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<PlanValidationResult> ValidatePlanWithPromptAsync(Guid ticketId, string customPrompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate code implementation against approved plan
    /// </summary>
    /// <param name="ticketId">Ticket ID with approved plan</param>
    /// <param name="diffContent">Git diff of code changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alignment validation result</returns>
    Task<CodePlanAlignmentResult> ValidateCodeVsPlanAsync(Guid ticketId, string diffContent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of plan validation
/// </summary>
public class PlanValidationResult
{
    public Guid TicketId { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public int Score { get; set; } // 0-100
    public string? RiskLevel { get; set; } // Low, Medium, High, Critical (for security)
    public List<string> Findings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
}

/// <summary>
/// Result of code-vs-plan alignment validation
/// </summary>
public class CodePlanAlignmentResult
{
    public Guid TicketId { get; set; }
    public int AlignmentScore { get; set; } // 0-100
    public List<string> ImplementedRequirements { get; set; } = new();
    public List<string> MissingRequirements { get; set; } = new();
    public List<string> Deviations { get; set; } = new();
    public List<string> UnplannedCode { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
}
