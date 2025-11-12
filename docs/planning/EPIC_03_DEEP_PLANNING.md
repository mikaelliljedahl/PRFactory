# Epic 3: Deep Planning Phase (MetaGPT-Inspired Multi-Agent Architecture)

**Status:** 🔴 Not Started
**Priority:** P2 (Important)
**Effort:** 3-4 weeks
**Dependencies:** Epic 1 (Team Review), Epic 2 (Multi-LLM)

---

## Strategic Goal

Make Phase 2 (Planning) the core value proposition of PRFactory by generating comprehensive, multi-artifact plans that simulate a full development team (PM, Architect, QA, Tech Lead).

**Current State:** `PlanningAgent` generates a single `IMPLEMENTATION_PLAN.md` file.

**Target State:** Enhanced `PlanningGraph` with multiple specialized agents generates a directory of artifacts (user stories, API design, database schema, test cases, implementation steps).

**Inspiration:** MetaGPT's multi-agent approach where different LLM personas simulate different team roles.

---

## Architecture Alignment

### Agent-Based Implementation (NOT CLI Commands)

This epic enhances the **PlanningGraph** with multiple specialized agents that execute sequentially, each calling LLMs through the `ICliAgent` interface.

**Key Architecture Points:**
- **Agents** are C# classes inheriting from `BaseAgent` (e.g., `PmUserStoriesAgent`, `ArchitectApiDesignAgent`)
- **Agents call LLMs** via `ICliAgent.ExecuteWithProjectContextAsync()` (which internally wraps Claude Code CLI)
- **Orchestration** happens within `PlanningGraph.ExecuteCoreAsync()` using the agent executor
- **Web UI** triggers the workflow and displays results (no CLI commands needed)
- **Revision** is handled through `PlanningGraph.ResumeAsync()` with rejection feedback

**NOT** CLI commands like "cli plan" or "cli revise" - those don't exist in the codebase.

---

## Success Criteria

✅ **Must Have:**
- Enhanced `PlanningGraph` generates 5 artifacts: user stories, API design, database schema, test cases, implementation steps
- Multi-step orchestration with 5 specialized agents (PM → Architect → QA → Tech Lead personas)
- Revision workflow via graph resumption with targeted artifact regeneration
- Web UI displays all plan artifacts in tabbed interface (not just single file)
- Database schema supports multiple artifact storage

✅ **Nice to Have:**
- Plan versioning (track revisions via `PlanVersion` entity)
- Plan diff viewer (show changes between versions)
- Additional artifact types (sequence diagrams, deployment guide, monitoring plan)
- Parallel agent execution where possible (e.g., API design + DB schema in parallel)

---

## Implementation Plan

### 1. Multi-Artifact Agent Architecture

#### New Agents to Implement

All agents inherit from `BaseAgent` and use `ICliAgent` for LLM calls:

1. **PmUserStoriesAgent** - Product Manager persona
   - Generates user stories with acceptance criteria
   - File: `/src/PRFactory.Infrastructure/Agents/Planning/PmUserStoriesAgent.cs`

2. **ArchitectApiDesignAgent** - Software Architect persona (API focus)
   - Generates OpenAPI specification (YAML)
   - File: `/src/PRFactory.Infrastructure/Agents/Planning/ArchitectApiDesignAgent.cs`

3. **ArchitectDbSchemaAgent** - Database Architect persona
   - Generates SQL DDL statements for schema changes
   - File: `/src/PRFactory.Infrastructure/Agents/Planning/ArchitectDbSchemaAgent.cs`

4. **QaTestCasesAgent** - QA Engineer persona
   - Generates comprehensive test cases
   - File: `/src/PRFactory.Infrastructure/Agents/Planning/QaTestCasesAgent.cs`

5. **TechLeadImplementationAgent** - Tech Lead persona
   - Generates detailed implementation steps
   - File: `/src/PRFactory.Infrastructure/Agents/Planning/TechLeadImplementationAgent.cs`

#### Agent Execution Flow

**Enhanced PlanningGraph:**

