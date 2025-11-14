# Feature 1: Enhanced Planning Prompts

**Goal**: Improve AI-generated plan quality through domain-specific prompt templates and architectural context injection.

**Estimated Effort**: 1 week
**Priority**: P1 (High Impact)
**Dependencies**: Epic 5 (Agent Framework) - PlanningAgent already implemented

---

## Executive Summary

Currently, the PlanningAgent uses generic prompts that don't leverage project-specific patterns, architectural decisions, or code examples. This results in plans that require multiple refinement cycles to align with the project's standards.

**This feature** creates domain-specific prompt templates (Web UI, REST API, Database, Background Jobs) that inject:
- Architectural patterns from `/docs/ARCHITECTURE.md`
- Technology stack with versions
- Code snippets from similar features
- Code style guidelines from `.editorconfig`

**Expected Impact**: 30% reduction in plan refinement requests, plans reference actual project patterns 90%+ of the time.

---

## Current State Analysis

### Existing Prompt Structure

From `/prompts/plan/anthropic/`:
- `system.txt` - Generic system prompt defining role
- `user_template.hbs` - Handlebars template with basic context

**Variables Available**:
```handlebars
{{ticketId}}
{{ticketTitle}}
{{ticketDescription}}
{{acceptanceCriteria}}
{{relevantFiles}}        # List of file paths only (no content)
{{repositoryName}}
{{branchName}}
```

### Limitations

1. **No Architectural Context**: Prompts don't mention Clean Architecture, DDD patterns, or Blazor conventions
2. **No Code Examples**: File paths listed but no actual code snippets
3. **Generic Instructions**: Same prompt for Web UI, API, Database tasks
4. **No Technology Stack**: Doesn't specify .NET 10, Entity Framework, Radzen, etc.
5. **No Code Style**: Doesn't reference .editorconfig rules

---

## Proposed Solution

### Domain-Specific Template System

Create specialized prompt templates for each ticket type:

```
/prompts/plan/anthropic/domains/
├── web_ui.txt              # Blazor component patterns
├── rest_api.txt            # Controller, DTO, service patterns
├── database.txt            # EF migrations, indexing strategies
├── background_jobs.txt     # Hangfire patterns, idempotency
└── refactoring.txt         # Refactoring patterns, testing updates
```

### Enhanced Context Injection

Extend PlanningAgent to inject:

1. **Architectural Patterns** (from `ARCHITECTURE.md`):
   - Clean Architecture layers (Domain → Application → Infrastructure → Web)
   - Repository pattern
   - Service layer separation
   - DTO mapping between layers

2. **Technology Stack**:
   - .NET 10 with C# 13
   - Entity Framework Core for data access
   - Blazor Server (not WebAssembly)
   - Radzen.Blazor v5.9.0 for UI components
   - LibGit2Sharp for git operations

3. **Code Examples** (3-5 snippets from similar features):
   - Entity definition
   - Service interface and implementation
   - Blazor component with code-behind
   - Repository methods

4. **Code Style Guidelines** (from `.editorconfig`):
   - UTF-8 without BOM
   - 4 spaces for indentation
   - File scoped namespaces
   - var keyword usage rules

---

## Implementation Plan

### Step 1: Create Domain Templates (2 days)

**File**: `/prompts/plan/anthropic/domains/web_ui.txt`

```text
You are an expert Blazor Server developer working on the {{repositoryName}} project.

# Project Architecture

This project uses **Clean Architecture** with the following layers:
- **Domain** (`PRFactory.Domain`) - Entities, value objects, domain logic
- **Application** (`PRFactory.Core`) - Interfaces, DTOs, application services
- **Infrastructure** (`PRFactory.Infrastructure`) - Implementations, EF Core, external integrations
- **Web** (`PRFactory.Web`) - Blazor Server UI, pages, components

# Technology Stack

- **.NET 10** with C# 13
- **Blazor Server** (NOT Blazor WebAssembly)
- **Entity Framework Core** for data access
- **Radzen.Blazor v5.9.0** for rich UI components
- **Bootstrap 5** for CSS styling
- **Markdig** for markdown rendering

# Blazor UI Patterns (CRITICAL - ALWAYS FOLLOW)

## Component Organization

```
/PRFactory.Web/
├── Pages/                    # Routable pages (@page directive)
│   ├── FeatureName/
│   │   ├── Index.razor      # Page markup
│   │   └── Index.razor.cs   # Code-behind (REQUIRED)
├── Components/               # Business components (domain logic)
│   └── FeatureName/
│       ├── ComponentName.razor
│       └── ComponentName.razor.cs  # Code-behind (REQUIRED)
└── UI/                       # Pure UI components (no business logic)
    └── Category/
        └── ReusableUI.razor
