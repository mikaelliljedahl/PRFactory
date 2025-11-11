# Prompt Library: Database-Driven Template Management

**Requirement**: Prompt templates should be stored in the database, not as files on disk.

---

## Design Rationale

### Why Database-Driven Prompts?

**Problems with File-Based Prompts**:
- ❌ Requires file system access and deployment for updates
- ❌ No versioning or audit trail
- ❌ Cannot be customized by tenants at runtime
- ❌ Difficult to A/B test different prompts
- ❌ No UI for prompt management

**Benefits of Database-Driven Prompts**:
- ✅ **Runtime Customization**: Tenants modify prompts without redeployment
- ✅ **Version Control**: Track prompt changes over time
- ✅ **Audit Trail**: Who changed what, when
- ✅ **A/B Testing**: Compare prompt effectiveness
- ✅ **UI Management**: Edit prompts through web interface
- ✅ **Per-Agent Configuration**: Link prompts to specific workflow agents
- ✅ **Multi-Tenant**: System templates + tenant overrides

---

## Architecture

### Entity: PromptTemplate

**File**: `/src/PRFactory.Domain/Entities/PromptTemplate.cs`

```csharp
public class PromptTemplate
{
    public Guid Id { get; private init; }

    // Ownership
    public Guid? TenantId { get; private init; }  // null = system template
    public bool IsSystemTemplate => TenantId == null;

    // Identification
    public string Name { get; private set; }  // "code-review-specialist"
    public string DisplayName { get; private set; }  // "Code Review Specialist"
    public string Description { get; private set; }  // "Reviews PRs for quality..."
    public string Category { get; private set; }  // "Review", "Planning", "Implementation"
    public string Icon { get; private set; }  // "search-code" (for UI)
    public string Color { get; private set; }  // "#3B82F6" (for UI)

    // Prompt Content
    public string SystemPrompt { get; private set; }  // Base system instructions
    public string UserPromptTemplate { get; private set; }  // Handlebars template
    public PromptTemplateFormat Format { get; private set; }  // Handlebars, Scriban, Plain

    // Model Configuration
    public string? RecommendedModel { get; private set; }  // "claude-sonnet-4-5"
    public Guid? PreferredLlmProviderId { get; private set; }  // Link to TenantLlmProvider
    public int? DefaultMaxTokens { get; private set; }  // 8000
    public double? DefaultTemperature { get; private set; }  // 0.7

    // Versioning & Metadata
    public int Version { get; private set; }  // 1, 2, 3...
    public Guid? ParentTemplateId { get; private set; }  // For tenant overrides
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    // Usage Tracking
    public int UsageCount { get; private set; }  // Times used
    public DateTime? LastUsedAt { get; private set; }

    // Methods
    public void UpdateContent(string systemPrompt, string userPromptTemplate, string? updatedBy);
    public void UpdateConfiguration(string? recommendedModel, Guid? llmProviderId, int? maxTokens, double? temperature);
    public void Activate();
    public void Deactivate();
    public void IncrementUsage();
    public PromptTemplate CreateTenantOverride(Guid tenantId, string createdBy);
}

public enum PromptTemplateFormat
{
    Plain = 0,
    Handlebars = 1,
    Scriban = 2
}
```

---

### Entity: PromptTemplateVariable

**File**: `/src/PRFactory.Domain/Entities/PromptTemplateVariable.cs`

Documents available variables for a template category.

```csharp
public class PromptTemplateVariable
{
    public Guid Id { get; private init; }
    public string Category { get; private set; }  // "Review", "Planning", etc.
    public string Name { get; private set; }  // "ticket_number"
    public string DisplayName { get; private set; }  // "Ticket Number"
    public string Description { get; private set; }  // "Jira/ADO ticket key"
    public string DataType { get; private set; }  // "string", "int", "array", "object"
    public string? ExampleValue { get; private set; }  // "PROJ-123"
    public bool IsRequired { get; private set; }
    public string? ObjectStructure { get; private set; }  // JSON schema for objects/arrays
}
```

