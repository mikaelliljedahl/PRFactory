using System.Text;

namespace PRFactory.AgentTools.Security;

/// <summary>
/// Defines and enforces resource limits to prevent exhaustion attacks
/// </summary>
public static class ResourceLimits
{
    /// <summary>
    /// Maximum file size for read operations (10MB)
    /// </summary>
    public const long MaxFileSize = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum content size for write operations (1MB)
    /// </summary>
    public const long MaxWriteSize = 1 * 1024 * 1024;

    /// <summary>
    /// Maximum number of result lines for search operations
    /// </summary>
    public const int MaxResultLines = 1000;

    /// <summary>
    /// Maximum number of results for glob operations
    /// </summary>
    public const int MaxGlobResults = 1000;

    /// <summary>
    /// Default timeout for tool operations
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validate file size does not exceed limit
    /// </summary>
    /// <param name="fileSize">File size in bytes</param>
    /// <param name="maxSize">Maximum allowed size (defaults to MaxFileSize)</param>
    /// <exception cref="InvalidOperationException">Thrown when file size exceeds limit</exception>
    public static void ValidateFileSize(long fileSize, long maxSize = MaxFileSize)
    {
        if (fileSize < 0)
            throw new ArgumentException("File size cannot be negative", nameof(fileSize));

        if (fileSize > maxSize)
        {
            throw new InvalidOperationException(
                $"File size {fileSize} bytes exceeds limit {maxSize} bytes");
        }
    }

    /// <summary>
    /// Validate content size does not exceed limit
    /// </summary>
    /// <param name="content">Content to validate</param>
    /// <param name="maxSize">Maximum allowed size (defaults to MaxWriteSize)</param>
    /// <exception cref="ArgumentNullException">Thrown when content is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when content size exceeds limit</exception>
    public static void ValidateContentSize(string content, long maxSize = MaxWriteSize)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        var size = Encoding.UTF8.GetByteCount(content);
        if (size > maxSize)
        {
            throw new InvalidOperationException(
                $"Content size {size} bytes exceeds limit {maxSize} bytes");
        }
    }
}
