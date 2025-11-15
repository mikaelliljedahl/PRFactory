# Part 3: Storage & Context Building - Implementation Plan

**Epic**: Deep Planning (Epic 03)
**Component**: PlanArtifactStorageAgent + Epic 07 Refactoring
**Estimated Effort**: 1-2 days
**Dependencies**: Part 1 (Database Foundation - ✅ Complete), Epic 07 (ArchitectureContextService - ✅ Available)
**Status**: 🚧 Not Implemented

---

## Overview

This part implements two critical components:
1. **PlanArtifactStorageAgent** - Saves the 5 planning artifacts to the database with version tracking
2. **Epic 07 Integration Refactoring** - Replaces custom context building with Epic 07's `ArchitectureContextService`

---

## Task 1: Implement PlanArtifactStorageAgent

### Purpose

Saves all 5 plan artifacts to the database, creating version snapshots when updating existing plans.

### Files to Create/Modify

**Create:**
- `src/PRFactory.Infrastructure/Agents/Planning/PlanArtifactStorageAgent.cs`
- `tests/PRFactory.Tests/Agents/Planning/PlanArtifactStorageAgentTests.cs`

### PlanArtifactStorageAgent Implementation

**File**: `src/PRFactory.Infrastructure/Agents/Planning/PlanArtifactStorageAgent.cs`

```csharp
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Base;
using Microsoft.Extensions.Logging;

namespace PRFactory.Infrastructure.Agents.Planning;

/// <summary>
/// Stores plan artifacts in the database with versioning support.
/// Creates new Plan on first generation, updates with version snapshots on revision.
/// </summary>
public class PlanArtifactStorageAgent : BaseAgent
{
    private readonly IPlanRepository _planRepository;
    private readonly ILogger<PlanArtifactStorageAgent> _logger;

    public override string Name => "Plan Artifact Storage Agent";
    public override string Description => "Stores plan artifacts in database with versioning";

    public PlanArtifactStorageAgent(
        IPlanRepository planRepository,
        ILogger<PlanArtifactStorageAgent> logger)
    {
        ArgumentNullException.ThrowIfNull(planRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _planRepository = planRepository;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var ticketId = context.Ticket.Id;

        // Extract artifacts from context
        var userStories = context.State.GetValueOrDefault("UserStories") as string;
        var apiDesign = context.State.GetValueOrDefault("ApiDesign") as string;
        var dbSchema = context.State.GetValueOrDefault("DatabaseSchema") as string;
        var testCases = context.State.GetValueOrDefault("TestCases") as string;
        var implementationSteps = context.State.GetValueOrDefault("ImplementationSteps") as string;

        // Validate at least one artifact exists
        if (string.IsNullOrEmpty(userStories) &&
            string.IsNullOrEmpty(apiDesign) &&
            string.IsNullOrEmpty(dbSchema) &&
            string.IsNullOrEmpty(testCases) &&
            string.IsNullOrEmpty(implementationSteps))
        {
            return AgentResult.Failed("No artifacts found in context to store");
        }

        // Check if plan already exists for this ticket
        var existingPlan = await _planRepository.GetByTicketIdAsync(ticketId, cancellationToken);

        if (existingPlan != null)
        {
            // Update existing plan with new artifacts
            var revisionReason = context.State.GetValueOrDefault("RevisionFeedback") as string;
            var createdBy = context.State.GetValueOrDefault("ApprovedBy") as string ?? "system";

            _logger.LogInformation(
                "Updating existing plan for ticket {TicketKey} (current version: {Version})",
                context.Ticket.TicketKey,
                existingPlan.Version);

            existingPlan.UpdateArtifacts(
                userStories: userStories,
                apiDesign: apiDesign,
                databaseSchema: dbSchema,
                testCases: testCases,
                implementationSteps: implementationSteps,
                createdBy: createdBy,
                revisionReason: revisionReason);

            await _planRepository.UpdateAsync(existingPlan, cancellationToken);

            _logger.LogInformation(
                "Updated plan for ticket {TicketKey} to version {Version}",
                context.Ticket.TicketKey,
                existingPlan.Version);

            return AgentResult.Completed(new Dictionary<string, object>
            {
                ["PlanId"] = existingPlan.Id,
                ["Version"] = existingPlan.Version,
                ["IsUpdate"] = true
            });
        }
        else
        {
            // Create new plan
            var plan = Plan.Create(
                ticketId: ticketId,
                userStories: userStories,
                apiDesign: apiDesign,
                databaseSchema: dbSchema,
                testCases: testCases,
                implementationSteps: implementationSteps);

            await _planRepository.AddAsync(plan, cancellationToken);

            _logger.LogInformation(
                "Created new plan for ticket {TicketKey} (version 1)",
                context.Ticket.TicketKey);

            return AgentResult.Completed(new Dictionary<string, object>
            {
                ["PlanId"] = plan.Id,
                ["Version"] = plan.Version,
                ["IsUpdate"] = false
            });
        }
    }
}
```

