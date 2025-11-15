namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents an implementation plan for a ticket.
/// Supports both legacy single-file plans and new multi-artifact plans.
/// </summary>
public class Plan
{
    /// <summary>
    /// Unique identifier for the plan
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ticket this plan belongs to
    /// </summary>
    public Guid TicketId { get; private set; }

    /// <summary>
    /// Navigation property to the ticket
    /// </summary>
    public Ticket? Ticket { get; }

    /// <summary>
    /// Legacy single-file plan content (kept for backward compatibility)
    /// </summary>
    public string? Content { get; private set; }

    /// <summary>
    /// User stories artifact (Product Manager persona output)
    /// </summary>
    public string? UserStories { get; private set; }

    /// <summary>
    /// API design artifact (Software Architect persona output - OpenAPI YAML)
    /// </summary>
    public string? ApiDesign { get; private set; }

    /// <summary>
    /// Database schema artifact (Database Architect persona output - SQL DDL)
    /// </summary>
    public string? DatabaseSchema { get; private set; }

    /// <summary>
    /// Test cases artifact (QA Engineer persona output)
    /// </summary>
    public string? TestCases { get; private set; }

    /// <summary>
    /// Implementation steps artifact (Tech Lead persona output)
    /// </summary>
    public string? ImplementationSteps { get; private set; }

    /// <summary>
    /// Version number (incremented on each revision)
    /// </summary>
    public int Version { get; private set; } = 1;

    /// <summary>
    /// When the plan was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the plan was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Navigation property to version history
    /// </summary>
    public List<PlanVersion> Versions { get; private set; } = new();

    /// <summary>
    /// Determines if this plan uses multi-artifact format
    /// </summary>
    public bool HasMultipleArtifacts =>
        !string.IsNullOrEmpty(UserStories) ||
        !string.IsNullOrEmpty(ApiDesign) ||
        !string.IsNullOrEmpty(DatabaseSchema) ||
        !string.IsNullOrEmpty(TestCases) ||
        !string.IsNullOrEmpty(ImplementationSteps);

    private Plan() { }

    /// <summary>
    /// Creates a new plan for a ticket
    /// </summary>
    public static Plan Create(Guid ticketId, string? content = null)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID cannot be empty", nameof(ticketId));

        return new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    /// <summary>
    /// Creates a new plan with multi-artifact content
    /// </summary>
    public static Plan CreateWithArtifacts(
        Guid ticketId,
        string? userStories = null,
        string? apiDesign = null,
        string? databaseSchema = null,
        string? testCases = null,
        string? implementationSteps = null)
    {
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID cannot be empty", nameof(ticketId));

        return new Plan
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserStories = userStories,
            ApiDesign = apiDesign,
            DatabaseSchema = databaseSchema,
            TestCases = testCases,
            ImplementationSteps = implementationSteps,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the legacy content field
    /// </summary>
    public void UpdateContent(string content)
    {
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates artifacts and increments version
    /// </summary>
    public void UpdateArtifacts(
        string? userStories = null,
        string? apiDesign = null,
        string? databaseSchema = null,
        string? testCases = null,
        string? implementationSteps = null,
        string? createdBy = null,
        string? revisionReason = null)
    {
        // Create version snapshot before updating
        var version = CreateVersion(createdBy, revisionReason);
        Versions.Add(version);

        // Update artifacts (only update non-null values)
        if (userStories != null) UserStories = userStories;
        if (apiDesign != null) ApiDesign = apiDesign;
        if (databaseSchema != null) DatabaseSchema = databaseSchema;
        if (testCases != null) TestCases = testCases;
        if (implementationSteps != null) ImplementationSteps = implementationSteps;

        // Increment version
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a snapshot of current artifacts as a new version
    /// </summary>
    public PlanVersion CreateVersion(string? createdBy = null, string? revisionReason = null)
    {
        return new PlanVersion(new PlanVersionParameters
        {
            PlanId = Id,
            Version = Version,
            UserStories = UserStories,
            ApiDesign = ApiDesign,
            DatabaseSchema = DatabaseSchema,
            TestCases = TestCases,
            ImplementationSteps = ImplementationSteps,
            CreatedBy = createdBy,
            RevisionReason = revisionReason
        });
    }
}
