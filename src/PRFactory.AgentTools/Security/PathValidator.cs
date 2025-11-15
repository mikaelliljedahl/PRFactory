using System.Security;

namespace PRFactory.AgentTools.Security;

/// <summary>
/// Validates file paths to prevent directory traversal attacks
/// </summary>
public static class PathValidator
{
    /// <summary>
    /// Validate and resolve a file path within workspace.
    /// Throws SecurityException if path is outside workspace.
    /// </summary>
    /// <param name="filePath">Relative file path to validate</param>
    /// <param name="workspacePath">Workspace root path</param>
    /// <returns>Fully resolved absolute path within workspace</returns>
    /// <exception cref="ArgumentException">Thrown when path parameters are invalid</exception>
    /// <exception cref="SecurityException">Thrown when path is outside workspace or contains malicious patterns</exception>
    public static string ValidateAndResolve(string filePath, string workspacePath)
    {
        // 1. Prevent obvious attacks
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));

        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path is required", nameof(workspacePath));

        if (filePath.Contains(".."))
            throw new SecurityException("Path contains '..' which is not allowed");

        if (Path.IsPathRooted(filePath))
            throw new SecurityException("Absolute paths are not allowed");

        // 2. Combine and normalize
        var fullPath = Path.GetFullPath(Path.Combine(workspacePath, filePath));
        var normalizedWorkspace = Path.GetFullPath(workspacePath);

        // 3. Ensure within workspace
        if (!fullPath.StartsWith(normalizedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException(
                $"Access denied: Path '{filePath}' resolves to '{fullPath}' " +
                $"which is outside workspace '{normalizedWorkspace}'");
        }

        return fullPath;
    }
}