### Unit Tests

**File**: `tests/PRFactory.Tests/Agents/Planning/PlanArtifactStorageAgentTests.cs`

```csharp
using Xunit;
using Moq;
using PRFactory.Infrastructure.Agents.Planning;
using PRFactory.Domain.Interfaces;
using PRFactory.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace PRFactory.Tests.Agents.Planning;

public class PlanArtifactStorageAgentTests
{
    private readonly Mock<IPlanRepository> _mockPlanRepo;
    private readonly PlanArtifactStorageAgent _agent;

    public PlanArtifactStorageAgentTests()
    {
        _mockPlanRepo = new Mock<IPlanRepository>();
        _agent = new PlanArtifactStorageAgent(
            _mockPlanRepo.Object,
            Mock.Of<ILogger<PlanArtifactStorageAgent>>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingPlan_CreatesNewPlan()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        _mockPlanRepo.Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        _mockPlanRepo.Verify(x => x.AddAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False((bool)result.Data!["IsUpdate"]);
        Assert.Equal(1, result.Data["Version"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingPlan_UpdatesAndCreatesVersion()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        var existingPlan = Plan.Create(ticketId, "Old stories", null, null, null, null);
        _mockPlanRepo.Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        _mockPlanRepo.Verify(x => x.UpdateAsync(existingPlan, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True((bool)result.Data!["IsUpdate"]);
        Assert.Equal(2, result.Data["Version"]); // Version incremented
        Assert.Single(existingPlan.Versions); // Version snapshot created
    }

    [Fact]
    public async Task ExecuteAsync_WithNoArtifacts_ReturnsFailure()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = new AgentContext
        {
            Ticket = CreateTicket(ticketId),
            State = new Dictionary<string, object>() // No artifacts
        };

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Failed, result.Status);
        Assert.Contains("No artifacts", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithRevisionFeedback_StoresInVersionHistory()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);
        context.State["RevisionFeedback"] = "Add rate limiting to API";

        var existingPlan = Plan.Create(ticketId, "Old stories", null, null, null, null);
        _mockPlanRepo.Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Single(existingPlan.Versions);
        Assert.Equal("Add rate limiting to API", existingPlan.Versions[0].RevisionReason);
    }

    [Fact]
    public async Task ExecuteAsync_WithCreatedBy_StoresInVersionHistory()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);
        context.State["ApprovedBy"] = "john.doe@example.com";

        var existingPlan = Plan.Create(ticketId, "Old stories", null, null, null, null);
        _mockPlanRepo.Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlan);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        Assert.Single(existingPlan.Versions);
        Assert.Equal("john.doe@example.com", existingPlan.Versions[0].CreatedBy);
    }

    [Fact]
    public async Task ExecuteAsync_StoresAllFiveArtifacts()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var context = CreateContext(ticketId);

        _mockPlanRepo.Setup(x => x.GetByTicketIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        Plan? capturedPlan = null;
        _mockPlanRepo.Setup(x => x.AddAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()))
            .Callback<Plan, CancellationToken>((p, ct) => capturedPlan = p);

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPlan);
        Assert.Equal("# User Stories...", capturedPlan.UserStories);
        Assert.Equal("openapi: 3.0.0...", capturedPlan.ApiDesign);
        Assert.Equal("CREATE TABLE...", capturedPlan.DatabaseSchema);
        Assert.Equal("# Test Cases...", capturedPlan.TestCases);
        Assert.Equal("# Implementation...", capturedPlan.ImplementationSteps);
    }

    private AgentContext CreateContext(Guid ticketId)
    {
        return new AgentContext
        {
            Ticket = CreateTicket(ticketId),
            State = new Dictionary<string, object>
            {
                ["UserStories"] = "# User Stories...",
                ["ApiDesign"] = "openapi: 3.0.0...",
                ["DatabaseSchema"] = "CREATE TABLE...",
                ["TestCases"] = "# Test Cases...",
                ["ImplementationSteps"] = "# Implementation..."
            }
        };
    }

    private Ticket CreateTicket(Guid id)
    {
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create("Test Tenant", "test-slug", "azure", "ext-123");
        var repoId = Guid.NewGuid();
        var repository = Repository.Create(tenantId, "Test Repo", GitPlatform.GitHub, "http://github.com/test/repo");

        return Ticket.Create(
            tenantId: tenantId,
            repositoryId: repoId,
            ticketKey: "TEST-123",
            title: "Test ticket",
            description: "Test description",
            externalUrl: "http://example.com");
    }
}
```