```
Start
  ↓
PmUserStoriesAgent → AgentContext.UserStories
  ↓
[Parallel Execution]
  ├─ ArchitectApiDesignAgent → AgentContext.ApiDesign
  └─ ArchitectDbSchemaAgent → AgentContext.DatabaseSchema
  ↓
QaTestCasesAgent → AgentContext.TestCases
  ↓
TechLeadImplementationAgent → AgentContext.ImplementationSteps
  ↓
PlanArtifactStorageAgent (stores all artifacts in DB)
  ↓
GitPlanAgent (commits all artifacts to feature branch)
  ↓
JiraPostAgent (posts summary to Jira)
  ↓
HumanWaitAgent [SUSPEND for review/approval]
  ↓
[If approved] → Complete
[If rejected with feedback] → RevisionAgent (regenerates specific artifacts)
```

#### Output Directory Structure

Artifacts committed to Git by `GitPlanAgent`:

```
feature/{ticket-key}-implementation-plan/
├── plan/
│   ├── 01-user_stories.md
│   ├── 02-api_design.yml
│   ├── 03-database_schema.sql
│   ├── 04-test_cases.md
│   └── 05-implementation_steps.md
└── IMPLEMENTATION_PLAN.md (summary pointing to artifacts)
```

---

### 2. Agent Implementation Details

#### Example: PmUserStoriesAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/PmUserStoriesAgent.cs`

```csharp
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Product Manager persona agent that generates user stories with acceptance criteria.
/// </summary>
public class PmUserStoriesAgent : BaseAgent
{
    private readonly ICliAgent _cliAgent;
    private readonly ILogger<PmUserStoriesAgent> _logger;

    public override string Name => "PM User Stories Agent";
    public override string Description => "Generates user stories with acceptance criteria using Product Manager persona";

    public PmUserStoriesAgent(
        ICliAgent cliAgent,
        ILogger<PmUserStoriesAgent> logger)
    {
        _cliAgent = cliAgent;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating user stories for ticket {TicketKey}",
            context.Ticket.TicketKey);

        // Build comprehensive prompt for PM persona
        var prompt = BuildUserStoriesPrompt(context);

        // Call LLM via ICliAgent (wraps Claude Code CLI)
        var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        if (!cliResponse.Success)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"CLI agent execution failed: {cliResponse.ErrorMessage}"
            };
        }

        // Extract user stories from response
        var userStories = ExtractUserStories(cliResponse.Content);

        // Store in context for next agents
        context.State["UserStories"] = userStories;

        return new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["UserStories"] = userStories,
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

Refined Requirements (from analysis):
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
**As a** [persona]
**I want** [feature/capability]
**So that** [benefit/value]

### Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

### Edge Cases
- Edge case 1
- Edge case 2

## Story 2: [Story Title]
...

## Non-Functional Requirements
- Performance requirements
- Security requirements
- Scalability considerations
- Accessibility requirements

Output ONLY the markdown content (no preamble or explanation).";
    }

    private string ExtractUserStories(string cliResponse)
    {
        // Extract markdown content from CLI response
        // Handle cases where LLM adds explanatory text before/after
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

        return contentLines.Count > 0
            ? string.Join("\n", contentLines)
            : cliResponse; // Fallback to full response
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
}
```

#### Example: ArchitectApiDesignAgent

**File:** `/src/PRFactory.Infrastructure/Agents/Planning/ArchitectApiDesignAgent.cs`

```csharp
/// <summary>
/// Software Architect persona agent that generates OpenAPI specification for required endpoints.
/// </summary>
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
        _logger.LogInformation(
            "Generating API design for ticket {TicketKey}",
            context.Ticket.TicketKey);

        // Get user stories from previous agent
        var userStories = context.State["UserStories"] as string
            ?? throw new InvalidOperationException("UserStories not found in context");

        // Build codebase context (existing API patterns)
        var codebaseContext = await _contextBuilder.BuildApiDesignContextAsync(
            context.Repository,
            context.RepositoryPath!,
            cancellationToken);

        var prompt = BuildApiDesignPrompt(context, userStories, codebaseContext);

        var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
            prompt,
            context.RepositoryPath!,
            cancellationToken);

        if (!cliResponse.Success)
        {
            return new AgentResult
            {
                Status = AgentStatus.Failed,
                Error = $"API design generation failed: {cliResponse.ErrorMessage}"
            };
        }

        var apiDesign = ExtractYamlContent(cliResponse.Content);

        // Validate OpenAPI YAML (basic validation)
        ValidateOpenApiYaml(apiDesign);

        context.State["ApiDesign"] = apiDesign;

        return new AgentResult
        {
            Status = AgentStatus.Completed,
            Output = new Dictionary<string, object>
            {
                ["ApiDesign"] = apiDesign,
                ["EndpointCount"] = CountEndpoints(apiDesign)
            }
        };
    }

    private string BuildApiDesignPrompt(
        AgentContext context,
        string userStories,
        string codebaseContext)
    {
        return $@"You are a Software Architect designing API endpoints.

<role>
Your role is to:
1. Design RESTful API endpoints based on user stories
2. Follow existing codebase patterns and conventions
3. Define request/response schemas with proper validation
4. Include error handling and status codes
5. Design for scalability and maintainability
</role>

<user_stories>
{userStories}
</user_stories>

<existing_api_patterns>
{codebaseContext}
</existing_api_patterns>

<ticket_context>
Key: {context.Ticket.TicketKey}
Title: {context.Ticket.Title}
</ticket_context>

Generate an OpenAPI 3.0 specification (YAML) for the required endpoints.

Include:
- Paths and HTTP methods (GET, POST, PUT, DELETE, PATCH)
- Request body schemas (with validation rules)
- Response schemas (success and error cases)
- HTTP status codes (200, 201, 400, 401, 404, 500, etc.)
- Authentication/authorization requirements
- Query parameters and path parameters
- Error response format (consistent with existing APIs)

Follow the existing API conventions found in the codebase.

Output ONLY valid OpenAPI 3.0 YAML (no preamble or explanation).

Example structure:
```yaml
openapi: 3.0.0
info:
  title: {context.Ticket.TicketKey} API
  version: 1.0.0
