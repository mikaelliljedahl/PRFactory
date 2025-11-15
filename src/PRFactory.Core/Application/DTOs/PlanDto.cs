namespace PRFactory.Core.Application.DTOs;

/// <summary>
/// DTO for displaying implementation plan information
/// </summary>
public class PlanDto
{
    /// <summary>
    /// Unique identifier for the plan
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ticket this plan belongs to
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// Legacy single-file plan content (kept for backward compatibility)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// User stories artifact (Product Manager persona output)
    /// </summary>
    public string? UserStories { get; set; }

    /// <summary>
    /// API design artifact (Software Architect persona output - OpenAPI YAML)
    /// </summary>
    public string? ApiDesign { get; set; }

    /// <summary>
    /// Database schema artifact (Database Architect persona output - SQL DDL)
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// Test cases artifact (QA Engineer persona output)
    /// </summary>
    public string? TestCases { get; set; }

    /// <summary>
    /// Implementation steps artifact (Tech Lead persona output)
    /// </summary>
    public string? ImplementationSteps { get; set; }

    /// <summary>
    /// Version number (incremented on each revision)
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// When the plan was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the plan was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Determines if this plan uses multi-artifact format
    /// </summary>
    public bool HasMultipleArtifacts =>
        !string.IsNullOrEmpty(UserStories) ||
        !string.IsNullOrEmpty(ApiDesign) ||
        !string.IsNullOrEmpty(DatabaseSchema) ||
        !string.IsNullOrEmpty(TestCases) ||
        !string.IsNullOrEmpty(ImplementationSteps);

    /// <summary>
    /// Version history (optional)
    /// </summary>
    public List<PlanVersionDto>? Versions { get; set; }
}

/// <summary>
/// DTO for displaying plan version history
/// </summary>
public class PlanVersionDto
{
    /// <summary>
    /// Unique identifier for the version
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The plan this version belongs to
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Snapshot of user stories at this version
    /// </summary>
    public string? UserStories { get; set; }

    /// <summary>
    /// Snapshot of API design at this version
    /// </summary>
    public string? ApiDesign { get; set; }

    /// <summary>
    /// Snapshot of database schema at this version
    /// </summary>
    public string? DatabaseSchema { get; set; }

    /// <summary>
    /// Snapshot of test cases at this version
    /// </summary>
    public string? TestCases { get; set; }

    /// <summary>
    /// Snapshot of implementation steps at this version
    /// </summary>
    public string? ImplementationSteps { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created this version (username or user ID)
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Reason for creating this revision
    /// </summary>
    public string? RevisionReason { get; set; }
}
