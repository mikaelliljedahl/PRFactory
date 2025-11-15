namespace PRFactory.Core.Application.Services;

/// <summary>
/// Service for managing workspace directories and files for tickets.
/// Centralizes workspace path logic and file operations.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Gets the root workspace directory for a ticket.
    /// Example: /var/prfactory/workspace/{guid}/
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Absolute path to workspace directory</returns>
    string GetWorkspaceDirectory(Guid ticketId);

    /// <summary>
    /// Gets the repository path within the workspace.
    /// Example: /var/prfactory/workspace/{guid}/repo/
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Absolute path to repository directory</returns>
    string GetRepositoryPath(Guid ticketId);

    /// <summary>
    /// Gets the diff.patch file path for a ticket.
    /// Example: /var/prfactory/workspace/{guid}/diff.patch
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Absolute path to diff.patch file</returns>
    string GetDiffPath(Guid ticketId);

    /// <summary>
    /// Reads the diff content for a ticket if it exists.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Diff content, or null if file doesn't exist</returns>
    Task<string?> ReadDiffAsync(Guid ticketId);

    /// <summary>
    /// Writes diff content to the workspace.
    /// Creates the workspace directory if it doesn't exist.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="diffContent">The diff content to write</param>
    Task WriteDiffAsync(Guid ticketId, string diffContent);

    /// <summary>
    /// Checks if a diff file exists for a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>True if diff.patch exists</returns>
    Task<bool> DiffExistsAsync(Guid ticketId);

    /// <summary>
    /// Deletes the diff file for a ticket (e.g., after PR created).
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    Task DeleteDiffAsync(Guid ticketId);

    /// <summary>
    /// Creates workspace directory for a ticket if it doesn't exist.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <returns>Workspace directory path</returns>
    Task<string> EnsureWorkspaceExistsAsync(Guid ticketId);
}