paths:
  /api/resource:
    get:
      summary: Get resources
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Resource'
components:
  schemas:
    Resource:
      type: object
      properties:
        id:
          type: string
          format: uuid
```

Now generate the OpenAPI specification:";
    }

    private string ExtractYamlContent(string cliResponse)
    {
        // Extract YAML from markdown code blocks if present
        var yamlPattern = @"```ya?ml\s*(.*?)\s*```";
        var match = Regex.Match(cliResponse, yamlPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : cliResponse.Trim();
    }

    private void ValidateOpenApiYaml(string yaml)
    {
        // Basic validation - check if it's valid YAML with required OpenAPI fields
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yaml);

            if (!yamlObject.ContainsKey("openapi"))
                throw new InvalidOperationException("Missing 'openapi' version field");

            if (!yamlObject.ContainsKey("paths"))
                throw new InvalidOperationException("Missing 'paths' field");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid OpenAPI YAML: {ex.Message}", ex);
        }
    }

    private int CountEndpoints(string apiDesign)
    {
        // Simple endpoint counter (counts path definitions)
        return Regex.Matches(apiDesign, @"^\s{2}/api/", RegexOptions.Multiline).Count;
    }
}
```

---

### 3. Enhanced PlanningGraph

**File:** `/src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs`

Update the `ExecuteCoreAsync` method to orchestrate the new agents:

```csharp
protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
    IAgentMessage inputMessage,
    GraphContext context,
    CancellationToken cancellationToken)
{
    var ticketId = inputMessage.TicketId;

    try
    {
        _logger.LogInformation(
            "Starting enhanced planning workflow for ticket {TicketId}",
            ticketId);

        // Step 1: PM User Stories (sequential - foundation for all other artifacts)
        var userStoriesMessage = await _executor.ExecuteAsync<PmUserStoriesAgent>(
            inputMessage, context, cancellationToken);

        // Step 2: Architect API Design + DB Schema (parallel - independent artifacts)
        var apiDesignTask = _executor.ExecuteAsync<ArchitectApiDesignAgent>(
            userStoriesMessage, context, cancellationToken);
        var dbSchemaTask = _executor.ExecuteAsync<ArchitectDbSchemaAgent>(
            userStoriesMessage, context, cancellationToken);

        await Task.WhenAll(apiDesignTask, dbSchemaTask);

        var apiDesignMessage = await apiDesignTask;
        var dbSchemaMessage = await dbSchemaTask;

        // Step 3: QA Test Cases (sequential - depends on API + DB design)
        var testCasesMessage = await _executor.ExecuteAsync<QaTestCasesAgent>(
            apiDesignMessage, context, cancellationToken);

        // Step 4: Tech Lead Implementation Steps (sequential - depends on all previous artifacts)
        var implementationMessage = await _executor.ExecuteAsync<TechLeadImplementationAgent>(
            testCasesMessage, context, cancellationToken);

        // Step 5: Store all artifacts in database
        var storageMessage = await _executor.ExecuteAsync<PlanArtifactStorageAgent>(
            implementationMessage, context, cancellationToken);

        // Step 6: Commit artifacts to Git (parallel with Jira post)
        var gitTask = _executor.ExecuteAsync<GitPlanAgent>(
            storageMessage, context, cancellationToken);
        var jiraTask = _executor.ExecuteAsync<JiraPostAgent>(
            storageMessage, context, cancellationToken);

        await Task.WhenAll(gitTask, jiraTask);

        // Step 7: Suspend for human review
        await _executor.ExecuteAsync<HumanWaitAgent>(
            storageMessage, context, cancellationToken);

        _logger.LogInformation(
            "Enhanced planning workflow completed for ticket {TicketId}. Awaiting human review.",
            ticketId);

        return new GraphExecutionResult
        {
            Status = GraphExecutionStatus.Suspended,
            SuspensionReason = "Awaiting plan approval or revision feedback",
            SuspendedAt = DateTime.UtcNow,
            NextExpectedMessage = typeof(PlanApprovedMessage).Name // or PlanRejectedMessage
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Enhanced planning workflow failed for ticket {TicketId}",
            ticketId);

        return new GraphExecutionResult
        {
            Status = GraphExecutionStatus.Failed,
            Error = ex.Message
        };
    }
}
```

