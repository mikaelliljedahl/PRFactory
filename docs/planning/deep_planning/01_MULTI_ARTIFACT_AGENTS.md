# Multi-Artifact Agents Implementation Plan

**Epic:** Deep Planning Phase (Epic 3)
**Component:** Multi-Artifact Agent Architecture
**Estimated Effort:** 1-1.5 weeks
**Dependencies:** None (foundation for all other components)

---

## Overview

This implementation plan covers the development of 5 specialized agents that work together to generate comprehensive planning artifacts simulating a full development team (PM, Architect, QA, Tech Lead).

---

## Architecture

### Agent Hierarchy

```
BaseAgent (existing)
  └─ Planning Agents (new)
      ├─ PmUserStoriesAgent
      ├─ ArchitectApiDesignAgent
      ├─ ArchitectDbSchemaAgent
      ├─ QaTestCasesAgent
      └─ TechLeadImplementationAgent
```

### Execution Flow

```
AgentContext (shared state)
  ↓
PmUserStoriesAgent
  → stores UserStories in context.State["UserStories"]
  ↓
[Parallel Execution]
  ArchitectApiDesignAgent (reads UserStories)
    → stores ApiDesign in context.State["ApiDesign"]
  ArchitectDbSchemaAgent (reads UserStories)
    → stores DatabaseSchema in context.State["DatabaseSchema"]
  ↓
QaTestCasesAgent (reads UserStories, ApiDesign, DatabaseSchema)
  → stores TestCases in context.State["TestCases"]
  ↓
TechLeadImplementationAgent (reads all previous artifacts)
  → stores ImplementationSteps in context.State["ImplementationSteps"]
```

---

## Implementation Details

### 1. PmUserStoriesAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/PmUserStoriesAgent.cs`

#### Responsibilities
- Analyze ticket description, refined requirements, and Q&A context
- Generate user stories with "As a..., I want..., So that..." format
- Define acceptance criteria for each story
- Identify edge cases and non-functional requirements

#### Key Methods

