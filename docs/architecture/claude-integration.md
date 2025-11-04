# Claude AI Integration Architecture

## Overview

The Claude AI integration orchestrates AI-powered analysis, question generation, planning, and optional code implementation. It manages context, prompt engineering, conversation history, and token usage.

## Key Responsibilities

- **Codebase Analysis**: Analyze repository structure and relevant files
- **Question Generation**: Generate clarifying questions based on ticket and codebase
- **Implementation Planning**: Create detailed, actionable implementation plans
- **Code Generation**: (Optional) Generate code changes based on approved plans
- **Context Management**: Build and maintain relevant context for AI conversations
- **Token Optimization**: Minimize token usage while maximizing quality
- **Conversation History**: Track AI interactions per ticket for continuity

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Workflow Engine                            │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────────┐
│           IClaudeService (Interface)                    │
│  • AnalyzeCodebaseAsync()                               │
│  • GenerateQuestionsAsync()                             │
│  • GenerateImplementationPlanAsync()                    │
│  • ImplementCodeAsync()                                 │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
         ▼           ▼           ▼
┌──────────────┐ ┌──────────┐ ┌──────────────┐
│   Prompt     │ │ Context  │ │ Conversation │
│  Templates   │ │ Builder  │ │   History    │
└──────────────┘ └──────────┘ └──────────────┘
         │           │           │
         └───────────┼───────────┘
                     ▼
         ┌───────────────────────┐
         │  ClaudeClient         │
         │  (Anthropic SDK)      │
         └───────────┬───────────┘
                     │
                     │ HTTPS
                     ▼
         ┌───────────────────────┐
         │   Anthropic API       │
         │   (Claude Sonnet 4.5) │
         └───────────────────────┘
```

## Core Service Interface

```csharp
// PRFactory.Core/Application/Services/IClaudeService.cs
public interface IClaudeService
{
    /// <summary>
    /// Analyze codebase to understand structure and identify relevant files
    /// </summary>
    Task<CodebaseAnalysis> AnalyzeCodebaseAsync(
        Ticket ticket,
        string repositoryPath,
        CancellationToken ct = default
    );

    /// <summary>
    /// Generate clarifying questions based on ticket and analysis
    /// </summary>
    Task<List<Question>> GenerateQuestionsAsync(
        Ticket ticket,
        CodebaseAnalysis analysis,
        CancellationToken ct = default
    );

    /// <summary>
    /// Generate implementation plan based on ticket, answers, and codebase
    /// </summary>
    Task<ImplementationPlan> GenerateImplementationPlanAsync(
        Ticket ticket,
        CancellationToken ct = default
    );

    /// <summary>
    /// Implement code changes based on approved plan
    /// </summary>
    Task<CodeImplementation> ImplementCodeAsync(
        Ticket ticket,
        string repositoryPath,
        CancellationToken ct = default
    );
}

// DTOs
public record CodebaseAnalysis(
    List<string> RelevantFiles,
    Dictionary<string, string> FileContents,
    string Architecture,
    List<string> Patterns,
    List<string> Dependencies
);

public record ImplementationPlan(
    string MainPlan,
    string AffectedFiles,
    string TestStrategy,
    int EstimatedComplexity  // 1-5
);

public record CodeImplementation(
    Dictionary<string, string> ModifiedFiles,  // Path -> New content
    List<string> CreatedFiles,
    string Summary
);
```

## Claude Client Wrapper

```csharp
// PRFactory.Infrastructure/Claude/ClaudeClient.cs
public interface IClaudeClient
{
    Task<string> SendMessageAsync(
        string systemPrompt,
        List<Message> conversationHistory,
        int maxTokens = 8000,
        CancellationToken ct = default
    );

    Task<StreamingResponse> SendMessageStreamingAsync(
        string systemPrompt,
        List<Message> conversationHistory,
        int maxTokens = 8000,
        CancellationToken ct = default
    );
}

public class ClaudeClient : IClaudeClient
{
    private readonly AnthropicClient _anthropicClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeClient> _logger;
    private readonly ITokenUsageTracker _tokenTracker;

    public ClaudeClient(IConfiguration configuration, ILogger<ClaudeClient> logger, ITokenUsageTracker tokenTracker)
    {
        var apiKey = configuration["Claude:ApiKey"];
        _anthropicClient = new AnthropicClient(apiKey);
        _configuration = configuration;
        _logger = logger;
        _tokenTracker = tokenTracker;
    }