#### Revision Workflow via Graph Resumption

**File:** `/src/PRFactory.Infrastructure/Agents/Graphs/PlanningGraph.cs`

```csharp
protected override async Task<GraphExecutionResult> ResumeCoreAsync(
    IAgentMessage resumeMessage,
    GraphContext context,
    CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "Resuming planning workflow for ticket {TicketId} with message {MessageType}",
        resumeMessage.TicketId,
        resumeMessage.GetType().Name);

    switch (resumeMessage)
    {
        case PlanApprovedMessage approved:
            // Transition to implementation phase or complete
            await UpdateTicketStateAsync(approved.TicketId, WorkflowState.PlanApproved);

            // Emit event for WorkflowOrchestrator to start ImplementationGraph
            return new GraphExecutionResult
            {
                Status = GraphExecutionStatus.Completed,
                EmittedEvent = new PlanApprovedEvent(approved.TicketId, DateTime.UtcNow)
            };

        case PlanRejectedMessage rejected:
            // Determine which artifacts need regeneration based on feedback
            var artifactsToRegenerate = DetermineArtifactsToRegenerate(rejected.Feedback);

            _logger.LogInformation(
                "Regenerating artifacts for ticket {TicketId}: {Artifacts}",
                rejected.TicketId,
                string.Join(", ", artifactsToRegenerate));

            // Execute RevisionAgent to regenerate specific artifacts
            var revisionResult = await _executor.ExecuteAsync<PlanRevisionAgent>(
                rejected,
                context,
                cancellationToken);

            // Re-run storage, Git commit, and Jira post
            var storageMessage = new PlanArtifactsGeneratedMessage(rejected.TicketId, DateTime.UtcNow);

            await _executor.ExecuteAsync<PlanArtifactStorageAgent>(
                storageMessage, context, cancellationToken);

            var gitTask = _executor.ExecuteAsync<GitPlanAgent>(
                storageMessage, context, cancellationToken);
            var jiraTask = _executor.ExecuteAsync<JiraPostAgent>(
                storageMessage, context, cancellationToken);

            await Task.WhenAll(gitTask, jiraTask);

            // Suspend again for re-review
            await _executor.ExecuteAsync<HumanWaitAgent>(
                storageMessage, context, cancellationToken);

            return new GraphExecutionResult
            {
                Status = GraphExecutionStatus.Suspended,
                SuspensionReason = "Awaiting plan re-approval after revision",
                SuspendedAt = DateTime.UtcNow
            };

        default:
            throw new InvalidOperationException(
                $"Unexpected resume message type: {resumeMessage.GetType().Name}");
    }
}

private List<string> DetermineArtifactsToRegenerate(string feedback)
{
    // Simple keyword-based detection (can be enhanced with LLM analysis)
    var artifacts = new List<string>();

    if (feedback.Contains("user stor", StringComparison.OrdinalIgnoreCase))
        artifacts.Add("UserStories");

    if (feedback.Contains("api", StringComparison.OrdinalIgnoreCase) ||
        feedback.Contains("endpoint", StringComparison.OrdinalIgnoreCase))
        artifacts.Add("ApiDesign");

    if (feedback.Contains("database", StringComparison.OrdinalIgnoreCase) ||
        feedback.Contains("schema", StringComparison.OrdinalIgnoreCase) ||
        feedback.Contains("table", StringComparison.OrdinalIgnoreCase))
        artifacts.Add("DatabaseSchema");

    if (feedback.Contains("test", StringComparison.OrdinalIgnoreCase) ||
        feedback.Contains("qa", StringComparison.OrdinalIgnoreCase))
        artifacts.Add("TestCases");

    if (feedback.Contains("implementation", StringComparison.OrdinalIgnoreCase) ||
        feedback.Contains("steps", StringComparison.OrdinalIgnoreCase))
        artifacts.Add("ImplementationSteps");

    // If no specific artifacts identified, regenerate all
    return artifacts.Count > 0
        ? artifacts
        : new List<string> { "UserStories", "ApiDesign", "DatabaseSchema", "TestCases", "ImplementationSteps" };
}
```

