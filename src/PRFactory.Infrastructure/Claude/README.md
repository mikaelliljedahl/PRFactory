# Claude AI Integration

This directory contains the Claude AI integration for PRFactory, providing intelligent codebase analysis, question generation, implementation planning, and optional code generation capabilities.

## Architecture Overview

The Claude integration follows a modular design with clear separation of concerns:

```
Claude/
├── Models/                          # Data Transfer Objects
│   ├── CodebaseAnalysis.cs         # Analysis results
│   ├── ImplementationPlan.cs       # Plan structure
│   ├── CodeImplementation.cs       # Implementation results
│   ├── Message.cs                  # Conversation messages
│   ├── TokenUsage.cs              # Token tracking
│   └── StreamingResponse.cs        # Streaming API wrapper
├── ClaudeClient.cs                 # Anthropic SDK wrapper
├── ClaudeService.cs                # Main orchestration service
├── ContextBuilder.cs               # Context optimization
├── PromptTemplates.cs              # System prompts
├── TokenUsageTracker.cs            # Token usage tracking
├── IConversationHistoryRepository.cs # Conversation storage interface
└── ConversationHistoryRepository.cs  # In-memory implementation

Related:
../../../Core/Application/Services/IClaudeService.cs  # Service interface
```

## Components

### 1. ClaudeClient.cs
Wrapper around the Anthropic SDK providing:
- `SendMessageAsync()` - Standard message sending
- `SendMessageStreamingAsync()` - Streaming responses for long operations
- Automatic token usage tracking
- Error handling and retry logic

**Note**: Currently uses placeholder implementation. Integrate the actual Anthropic SDK for production use.

### 2. ClaudeService.cs
Main orchestration service implementing `IClaudeService`:
- `AnalyzeCodebaseAsync()` - Analyzes repository structure and identifies relevant files
- `GenerateQuestionsAsync()` - Generates 3-7 clarifying questions
- `GenerateImplementationPlanAsync()` - Creates detailed implementation plans
- `ImplementCodeAsync()` - Generates code based on approved plans

### 3. ContextBuilder.cs
Builds optimized context for Claude API calls:
- `BuildAnalysisContextAsync()` - Repository structure + README + ticket
- `BuildPlanningContextAsync()` - Ticket + answers + codebase analysis
- `BuildImplementationContextAsync()` - Approved plan + relevant files

Features:
- Directory tree generation with configurable depth
- Smart file truncation to stay within token limits
- Ignores common build/dependency directories (.git, node_modules, bin, obj)

### 4. PromptTemplates.cs
Static class containing carefully crafted system prompts:
- `ANALYSIS_SYSTEM_PROMPT` - For codebase analysis
- `QUESTIONS_SYSTEM_PROMPT` - For question generation (returns JSON)
- `PLANNING_SYSTEM_PROMPT` - For implementation planning (returns markdown)
- `IMPLEMENTATION_SYSTEM_PROMPT` - For code generation (returns JSON)
- `CODE_REVIEW_SYSTEM_PROMPT` - For code review

### 5. TokenUsageTracker.cs
Tracks and reports Claude API token usage:
- Records input/output tokens per request
- Calculates estimated costs based on Claude Sonnet 4.5 pricing
- Provides statistics by tenant and time period
- Supports per-ticket usage tracking

**Note**: Currently uses in-memory storage. Implement database persistence for production.

### 6. ConversationHistoryRepository.cs
Manages conversation history per ticket:
- Stores user messages and assistant responses
- Organizes by phase (analysis, questions, planning, implementation)
- Enables context continuity across workflow steps
- Supports retrieval by ticket ID and phase

**Note**: Currently uses in-memory storage. Implement database persistence for production.

## Usage Example

```csharp
// Inject the service
public class WorkflowEngine
{
    private readonly IClaudeService _claudeService;

    public async Task ProcessTicket(Ticket ticket, string repoPath)
    {
        // Step 1: Analyze codebase
        var analysis = await _claudeService.AnalyzeCodebaseAsync(ticket, repoPath);

        // Step 2: Generate clarifying questions
        var questions = await _claudeService.GenerateQuestionsAsync(ticket, analysis);

        // Step 3: After user answers questions, generate plan
        var plan = await _claudeService.GenerateImplementationPlanAsync(ticket);

        // Step 4: (Optional) After plan approval, implement code
        var implementation = await _claudeService.ImplementCodeAsync(ticket, repoPath);
    }
}
```

