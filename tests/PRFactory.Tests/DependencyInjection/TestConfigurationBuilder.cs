using Microsoft.Extensions.Configuration;

namespace PRFactory.Tests.DependencyInjection;

/// <summary>
/// Helper class for creating test configurations with common settings
/// </summary>
public static class TestConfigurationBuilder
{
    /// <summary>
    /// Creates a test configuration with commonly required settings
    /// </summary>
    /// <param name="encryptionKey">Encryption key (null to omit)</param>
    /// <param name="connectionString">Database connection string (null for default in-memory)</param>
    /// <param name="enableSensitiveLogging">Enable EF sensitive data logging</param>
    /// <param name="enableDetailedErrors">Enable detailed EF errors</param>
    /// <returns>IConfiguration instance</returns>
    public static IConfiguration CreateTestConfiguration(
        string? encryptionKey = "dGVzdC1lbmNyeXB0aW9uLWtleS12YWxpZC1iYXNlNjQ=",
        string? connectionString = null,
        bool enableSensitiveLogging = false,
        bool enableDetailedErrors = false)
    {
        var configDict = new Dictionary<string, string?>();

        if (encryptionKey != null)
        {
            configDict["Encryption:Key"] = encryptionKey;
        }

        configDict["ConnectionStrings:DefaultConnection"] = connectionString ?? "Data Source=:memory:";
        configDict["Logging:EnableSensitiveDataLogging"] = enableSensitiveLogging.ToString();
        configDict["Logging:EnableDetailedErrors"] = enableDetailedErrors.ToString();
        configDict["AgentHost:Enabled"] = "true";
        configDict["ClaudeCodeCli:Path"] = "/usr/local/bin/claude";

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    /// <summary>
    /// Creates a configuration without encryption key (should cause registration to fail)
    /// </summary>
    public static IConfiguration CreateConfigurationWithoutEncryption()
    {
        var configDict = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
            { "AgentHost:Enabled", "true" },
            { "ClaudeCodeCli:Path", "/usr/local/bin/claude" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }
}