    public async Task<string> SendMessageAsync(
        string systemPrompt,
        List<Message> conversationHistory,
        int maxTokens = 8000,
        CancellationToken ct = default)
    {
        var model = _configuration["Claude:Model"] ?? "claude-sonnet-4-5-20250929";

        var request = new MessageRequest
        {
            Model = model,
            MaxTokens = maxTokens,
            System = systemPrompt,
            Messages = conversationHistory.Select(m => new Anthropic.SDK.Messaging.Message
            {
                Role = m.Role,
                Content = m.Content
            }).ToList()
        };

        _logger.LogInformation("Sending message to Claude. Model: {Model}, MaxTokens: {MaxTokens}",
            model, maxTokens);

        var response = await _anthropicClient.Messages.CreateAsync(request, ct);

        // Track token usage
        await _tokenTracker.TrackUsageAsync(new TokenUsage
        {
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens,
            Model = model,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Claude response received. Input tokens: {Input}, Output tokens: {Output}",
            response.Usage.InputTokens,
            response.Usage.OutputTokens);

        return response.Content.First().Text;
    }

    public async Task<StreamingResponse> SendMessageStreamingAsync(
        string systemPrompt,
        List<Message> conversationHistory,
        int maxTokens = 8000,
        CancellationToken ct = default)
    {
        // For long-running operations like code generation
        var model = _configuration["Claude:Model"] ?? "claude-sonnet-4-5-20250929";

        var stream = _anthropicClient.Messages.CreateStreamAsync(new MessageRequest
        {
            Model = model,
            MaxTokens = maxTokens,
            System = systemPrompt,
            Messages = conversationHistory.Select(m => new Anthropic.SDK.Messaging.Message
            {
                Role = m.Role,
                Content = m.Content
            }).ToList()
        }, ct);

        return new StreamingResponse(stream);
    }
}

public record Message(string Role, string Content);

public class StreamingResponse
{
    private readonly IAsyncEnumerable<Anthropic.SDK.Messaging.StreamingMessage> _stream;

    public StreamingResponse(IAsyncEnumerable<Anthropic.SDK.Messaging.StreamingMessage> stream)
    {
        _stream = stream;
    }

    public async IAsyncEnumerable<string> StreamTextAsync()
    {
        await foreach (var chunk in _stream)
        {
            if (chunk.Delta?.Text != null)
            {
                yield return chunk.Delta.Text;
            }
        }
    }
}
```

## Context Builder

Builds optimized context for Claude based on ticket and codebase.

```csharp
// PRFactory.Infrastructure/Claude/ContextBuilder.cs
public interface IContextBuilder
{
    Task<string> BuildAnalysisContextAsync(Ticket ticket, string repoPath);
    Task<string> BuildPlanningContextAsync(Ticket ticket, CodebaseAnalysis analysis);
    Task<string> BuildImplementationContextAsync(Ticket ticket, string repoPath);
}

public class ContextBuilder : IContextBuilder
{
    private readonly ILogger<ContextBuilder> _logger;

    public async Task<string> BuildAnalysisContextAsync(Ticket ticket, string repoPath)
    {
        var sb = new StringBuilder();

        // Repository structure
        sb.AppendLine("## Repository Structure");
        sb.AppendLine("```");
        sb.AppendLine(await GetDirectoryTreeAsync(repoPath, maxDepth: 3));
        sb.AppendLine("```");
        sb.AppendLine();

        // Key files (README, architecture docs, etc.)
        sb.AppendLine("## Key Documentation");
        var readme = await TryReadFileAsync(repoPath, "README.md");
        if (readme != null)
        {
            sb.AppendLine("### README.md");
            sb.AppendLine(readme);
            sb.AppendLine();
        }

        // Ticket information
        sb.AppendLine("## Ticket Information");
        sb.AppendLine($"**Title**: {ticket.Title}");
        sb.AppendLine($"**Description**: {ticket.Description}");
        sb.AppendLine();

        return sb.ToString();
    }

