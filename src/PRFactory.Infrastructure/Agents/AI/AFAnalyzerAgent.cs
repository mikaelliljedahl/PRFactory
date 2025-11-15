using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.AI;
using PRFactory.Core.Application.AgentUI;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using System.Text.Json;

namespace PRFactory.Infrastructure.Agents.AI;

/// <summary>
/// AF-based analyzer agent with autonomous tool use for codebase analysis.
/// Uses Microsoft Agent Framework SDK for multi-turn reasoning and tool integration.
/// </summary>
public class AFAnalyzerAgent : BaseAgent
{
    private readonly IAIAgentService _aiAgentService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IAgentConfigurationRepository _configRepository;
    private readonly ITenantContext _tenantContext;

    public override string Name => "AFAnalyzerAgent";
    public override string Description =>
        "AI-powered analyzer agent with autonomous tool use for codebase analysis";

    public AFAnalyzerAgent(
        IAIAgentService aiAgentService,
        IToolRegistry toolRegistry,
        IAgentConfigurationRepository configRepository,
        ITenantContext tenantContext,
        ILogger<AFAnalyzerAgent> logger) : base(logger)
    {
        _aiAgentService = aiAgentService ?? throw new ArgumentNullException(nameof(aiAgentService));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Starting AFAnalyzerAgent execution for ticket {TicketId}",
            context.TicketId);

