namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Represents a single success criterion for a ticket.
/// Success criteria define what must be achieved for the ticket to be considered complete.
/// </summary>
public record SuccessCriterion
{
    /// <summary>
    /// Category of the success criterion
    /// </summary>
    public SuccessCriterionCategory Category { get; init; }

    /// <summary>
    /// Detailed description of what needs to be achieved
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Priority level: 0=must-have, 1=should-have, 2=nice-to-have
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Indicates whether this criterion can be objectively tested/verified
    /// </summary>
    public bool IsTestable { get; init; }

    /// <summary>
    /// Creates a new success criterion with validation
    /// </summary>
    public SuccessCriterion(
        SuccessCriterionCategory category,
        string description,
        int priority,
        bool isTestable)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Success criterion description cannot be empty", nameof(description));

        if (priority < 0 || priority > 2)
            throw new ArgumentException("Priority must be 0 (must-have), 1 (should-have), or 2 (nice-to-have)", nameof(priority));

        Category = category;
        Description = description;
        Priority = priority;
        IsTestable = isTestable;
    }

    /// <summary>
    /// Creates a must-have success criterion (Priority 0)
    /// </summary>
    public static SuccessCriterion CreateMustHave(
        SuccessCriterionCategory category,
        string description,
        bool isTestable = true)
    {
        return new SuccessCriterion(category, description, 0, isTestable);
    }

    /// <summary>
    /// Creates a should-have success criterion (Priority 1)
    /// </summary>
    public static SuccessCriterion CreateShouldHave(
        SuccessCriterionCategory category,
        string description,
        bool isTestable = true)
    {
        return new SuccessCriterion(category, description, 1, isTestable);
    }

    /// <summary>
    /// Creates a nice-to-have success criterion (Priority 2)
    /// </summary>
    public static SuccessCriterion CreateNiceToHave(
        SuccessCriterionCategory category,
        string description,
        bool isTestable = true)
    {
        return new SuccessCriterion(category, description, 2, isTestable);
    }

    /// <summary>
    /// Gets a human-readable priority label
    /// </summary>
    public string GetPriorityLabel() => Priority switch
    {
        0 => "Must-Have",
        1 => "Should-Have",
        2 => "Nice-to-Have",
        _ => "Unknown"
    };
}

/// <summary>
/// Categories for success criteria
/// </summary>
public enum SuccessCriterionCategory
{
    /// <summary>
    /// Functional requirements - what the feature must do
    /// </summary>
    Functional,

    /// <summary>
    /// Technical requirements - implementation constraints and technical details
    /// </summary>
    Technical,

    /// <summary>
    /// Testing requirements - what tests must pass or be created
    /// </summary>
    Testing,

    /// <summary>
    /// User experience requirements - UX/UI requirements
    /// </summary>
    UX,

    /// <summary>
    /// Security requirements - security constraints and requirements
    /// </summary>
    Security,

    /// <summary>
    /// Performance requirements - performance targets and constraints
    /// </summary>
    Performance
}