```csharp
protected override async Task<AgentResult> ExecuteAsync(
    AgentContext context,
    CancellationToken cancellationToken)
{
    // 1. Validate context (ticket, analysis required)
    ValidateContext(context);

    // 2. Build comprehensive prompt
    var prompt = BuildUserStoriesPrompt(context);

    // 3. Call LLM via ICliAgent
    var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
        prompt,
        context.RepositoryPath!,
        cancellationToken);

    // 4. Extract and validate user stories
    var userStories = ExtractUserStories(cliResponse.Content);
    ValidateUserStoriesFormat(userStories);

    // 5. Store in context
    context.State["UserStories"] = userStories;

    // 6. Return success result
    return new AgentResult
    {
        Status = AgentStatus.Completed,
        Output = new Dictionary<string, object>
        {
            ["UserStories"] = userStories,
            ["StoryCount"] = CountStories(userStories),
            ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
        }
    };
}

private string BuildUserStoriesPrompt(AgentContext context)
{
    var ticket = context.Ticket;
    var analysis = context.Analysis;

    return $@"You are a Product Manager analyzing a ticket and writing user stories.

<role>
Your role is to:
1. Understand user needs from the ticket description
2. Break down requirements into clear user stories
3. Define acceptance criteria for each story
4. Identify edge cases and non-functional requirements
</role>

<ticket>
Key: {ticket.TicketKey}
Title: {ticket.Title}
Description:
{ticket.Description}

Refined Requirements:
{ticket.RefinedDescription ?? ticket.Description}

Q&A Context:
{FormatQuestionsAndAnswers(ticket)}
</ticket>

<codebase_analysis>
Architecture: {analysis?.Architecture}
Affected Files: {string.Join(", ", analysis?.AffectedFiles ?? [])}
Technical Considerations:
{string.Join("\n", analysis?.TechnicalConsiderations ?? [])}
</codebase_analysis>

Generate user stories in the following format:

# User Stories

## Story 1: [Story Title]
**As a** [persona (user, admin, developer, API consumer, etc.)]
**I want** [feature/capability]
**So that** [benefit/value]

### Acceptance Criteria
- [ ] Criterion 1 (specific, measurable, testable)
- [ ] Criterion 2
- [ ] Criterion 3

### Edge Cases
- Edge case 1
- Edge case 2

## Story 2: [Story Title]
...

## Non-Functional Requirements
- Performance requirements (e.g., response time < 200ms)
- Security requirements (e.g., authentication, authorization, data encryption)
- Scalability considerations (e.g., handle 10,000 concurrent users)
- Accessibility requirements (e.g., WCAG 2.1 AA compliance)
- Observability requirements (e.g., logging, metrics, tracing)

Output ONLY the markdown content (no preamble or explanation).";
}

private string ExtractUserStories(string cliResponse)
{
    // Strategy 1: Look for markdown heading
    var lines = cliResponse.Split('\n');
    var inContent = false;
    var contentLines = new List<string>();

    foreach (var line in lines)
    {
        if (line.TrimStart().StartsWith("# User Stories"))
        {
            inContent = true;
        }

        if (inContent)
        {
            contentLines.Add(line);
        }
    }

    if (contentLines.Count > 0)
    {
        return string.Join("\n", contentLines).Trim();
    }

    // Strategy 2: Look for markdown code block
    var codeBlockPattern = @"```markdown\s*(.*?)\s*```";
    var match = Regex.Match(cliResponse, codeBlockPattern,
        RegexOptions.Singleline | RegexOptions.IgnoreCase);

    if (match.Success)
    {
        return match.Groups[1].Value.Trim();
    }

    // Fallback: Use full response
    return cliResponse.Trim();
}

private void ValidateUserStoriesFormat(string userStories)
{
    // Basic validation
    if (string.IsNullOrWhiteSpace(userStories))
    {
        throw new InvalidOperationException("Generated user stories are empty");
    }

    // Check for required sections
    if (!userStories.Contains("# User Stories", StringComparison.OrdinalIgnoreCase))
    {
        _logger.LogWarning("User stories missing main heading");
    }

    // Check for at least one story
    if (!userStories.Contains("**As a**", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("No user stories found in expected format");
    }
}

private int CountStories(string userStories)
{
    return Regex.Matches(userStories, @"\*\*As a\*\*",
        RegexOptions.IgnoreCase).Count;
}

private string FormatQuestionsAndAnswers(Ticket ticket)
{
    if (ticket.Questions == null || ticket.Questions.Count == 0)
        return "No Q&A available";

    var formatted = new StringBuilder();
    foreach (var question in ticket.Questions)
    {
        formatted.AppendLine($"Q: {question.QuestionText}");
        formatted.AppendLine($"A: {question.Answer ?? "Not answered"}");
        formatted.AppendLine();
    }
    return formatted.ToString();
}
```

#### Unit Tests

**File:** `/tests/PRFactory.Infrastructure.Tests/Agents/Planning/PmUserStoriesAgentTests.cs`

```csharp
public class PmUserStoriesAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<ILogger<PmUserStoriesAgent>> _mockLogger;
    private readonly PmUserStoriesAgent _agent;

    public PmUserStoriesAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockLogger = new Mock<ILogger<PmUserStoriesAgent>>();
        _agent = new PmUserStoriesAgent(_mockCliAgent.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidContext_ReturnsUserStories()
    {
        // Arrange
        var context = CreateValidContext();
        var mockResponse = CreateMockUserStoriesResponse();

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.True(context.State.ContainsKey("UserStories"));
        Assert.NotNull(context.State["UserStories"]);

        var userStories = context.State["UserStories"] as string;
        Assert.Contains("# User Stories", userStories);
        Assert.Contains("**As a**", userStories);
    }

    [Fact]
    public async Task ExecuteAsync_CliAgentFails_ReturnsFailedStatus()
    {
        // Arrange
        var context = CreateValidContext();
        var failedResponse = new CliAgentResponse
        {
            Success = false,
            ErrorMessage = "LLM API timeout"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("LLM API timeout", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidUserStoriesFormat_ThrowsException()
    {
        // Arrange
        var context = CreateValidContext();
        var invalidResponse = new CliAgentResponse
        {
            Success = true,
            Content = "Invalid content without user stories format"
        };

        _mockCliAgent
            .Setup(x => x.ExecuteWithProjectContextAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidResponse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agent.ExecuteAsync(context, CancellationToken.None));
    }

    private AgentContext CreateValidContext()
    {
        return new AgentContext
        {
            Ticket = new Ticket
            {
                TicketKey = "PROJ-123",
                Title = "Add user authentication",
                Description = "Implement OAuth 2.0 authentication",
                RefinedDescription = "Implement OAuth 2.0 with JWT tokens",
                Questions = new List<Question>
                {
                    new Question
                    {
                        QuestionText = "Which OAuth provider?",
                        Answer = "Google OAuth 2.0"
                    }
                }
            },
            Analysis = new CodebaseAnalysis
            {
                Architecture = ".NET 10 Web API with Clean Architecture",
                AffectedFiles = new List<string> { "Controllers/AuthController.cs" },
                TechnicalConsiderations = new List<string> { "Existing JWT middleware" }
            },
            RepositoryPath = "/tmp/repo",
            State = new Dictionary<string, object>()
        };
    }

    private CliAgentResponse CreateMockUserStoriesResponse()
    {
        return new CliAgentResponse
        {
            Success = true,
            Content = @"# User Stories

## Story 1: User Login with Google OAuth
**As a** user
**I want** to log in using my Google account
**So that** I don't need to create a new password

### Acceptance Criteria
- [ ] User can click 'Sign in with Google' button
- [ ] User is redirected to Google OAuth consent screen
- [ ] After successful authentication, user is redirected back with JWT token
- [ ] JWT token includes user email and profile information

### Edge Cases
- User denies OAuth consent
- OAuth token expires during flow

## Non-Functional Requirements
- Authentication must complete within 5 seconds
- JWT tokens must be securely stored (httpOnly cookies)
- Must comply with OWASP authentication best practices",
            Metadata = new Dictionary<string, object>
            {
                ["tokens_used"] = 1500
            }
        };
    }
}
```