```

## Code-Behind Pattern (MANDATORY)

**ALWAYS separate .razor and .razor.cs files for Pages and business Components.**

Example:
```csharp
// MyPage.razor (markup only)
@page "/my-page"
@using PRFactory.Web.Services

<PageContainer Title="My Page">
    <Card Title="Content">
        <p>@message</p>
    </Card>
</PageContainer>

// MyPage.razor.cs (logic)
using Microsoft.AspNetCore.Components;
using PRFactory.Web.Services;

namespace PRFactory.Web.Pages.MyFeature;

public partial class MyPage
{
    [Inject]
    private IMyService MyService { get; set; } = null!;

    private string message = "Loading...";

    protected override async Task OnInitializedAsync()
    {
        message = await MyService.GetDataAsync();
    }
}
```

## Service Injection (NO HTTP CALLS)

**CRITICAL**: This is Blazor Server. NEVER use HttpClient to call API controllers in the same process.

```csharp
// ❌ WRONG - HTTP call within same process
public class MyService
{
    private readonly HttpClient _http;
    public async Task<Data> GetDataAsync()
    {
        return await _http.GetFromJsonAsync<Data>("/api/data"); // BAD!
    }
}

// ✅ CORRECT - Direct service injection
public class MyService
{
    private readonly IDataRepository _repo;
    public async Task<Data> GetDataAsync()
    {
        return await _repo.GetByIdAsync(id); // GOOD!
    }
}
```

## UI Component Usage

**Use existing UI components from `/UI/` directory:**
- `Card.razor` - Card container
- `LoadingButton.razor` - Button with loading state
- `FormTextField.razor` - Text input with validation
- `AlertMessage.razor` - Alert/notification
- `StatusBadge.razor` - Status indicator
- `EmptyState.razor` - Empty state UI

**Use Radzen components for advanced controls:**
- `RadzenDropDown` - Dropdown with search
- `RadzenDatePicker` - Date selection
- `RadzenTextArea` - Multi-line text

**NO JavaScript allowed** - Use pure Blazor Server patterns only.

# Ticket to Implement

**Title**: {{ticketTitle}}

**Description**:
{{ticketDescription}}

**Acceptance Criteria**:
{{acceptanceCriteria}}

# Relevant Code Examples

{{#each codeSnippets}}
**File**: `{{this.filePath}}`
```{{this.language}}
{{this.code}}
```

{{/each}}

# Code Style Guidelines

- UTF-8 encoding WITHOUT BOM (critical - CI will fail with BOM)
- File-scoped namespaces (namespace Foo.Bar;)
- 4 spaces for indentation (no tabs)
- Use `var` for obvious types
- Always use code-behind (.razor.cs) for Pages and business Components

# Task

Generate a detailed implementation plan that:
1. Follows the project's Clean Architecture layers
2. Uses existing UI components from `/UI/` where applicable
3. Follows code-behind pattern for all Pages and business Components
4. Injects services directly (NO HTTP calls within same process)
5. Uses Radzen components for advanced UI controls
6. Includes specific file paths, class names, method signatures
7. Provides test cases for new functionality
8. Follows the code style guidelines above

The plan should be in markdown format with these sections:
- Overview
- Files to Create
- Files to Modify
- Implementation Steps
- Test Cases
- Rollback Plan
```

**Create similar templates for**:
- `rest_api.txt` - Controller patterns, DTO mapping, service layer
- `database.txt` - EF migrations, entity configuration, indexes
- `background_jobs.txt` - Hangfire patterns, retry logic
- `refactoring.txt` - Refactoring patterns, test updates

**Estimated Time**: 2 days (all 5 templates)

---

### Step 2: Extract Architectural Context (1 day)

Create a service to extract and format architectural patterns:

**File**: `/src/PRFactory.Infrastructure/Agents/Services/ArchitectureContextService.cs`

