namespace PRFactory.Domain.ValueObjects;

/// <summary>
/// Represents the lifecycle status of a checkpoint.
/// </summary>
public enum CheckpointStatus
{
    /// <summary>
    /// Checkpoint is active and represents the current state of a workflow
    /// </summary>
    Active,

    /// <summary>
    /// Checkpoint has been resumed and workflow is continuing
    /// </summary>
    Resumed,

    /// <summary>
    /// Checkpoint has expired and is no longer valid (e.g., due to age or workflow completion)
    /// </summary>
    Expired,

    /// <summary>
    /// Checkpoint has been explicitly deleted
    /// </summary>
    Deleted
}
