namespace PRFactory.Infrastructure.Persistence.Encryption;

/// <summary>
/// Service for encrypting and decrypting sensitive data at rest.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    /// <param name="plaintext">The text to encrypt</param>
    /// <returns>Base64-encoded encrypted string</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="ciphertext">Base64-encoded encrypted string</param>
    /// <returns>Decrypted plaintext</returns>
    string Decrypt(string ciphertext);
}