```csharp
namespace PRFactory.Infrastructure.Agents.Services;

public interface IArchitectureContextService
{
    /// <summary>
    /// Get architectural patterns from ARCHITECTURE.md
    /// </summary>
    Task<string> GetArchitecturePatternsAsync();

    /// <summary>
    /// Get technology stack with versions
    /// </summary>
    string GetTechnologyStack();

    /// <summary>
    /// Get code style guidelines from .editorconfig
    /// </summary>
    string GetCodeStyleGuidelines();

    /// <summary>
    /// Get code snippets similar to the ticket type
    /// </summary>
    Task<List<CodeSnippet>> GetRelevantCodeSnippetsAsync(
        string ticketType,
        int maxSnippets = 3);
}

public class CodeSnippet
{
    public string FilePath { get; set; } = string.Empty;
    public string Language { get; set; } = "csharp";
    public string Code { get; set; } = string.Empty;
}

public class ArchitectureContextService : IArchitectureContextService
{
    private readonly IRepositoryFileService _fileService;
    private readonly ILogger<ArchitectureContextService> _logger;

    public ArchitectureContextService(
        IRepositoryFileService fileService,
        ILogger<ArchitectureContextService> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<string> GetArchitecturePatternsAsync()
    {
        // Read ARCHITECTURE.md and extract key patterns section
        var archDoc = await _fileService.ReadFileAsync("docs/ARCHITECTURE.md");

        // Extract relevant sections (Clean Architecture, layers, patterns)
        var patterns = ExtractArchitectureSection(archDoc);

        return patterns;
    }

    public string GetTechnologyStack()
    {
        return @"
- .NET 10 with C# 13
- Entity Framework Core (Code First)
- Blazor Server (NOT WebAssembly)
- Radzen.Blazor v5.9.0
- Bootstrap 5
- LibGit2Sharp for git operations
- Markdig for markdown rendering
- Hangfire for background jobs
        ".Trim();
    }

    public string GetCodeStyleGuidelines()
    {
        return @"
- UTF-8 encoding WITHOUT BOM (mandatory)
- File-scoped namespaces: namespace Foo.Bar;
- 4 spaces for indentation (no tabs)
- Use var for obvious types
- Code-behind pattern for Pages and business Components
- Max line length: 120 characters
        ".Trim();
    }

    public async Task<List<CodeSnippet>> GetRelevantCodeSnippetsAsync(
        string ticketType,
        int maxSnippets = 3)
    {
        var snippets = new List<CodeSnippet>();

        // Based on ticket type, find relevant examples
        switch (ticketType.ToLower())
        {
            case "web ui":
            case "ui":
                snippets.Add(await GetSnippetAsync(
                    "src/PRFactory.Web/Pages/Tickets/Index.razor.cs",
                    50));  // First 50 lines
                snippets.Add(await GetSnippetAsync(
                    "src/PRFactory.Domain/Entities/Ticket.cs",
                    30));
                break;

            case "rest api":
            case "api":
                snippets.Add(await GetSnippetAsync(
                    "src/PRFactory.Api/Controllers/TicketsController.cs",
                    50));
                snippets.Add(await GetSnippetAsync(
                    "src/PRFactory.Core/Application/DTOs/TicketDto.cs",
                    20));
                break;

            case "database":
                snippets.Add(await GetSnippetAsync(
                    "src/PRFactory.Domain/Entities/Ticket.cs",
                    40));
                snippets.Add(await GetSnippetAsync(
                    "src/PRFactory.Infrastructure/Persistence/Migrations/",
                    30));
                break;

            case "background job":
                // Find Hangfire job examples
                break;
        }

        return snippets.Take(maxSnippets).ToList();
    }

    private async Task<CodeSnippet> GetSnippetAsync(string filePath, int maxLines)
    {
        var content = await _fileService.ReadFileLinesAsync(filePath, 0, maxLines);

        return new CodeSnippet
        {
            FilePath = filePath,
            Language = "csharp",
            Code = string.Join("\n", content)
        };
    }

    private string ExtractArchitectureSection(string archDoc)
    {
        // Extract Clean Architecture section from ARCHITECTURE.md
        // Return formatted summary
        return "Clean Architecture with Domain → Application → Infrastructure → Web";
    }
}
```

**Estimated Time**: 1 day

---

### Step 3: Update PlanningAgent (1 day)

Modify PlanningAgent to use domain templates and inject enhanced context:

**File**: `/src/PRFactory.Infrastructure/Agents/PlanningAgent.cs`