---

### 2. ArchitectApiDesignAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/ArchitectApiDesignAgent.cs`

#### Responsibilities
- Design RESTful API endpoints based on user stories
- Generate OpenAPI 3.0 specification (YAML)
- Follow existing codebase API patterns
- Define request/response schemas with validation rules
- Include error handling and status codes

#### Key Dependencies
- **IContextBuilder** - For extracting existing API patterns from codebase

#### Implementation

```csharp
public class ArchitectApiDesignAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly IContextBuilder _contextBuilder;
    private readonly ILogger<ArchitectApiDesignAgent> _logger;

    public override string Name => "Architect API Design Agent";
    public override string Description => "Generates OpenAPI specification using Software Architect persona";

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        // 1. Get user stories from previous agent
        var userStories = GetRequiredStateValue<string>(context, "UserStories");

        // 2. Build codebase context (existing API patterns)
        var codebaseContext = await _contextBuilder.BuildApiDesignContextAsync(
            context.Repository,
            context.RepositoryPath!,
            cancellationToken);

        // 3. Build prompt
        var prompt = BuildApiDesignPrompt(context, userStories, codebaseContext);

        // 4. Call LLM
        var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        if (!cliResponse.Success)
        {
            return AgentResult.Failed($"API design generation failed: {cliResponse.ErrorMessage}");
        }

        // 5. Extract and validate YAML
        var apiDesign = ExtractYamlContent(cliResponse.Content);
        ValidateOpenApiYaml(apiDesign);

        // 6. Store in context
        context.State["ApiDesign"] = apiDesign;

        return AgentResult.Completed(new Dictionary<string, object>
        {
            ["ApiDesign"] = apiDesign,
            ["EndpointCount"] = CountEndpoints(apiDesign),
            ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
        });
    }

    private T GetRequiredStateValue<T>(AgentContext context, string key)
    {
        if (!context.State.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"{key} not found in context. Ensure previous agents executed successfully.");
        }

        if (value is not T typedValue)
        {
            throw new InvalidOperationException($"{key} is not of expected type {typeof(T).Name}");
        }

        return typedValue;
    }

    private void ValidateOpenApiYaml(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yaml);

            // Validate required OpenAPI 3.0 fields
            if (!yamlObject.ContainsKey("openapi"))
                throw new InvalidOperationException("Missing 'openapi' version field");

            if (!yamlObject.ContainsKey("info"))
                throw new InvalidOperationException("Missing 'info' field");

            if (!yamlObject.ContainsKey("paths"))
                throw new InvalidOperationException("Missing 'paths' field");

            // Validate version is 3.x
            var version = yamlObject["openapi"]?.ToString() ?? "";
            if (!version.StartsWith("3."))
                throw new InvalidOperationException($"Expected OpenAPI 3.x, got {version}");

            _logger.LogInformation("OpenAPI YAML validation passed");
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException($"Invalid YAML syntax: {ex.Message}", ex);
        }
    }

    private int CountEndpoints(string apiDesign)
    {
        // Count path definitions (lines starting with "  /api/" or similar)
        var pathPattern = @"^\s{2,4}/[a-zA-Z]";
        return Regex.Matches(apiDesign, pathPattern, RegexOptions.Multiline).Count;
    }
}
```

