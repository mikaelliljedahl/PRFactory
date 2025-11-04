namespace PRFactory.Infrastructure.Claude.Models;

/// <summary>
/// Represents a message in a Claude conversation
/// </summary>
/// <param name="Role">The role (user or assistant)</param>
/// <param name="Content">The message content</param>
public record Message(string Role, string Content);