```csharp
public class PlanningAgent : AgentBase
{
    private readonly IArchitectureContextService _architectureContext;
    private readonly IAgentPromptService _promptService;

    // Update BuildSystemPromptAsync to load domain template
    protected override async Task<string> BuildSystemPromptAsync(
        TicketContext context,
        CancellationToken cancellationToken = default)
    {
        // Determine domain template based on ticket type
        var domainTemplate = GetDomainTemplate(context.Ticket.Type);

        // Load base system prompt
        var basePrompt = await _promptService.LoadPromptAsync(
            $"plan/anthropic/{domainTemplate}");

        return basePrompt;
    }

    protected override async Task<string> BuildUserPromptAsync(
        TicketContext context,
        CancellationToken cancellationToken = default)
    {
        // Load user template
        var template = await _promptService.LoadPromptAsync(
            "plan/anthropic/user_template.hbs");

        // Get architectural context
        var archPatterns = await _architectureContext.GetArchitecturePatternsAsync();
        var techStack = _architectureContext.GetTechnologyStack();
        var codeStyle = _architectureContext.GetCodeStyleGuidelines();
        var codeSnippets = await _architectureContext.GetRelevantCodeSnippetsAsync(
            context.Ticket.Type);

        // Prepare template variables
        var variables = new Dictionary<string, object>
        {
            ["ticketId"] = context.Ticket.Id,
            ["ticketTitle"] = context.Ticket.Title,
            ["ticketDescription"] = context.Ticket.Description,
            ["acceptanceCriteria"] = context.Ticket.AcceptanceCriteria,
            ["ticketType"] = context.Ticket.Type,
            ["repositoryName"] = context.Repository.Name,
            ["branchName"] = context.BranchName,
            ["relevantFiles"] = context.RelevantFiles,
            ["architecturePatterns"] = archPatterns,
            ["technologyStack"] = techStack,
            ["codeStyleGuidelines"] = codeStyle,
            ["codeSnippets"] = codeSnippets
        };

        // Render template with Handlebars
        var prompt = HandleBarsHelpers.Render(template, variables);

        return prompt;
    }

    private string GetDomainTemplate(string ticketType)
    {
        return ticketType.ToLower() switch
        {
            "web ui" => "domains/web_ui.txt",
            "ui" => "domains/web_ui.txt",
            "rest api" => "domains/rest_api.txt",
            "api" => "domains/rest_api.txt",
            "database" => "domains/database.txt",
            "background job" => "domains/background_jobs.txt",
            "refactoring" => "domains/refactoring.txt",
            _ => "system.txt"  // Fallback to generic
        };
    }
}
```

**Estimated Time**: 1 day

---

### Step 4: Service Registration (0.5 days)

Register new service in DI container:

**File**: `/src/PRFactory.Infrastructure/DependencyInjection.cs`

```csharp
// Add to AddInfrastructure method
services.AddScoped<IArchitectureContextService, ArchitectureContextService>();
```

---

### Step 5: Unit Tests (1.5 days)

**File**: `/tests/PRFactory.Infrastructure.Tests/Agents/Services/ArchitectureContextServiceTests.cs`

```csharp
public class ArchitectureContextServiceTests
{
    [Fact]
    public async Task GetArchitecturePatterns_ReturnsNonEmptyString()
    {
        // Arrange
        var fileService = MockFileService();
        var service = new ArchitectureContextService(fileService, NullLogger);

        // Act
        var patterns = await service.GetArchitecturePatternsAsync();

        // Assert
        Assert.NotEmpty(patterns);
        Assert.Contains("Clean Architecture", patterns);
    }

    [Fact]
    public void GetTechnologyStack_IncludesDotNet10()
    {
        // Arrange
        var service = CreateService();

        // Act
        var stack = service.GetTechnologyStack();

        // Assert
        Assert.Contains(".NET 10", stack);
        Assert.Contains("Blazor Server", stack);
    }

    [Theory]
    [InlineData("Web UI", 3)]
    [InlineData("REST API", 3)]
    [InlineData("Database", 3)]
    public async Task GetRelevantCodeSnippets_ReturnsSnippetsForTicketType(
        string ticketType,
        int expectedCount)
    {
        // Arrange
        var service = CreateService();

        // Act
        var snippets = await service.GetRelevantCodeSnippetsAsync(ticketType);

        // Assert
        Assert.NotEmpty(snippets);
        Assert.True(snippets.Count <= expectedCount);
        Assert.All(snippets, s => Assert.NotEmpty(s.Code));
    }
}
```

**File**: `/tests/PRFactory.Infrastructure.Tests/Agents/PlanningAgentTests.cs`