#### IContextBuilder Extension

**File:** `/src/PRFactory.Core/Application/Services/IContextBuilder.cs`

Add new method:

```csharp
public interface IContextBuilder
{
    // Existing methods...
    Task<string> BuildAnalysisContextAsync(Ticket ticket, string repositoryPath);

    // New method for API design context
    Task<string> BuildApiDesignContextAsync(
        Repository repository,
        string repositoryPath,
        CancellationToken cancellationToken);
}
```

**File:** `/src/PRFactory.Infrastructure/Services/ContextBuilder.cs`

```csharp
public async Task<string> BuildApiDesignContextAsync(
    Repository repository,
    string repositoryPath,
    CancellationToken cancellationToken)
{
    var sb = new StringBuilder();

    sb.AppendLine("## Existing API Patterns");
    sb.AppendLine();

    // Find existing controllers
    var controllerFiles = Directory.GetFiles(
        repositoryPath,
        "*Controller.cs",
        SearchOption.AllDirectories);

    if (controllerFiles.Length == 0)
    {
        sb.AppendLine("No existing API controllers found.");
        return sb.ToString();
    }

    sb.AppendLine($"Found {controllerFiles.Length} existing controllers:");
    sb.AppendLine();

    // Extract patterns from first 3 controllers (avoid context overload)
    foreach (var controllerFile in controllerFiles.Take(3))
    {
        var fileName = Path.GetFileName(controllerFile);
        var content = await File.ReadAllTextAsync(controllerFile, cancellationToken);

        sb.AppendLine($"### {fileName}");
        sb.AppendLine();

        // Extract route patterns
        var routePattern = @"\[Route\(""([^""]+)""\)\]";
        var routeMatches = Regex.Matches(content, routePattern);
        if (routeMatches.Count > 0)
        {
            sb.AppendLine("Routes:");
            foreach (Match match in routeMatches)
            {
                sb.AppendLine($"- {match.Groups[1].Value}");
            }
            sb.AppendLine();
        }

        // Extract HTTP method patterns
        var httpMethodPattern = @"\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\(""?([^""\]]*?)""?\)\]";
        var methodMatches = Regex.Matches(content, httpMethodPattern);
        if (methodMatches.Count > 0)
        {
            sb.AppendLine("Endpoints:");
            foreach (Match match in methodMatches)
            {
                var method = match.Groups[1].Value;
                var path = match.Groups[2].Value;
                sb.AppendLine($"- {method}: {path}");
            }
            sb.AppendLine();
        }

        // Extract DTOs used
        var dtoPattern = @"Task<ActionResult<(\w+)>>";
        var dtoMatches = Regex.Matches(content, dtoPattern);
        if (dtoMatches.Count > 0)
        {
            sb.AppendLine("Response DTOs:");
            var dtos = dtoMatches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct();
            foreach (var dto in dtos)
            {
                sb.AppendLine($"- {dto}");
            }
            sb.AppendLine();
        }
    }

    return sb.ToString();
}
```

---

### 3. ArchitectDbSchemaAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/ArchitectDbSchemaAgent.cs`

#### Responsibilities
- Generate SQL DDL statements for schema changes
- Design new tables, columns, indexes, foreign keys
- Follow existing database schema patterns
- Include migration considerations

#### Implementation Highlights