---

### 4. Database Schema Updates

**New Migration:** `AddPlanArtifactsColumns`

**File:** `/src/PRFactory.Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_AddPlanArtifactsColumns.cs`

```csharp
public partial class AddPlanArtifactsColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add artifact columns to Plans table
        migrationBuilder.AddColumn<string>(
            name: "UserStories",
            table: "Plans",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ApiDesign",
            table: "Plans",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DatabaseSchema",
            table: "Plans",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TestCases",
            table: "Plans",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ImplementationSteps",
            table: "Plans",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Version",
            table: "Plans",
            type: "int",
            nullable: false,
            defaultValue: 1);

        // Optional: Plan versioning table
        migrationBuilder.CreateTable(
            name: "PlanVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                UserStories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ApiDesign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DatabaseSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                TestCases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ImplementationSteps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", nullable: true),
                RevisionReason = table.Column<string>(type: "nvarchar(1000)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlanVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlanVersions_Plans_PlanId",
                    column: x => x.PlanId,
                    principalTable: "Plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PlanVersions_PlanId",
            table: "PlanVersions",
            column: "PlanId");

        migrationBuilder.CreateIndex(
            name: "IX_PlanVersions_PlanId_Version",
            table: "PlanVersions",
            columns: new[] { "PlanId", "Version" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PlanVersions");

        migrationBuilder.DropColumn(name: "UserStories", table: "Plans");
        migrationBuilder.DropColumn(name: "ApiDesign", table: "Plans");
        migrationBuilder.DropColumn(name: "DatabaseSchema", table: "Plans");
        migrationBuilder.DropColumn(name: "TestCases", table: "Plans");
        migrationBuilder.DropColumn(name: "ImplementationSteps", table: "Plans");
        migrationBuilder.DropColumn(name: "Version", table: "Plans");
    }
}
```

**Update Plan Entity:**

**File:** `/src/PRFactory.Domain/Entities/Plan.cs`

```csharp
public class Plan : BaseEntity
{
    public Guid TicketId { get; set; }
    public virtual Ticket Ticket { get; set; } = null!;

    // Original single-file plan (kept for backward compatibility)
    public string? Content { get; set; }

    // Multi-artifact fields
    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<PlanVersion> Versions { get; set; } = new List<PlanVersion>();
}

public class PlanVersion : BaseEntity
{
    public Guid PlanId { get; set; }
    public virtual Plan Plan { get; set; } = null!;

    public int Version { get; set; }

    public string? UserStories { get; set; }
    public string? ApiDesign { get; set; }
    public string? DatabaseSchema { get; set; }
    public string? TestCases { get; set; }
    public string? ImplementationSteps { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? RevisionReason { get; set; }
}
```

---

### 5. Web UI Multi-Artifact Viewer

**Update:** `/src/PRFactory.Web/Pages/Tickets/Detail.razor` and `Detail.razor.cs`

#### Plan Artifacts Section (Detail.razor)