```csharp
public class PlanningAgentTests
{
    [Theory]
    [InlineData("Web UI", "domains/web_ui.txt")]
    [InlineData("REST API", "domains/rest_api.txt")]
    [InlineData("Database", "domains/database.txt")]
    public async Task BuildSystemPrompt_LoadsCorrectDomainTemplate(
        string ticketType,
        string expectedTemplate)
    {
        // Arrange
        var agent = CreateAgent();
        var context = CreateTicketContext(ticketType);

        // Act
        var prompt = await agent.BuildSystemPromptAsync(context);

        // Assert
        Assert.Contains(expectedTemplate, prompt);
    }

    [Fact]
    public async Task BuildUserPrompt_IncludesArchitectureContext()
    {
        // Arrange
        var agent = CreateAgent();
        var context = CreateTicketContext("Web UI");

        // Act
        var prompt = await agent.BuildUserPromptAsync(context);

        // Assert
        Assert.Contains("Clean Architecture", prompt);
        Assert.Contains(".NET 10", prompt);
        Assert.Contains("Code-behind pattern", prompt);
    }

    [Fact]
    public async Task BuildUserPrompt_IncludesCodeSnippets()
    {
        // Arrange
        var agent = CreateAgent();
        var context = CreateTicketContext("Web UI");

        // Act
        var prompt = await agent.BuildUserPromptAsync(context);

        // Assert
        Assert.Contains("Code Example", prompt);
        Assert.Contains("```csharp", prompt);
    }
}
```

**Estimated Time**: 1.5 days

---

## Acceptance Criteria

- [ ] 5 domain-specific prompt templates created in `/prompts/plan/anthropic/domains/`
- [ ] ArchitectureContextService implemented with all methods
- [ ] PlanningAgent updated to load domain templates based on ticket type
- [ ] System prompt includes architectural patterns, tech stack, code style
- [ ] User prompt includes 3+ relevant code snippets
- [ ] Service registered in DI container
- [ ] Unit tests for ArchitectureContextService (80%+ coverage)
- [ ] Unit tests for PlanningAgent prompt building (80%+ coverage)
- [ ] Manual test: Generate plan for Web UI ticket → Plan references Blazor patterns
- [ ] Manual test: Generate plan for REST API ticket → Plan references controller patterns

---

## Testing Plan

### Manual Testing Scenarios

**Scenario 1: Web UI Plan Generation**
1. Create ticket with Type = "Web UI"
2. Trigger plan generation
3. **Verify**: Generated plan includes:
   - Blazor component patterns
   - Code-behind separation (.razor + .razor.cs)
   - Service injection (no HTTP calls)
   - Radzen component usage
   - Bootstrap 5 styling

**Scenario 2: REST API Plan Generation**
1. Create ticket with Type = "REST API"
2. Trigger plan generation
3. **Verify**: Generated plan includes:
   - Controller patterns
   - DTO mapping
   - Service layer delegation
   - API versioning considerations

**Scenario 3: Code Snippets Injection**
1. Create any ticket type
2. Trigger plan generation
3. **Verify**: Plan includes 3+ code snippets from codebase
4. **Verify**: Snippets are relevant to ticket type

---

## Files Created/Modified

### New Files (12 files)

**Prompt Templates** (5 files):
- `/prompts/plan/anthropic/domains/web_ui.txt`
- `/prompts/plan/anthropic/domains/rest_api.txt`
- `/prompts/plan/anthropic/domains/database.txt`
- `/prompts/plan/anthropic/domains/background_jobs.txt`
- `/prompts/plan/anthropic/domains/refactoring.txt`

**Service Layer** (2 files):
- `/src/PRFactory.Core/Application/Services/IArchitectureContextService.cs`
- `/src/PRFactory.Infrastructure/Agents/Services/ArchitectureContextService.cs`

**Unit Tests** (2 files):
- `/tests/PRFactory.Infrastructure.Tests/Agents/Services/ArchitectureContextServiceTests.cs`
- `/tests/PRFactory.Infrastructure.Tests/Agents/PlanningAgentTests.cs`

### Modified Files (2 files)

- `/src/PRFactory.Infrastructure/Agents/PlanningAgent.cs` - Load domain templates, inject context
- `/src/PRFactory.Infrastructure/DependencyInjection.cs` - Register service

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **Code snippets too large** | Medium | Limit to first 50 lines, prioritize key methods |
| **Prompt token limits** | High | Monitor token usage, truncate if approaching limits |
| **Template maintenance** | Medium | Version control templates, document variables clearly |
| **Domain template selection** | Low | Provide clear mapping, fallback to generic template |

---

## Success Metrics

**Before Enhancement**:
- Plans require 2.5 average refinement cycles
- Plans rarely reference project patterns
- 60% of plans need architectural corrections

**After Enhancement**:
- Plans require 1.5-2.0 average refinement cycles (30% improvement)
- 90%+ of plans reference actual project patterns
- 20% reduction in architectural correction requests

---

## Future Enhancements

**Phase 2** (not in this epic):
- AI learns from approved plans (reinforcement learning)
- Custom templates per repository
- Template validation in admin UI
- A/B testing of prompt variations
- Automatic template updates based on codebase changes

---

**End of Feature 1: Enhanced Planning Prompts**