```csharp
protected override async Task<AgentResult> ExecuteAsync(
    AgentContext context,
    CancellationToken cancellationToken)
{
    var userStories = GetRequiredStateValue<string>(context, "UserStories");

    // Build existing schema context
    var existingSchema = await _contextBuilder.BuildDatabaseSchemaContextAsync(
        context.Repository,
        context.RepositoryPath!,
        cancellationToken);

    var prompt = BuildDatabaseSchemaPrompt(context, userStories, existingSchema);

    var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
        prompt,
        context.RepositoryPath!,
        cancellationToken);

    if (!cliResponse.Success)
    {
        return AgentResult.Failed($"Database schema generation failed: {cliResponse.ErrorMessage}");
    }

    var dbSchema = ExtractSqlContent(cliResponse.Content);
    ValidateSqlSyntax(dbSchema);

    context.State["DatabaseSchema"] = dbSchema;

    return AgentResult.Completed(new Dictionary<string, object>
    {
        ["DatabaseSchema"] = dbSchema,
        ["TableCount"] = CountTables(dbSchema),
        ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
    });
}

private void ValidateSqlSyntax(string sql)
{
    // Basic validation
    if (string.IsNullOrWhiteSpace(sql))
    {
        throw new InvalidOperationException("Generated SQL schema is empty");
    }

    // Check for dangerous operations
    if (sql.Contains("DROP DATABASE", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("SQL contains dangerous DROP DATABASE statement");
    }

    // Ensure it contains CREATE or ALTER statements
    var hasValidStatements =
        sql.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ||
        sql.Contains("ALTER TABLE", StringComparison.OrdinalIgnoreCase) ||
        sql.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase);

    if (!hasValidStatements)
    {
        throw new InvalidOperationException("SQL schema missing CREATE/ALTER statements");
    }
}

private int CountTables(string sql)
{
    return Regex.Matches(sql, @"CREATE TABLE", RegexOptions.IgnoreCase).Count;
}
```

---

### 4. QaTestCasesAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/QaTestCasesAgent.cs`

#### Responsibilities
- Generate comprehensive test cases
- Cover happy path, edge cases, error handling
- Reference API design and database schema
- Include integration and performance test scenarios

#### Implementation Highlights

```csharp
protected override async Task<AgentResult> ExecuteAsync(
    AgentContext context,
    CancellationToken cancellationToken)
{
    // Get all previous artifacts
    var userStories = GetRequiredStateValue<string>(context, "UserStories");
    var apiDesign = GetRequiredStateValue<string>(context, "ApiDesign");
    var dbSchema = GetRequiredStateValue<string>(context, "DatabaseSchema");

    var prompt = BuildTestCasesPrompt(context, userStories, apiDesign, dbSchema);

    var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
        prompt,
        context.RepositoryPath!,
        cancellationToken);

    if (!cliResponse.Success)
    {
        return AgentResult.Failed($"Test case generation failed: {cliResponse.ErrorMessage}");
    }

    var testCases = ExtractTestCases(cliResponse.Content);
    ValidateTestCasesFormat(testCases);

    context.State["TestCases"] = testCases;

    return AgentResult.Completed(new Dictionary<string, object>
    {
        ["TestCases"] = testCases,
        ["TestCaseCount"] = CountTestCases(testCases),
        ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
    });
}
```

---

### 5. TechLeadImplementationAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/TechLeadImplementationAgent.cs`

#### Responsibilities
- Generate detailed implementation steps
- Reference all previous artifacts (user stories, API design, DB schema, test cases)
- Provide file-level guidance (which files to create/modify)
- Include configuration changes and migration scripts

#### Implementation Highlights

```csharp
protected override async Task<AgentResult> ExecuteAsync(
    AgentContext context,
    CancellationToken cancellationToken)
{
    // Get all previous artifacts
    var userStories = GetRequiredStateValue<string>(context, "UserStories");
    var apiDesign = GetRequiredStateValue<string>(context, "ApiDesign");
    var dbSchema = GetRequiredStateValue<string>(context, "DatabaseSchema");
    var testCases = GetRequiredStateValue<string>(context, "TestCases");

    // Build comprehensive codebase context
    var codebaseContext = await _contextBuilder.BuildImplementationContextAsync(
        context.Repository,
        context.RepositoryPath!,
        cancellationToken);

    var prompt = BuildImplementationStepsPrompt(
        context, userStories, apiDesign, dbSchema, testCases, codebaseContext);

    var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
        prompt,
        context.RepositoryPath!,
        cancellationToken);

    if (!cliResponse.Success)
    {
        return AgentResult.Failed($"Implementation steps generation failed: {cliResponse.ErrorMessage}");
    }

    var implementationSteps = ExtractImplementationSteps(cliResponse.Content);
    ValidateImplementationStepsFormat(implementationSteps);

    context.State["ImplementationSteps"] = implementationSteps;

    return AgentResult.Completed(new Dictionary<string, object>
    {
        ["ImplementationSteps"] = implementationSteps,
        ["StepCount"] = CountSteps(implementationSteps),
        ["FileCount"] = CountMentionedFiles(implementationSteps),
        ["TokensUsed"] = cliResponse.Metadata.GetValueOrDefault("tokens_used", 0)
    });
}
```