**Test Coverage**: 7 tests covering all scenarios (create, update, no artifacts, revision, created by, all artifacts stored)

---

## Task 2: Refactor Part 2 Agents to Use Epic 07

### Purpose

Remove duplicate code and leverage Epic 07's proven `ArchitectureContextService` for codebase context.

### Files to Modify

**Modify:**
- `src/PRFactory.Infrastructure/Claude/ContextBuilder.cs` - Remove custom methods
- `src/PRFactory.Infrastructure/Agents/Planning/PmUserStoriesAgent.cs`
- `src/PRFactory.Infrastructure/Agents/Planning/ArchitectApiDesignAgent.cs`
- `src/PRFactory.Infrastructure/Agents/Planning/ArchitectDbSchemaAgent.cs`
- `src/PRFactory.Infrastructure/Agents/Planning/QaTestCasesAgent.cs`
- `src/PRFactory.Infrastructure/Agents/Planning/TechLeadImplementationAgent.cs`
- All 5 agent test files (update mocks)

### Step 1: Remove Custom IContextBuilder Methods

**File**: `src/PRFactory.Infrastructure/Claude/ContextBuilder.cs`

Remove these methods:
```csharp
// DELETE THESE:
Task<string> BuildApiDesignContextAsync(Repository repository, string repositoryPath, CancellationToken cancellationToken);
Task<string> BuildDatabaseSchemaContextAsync(Repository repository, string repositoryPath, CancellationToken cancellationToken);
Task<string> BuildImplementationContextAsync(Repository repository, string repositoryPath, CancellationToken cancellationToken);
```

### Step 2: Update Agent Constructor Injection

**Example**: `ArchitectApiDesignAgent.cs`

```csharp
// BEFORE:
public ArchitectApiDesignAgent(
    ICliAgent cliAgent,
    IContextBuilder contextBuilder,  // OLD
    ILogger<ArchitectApiDesignAgent> logger)

// AFTER:
public ArchitectApiDesignAgent(
    ICliAgent cliAgent,
    IArchitectureContextService architectureContextService,  // NEW
    ILogger<ArchitectApiDesignAgent> logger)
```

### Step 3: Update ExecuteAsync to Use Epic 07 Service

**Example**: `ArchitectApiDesignAgent.cs`