## Configuration

Add to `appsettings.json`:

```json
{
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 8000
  }
}
```

Register services in `Program.cs`:

```csharp
// Register Claude services
services.AddHttpClient<IClaudeClient, ClaudeClient>();
services.AddScoped<IClaudeService, ClaudeService>();
services.AddScoped<IContextBuilder, ContextBuilder>();
services.AddScoped<ITokenUsageTracker, TokenUsageTracker>();
services.AddScoped<IConversationHistoryRepository, ConversationHistoryRepository>();
```

## Token Optimization

The integration implements several strategies to minimize token usage:

1. **Context Truncation**: Files limited to 100 lines, directories to depth 3
2. **Smart File Selection**: Only includes relevant files based on analysis
3. **Structured Prompts**: Clear, concise system prompts
4. **Conversation History**: Maintains context without redundant information
5. **Token Tracking**: Monitors usage to identify optimization opportunities

## Response Parsing

The service includes robust JSON parsing helpers:

- `ExtractJsonFromResponse()` - Handles markdown code blocks and raw JSON
- `ParseAnalysisResponse()` - Extracts structured data from analysis
- `SplitPlanSections()` - Parses markdown plans into sections
- `ExtractComplexity()` - Parses complexity ratings

## Error Handling

All methods include:
- Try-catch blocks for file operations
- Logging of errors and warnings
- Graceful degradation when files can't be read
- Cancellation token support

## Testing

Example test structure:

```csharp
[Fact]
public async Task AnalyzeCodebaseAsync_ShouldIdentifyRelevantFiles()
{
    // Arrange
    var mockClient = new Mock<IClaudeClient>();
    mockClient.Setup(x => x.SendMessageAsync(
        It.IsAny<string>(),
        It.IsAny<List<Message>>(),
        It.IsAny<int>(),
        It.IsAny<CancellationToken>()))
        .ReturnsAsync("Mock analysis response with src/File.cs mentioned");

    var service = new ClaudeService(mockClient.Object, ...);

    // Act
    var analysis = await service.AnalyzeCodebaseAsync(ticket, repoPath);

    // Assert
    Assert.NotEmpty(analysis.RelevantFiles);
}
```

## Production Readiness Checklist

Before deploying to production:

- [ ] Integrate actual Anthropic SDK in `ClaudeClient.cs`
- [ ] Replace in-memory storage with database persistence
- [ ] Implement retry logic with exponential backoff
- [ ] Add rate limiting to prevent API quota exhaustion
- [ ] Store API keys in Azure Key Vault or similar
- [ ] Add comprehensive error handling and logging
- [ ] Implement token budget limits per tenant
- [ ] Add monitoring and alerting for API failures
- [ ] Create integration tests with real API calls
- [ ] Implement caching for repeated queries
- [ ] Add content filtering for sensitive data
- [ ] Configure appropriate timeout values

## Cost Estimation

Based on Claude Sonnet 4.5 pricing (as of 2025):
- Input: $3 per million tokens
- Output: $15 per million tokens

Typical ticket workflow:
- Analysis: ~2,000 input + 1,000 output tokens
- Questions: ~1,000 input + 500 output tokens
- Planning: ~3,000 input + 2,000 output tokens
- Implementation: ~5,000 input + 4,000 output tokens

**Total per ticket**: ~11,000 input + 7,500 output = **~$0.15**

## Security Considerations

- API keys stored securely (not in code)
- Per-tenant API keys for multi-tenancy
- Never include secrets/credentials in prompts
- Content filtering before sending to API
- Rate limiting to prevent abuse
- Audit logging of all API calls

## Monitoring

Key metrics to track:
- Tokens used per ticket
- Cost per ticket
- Response time per phase
- Success/failure rate
- Quality scores (human feedback)

## Future Enhancements

Potential improvements:
- Prompt caching for repeated context
- Fine-tuned models for specific patterns
- Multi-model support (Claude Opus for complex tasks)
- Parallel question generation
- Interactive refinement loops
- Code review integration
- Automated testing of generated code

## Support

For issues or questions:
1. Check the architecture documentation: `/docs/architecture/claude-integration.md`
2. Review logs for detailed error messages
3. Monitor token usage in the tracking dashboard
4. Contact the development team

## License

Copyright (c) 2025 PRFactory. All rights reserved.
