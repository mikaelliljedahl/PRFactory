using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Persistence.Encryption;

/// <summary>
/// AES-256 encryption service for encrypting sensitive data at rest.
/// Uses AES-GCM for authenticated encryption.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _encryptionKey;
    private readonly ILogger<AesEncryptionService> _logger;

    // AES-GCM nonce size (12 bytes is recommended)
    private const int NonceSize = 12;
    // AES-GCM tag size (16 bytes)
    private const int TagSize = 16;

    public AesEncryptionService(string base64Key, ILogger<AesEncryptionService> logger)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new ArgumentException("Encryption key cannot be empty", nameof(base64Key));
        }

        try
        {
            _encryptionKey = Convert.FromBase64String(base64Key);

            if (_encryptionKey.Length != 32) // 256 bits
            {
                throw new ArgumentException("Encryption key must be 256 bits (32 bytes)", nameof(base64Key));
            }
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid base64 encryption key", nameof(base64Key), ex);
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        try
        {
            // Generate random nonce
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Convert plaintext to bytes
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            // Create cipher
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagSize];

            using var aesGcm = new AesGcm(_encryptionKey, TagSize);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine nonce + tag + ciphertext
            byte[] result = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return ciphertext;
        }

        try
        {
            byte[] combined = Convert.FromBase64String(ciphertext);

            if (combined.Length < NonceSize + TagSize)
            {
                throw new ArgumentException("Invalid ciphertext format");
            }

            // Extract nonce, tag, and ciphertext
            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            byte[] ciphertextBytes = new byte[combined.Length - NonceSize - TagSize];

            Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(combined, NonceSize + TagSize, ciphertextBytes, 0, ciphertextBytes.Length);

            // Decrypt
            byte[] plaintext = new byte[ciphertextBytes.Length];

            using var aesGcm = new AesGcm(_encryptionKey, TagSize);
            aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt data - invalid key or corrupted data");
            throw new InvalidOperationException("Decryption failed", ex);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to decrypt data - invalid format");
            throw new ArgumentException("Invalid ciphertext format", nameof(ciphertext), ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }
}

/// <summary>
/// Helper methods for generating encryption keys.
/// </summary>
public static class EncryptionKeyGenerator
{
    /// <summary>
    /// Generates a new random 256-bit encryption key and returns it as a base64 string.
    /// </summary>
    public static string GenerateKey()
    {
        byte[] key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