```csharp
protected override async Task<AgentResult> ExecuteAsync(
    AgentContext context,
    CancellationToken cancellationToken)
{
    var userStories = GetRequiredStateValue<string>(context, "UserStories");

    // Get repository path
    var repositoryPath = GetRepositoryPath(context);

    // BEFORE (custom context building):
    // var apiContext = await _contextBuilder.BuildApiDesignContextAsync(
    //     context.Repository, repositoryPath, cancellationToken);

    // AFTER (Epic 07 service):
    var architecturePatterns = await _architectureContextService.GetArchitecturePatternsAsync(
        repositoryPath, cancellationToken);
    var techStack = _architectureContextService.GetTechnologyStack();
    var codeStyle = _architectureContextService.GetCodeStyleGuidelines();
    var codeSnippets = await _architectureContextService.GetRelevantCodeSnippetsAsync(
        repositoryPath,
        context.Ticket.Description,
        maxSnippets: 3,
        cancellationToken);

    var apiContext = BuildApiDesignContext(
        architecturePatterns, techStack, codeStyle, codeSnippets);

    // Build prompt with context
    var prompt = BuildApiDesignPrompt(userStories, apiContext);

    // Execute LLM call
    var cliResponse = await _cliAgent.ExecutePromptAsync(prompt, cancellationToken);

    if (!cliResponse.Success)
    {
        return AgentResult.Failed($"API design generation failed: {cliResponse.ErrorMessage}");
    }

    // Extract and validate YAML
    var apiDesign = ExtractYamlFromResponse(cliResponse.Content);
    ValidateOpenApiYaml(apiDesign);

    // Store in context
    context.State["ApiDesign"] = apiDesign;

    return AgentResult.Completed(new Dictionary<string, object>
    {
        ["ArtifactType"] = "ApiDesign",
        ["ContentLength"] = apiDesign.Length
    });
}

private string BuildApiDesignContext(
    string architecturePatterns,
    string techStack,
    string codeStyle,
    List<CodeSnippet> codeSnippets)
{
    var snippetText = string.Join("\n\n", codeSnippets.Select(s =>
        $"File: {s.FilePath}\n```{s.Language}\n{s.Code}\n```\n{s.Description}"));

    return $@"
<architecture_patterns>
{architecturePatterns}
</architecture_patterns>

<technology_stack>
{techStack}
</technology_stack>

<code_style_guidelines>
{codeStyle}
</code_style_guidelines>

<existing_api_examples>
{snippetText}
</existing_api_examples>
";
}
```

### Step 4: Update Agent Unit Tests

**Example**: `ArchitectApiDesignAgentTests.cs`

```csharp
public class ArchitectApiDesignAgentTests
{
    private readonly Mock<ICliAgent> _mockCliAgent;
    private readonly Mock<IArchitectureContextService> _mockArchContextService;  // NEW
    private readonly ArchitectApiDesignAgent _agent;

    public ArchitectApiDesignAgentTests()
    {
        _mockCliAgent = new Mock<ICliAgent>();
        _mockArchContextService = new Mock<IArchitectureContextService>();

        // Setup Epic 07 service mocks
        _mockArchContextService.Setup(x => x.GetArchitecturePatternsAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Clean Architecture patterns...");

        _mockArchContextService.Setup(x => x.GetTechnologyStack())
            .Returns(".NET 10, Blazor Server...");

        _mockArchContextService.Setup(x => x.GetCodeStyleGuidelines())
            .Returns("UTF-8 without BOM, file-scoped namespaces...");

        _mockArchContextService.Setup(x => x.GetRelevantCodeSnippetsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CodeSnippet>
            {
                new() { FilePath = "src/Example.cs", Language = "csharp", Code = "public class Example {}" }
            });

        _agent = new ArchitectApiDesignAgent(
            _mockCliAgent.Object,
            _mockArchContextService.Object,
            Mock.Of<ILogger<ArchitectApiDesignAgent>>());
    }

    [Fact]
    public async Task ExecuteAsync_CallsArchitectureContextService()
    {
        // Arrange
        var context = CreateContext();
        _mockCliAgent.Setup(x => x.ExecutePromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CliResponse { Success = true, Content = "```yaml\nopenapi: 3.0.0\ninfo:\n  title: API\npaths:\n  /test:\n    get:\n```" });

        // Act
        var result = await _agent.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(AgentStatus.Completed, result.Status);
        _mockArchContextService.Verify(x => x.GetArchitecturePatternsAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockArchContextService.Verify(x => x.GetTechnologyStack(), Times.Once);
        _mockArchContextService.Verify(x => x.GetCodeStyleGuidelines(), Times.Once);
        _mockArchContextService.Verify(x => x.GetRelevantCodeSnippetsAsync(
            It.IsAny<string>(), It.IsAny<string>(), 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ... rest of tests remain similar
}
```

