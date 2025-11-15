using System.Security.Cryptography;
using System.Text;

namespace PRFactory.Web.Middleware;

/// <summary>
/// Middleware for validating Jira webhook HMAC signatures
/// </summary>
public class JiraWebhookAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JiraWebhookAuthenticationMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public JiraWebhookAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JiraWebhookAuthenticationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate webhook endpoints
        if (context.Request.Path.StartsWithSegments("/api/webhooks/jira"))
        {
            var webhookSecret = _configuration["Jira:WebhookSecret"];

            // Skip validation if no secret is configured (development mode)
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("Jira webhook secret is not configured. Skipping HMAC validation.");
                await _next(context);
                return;
            }

            // Read the request body
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Get the signature from the header
            if (!context.Request.Headers.TryGetValue("X-Hub-Signature", out var signatureHeader))
            {
                _logger.LogWarning("Jira webhook request missing X-Hub-Signature header");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Missing signature header" });
                return;
            }

            // Validate the signature
            if (!ValidateSignature(body, signatureHeader.ToString(), webhookSecret))
            {
                _logger.LogWarning("Jira webhook signature validation failed");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid signature" });
                return;
            }

            _logger.LogInformation("Jira webhook signature validated successfully");
        }

        await _next(context);
    }

    private static bool ValidateSignature(string body, string signature, string secret)
    {
        try
        {
            // Jira uses HMAC-SHA256 with format: sha256=<hash>
            if (!signature.StartsWith("sha256="))
            {
                return false;
            }

            var signatureHash = signature.Substring(7); // Remove "sha256=" prefix

            // Calculate the expected signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();

            // Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signatureHash),
                Encoding.UTF8.GetBytes(expectedSignature));
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Extension methods for Jira webhook authentication middleware
/// </summary>
public static class JiraWebhookAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJiraWebhookAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JiraWebhookAuthenticationMiddleware>();
    }
}