**Example Variables**:
```json
[
  {
    "category": "Review",
    "name": "ticket_number",
    "displayName": "Ticket Number",
    "description": "Jira/ADO ticket key",
    "dataType": "string",
    "exampleValue": "PROJ-123",
    "isRequired": true
  },
  {
    "category": "Review",
    "name": "file_changes",
    "displayName": "File Changes",
    "description": "Array of changed files with diffs",
    "dataType": "array",
    "exampleValue": "[{file_path: '...', diff: '...'}]",
    "isRequired": true,
    "objectStructure": "{file_path: string, diff: string, language: string, ...}"
  }
]
```

---

### Entity: PromptTemplateVersion

**File**: `/src/PRFactory.Domain/Entities/PromptTemplateVersion.cs`

Tracks prompt template changes over time.

```csharp
public class PromptTemplateVersion
{
    public Guid Id { get; private init; }
    public Guid PromptTemplateId { get; private init; }
    public int VersionNumber { get; private init; }
    public string SystemPrompt { get; private init; }
    public string UserPromptTemplate { get; private init; }
    public string ChangeDescription { get; private set; }
    public string CreatedBy { get; private init; }
    public DateTime CreatedAt { get; private init; }
}
```

---

### Entity: AgentWorkflow

**File**: `/src/PRFactory.Domain/Entities/AgentWorkflow.cs`

Links prompt templates to workflow agents.

```csharp
public class AgentWorkflow
{
    public Guid Id { get; private init; }
    public Guid? TenantId { get; private init; }  // null = system workflow
    public string WorkflowName { get; private set; }  // "code-review", "planning", "implementation"
    public string AgentName { get; private set; }  // "CodeReviewAgent", "PlanningAgent"
    public Guid PromptTemplateId { get; private set; }  // Link to prompt
    public int ExecutionOrder { get; private set; }  // For multi-agent workflows
    public bool IsActive { get; private set; }

    // Navigation
    public PromptTemplate PromptTemplate { get; private set; }
}
```

**Example Configuration**:
```
Workflow: "code-review"
├── Agent: CodeReviewAgent
│   └── PromptTemplate: "code-review-specialist"
├── Agent: PostReviewCommentsAgent
│   └── PromptTemplate: "review-comment-formatter"
└── Agent: PostApprovalCommentAgent
    └── PromptTemplate: "approval-comment-template"
```

---

## Repository Interface

**File**: `/src/PRFactory.Core/Application/Repositories/IPromptTemplateRepository.cs`

```csharp
public interface IPromptTemplateRepository
{
    // CRUD
    Task<PromptTemplate> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PromptTemplate?> GetByNameAsync(string name, Guid? tenantId, CancellationToken ct = default);
    Task<List<PromptTemplate>> GetAllAsync(Guid? tenantId, CancellationToken ct = default);
    Task<List<PromptTemplate>> GetByCategoryAsync(string category, Guid? tenantId, CancellationToken ct = default);
    Task AddAsync(PromptTemplate template, CancellationToken ct = default);
    Task UpdateAsync(PromptTemplate template, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // System vs Tenant Templates
    Task<List<PromptTemplate>> GetSystemTemplatesAsync(CancellationToken ct = default);
    Task<List<PromptTemplate>> GetTenantTemplatesAsync(Guid tenantId, CancellationToken ct = default);
    Task<PromptTemplate?> GetEffectiveTemplateAsync(string name, Guid tenantId, CancellationToken ct = default);

    // Versioning
    Task<List<PromptTemplateVersion>> GetVersionHistoryAsync(Guid templateId, CancellationToken ct = default);
    Task<PromptTemplateVersion> GetVersionAsync(Guid templateId, int version, CancellationToken ct = default);
    Task SaveVersionAsync(PromptTemplateVersion version, CancellationToken ct = default);

    // Variables
    Task<List<PromptTemplateVariable>> GetVariablesByCategoryAsync(string category, CancellationToken ct = default);

    // Agent Workflows
    Task<AgentWorkflow?> GetAgentWorkflowAsync(string workflowName, string agentName, Guid? tenantId, CancellationToken ct = default);
    Task<List<AgentWorkflow>> GetWorkflowAgentsAsync(string workflowName, Guid? tenantId, CancellationToken ct = default);
}
```

---

## Service Interface

**File**: `/src/PRFactory.Core/Application/Services/IPromptLibraryService.cs`