```razor
@* Plan Artifacts Section *@
@if (ticket?.Plan != null && ticket.Plan.HasMultipleArtifacts)
{
    <Card Title="Implementation Plan Artifacts" Icon="folder" Class="mb-4">
        <RadzenTabs>
            <Tabs>
                <RadzenTabsItem Text="User Stories">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(ticket.Plan.UserStories))
                        {
                            <MarkdownViewer Content="@ticket.Plan.UserStories" />
                        }
                        else
                        {
                            <EmptyState Message="User stories not yet generated" Icon="file-earmark-text" />
                        }
                    </div>
                </RadzenTabsItem>

                <RadzenTabsItem Text="API Design">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(ticket.Plan.ApiDesign))
                        {
                            <CodeEditor Content="@ticket.Plan.ApiDesign"
                                        Language="yaml"
                                        ReadOnly="true"
                                        Height="600px" />
                        }
                        else
                        {
                            <EmptyState Message="API design not yet generated" Icon="code-slash" />
                        }
                    </div>
                </RadzenTabsItem>

                <RadzenTabsItem Text="Database Schema">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(ticket.Plan.DatabaseSchema))
                        {
                            <CodeEditor Content="@ticket.Plan.DatabaseSchema"
                                        Language="sql"
                                        ReadOnly="true"
                                        Height="600px" />
                        }
                        else
                        {
                            <EmptyState Message="Database schema not yet generated" Icon="database" />
                        }
                    </div>
                </RadzenTabsItem>

                <RadzenTabsItem Text="Test Cases">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(ticket.Plan.TestCases))
                        {
                            <MarkdownViewer Content="@ticket.Plan.TestCases" />
                        }
                        else
                        {
                            <EmptyState Message="Test cases not yet generated" Icon="check2-square" />
                        }
                    </div>
                </RadzenTabsItem>

                <RadzenTabsItem Text="Implementation Steps">
                    <div class="p-3">
                        @if (!string.IsNullOrEmpty(ticket.Plan.ImplementationSteps))
                        {
                            <MarkdownViewer Content="@ticket.Plan.ImplementationSteps" />
                        }
                        else
                        {
                            <EmptyState Message="Implementation steps not yet generated" Icon="list-ol" />
                        }
                    </div>
                </RadzenTabsItem>

                @* Optional: Version History Tab *@
                @if (ticket.Plan.Versions?.Any() == true)
                {
                    <RadzenTabsItem Text="Version History">
                        <div class="p-3">
                            <PlanVersionHistory Versions="@ticket.Plan.Versions"
                                                OnVersionSelected="HandleVersionSelected" />
                        </div>
                    </RadzenTabsItem>
                }
            </Tabs>
        </RadzenTabs>
    </Card>

    @* Plan Revision Section *@
    @if (ticket.State == WorkflowState.PlanGenerated ||
         ticket.State == WorkflowState.PlanRejected)
    {
        <Card Title="Revise Plan" Icon="pencil" Class="mb-4">
            <FormField Label="Revision Instructions">
                <InputTextArea @bind-Value="revisionFeedback"
                               rows="4"
                               class="form-control"
                               placeholder="e.g., Add rate limiting to the API endpoints, include caching strategy in implementation steps" />
            </FormField>

            <div class="d-flex gap-2 mt-3">
                <LoadingButton OnClick="HandleRevise"
                               IsLoading="@isRevising"
                               Icon="arrow-repeat"
                               Class="btn-warning">
                    Revise Plan
                </LoadingButton>

                <LoadingButton OnClick="HandleApprove"
                               IsLoading="@isApproving"
                               Icon="check-circle"
                               Class="btn-success">
                    Approve Plan
                </LoadingButton>
            </div>
        </Card>
    }
}
else if (ticket?.Plan != null)
{
    @* Fallback for old single-file plans *@
    <Card Title="Implementation Plan" Icon="file-earmark-text" Class="mb-4">
        <MarkdownViewer Content="@ticket.Plan.Content" />
    </Card>
}
```

#### Code-Behind (Detail.razor.cs)

```csharp
namespace PRFactory.Web.Pages.Tickets;

public partial class Detail
{
    [Parameter]
    public Guid TicketId { get; set; }

    [Inject]
    private ITicketService TicketService { get; set; } = null!;

    [Inject]
    private IWorkflowOrchestrator WorkflowOrchestrator { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private TicketDto? ticket;
    private string? revisionFeedback;
    private bool isRevising;
    private bool isApproving;

    protected override async Task OnInitializedAsync()
    {
        ticket = await TicketService.GetByIdAsync(TicketId);
    }

    private async Task HandleRevise()
    {
        if (string.IsNullOrWhiteSpace(revisionFeedback))
        {
            // Show validation error
            return;
        }

        isRevising = true;
        try
        {
            // Resume PlanningGraph with rejection message
            await WorkflowOrchestrator.ResumeAsync(
                TicketId,
                new PlanRejectedMessage(
                    TicketId,
                    revisionFeedback,
                    Regenerate: true,
                    Timestamp: DateTime.UtcNow));

            // Refresh ticket to show new state
            ticket = await TicketService.GetByIdAsync(TicketId);
            revisionFeedback = string.Empty;

            // Show success notification
        }
        finally
        {
            isRevising = false;
        }
    }

    private async Task HandleApprove()
    {
        isApproving = true;
        try
        {
            // Resume PlanningGraph with approval message
            await WorkflowOrchestrator.ResumeAsync(
                TicketId,
                new PlanApprovedMessage(
                    TicketId,
                    ApprovedBy: "current-user",  // Get from auth context
                    Timestamp: DateTime.UtcNow));

            // Navigate to next phase or success page
            Navigation.NavigateTo($"/tickets/{TicketId}");
        }
        finally
        {
            isApproving = false;
        }
    }

    private async Task HandleVersionSelected(PlanVersion version)
    {
        // Load selected version into view (optional feature)
        // Could show diff between current and selected version
    }
}
```