        try
        {
            // 1. Load agent configuration from database
            var config = await LoadConfigurationAsync(
                Guid.Parse(context.TenantId),
                cancellationToken);

            // 2. Get enabled tools
            var enabledToolNames = ParseEnabledTools(config.EnabledTools);
            var tools = GetEnabledTools(
                Guid.Parse(context.TenantId),
                enabledToolNames);

            // 3. Build AI agent configuration
            var aiConfig = new AIAgentConfiguration
            {
                AgentName = "AnalyzerAgent",
                Instructions = config.Instructions ?? GetDefaultInstructions(),
                EnabledTools = enabledToolNames,
                MaxTokens = config.MaxTokens,
                Temperature = config.Temperature,
                StreamingEnabled = config.StreamingEnabled
            };

            // 4. Create AI agent
            var agent = await _aiAgentService.CreateAgentAsync(
                aiConfig, tools, cancellationToken);

            // 5. Build user message from context
            var userMessage = BuildAnalysisPrompt(context);

            // 6. Execute agent with streaming (collect all chunks)
            var responseChunks = new List<AgentStreamChunk>();
            await foreach (var chunk in _aiAgentService.ExecuteAgentAsync(
                agent, userMessage, new List<AgentChatMessage>(), cancellationToken))
            {
                responseChunks.Add(chunk);

                // Log tool usage
                if (chunk.Type == ChunkType.ToolUse)
                {
                    Logger.LogInformation(
                        "Agent using tool: {ToolName}",
                        chunk.Content);
                }
            }

            // 7. Extract structured output
            var analysis = ExtractAnalysisFromChunks(responseChunks);

            // 8. Store analysis in context
            context.Analysis = ParseAnalysisToCodebaseAnalysis(analysis);
            context.State["Analysis"] = analysis;

            Logger.LogInformation(
                "AFAnalyzerAgent completed successfully for ticket {TicketId}",
                context.TicketId);

            // 9. Return result
            return new AgentResult
            {
                Status = AgentStatus.Completed,
                Output = new Dictionary<string, object>
                {
                    ["Analysis"] = analysis,
                    ["ToolsUsed"] = responseChunks
                        .Where(c => c.Type == ChunkType.ToolUse)
                        .Select(c => c.Content)
                        .ToList(),
                    ["ReasoningSteps"] = responseChunks
                        .Where(c => c.Type == ChunkType.Reasoning)
                        .Select(c => c.Content)
                        .ToList()
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "AFAnalyzerAgent failed for ticket {TicketId}",
                context.TicketId);

            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"Analysis failed: {ex.Message}",
                ErrorDetails = ex.ToString()
            };
        }
    }

    private async Task<Domain.Entities.AgentConfiguration> LoadConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetByTenantAndNameAsync(
            tenantId, "AnalyzerAgent", cancellationToken);

        if (config != null)
        {
            Logger.LogInformation(
                "Loaded agent configuration from database for tenant {TenantId}",
                tenantId);
            return config;
        }

        Logger.LogInformation(
            "No agent configuration found in database, using default configuration");
        return CreateDefaultConfiguration(tenantId);
    }

    private Domain.Entities.AgentConfiguration CreateDefaultConfiguration(Guid tenantId)
    {
        return new Domain.Entities.AgentConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = "AnalyzerAgent",
            Instructions = GetDefaultInstructions(),
            EnabledTools = JsonSerializer.Serialize(new[]
            {
                "ReadFileTool",
                "GrepTool",
                "GlobTool",
                "CodeSearchTool",
                "GetJiraTicketTool"
            }),
            MaxTokens = 8000,
            Temperature = 0.3f,
            StreamingEnabled = true,
            RequiresApproval = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private string GetDefaultInstructions()
    {
        return @"You are an expert software architect analyzing codebases for PRFactory.

Your role:
1. Analyze the codebase to understand structure, dependencies, and architecture
2. Identify files relevant to the ticket requirements
3. Assess the impact of proposed changes
4. Generate clarifying questions about requirements
5. Provide structured analysis for the planning phase

Tools available:
- ReadFileTool: Read source code files
- GrepTool: Search for patterns in code
- GlobTool: Find files by pattern
- CodeSearchTool: Semantic code search with context
- GetJiraTicketTool: Fetch ticket details

Be thorough but concise. Focus on actionable insights.

When you complete your analysis, provide a structured summary including:
- Summary of the codebase architecture
- List of affected files
- Technical considerations
- Potential risks or challenges
- 3-5 clarifying questions about the requirements";
    }

    private string[] ParseEnabledTools(string enabledToolsJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(enabledToolsJson))
            {
                return Array.Empty<string>();
            }

            var tools = JsonSerializer.Deserialize<string[]>(enabledToolsJson);
            return tools ?? Array.Empty<string>();
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(
                ex,
                "Failed to parse enabled tools JSON: {Json}",
                enabledToolsJson);
            return Array.Empty<string>();
        }
    }

    private IEnumerable<object> GetEnabledTools(Guid tenantId, string[] enabledToolNames)
    {
        if (enabledToolNames.Length == 0)
        {
            Logger.LogWarning("No tools enabled for agent, using all available tools");
            return _toolRegistry.GetAllTools().Cast<object>().ToList();
        }

        // ToolRegistry.GetTools returns ITool instances
        // Cast to object to match IAIAgentService interface
        var tools = _toolRegistry.GetTools(tenantId, enabledToolNames)
            .Cast<object>()
            .ToList();

        Logger.LogInformation(
            "Loaded {Count} tools for AFAnalyzerAgent",
            tools.Count);

        return tools;
    }

    private string BuildAnalysisPrompt(AgentContext context)
    {
        var ticketId = context.TicketId;
        var ticketDescription = context.State.GetValueOrDefault("TicketDescription", "")?.ToString()
            ?? context.Ticket?.Description
            ?? "";
        var ticketTitle = context.Ticket?.Title ?? "";
        var repositoryPath = context.State.GetValueOrDefault("RepositoryPath", "")?.ToString()
            ?? context.RepositoryPath
            ?? "";

        return $@"Analyze the following ticket and codebase:

Ticket ID: {ticketId}
Title: {ticketTitle}
Description: {ticketDescription}

Repository Path: {repositoryPath}

Tasks:
1. Read the ticket details (if available via GetJiraTicketTool)
2. Search the codebase for relevant files using GlobTool and GrepTool
3. Read key files using ReadFileTool to understand the architecture
4. Identify dependencies and architectural patterns
5. Generate 3-5 clarifying questions about the requirements
6. Provide a structured analysis summary

Please use your tools to thoroughly analyze the codebase and provide actionable insights.";
    }

    private string ExtractAnalysisFromChunks(List<AgentStreamChunk> chunks)
    {
        // Find the Response chunks and combine them
        var responseChunks = chunks
            .Where(c => c.Type == ChunkType.Response)
            .Select(c => c.Content)
            .ToList();

        if (responseChunks.Count == 0)
        {
            Logger.LogWarning("No response chunks found in agent output");
            return "No analysis generated";
        }

        return string.Join("\n", responseChunks);
    }

    private CodebaseAnalysis ParseAnalysisToCodebaseAnalysis(string analysis)
    {
        // Try to extract structured data from the analysis text
        // This is a simple implementation - could be enhanced with JSON parsing
        return new CodebaseAnalysis
        {
            Summary = analysis,
            AffectedFiles = new List<string>(),
            TechnicalConsiderations = new List<string>(),
            Architecture = analysis,
            AnalyzedAt = DateTime.UtcNow,
            Patterns = new List<string>(),
            Dependencies = new List<string>(),
            RelevantFiles = new List<string>(),
            FileContents = new Dictionary<string, string>()
        };
    }
}
