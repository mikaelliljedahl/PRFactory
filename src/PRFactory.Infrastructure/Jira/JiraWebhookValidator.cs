using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace PRFactory.Infrastructure.Jira;

/// <summary>
/// Validates Jira webhook signatures using HMAC SHA256.
/// </summary>
public interface IJiraWebhookValidator
{
    /// <summary>
    /// Validates the signature of a webhook payload.
    /// </summary>
    /// <param name="rawBody">The raw request body as received from Jira.</param>
    /// <param name="signature">The signature from the X-Hub-Signature header.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    bool ValidateSignature(string rawBody, string signature);

    /// <summary>
    /// Validates the signature of a webhook payload using byte array.
    /// </summary>
    /// <param name="bodyBytes">The raw request body bytes.</param>
    /// <param name="signature">The signature from the X-Hub-Signature header.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    bool ValidateSignature(byte[] bodyBytes, string signature);
}

/// <summary>
/// Implementation of HMAC SHA256 webhook signature validation for Jira webhooks.
/// </summary>
/// <remarks>
/// Jira webhooks include an X-Hub-Signature header containing an HMAC SHA256 signature
/// of the request body. This validator ensures the webhook is authentic and hasn't been tampered with.
///
/// The signature format is: "sha256={hex-encoded-hash}"
/// </remarks>
public class JiraWebhookValidator : IJiraWebhookValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JiraWebhookValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraWebhookValidator"/> class.
    /// </summary>
    /// <param name="configuration">Configuration for accessing the webhook secret.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public JiraWebhookValidator(
        IConfiguration configuration,
        ILogger<JiraWebhookValidator> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool ValidateSignature(string rawBody, string signature)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            _logger.LogWarning("Webhook validation failed: Raw body is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Webhook validation failed: Signature is null or empty");
            return false;
        }

        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        return ValidateSignature(bodyBytes, signature);
    }

    /// <inheritdoc />
    public bool ValidateSignature(byte[] bodyBytes, string signature)
    {
        if (bodyBytes == null || bodyBytes.Length == 0)
        {
            _logger.LogWarning("Webhook validation failed: Body bytes are null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Webhook validation failed: Signature is null or empty");
            return false;
        }

        try
        {
            var secret = GetWebhookSecret();

            var expectedSignature = ComputeHmacSha256(bodyBytes, secret);

            // Use constant-time comparison to prevent timing attacks
            var isValid = ConstantTimeEquals(signature, expectedSignature);

            if (!isValid)
            {
                _logger.LogWarning("Webhook validation failed: Signature mismatch. Expected format: sha256={{hash}}");
            }
            else
            {
                _logger.LogDebug("Webhook signature validated successfully");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Retrieves the webhook secret from configuration.
    /// </summary>
    /// <returns>The webhook secret.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the webhook secret is not configured.</exception>
    private string GetWebhookSecret()
    {
        var secret = _configuration["Jira:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Jira webhook secret is not configured. Set 'Jira:WebhookSecret' in appsettings.json");
            throw new InvalidOperationException(
                "Jira webhook secret not configured. Set 'Jira:WebhookSecret' in configuration.");
        }

        return secret;
    }

    /// <summary>
    /// Computes the HMAC SHA256 hash of the data using the secret.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="secret">The secret key.</param>
    /// <returns>The signature in the format "sha256={hex-hash}".</returns>
    private string ComputeHmacSha256(byte[] data, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(data);

        // Convert to hex string (lowercase)
        var hashHex = BitConverter.ToString(hashBytes)
            .Replace("-", "")
            .ToLowerInvariant();

        return $"sha256={hashHex}";
    }

    /// <summary>
    /// Performs a constant-time string comparison to prevent timing attacks.
    /// </summary>
    /// <param name="a">First string to compare.</param>
    /// <param name="b">Second string to compare.</param>
    /// <returns>True if strings are equal; otherwise, false.</returns>
    /// <remarks>
    /// This prevents timing attacks where an attacker could determine the correct
    /// signature by measuring how long the comparison takes.
    /// </remarks>
    private bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

        var result = 0;

        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}

/// <summary>
/// Extension methods for webhook validation.
/// </summary>
public static class WebhookValidatorExtensions
{
    /// <summary>
    /// Validates a webhook signature and throws an exception if invalid.
    /// </summary>
    /// <param name="validator">The webhook validator.</param>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="signature">The signature from the header.</param>
    /// <exception cref="WebhookValidationException">Thrown if the signature is invalid.</exception>
    public static void ValidateSignatureOrThrow(
        this IJiraWebhookValidator validator,
        string rawBody,
        string signature)
    {
        if (!validator.ValidateSignature(rawBody, signature))
        {
            throw new WebhookValidationException("Invalid webhook signature");
        }
    }

    /// <summary>
    /// Validates a webhook signature and throws an exception if invalid.
    /// </summary>
    /// <param name="validator">The webhook validator.</param>
    /// <param name="bodyBytes">The raw request body bytes.</param>
    /// <param name="signature">The signature from the header.</param>
    /// <exception cref="WebhookValidationException">Thrown if the signature is invalid.</exception>
    public static void ValidateSignatureOrThrow(
        this IJiraWebhookValidator validator,
        byte[] bodyBytes,
        string signature)
    {
        if (!validator.ValidateSignature(bodyBytes, signature))
        {
            throw new WebhookValidationException("Invalid webhook signature");
        }
    }
}

/// <summary>
/// Exception thrown when webhook validation fails.
/// </summary>
public class WebhookValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WebhookValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WebhookValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