```csharp
public interface IPromptLibraryService
{
    // Template Management
    Task<PromptTemplate> GetTemplateAsync(string name, Guid? tenantId, CancellationToken ct = default);
    Task<PromptTemplate> CreateTemplateAsync(CreatePromptTemplateRequest request, CancellationToken ct = default);
    Task<PromptTemplate> UpdateTemplateAsync(Guid id, UpdatePromptTemplateRequest request, CancellationToken ct = default);
    Task DeleteTemplateAsync(Guid id, CancellationToken ct = default);

    // Tenant Overrides
    Task<PromptTemplate> CreateTenantOverrideAsync(Guid systemTemplateId, Guid tenantId, string createdBy, CancellationToken ct = default);
    Task<PromptTemplate> GetEffectiveTemplateAsync(string name, Guid tenantId, CancellationToken ct = default);

    // Rendering
    Task<string> RenderTemplateAsync(Guid templateId, object variables, CancellationToken ct = default);
    Task<string> RenderTemplateAsync(string name, Guid tenantId, object variables, CancellationToken ct = default);

    // Validation
    Task<PromptValidationResult> ValidateTemplateAsync(string userPromptTemplate, PromptTemplateFormat format, CancellationToken ct = default);
    Task<List<string>> ExtractVariablesAsync(string template, PromptTemplateFormat format, CancellationToken ct = default);

    // Usage Tracking
    Task TrackUsageAsync(Guid templateId, CancellationToken ct = default);
    Task<PromptUsageStatistics> GetUsageStatisticsAsync(Guid templateId, CancellationToken ct = default);

    // Agent Workflows
    Task LinkTemplateToWorkflowAsync(string workflowName, string agentName, Guid templateId, Guid? tenantId, CancellationToken ct = default);
    Task<PromptTemplate> GetWorkflowTemplateAsync(string workflowName, string agentName, Guid? tenantId, CancellationToken ct = default);
}
```

---

## UI Pages

### 1. Prompt Library Page (`/agent-prompts`)

Browse and manage prompt templates.

**Features**:
- **System Templates** (read-only, shown in blue)
- **Tenant Templates** (editable, shown in green)
- **Filter by category** (Planning, Implementation, Review)
- **Search by name** or description
- **Copy system template** to create tenant override
- **Version history** view
- **Usage statistics** (times used, last used)

### 2. Template Editor (`/agent-prompts/{id}/edit`)

Edit prompt template with live preview.

**Features**:
- **Split view**: Template editor + rendered preview
- **Variable documentation**: Show available variables with examples
- **Syntax highlighting**: For Handlebars syntax
- **Validation**: Real-time template validation
- **Test with sample data**: Preview with example variables
- **Version notes**: Add change description for version history

### 3. Template Preview (`/agent-prompts/{id}/preview`)

Preview template with sample data.

**Features**:
- **Variable input form**: Fill in sample variable values
- **Rendered output**: See final prompt after rendering
- **Copy to clipboard**: Copy rendered prompt
- **Compare versions**: Side-by-side version comparison

### 4. Workflow Configuration (`/admin/agent-workflows`)

Link prompt templates to workflow agents.

**Features**:
- **Visual workflow editor**: Drag-and-drop agent sequence
- **Template selection**: Choose prompt template for each agent
- **Execution order**: Set agent execution order
- **Per-tenant workflows**: Override system workflows

---

## Database Seeding

**File**: `/src/PRFactory.Infrastructure/Data/Seed/PromptTemplateSeed.cs`

Seed system templates on initial deployment.

