namespace PRFactory.Domain.Entities;

/// <summary>
/// Status of a plan review by an individual reviewer
/// </summary>
public enum ReviewStatus
{
    /// <summary>
    /// Review has been assigned but not yet completed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Reviewer has approved the plan
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Reviewer has rejected the plan and requested refinement (keep structure, apply specific changes)
    /// </summary>
    RejectedForRefinement = 2,

    /// <summary>
    /// Reviewer has rejected the plan and requested complete regeneration (start from scratch)
    /// </summary>
    RejectedForRegeneration = 3
}
