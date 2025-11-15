using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Agents;

/// <summary>
/// Stub implementation of ICliAgent for Epic 05 development.
/// Returns placeholder responses until full CLI agent integration is complete.
/// </summary>
public class CliAgentStub : ICliAgent
{
    private readonly ILogger<CliAgentStub> _logger;

    public CliAgentStub(ILogger<CliAgentStub> logger)
    {
        _logger = logger;
    }

    public string AgentName => "CLI Agent Stub";

    public bool SupportsStreaming => false;

    public CliAgentCapabilities GetCapabilities()
    {
        _logger.LogWarning("CliAgentStub.GetCapabilities called - returning stub capabilities");

        return new CliAgentCapabilities
        {
            SupportsCodeGeneration = false,
            SupportsFileOperations = false,
            SupportsProjectContext = false,
            SupportsStreaming = false,
            MaxTokens = 0,
            SupportedFormats = new List<string>(),
            ModelName = "stub"
        };
    }

    public Task<CliAgentResponse> ExecutePromptAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("CliAgentStub.ExecutePromptAsync called - returning placeholder response. Prompt length: {PromptLength}", prompt?.Length ?? 0);

        var response = new CliAgentResponse
        {
            Success = true,
            Content = "This is a stub response. CLI agent integration is not yet implemented.",
            ErrorMessage = null,
            Metadata = new Dictionary<string, object>
            {
                { "stub", true },
                { "timestamp", DateTime.UtcNow }
            },
            FileOperations = new List<FileOperation>(),
            ExitCode = 0
        };

        return Task.FromResult(response);
    }

    public Task<CliAgentResponse> ExecuteWithProjectContextAsync(
        string prompt,
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("CliAgentStub.ExecuteWithProjectContextAsync called - returning placeholder response. Prompt length: {PromptLength}, Project path: {ProjectPath}",
            prompt?.Length ?? 0,
            projectPath);

        var response = new CliAgentResponse
        {
            Success = true,
            Content = "This is a stub response with project context. CLI agent integration is not yet implemented.",
            ErrorMessage = null,
            Metadata = new Dictionary<string, object>
            {
                { "stub", true },
                { "timestamp", DateTime.UtcNow },
                { "project_path", projectPath }
            },
            FileOperations = new List<FileOperation>(),
            ExitCode = 0
        };

        return Task.FromResult(response);
    }

    public Task<CliAgentResponse> ExecuteStreamingAsync(
        string prompt,
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("CliAgentStub.ExecuteStreamingAsync called - returning placeholder response. Streaming is not supported by stub. Prompt length: {PromptLength}",
            prompt?.Length ?? 0);

        // Invoke callback once with stub message
        onOutputReceived?.Invoke("Stub streaming output. CLI agent integration is not yet implemented.");

        var response = new CliAgentResponse
        {
            Success = true,
            Content = "Stub streaming completed. CLI agent integration is not yet implemented.",
            ErrorMessage = null,
            Metadata = new Dictionary<string, object>
            {
                { "stub", true },
                { "timestamp", DateTime.UtcNow },
                { "streaming", true }
            },
            FileOperations = new List<FileOperation>(),
            ExitCode = 0
        };

        return Task.FromResult(response);
    }
}