---

## Dependency Injection Registration

**File:** `/src/PRFactory.Infrastructure/DependencyInjection.cs`

Update `AddInfrastructure` method:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing registrations ...

    // Register new planning agents
    services.AddTransient<Agents.Planning.PmUserStoriesAgent>();
    services.AddTransient<Agents.Planning.ArchitectApiDesignAgent>();
    services.AddTransient<Agents.Planning.ArchitectDbSchemaAgent>();
    services.AddTransient<Agents.Planning.QaTestCasesAgent>();
    services.AddTransient<Agents.Planning.TechLeadImplementationAgent>();

    return services;
}
```

---

## Testing Strategy

### Unit Tests

Each agent requires comprehensive unit tests:

1. **Happy path tests**
   - Valid context → successful execution
   - Correct artifact stored in context
   - Proper format validation

2. **Error handling tests**
   - Missing required context → exception
   - CLI agent failure → failed result
   - Invalid artifact format → exception

3. **Validation tests**
   - User stories contain required sections
   - OpenAPI YAML is valid
   - SQL doesn't contain dangerous statements
   - Test cases cover happy path and edge cases

### Integration Tests

**File:** `/tests/PRFactory.Infrastructure.Tests/Agents/Planning/PlanningAgentsIntegrationTests.cs`

```csharp
public class PlanningAgentsIntegrationTests : IClassFixture<TestFixture>
{
    [Fact]
    public async Task ExecuteAllAgents_ValidContext_GeneratesAllArtifacts()
    {
        // Arrange
        var context = CreateIntegrationTestContext();

        var pmAgent = _serviceProvider.GetRequiredService<PmUserStoriesAgent>();
        var apiAgent = _serviceProvider.GetRequiredService<ArchitectApiDesignAgent>();
        var dbAgent = _serviceProvider.GetRequiredService<ArchitectDbSchemaAgent>();
        var qaAgent = _serviceProvider.GetRequiredService<QaTestCasesAgent>();
        var techLeadAgent = _serviceProvider.GetRequiredService<TechLeadImplementationAgent>();

        // Act
        var pmResult = await pmAgent.ExecuteAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, pmResult.Status);

        var apiResult = await apiAgent.ExecuteAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, apiResult.Status);

        var dbResult = await dbAgent.ExecuteAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, dbResult.Status);

        var qaResult = await qaAgent.ExecuteAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, qaResult.Status);

        var techLeadResult = await techLeadAgent.ExecuteAsync(context, CancellationToken.None);
        Assert.Equal(AgentStatus.Completed, techLeadResult.Status);

        // Assert
        Assert.True(context.State.ContainsKey("UserStories"));
        Assert.True(context.State.ContainsKey("ApiDesign"));
        Assert.True(context.State.ContainsKey("DatabaseSchema"));
        Assert.True(context.State.ContainsKey("TestCases"));
        Assert.True(context.State.ContainsKey("ImplementationSteps"));
    }
}
```

---

## Acceptance Criteria

- [ ] All 5 agents implemented with `BaseAgent` inheritance
- [ ] All agents use `ICliAgent` for LLM calls (no direct HTTP)
- [ ] Comprehensive prompt templates for each persona
- [ ] Artifact extraction and validation for each agent
- [ ] Context state management (passing artifacts between agents)
- [ ] Unit tests for each agent (80% coverage minimum)
- [ ] Integration tests for full agent chain
- [ ] Error handling and retry logic
- [ ] Structured logging with correlation IDs
- [ ] Dependency injection registration
- [ ] `IContextBuilder` extensions for API and DB schema context

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| LLM generates invalid formats (YAML, SQL) | High | Add validation and retry logic |
| LLM context window exceeded | Medium | Optimize context building (smart file selection) |
| Agent execution timeout | Medium | Increase timeouts for planning agents |
| Prompt drift (different LLM providers) | Medium | Test with multiple providers (Claude, GPT) |
| Missing dependencies between artifacts | High | Ensure agents read previous artifacts from context |

---

## Next Steps

After completing agent implementation:
1. Implement `PlanArtifactStorageAgent` (saves artifacts to database)
2. Update `PlanningGraph` with new agent orchestration
3. Proceed to database schema changes (see `02_DATABASE_SCHEMA.md`)
