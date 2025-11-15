using System.Net;
using System.Security;

namespace PRFactory.AgentTools.Security;

/// <summary>
/// Protects against Server-Side Request Forgery (SSRF) attacks
/// </summary>
public static class SsrfProtection
{
    private static readonly string[] BlockedHosts = {
        "localhost", "127.0.0.1", "::1", "0.0.0.0",
        "169.254.169.254",          // AWS metadata
        "metadata.google.internal", // GCP metadata
        "metadata.azure.com"        // Azure metadata
    };

    /// <summary>
    /// Validate URL is not targeting internal/private resources.
    /// Throws SecurityException if URL is blocked.
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <exception cref="ArgumentException">Thrown when URL is invalid</exception>
    /// <exception cref="SecurityException">Thrown when URL targets blocked or private resources</exception>
    public static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL is required", nameof(url));

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException ex)
        {
            throw new ArgumentException($"Invalid URL format: {url}", nameof(url), ex);
        }

        // 1. Check blocked hosts
        if (BlockedHosts.Any(h =>
            uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)))
        {
            throw new SecurityException(
                $"Access to '{uri.Host}' is blocked for security reasons");
        }

        // 2. Resolve DNS and check for private IPs
        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(uri.Host);
        }
        catch (Exception ex)
        {
            throw new SecurityException(
                $"Failed to resolve host '{uri.Host}': {ex.Message}", ex);
        }

        foreach (var ip in addresses)
        {
            if (IsPrivateOrLoopback(ip))
            {
                throw new SecurityException(
                    $"Access to private/loopback IP '{ip}' is blocked");
            }
        }
    }

    /// <summary>
    /// Check if an IP address is private or loopback
    /// </summary>
    /// <param name="ip">IP address to check</param>
    /// <returns>True if IP is private or loopback</returns>
    private static bool IsPrivateOrLoopback(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        var bytes = ip.GetAddressBytes();

        // IPv6: Just check loopback (already checked above)
        if (bytes.Length != 4)
            return false;

        // Check common private IPv4 ranges
        return (bytes[0] == 10) ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }
}