    public async Task<string> BuildPlanningContextAsync(Ticket ticket, CodebaseAnalysis analysis)
    {
        var sb = new StringBuilder();

        // Ticket & answers
        sb.AppendLine("## Ticket Information");
        sb.AppendLine($"**Title**: {ticket.Title}");
        sb.AppendLine($"**Description**: {ticket.Description}");
        sb.AppendLine();

        sb.AppendLine("## Clarifying Questions & Answers");
        foreach (var question in ticket.Questions)
        {
            var answer = ticket.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            sb.AppendLine($"**Q**: {question.Text}");
            sb.AppendLine($"**A**: {answer?.Text ?? "(No answer provided)"}");
            sb.AppendLine();
        }

        // Codebase analysis
        sb.AppendLine("## Codebase Analysis");
        sb.AppendLine($"**Architecture**: {analysis.Architecture}");
        sb.AppendLine();

        sb.AppendLine("**Patterns Identified**:");
        foreach (var pattern in analysis.Patterns)
        {
            sb.AppendLine($"- {pattern}");
        }
        sb.AppendLine();

        sb.AppendLine("## Relevant Files");
        foreach (var file in analysis.RelevantFiles.Take(10))
        {
            sb.AppendLine($"### {file}");
            if (analysis.FileContents.TryGetValue(file, out var content))
            {
                sb.AppendLine("```");
                sb.AppendLine(TruncateContent(content, maxLines: 100));
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public async Task<string> BuildImplementationContextAsync(Ticket ticket, string repoPath)
    {
        var sb = new StringBuilder();

        // Read the implementation plan from the branch
        var planPath = Path.Combine(repoPath, "IMPLEMENTATION_PLAN.md");
        if (File.Exists(planPath))
        {
            sb.AppendLine("## Approved Implementation Plan");
            sb.AppendLine(await File.ReadAllTextAsync(planPath));
            sb.AppendLine();
        }

        // Include relevant files
        sb.AppendLine("## Current Codebase");
        // ... similar to BuildPlanningContextAsync

        return sb.ToString();
    }

    private async Task<string> GetDirectoryTreeAsync(string path, int maxDepth, int currentDepth = 0)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', currentDepth * 2);

        var dirInfo = new DirectoryInfo(path);
        sb.AppendLine($"{indent}{dirInfo.Name}/");

        if (currentDepth >= maxDepth) return sb.ToString();

        try
        {
            // Directories
            foreach (var dir in dirInfo.GetDirectories().Where(d => !IsIgnoredDirectory(d.Name)))
            {
                sb.Append(await GetDirectoryTreeAsync(dir.FullName, maxDepth, currentDepth + 1));
            }

            // Files
            foreach (var file in dirInfo.GetFiles().Take(50))
            {
                sb.AppendLine($"{indent}  {file.Name}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip inaccessible directories
        }

        return sb.ToString();
    }

    private bool IsIgnoredDirectory(string name)
    {
        var ignored = new[] { ".git", "node_modules", "bin", "obj", ".vs", "packages" };
        return ignored.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    private string TruncateContent(string content, int maxLines)
    {
        var lines = content.Split('\n');
        if (lines.Length <= maxLines) return content;

        return string.Join('\n', lines.Take(maxLines)) + "\n... (truncated)";
    }

    private async Task<string?> TryReadFileAsync(string repoPath, string relativePath)
    {
        var fullPath = Path.Combine(repoPath, relativePath);
        if (!File.Exists(fullPath)) return null;

        try
        {
            return await File.ReadAllTextAsync(fullPath);
        }
        catch
        {
            return null;
        }
    }
}
```

## Prompt Templates

```csharp
// PRFactory.Infrastructure/Claude/PromptTemplates/PromptTemplates.cs
public static class PromptTemplates
{
    public const string ANALYSIS_SYSTEM_PROMPT = @"
You are an expert software architect analyzing a codebase to help refine a vague feature request.

Your task is to:
1. Understand the repository structure and architecture
2. Identify the files and components relevant to the ticket
3. Recognize existing patterns and conventions
4. Identify what information is missing to implement the ticket

Provide a structured analysis including:
- Relevant file paths
- Architecture style (e.g., MVC, Clean Architecture, Microservices)
- Coding patterns and conventions observed
- Key dependencies
";

    public const string QUESTIONS_SYSTEM_PROMPT = @"
You are an expert business analyst helping to refine requirements for a software feature.

Based on the ticket description and codebase analysis, generate 3-7 **specific, actionable** clarifying questions that:
1. Fill gaps in the requirements
2. Clarify edge cases
3. Determine technical approach preferences
4. Identify testing requirements

Categories:
- **Requirements**: What exactly should the feature do?
- **Technical**: How should it integrate with existing code?
- **Testing**: What test coverage is needed?
- **UX**: How should it behave from user perspective?

Format your response as a JSON array:
```json
[
  {
    ""category"": ""Requirements"",
    ""text"": ""Should this feature support existing users or only new registrations?""
  },
  ...
]
```
";

    public const string PLANNING_SYSTEM_PROMPT = @"
You are an expert software engineer creating a detailed implementation plan.

Based on the ticket, answers to clarifying questions, and codebase analysis, create a comprehensive implementation plan.

Your plan should include:

## 1. Implementation Approach
- High-level strategy
- Key design decisions
- Rationale for approach

## 2. Files to Modify
List each file with specific changes:
- `path/to/file.cs` - Add method X, update class Y

## 3. Files to Create
List new files needed:
- `path/to/newfile.cs` - Purpose and key components

## 4. Testing Strategy
- Unit tests to add/modify
- Integration tests needed
- Manual testing checklist

## 5. Potential Risks
- Edge cases to watch
- Dependencies that might break
- Performance considerations

## 6. Estimated Complexity
Rate 1-5 where:
- 1 = Simple (< 1 hour)
- 2 = Easy (1-2 hours)
- 3 = Medium (2-4 hours)
- 4 = Complex (4-8 hours)
- 5 = Very Complex (> 8 hours)

Format as **markdown** with clear sections.
";

    public const string IMPLEMENTATION_SYSTEM_PROMPT = @"
You are an expert software engineer implementing code changes based on an approved plan.

Rules:
1. Follow the approved implementation plan exactly
2. Match existing code style and patterns
3. Include proper error handling
4. Add XML documentation comments
5. Write unit tests for new/modified logic
6. DO NOT modify files not mentioned in the plan

For each file, provide:
```json
{
  ""action"": ""modify"" | ""create"",
  ""path"": ""relative/path/to/file"",
  ""content"": ""full file content""
}
```

Return a JSON array of file changes.
";
}
```

## Service Implementation

```csharp
// PRFactory.Infrastructure/Claude/ClaudeService.cs
public class ClaudeService : IClaudeService
{
    private readonly IClaudeClient _client;
    private readonly IContextBuilder _contextBuilder;
    private readonly IConversationHistoryRepository _historyRepo;
    private readonly ILogger<ClaudeService> _logger;

    public async Task<CodebaseAnalysis> AnalyzeCodebaseAsync(
        Ticket ticket,
        string repositoryPath,
        CancellationToken ct = default)
    {
        var context = await _contextBuilder.BuildAnalysisContextAsync(ticket, repositoryPath);

        var messages = new List<Message>
        {
            new("user", context)
        };

        var response = await _client.SendMessageAsync(
            PromptTemplates.ANALYSIS_SYSTEM_PROMPT,
            messages,
            maxTokens: 4000,
            ct
        );

        // Save conversation
        await _historyRepo.AddMessageAsync(ticket.Id, "analysis", messages[0], response);

        // Parse response into CodebaseAnalysis
        var analysis = ParseAnalysisResponse(response);

        // Read relevant file contents
        var fileContents = new Dictionary<string, string>();
        foreach (var file in analysis.RelevantFiles.Take(20))
        {
            var fullPath = Path.Combine(repositoryPath, file);
            if (File.Exists(fullPath))
            {
                fileContents[file] = await File.ReadAllTextAsync(fullPath, ct);
            }
        }

        return analysis with { FileContents = fileContents };
    }

    public async Task<List<Question>> GenerateQuestionsAsync(
        Ticket ticket,
        CodebaseAnalysis analysis,
        CancellationToken ct = default)
    {
        var context = new StringBuilder();
        context.AppendLine($"## Ticket");
        context.AppendLine($"**Title**: {ticket.Title}");
        context.AppendLine($"**Description**: {ticket.Description}");
        context.AppendLine();
        context.AppendLine($"## Analysis Summary");
        context.AppendLine($"Architecture: {analysis.Architecture}");
        context.AppendLine($"Relevant files: {string.Join(", ", analysis.RelevantFiles.Take(5))}");

        var messages = new List<Message>
        {
            new("user", context.ToString())
        };

        var response = await _client.SendMessageAsync(
            PromptTemplates.QUESTIONS_SYSTEM_PROMPT,
            messages,
            maxTokens: 2000,
            ct
        );

        await _historyRepo.AddMessageAsync(ticket.Id, "questions", messages[0], response);

        // Parse JSON response
        var questionsJson = ExtractJsonFromResponse(response);
        var questionDtos = JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson);

        return questionDtos.Select(q => new Question
        {
            Id = Guid.NewGuid().ToString(),
            Text = q.Text,
            Category = q.Category,
            CreatedAt = DateTime.UtcNow
        }).ToList();
    }

    public async Task<ImplementationPlan> GenerateImplementationPlanAsync(
        Ticket ticket,
        CancellationToken ct = default)
    {
        // Get conversation history
        var history = await _historyRepo.GetConversationAsync(ticket.Id);

        // Build planning context
        var analysisResult = ParsePreviousAnalysis(history);
        var context = await _contextBuilder.BuildPlanningContextAsync(ticket, analysisResult);

        var messages = history.ToList();
        messages.Add(new Message("user", $"Now create a detailed implementation plan.\n\n{context}"));

        var response = await _client.SendMessageAsync(
            PromptTemplates.PLANNING_SYSTEM_PROMPT,
            messages,
            maxTokens: 8000,
            ct
        );

        await _historyRepo.AddMessageAsync(ticket.Id, "planning", messages.Last(), response);

        // Split response into sections
        var sections = SplitPlanSections(response);

        return new ImplementationPlan(
            MainPlan: response,
            AffectedFiles: sections.GetValueOrDefault("Files to Modify", ""),
            TestStrategy: sections.GetValueOrDefault("Testing Strategy", ""),
            EstimatedComplexity: ExtractComplexity(sections.GetValueOrDefault("Estimated Complexity", "3"))
        );
    }

    public async Task<CodeImplementation> ImplementCodeAsync(
        Ticket ticket,
        string repositoryPath,
        CancellationToken ct = default)
    {
        var context = await _contextBuilder.BuildImplementationContextAsync(ticket, repositoryPath);
        var history = await _historyRepo.GetConversationAsync(ticket.Id);

        var messages = history.ToList();
        messages.Add(new Message("user", $"Implement the approved plan.\n\n{context}"));

        var response = await _client.SendMessageAsync(
            PromptTemplates.IMPLEMENTATION_SYSTEM_PROMPT,
            messages,
            maxTokens: 16000,
            ct
        );

        await _historyRepo.AddMessageAsync(ticket.Id, "implementation", messages.Last(), response);

        // Parse JSON response with file changes
        var fileChangesJson = ExtractJsonFromResponse(response);
        var fileChanges = JsonSerializer.Deserialize<List<FileChangeDto>>(fileChangesJson);

        var modifiedFiles = fileChanges
            .ToDictionary(fc => fc.Path, fc => fc.Content);

        return new CodeImplementation(
            ModifiedFiles: modifiedFiles,
            CreatedFiles: fileChanges.Where(fc => fc.Action == "create").Select(fc => fc.Path).ToList(),
            Summary: "Implementation completed successfully"
        );
    }

    private CodebaseAnalysis ParseAnalysisResponse(string response)
    {
        // Simple parsing - in production, use structured output or better parsing
        var relevantFiles = new List<string>();
        var patterns = new List<string>();

        // Extract file paths (look for patterns like src/file.cs)
        var fileMatches = Regex.Matches(response, @"[a-zA-Z0-9_\-/\.]+\.(cs|js|ts|py|java)");
        relevantFiles.AddRange(fileMatches.Select(m => m.Value).Distinct());

        return new CodebaseAnalysis(
            RelevantFiles: relevantFiles,
            FileContents: new Dictionary<string, string>(),
            Architecture: "Clean Architecture",  // Extract from response
            Patterns: patterns,
            Dependencies: new List<string>()
        );
    }

    private string ExtractJsonFromResponse(string response)
    {
        var match = Regex.Match(response, @"```json\s*(\[.*?\])\s*```", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : response;
    }

    private Dictionary<string, string> SplitPlanSections(string plan)
    {
        var sections = new Dictionary<string, string>();
        var matches = Regex.Matches(plan, @"##\s*(\d+\.\s*)?(.+?)\s*\n(.*?)(?=##|\z)", RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var title = match.Groups[2].Value.Trim();
            var content = match.Groups[3].Value.Trim();
            sections[title] = content;
        }

        return sections;
    }

    private int ExtractComplexity(string complexitySection)
    {
        var match = Regex.Match(complexitySection, @"(\d)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 3;
    }
}

// DTOs for JSON parsing
record QuestionDto(string Category, string Text);
record FileChangeDto(string Action, string Path, string Content);
```

## Token Usage Tracking

```csharp
// PRFactory.Infrastructure/Claude/TokenUsageTracker.cs
public interface ITokenUsageTracker
{
    Task TrackUsageAsync(TokenUsage usage);
    Task<TokenStats> GetStatsAsync(Guid tenantId, DateTime from, DateTime to);
}

public class TokenUsageTracker : ITokenUsageTracker
{
    private readonly ApplicationDbContext _dbContext;

    public async Task TrackUsageAsync(TokenUsage usage)
    {
        _dbContext.TokenUsages.Add(usage);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<TokenStats> GetStatsAsync(Guid tenantId, DateTime from, DateTime to)
    {
        var usages = await _dbContext.TokenUsages
            .Where(u => u.TenantId == tenantId && u.Timestamp >= from && u.Timestamp <= to)
            .ToListAsync();

        return new TokenStats
        {
            TotalInputTokens = usages.Sum(u => u.InputTokens),
            TotalOutputTokens = usages.Sum(u => u.OutputTokens),
            EstimatedCost = CalculateCost(usages)
        };
    }

    private decimal CalculateCost(List<TokenUsage> usages)
    {
        // Claude Sonnet 4.5 pricing (as of 2025)
        const decimal INPUT_COST_PER_MTK = 3.00m;   // $3 per million tokens
        const decimal OUTPUT_COST_PER_MTK = 15.00m;  // $15 per million tokens

        var inputCost = usages.Sum(u => u.InputTokens) / 1_000_000m * INPUT_COST_PER_MTK;
        var outputCost = usages.Sum(u => u.OutputTokens) / 1_000_000m * OUTPUT_COST_PER_MTK;

        return inputCost + outputCost;
    }
}

public class TokenUsage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? TicketId { get; set; }
    public string Model { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public DateTime Timestamp { get; set; }
}

public record TokenStats(int TotalInputTokens, int TotalOutputTokens, decimal EstimatedCost);
```

## Configuration

```csharp
// appsettings.json
{
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 8000,
    "Temperature": 0.7
  }
}

// Program.cs
services.AddSingleton<IClaudeClient, ClaudeClient>();
services.AddScoped<IClaudeService, ClaudeService>();
services.AddScoped<IContextBuilder, ContextBuilder>();
services.AddScoped<ITokenUsageTracker, TokenUsageTracker>();
```

## Error Handling

```csharp
// Handle API errors with retry
var retryPolicy = Policy
    .Handle<AnthropicException>()
    .Or<HttpRequestException>()
    .WaitAndRetryAsync(
        3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning("Claude API call failed. Retry {RetryCount} after {Delay}s",
                retryCount, timeSpan.TotalSeconds);
        }
    );

await retryPolicy.ExecuteAsync(() => _client.SendMessageAsync(...));
```

## Testing

```csharp
[Fact]
public async Task GenerateQuestions_ShouldReturn3To7Questions()
{
    // Arrange
    var mockClient = new Mock<IClaudeClient>();
    mockClient.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<List<Message>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(GetMockQuestionResponse());

    var service = new ClaudeService(mockClient.Object, ...);

    // Act
    var questions = await service.GenerateQuestionsAsync(ticket, analysis);

    // Assert
    Assert.InRange(questions.Count, 3, 7);
    Assert.All(questions, q => Assert.NotEmpty(q.Text));
}
```

## Security Considerations

- API keys stored in Azure Key Vault
- Per-tenant API keys for multi-tenancy
- Rate limiting to prevent abuse
- Content filtering for sensitive data
- Never include secrets in prompts

## Performance Optimizations

1. **Context Truncation**: Limit file content to essential parts
2. **Caching**: Cache analysis results for similar tickets
3. **Parallel Requests**: Generate questions in parallel when possible
4. **Streaming**: Use streaming for long responses
5. **Token Budgets**: Set max tokens per phase

## Monitoring

### Key Metrics
- Tokens used per ticket
- Cost per ticket
- Response time per phase
- Success/failure rate
- Quality scores (human feedback)

### Alerts
- Token usage exceeds budget
- API errors > 5% rate
- Response time > 30s

## Summary

The Claude integration provides intelligent analysis and generation capabilities while maintaining:
- **Control**: Humans approve plans before implementation
- **Transparency**: All AI interactions logged and visible
- **Cost Management**: Token tracking and budgets
- **Quality**: Structured prompts and validation
- **Extensibility**: Easy to add new AI capabilities

---

This completes the architecture documentation. Review all documents:
- [Overview](./overview.md)
- [Core Engine](./core-engine.md)
- [Jira Integration](./jira-integration.md)
- [Git Integration](./git-integration.md)
- **Claude AI Integration** (this document)