---

### 6. Prompt Templates

All prompts are embedded in agent code (see examples above) rather than external files for simplicity. This follows the existing pattern in `AnalysisAgent`, `QuestionGenerationAgent`, etc.

**Alternative:** Store in database for multi-tenant customization (future enhancement).

---

## Acceptance Criteria

### Agents & Graph Orchestration
- [ ] 5 new agents implemented: `PmUserStoriesAgent`, `ArchitectApiDesignAgent`, `ArchitectDbSchemaAgent`, `QaTestCasesAgent`, `TechLeadImplementationAgent`
- [ ] `PlanArtifactStorageAgent` stores all artifacts in database
- [ ] `PlanRevisionAgent` regenerates specific artifacts based on feedback
- [ ] `PlanningGraph` orchestrates sequential + parallel execution
- [ ] All agents use `ICliAgent` for LLM calls (no direct HTTP)
- [ ] Comprehensive logging with structured telemetry

### Prompts & Personas
- [ ] PM persona prompt generates user stories with acceptance criteria
- [ ] Architect persona prompts generate valid OpenAPI YAML and SQL DDL
- [ ] QA persona prompt generates comprehensive test cases
- [ ] Tech Lead persona prompt generates detailed implementation steps
- [ ] Prompts work with multiple LLM providers (via `ICliAgent` abstraction)

### Web UI
- [ ] Ticket detail page displays all 5 artifacts in tabbed interface
- [ ] Syntax highlighting for YAML (API design) and SQL (schema)
- [ ] Markdown rendering for user stories, test cases, implementation steps
- [ ] "Revise Plan" UI accepts natural language feedback
- [ ] "Approve Plan" button triggers workflow transition
- [ ] Empty state handling when artifacts not yet generated
- [ ] Version history viewer (optional)

### Database
- [ ] `Plans` table updated with 5 artifact columns
- [ ] `PlanVersions` table created (optional for versioning)
- [ ] EF Core migrations generated and applied
- [ ] Plan versioning tracks revision history

### Git Integration
- [ ] `GitPlanAgent` commits all 5 artifacts to feature branch
- [ ] Directory structure: `plan/01-user_stories.md`, `plan/02-api_design.yml`, etc.
- [ ] Summary file `IMPLEMENTATION_PLAN.md` links to all artifacts

### Revision Workflow
- [ ] `PlanRejectedMessage` triggers graph resumption
- [ ] Feedback analysis determines which artifacts to regenerate
- [ ] Only affected artifacts regenerated (not full re-plan)
- [ ] New version created in `PlanVersions` table
- [ ] Updated artifacts committed to Git
- [ ] Jira comment posted with revision summary

---

## Migration Path

### Week 1: Agent Implementation (Foundation)
**Days 1-2: Core Agents**
- Implement `PmUserStoriesAgent` with comprehensive prompt
- Implement `ArchitectApiDesignAgent` with OpenAPI generation
- Unit tests for both agents

**Days 3-4: Specialized Agents**
- Implement `ArchitectDbSchemaAgent` with SQL DDL generation
- Implement `QaTestCasesAgent` with test scenario generation
- Unit tests for both agents

**Day 5: Integration Agent**
- Implement `TechLeadImplementationAgent` with multi-artifact context
- Implement `PlanArtifactStorageAgent` for database persistence
- Integration tests for agent chain

### Week 2: Graph Orchestration & Storage
**Days 1-2: Enhanced PlanningGraph**
- Update `PlanningGraph.ExecuteCoreAsync()` with new agent workflow
- Implement parallel execution for API + DB agents
- Implement sequential orchestration with dependency handling

