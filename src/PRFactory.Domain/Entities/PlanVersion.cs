namespace PRFactory.Domain.Entities;

/// <summary>
/// Parameters for creating a PlanVersion
/// </summary>
public class PlanVersionParameters
{
    public required Guid PlanId { get; init; }
    public required int Version { get; init; }
    public string? UserStories { get; init; }
    public string? ApiDesign { get; init; }
    public string? DatabaseSchema { get; init; }
    public string? TestCases { get; init; }
    public string? ImplementationSteps { get; init; }
    public string? CreatedBy { get; init; }
    public string? RevisionReason { get; init; }
}

/// <summary>
/// Represents a historical version of a plan's artifacts.
/// Created whenever a plan is revised.
/// </summary>
public class PlanVersion
{
    /// <summary>
    /// Unique identifier for the version
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The plan this version belongs to
    /// </summary>
    public Guid PlanId { get; private set; }

    /// <summary>
    /// Navigation property to the plan
    /// </summary>
    public Plan? Plan { get; }

    /// <summary>
    /// Version number
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Snapshot of user stories at this version
    /// </summary>
    public string? UserStories { get; private set; }

    /// <summary>
    /// Snapshot of API design at this version
    /// </summary>
    public string? ApiDesign { get; private set; }

    /// <summary>
    /// Snapshot of database schema at this version
    /// </summary>
    public string? DatabaseSchema { get; private set; }

    /// <summary>
    /// Snapshot of test cases at this version
    /// </summary>
    public string? TestCases { get; private set; }

    /// <summary>
    /// Snapshot of implementation steps at this version
    /// </summary>
    public string? ImplementationSteps { get; private set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Who created this version (username or user ID)
    /// </summary>
    public string? CreatedBy { get; private set; }

    /// <summary>
    /// Reason for creating this revision
    /// </summary>
    public string? RevisionReason { get; private set; }

    private PlanVersion() { }

    /// <summary>
    /// Creates a new plan version
    /// </summary>
    internal PlanVersion(PlanVersionParameters parameters)
    {
        Id = Guid.NewGuid();
        PlanId = parameters.PlanId;
        Version = parameters.Version;
        UserStories = parameters.UserStories;
        ApiDesign = parameters.ApiDesign;
        DatabaseSchema = parameters.DatabaseSchema;
        TestCases = parameters.TestCases;
        ImplementationSteps = parameters.ImplementationSteps;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = parameters.CreatedBy;
        RevisionReason = parameters.RevisionReason;
    }
}