```csharp
public static class PromptTemplateSeed
{
    public static async Task SeedSystemTemplatesAsync(PRFactoryDbContext context)
    {
        // Code Review Template
        var codeReviewTemplate = new PromptTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = null,  // System template
            Name = "code-review-specialist",
            DisplayName = "Code Review Specialist",
            Description = "Reviews pull requests for quality, security, and best practices",
            Category = "Review",
            Icon = "search-code",
            Color = "#3B82F6",
            SystemPrompt = @"You are an expert code reviewer with deep knowledge of software engineering best practices, security vulnerabilities, and performance optimization.

Your task is to review pull requests and provide constructive, actionable feedback.

Review Focus Areas:
1. **Security:** SQL injection, XSS, authentication/authorization issues, secrets in code
2. **Correctness:** Logic errors, edge cases, null handling, race conditions
3. **Performance:** Inefficient algorithms, N+1 queries, memory leaks, excessive allocations
4. **Maintainability:** Code duplication, complex logic, missing documentation, unclear naming
5. **Testing:** Missing test coverage, inadequate assertions, untested edge cases
6. **Architecture:** Violations of SOLID principles, tight coupling, poor separation of concerns

Output Format:
- **Critical Issues:** Bugs, security vulnerabilities, breaking changes (MUST fix)
- **Suggested Improvements:** Performance, maintainability, code quality (SHOULD fix)
- **Praise:** Highlight well-written code, good patterns, clever solutions

Tone: Constructive, specific, educational. Always explain WHY something is an issue and HOW to fix it.",
            UserPromptTemplate = @"# Code Review Request

## Ticket Information
**Ticket:** {{ticket_number}}
**Title:** {{ticket_title}}
**Description:**
{{ticket_description}}

## Implementation Plan
**Plan Path:** {{plan_path}}
**Plan Summary:**
{{plan_summary}}

## Pull Request Details
**PR URL:** {{pull_request_url}}
**Branch:** {{branch_name}} → {{target_branch}}
**Files Changed:** {{files_changed_count}}
**Lines:** +{{lines_added}} -{{lines_deleted}}

## Code Changes

{{#each file_changes}}
### File: {{this.file_path}}
**Change Type:** {{this.change_type}}
**Lines:** +{{this.lines_added}} -{{this.lines_deleted}}

```{{this.language}}
{{this.diff}}
```

{{/each}}

## Testing Coverage
**Tests Added:** {{tests_added}}
**Coverage:** {{test_coverage_percentage}}%

---

Please review this pull request and provide detailed feedback.",
            Format = PromptTemplateFormat.Handlebars,
            RecommendedModel = "gpt-4o",
            DefaultMaxTokens = 8000,
            DefaultTemperature = 0.3,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        context.PromptTemplates.Add(codeReviewTemplate);

        // Planning Template
        // Implementation Template
        // ... etc

        await context.SaveChangesAsync();
    }
}
```

---

## Migration Path

### Phase 1: Database Schema
1. Create `PromptTemplates` table
2. Create `PromptTemplateVersions` table
3. Create `PromptTemplateVariables` table
4. Create `AgentWorkflows` table

### Phase 2: Seed System Templates
1. Create database seeder
2. Add code review template
3. Add planning template
4. Add implementation template
5. Document all variables

### Phase 3: UI Implementation
1. Create Prompt Library page
2. Create Template Editor page
3. Create Template Preview page
4. Create Workflow Configuration page

### Phase 4: Service Layer
1. Implement `PromptLibraryService`
2. Implement `PromptTemplateRepository`
3. Update `PromptLoaderService` to load from database
4. Add template caching for performance

### Phase 5: Agent Integration
1. Update agents to load prompts from database
2. Link agents to default templates via `AgentWorkflows`
3. Support tenant-specific template overrides
4. Track template usage statistics

---

## Benefits Summary

### For End Users
- ✅ **Customize prompts** without redeployment
- ✅ **A/B test** different prompt variations
- ✅ **Version control** with rollback capability
- ✅ **Audit trail** of who changed what
- ✅ **UI-based editing** with live preview

### For Admins
- ✅ **Centralized management** of all prompts
- ✅ **Tenant isolation** with override support
- ✅ **Usage analytics** to optimize prompts
- ✅ **Template library** for reuse across workflows

### For Developers
- ✅ **No file system dependencies** (cloud-friendly)
- ✅ **Easy to test** different prompts
- ✅ **Programmatic access** via service layer
- ✅ **Versioning** for safe updates

---

## Next Steps

1. **Review this design** with team
2. **Create database migration** for new tables
3. **Implement repository** and service layer
4. **Build UI pages** for template management
5. **Seed system templates** from Epic 2 documentation
6. **Integrate with agents** in CodeReviewGraph
7. **Document template variables** for each category
8. **Add usage tracking** and analytics