**Days 3-4: Database Schema**
- Create `AddPlanArtifactsColumns` migration
- Update `Plan` entity with artifact properties
- Create `PlanVersion` entity and table
- Update repositories and DbContext

**Day 5: Revision Workflow**
- Implement `PlanRevisionAgent` for targeted regeneration
- Update `PlanningGraph.ResumeCoreAsync()` with revision logic
- Add artifact change detection logic

### Week 3: Web UI & Testing
**Days 1-2: Artifact Viewer**
- Build multi-artifact tabbed viewer component
- Add syntax highlighting (CodeMirror/Monaco for YAML/SQL)
- Add markdown renderer for user stories/test cases
- Empty state components

**Days 3-4: Revision UI**
- Build "Revise Plan" card with feedback input
- Implement approve/reject workflow in UI
- Add version history viewer (optional)
- Wire up to `IWorkflowOrchestrator` for resumption

**Day 5: End-to-End Testing**
- Test full workflow: trigger → analyze → plan (5 artifacts) → review → revise → approve
- Test parallel agent execution
- Test revision with targeted artifact regeneration
- Performance testing (LLM call latency, database writes)

### Week 4: Polish & Documentation
**Days 1-2: Error Handling & Resilience**
- Add retry logic for LLM failures
- Add validation for generated artifacts (OpenAPI, SQL)
- Improve error messages and user feedback

**Days 3-4: Logging & Observability**
- Add structured logging for all agents
- Add telemetry for artifact generation times
- Add metrics for revision frequency

**Day 5: Documentation & Handoff**
- Update ARCHITECTURE.md with new agents
- Update WORKFLOW.md with multi-artifact flow
- Create developer guide for adding new artifact types
- Demo to stakeholders

---

## Related Epics

- **Epic 1 (Team Review):** Team comments can reference specific artifacts (e.g., "See API design tab")
- **Epic 2 (Multi-LLM):** All agents use `ICliAgent` abstraction, works with any LLM provider
- **Epic 4 (Diff Viewer):** Code validation compares implementation against all plan artifacts
- **Future: Epic 5 (Sequence Diagrams):** Add `ArchitectSequenceDiagramAgent` for visual design artifacts

---

## Technical Debt & Future Enhancements

### Technical Debt to Address
1. **Prompt versioning:** Currently embedded in code; consider database storage for multi-tenant customization
2. **Artifact validation:** Basic YAML/SQL validation; consider OpenAPI schema validator, SQL parser
3. **Context building:** `IContextBuilder` needs optimization for large codebases (smart file selection)

### Future Enhancements (Post-Epic)
1. **Additional Artifacts:**
   - Sequence diagrams (PlantUML/Mermaid)
   - Deployment guide (Docker, K8s manifests)
   - Monitoring plan (metrics, alerts, dashboards)
   - Security checklist (OWASP compliance)

2. **AI-Powered Revision:**
   - Use LLM to analyze feedback and determine affected artifacts (replace keyword matching)
   - Suggest revision strategy before regeneration

3. **Plan Diff Viewer:**
   - Side-by-side comparison of artifact versions
   - Highlight changes between revisions
   - Accept/reject individual changes

4. **Export Capabilities:**
   - Export all artifacts as ZIP
   - Generate PDF documentation bundle
   - Export to Confluence/Notion

5. **Artifact Dependencies:**
   - Track dependencies between artifacts (e.g., API design impacts test cases)
   - Smart regeneration cascades (if API changes, auto-suggest test case update)

---

## Success Metrics

**Quantitative:**
- 5 artifacts generated per planning phase (100% of tickets)
- Average planning time < 10 minutes (from trigger to artifacts ready)
- Revision rate < 30% (most plans approved on first generation)
- Average revisions per plan < 1.5 (plans usually approved within 2 attempts)

**Qualitative:**
- Development teams report higher confidence in implementation (survey)
- Fewer implementation questions/blockers (reduced Slack/email volume)
- Faster code review (reviewers have comprehensive plan context)
- Improved test coverage (QA has test cases upfront)

---

**Next Steps:**
1. Review epic with architecture team (validate agent design)
2. Create tickets for Week 1 agents (PM, Architect API, Architect DB, QA, Tech Lead)
3. Set up development environment (ensure `ICliAgent` works with Claude CLI)
4. Begin Week 1: Core agent implementation