**Apply same pattern to all 5 planning agents.**

---

## Acceptance Criteria

- [ ] `PlanArtifactStorageAgent` creates new plans with all 5 artifacts
- [ ] `PlanArtifactStorageAgent` updates existing plans and creates version snapshots
- [ ] Version history includes `CreatedBy` and `RevisionReason`
- [ ] All 5 planning agents inject `IArchitectureContextService` (NOT `IContextBuilder`)
- [ ] Custom `IContextBuilder` methods removed from `ContextBuilder.cs`
- [ ] All 5 agents use Epic 07's service methods for context
- [ ] Agent unit tests updated to mock `IArchitectureContextService`
- [ ] 7+ unit tests for `PlanArtifactStorageAgent` (80% coverage)
- [ ] All tests pass (100% pass rate)
- [ ] Build succeeds with 0 warnings

---

## Testing Strategy

**Unit Tests:**
- Test storage agent with no existing plan (create)
- Test storage agent with existing plan (update + version)
- Test storage agent with no artifacts (failure)
- Test version history metadata (created by, revision reason)
- Test Epic 07 service calls in all 5 agents

**Integration Tests:**
- Execute full 5-agent chain → storage agent
- Verify all artifacts persisted to database
- Verify version increment on revision

---

## Implementation Checklist

### Task 1: PlanArtifactStorageAgent
- [ ] Create `PlanArtifactStorageAgent.cs` with create/update logic
- [ ] Inject `IPlanRepository`
- [ ] Handle new plan creation
- [ ] Handle existing plan updates with versioning
- [ ] Store revision feedback and created by
- [ ] Create 7 unit tests
- [ ] Verify all tests pass

### Task 2: Epic 07 Refactoring
- [ ] Remove custom methods from `IContextBuilder`/`ContextBuilder.cs`
- [ ] Update `PmUserStoriesAgent` to use `IArchitectureContextService`
- [ ] Update `ArchitectApiDesignAgent` to use `IArchitectureContextService`
- [ ] Update `ArchitectDbSchemaAgent` to use `IArchitectureContextService`
- [ ] Update `QaTestCasesAgent` to use `IArchitectureContextService`
- [ ] Update `TechLeadImplementationAgent` to use `IArchitectureContextService`
- [ ] Update all 5 agent test files with new mocks
- [ ] Verify all tests pass (2,600+ tests)
- [ ] Verify build with 0 warnings

---

## Estimated Effort

- **PlanArtifactStorageAgent**: 3-4 hours
- **Epic 07 Refactoring**: 4-6 hours
- **Testing & Verification**: 2 hours
- **Total**: 1-2 days

---

## Dependencies

**Epic 07 Services (Already Implemented):**
- `IArchitectureContextService`
- `ArchitectureContextService` implementation

**Part 1 (Already Implemented):**
- `Plan` entity with `UpdateArtifacts()` method
- `PlanVersion` entity
- `IPlanRepository` with `GetByTicketIdAsync`, `AddAsync`, `UpdateAsync`

---

## Success Metrics

- All artifacts stored correctly in database
- Version history tracks revisions accurately
- 100% test pass rate maintained
- Zero custom context building code (all Epic 07)
- Build with 0 warnings
