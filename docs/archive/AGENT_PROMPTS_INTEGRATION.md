> **ARCHIVED**: This implementation guide described the agent prompts system integration (now complete).
> The agent prompts system is now implemented. See [ARCHITECTURE.md](../ARCHITECTURE.md) section on Agent Prompts for current documentation.
>
> **Date Archived**: 2025-11-08
> **Implementation Date**: 2025-11-07

---

# Agent Prompts Integration

This document describes the integration of customizable AI agent prompts from the ClaudeCodeSettings repository into PRFactory.

## Overview

The system now supports customizable agent prompts that can be used across different workflow stages. Prompts are loaded from the `.claude/agents` folder and stored in the database as templates that can be:

- **System Templates**: Available to all tenants (read-only)
- **Tenant Templates**: Customized for specific tenants (editable)

## What Was Added

### 1. Domain Model

**File**: `src/PRFactory.Domain/Entities/AgentPromptTemplate.cs`

The `AgentPromptTemplate` entity stores:
- Name, description, and prompt content
- Recommended model (sonnet, opus, haiku)
- Category (Implementation, Testing, Planning, Analysis, etc.)
- Color for UI identification
- System/tenant classification

### 2. Repository Layer

**Files**:
- `src/PRFactory.Domain/Interfaces/IAgentPromptTemplateRepository.cs`
- `src/PRFactory.Infrastructure/Persistence/Repositories/AgentPromptTemplateRepository.cs`
- `src/PRFactory.Infrastructure/Persistence/Configurations/AgentPromptTemplateConfiguration.cs`

Provides data access for agent prompt templates with methods to:
- Get templates by name, category, or tenant
- Create, update, and delete tenant templates
- Clone system templates for customization

### 3. Prompt Loading Service

**File**: `src/PRFactory.Infrastructure/Agents/Services/AgentPromptLoaderService.cs`

Service that:
- Parses markdown files from `.claude/agents` folder
- Extracts YAML frontmatter (name, description, model, color)
- Creates system templates in the database
- Automatically categorizes prompts based on description

### 4. Prompt Access Service

**Files**:
- `src/PRFactory.Infrastructure/Agents/Services/IAgentPromptService.cs`
- `src/PRFactory.Infrastructure/Agents/Services/AgentPromptService.cs`

Provides agents with easy access to prompt templates:
- Get templates by name with tenant fallback
- Get templates by category
- Get all available templates for a tenant

### 5. API Endpoints

**File**: `src/PRFactory.Api/Controllers/AgentPromptTemplatesController.cs`

RESTful API for managing templates:

```
GET    /api/agent-prompt-templates/system
GET    /api/agent-prompt-templates/tenant/{tenantId}
GET    /api/agent-prompt-templates/category/{category}?tenantId=...
GET    /api/agent-prompt-templates/{id}
GET    /api/agent-prompt-templates/by-name/{name}?tenantId=...
POST   /api/agent-prompt-templates
POST   /api/agent-prompt-templates/{id}/clone?tenantId=...
PUT    /api/agent-prompt-templates/{id}
DELETE /api/agent-prompt-templates/{id}
```

### 6. Database Changes

**Modified**: `src/PRFactory.Infrastructure/Persistence/ApplicationDbContext.cs`

Added `AgentPromptTemplates` DbSet and entity configuration.

### 7. Dependency Injection

**Modified**: `src/PRFactory.Infrastructure/DependencyInjection.cs`

Registered:
- `IAgentPromptTemplateRepository`
- `IAgentPromptService`
- `AgentPromptLoaderService`

### 8. Agent Prompts

Downloaded 6 agent prompt templates from ClaudeCodeSettings repository:

**`.claude/agents/`**:
- `code-implementation.md` - Full implementation specialist
- `evaluation-specialist.md` - Code quality evaluation
- `simple-code-implementation-agent.md` - Simplified implementation
- `test-analysis-specialist.md` - Test failure analysis
- `test-fix-specialist.md` - Test fixing specialist
- `test-runner-specialist.md` - Test execution specialist

**`.claude/commands/`**:
- `analyze-tests.md` - Test analysis workflow
- `create-feature.md` - Feature creation workflow
- `fix-bugs.md` - Bug fixing workflow

## Next Steps

### 1. Create Database Migration

```bash
cd src/PRFactory.Infrastructure
dotnet ef migrations add AddAgentPromptTemplates --context ApplicationDbContext
```

This will create the migration for the `AgentPromptTemplates` table.

### 2. Load Initial Prompts

Add a startup task or admin endpoint to load system templates:

```csharp
// In Program.cs or a startup service
using var scope = app.Services.CreateScope();
var loaderService = scope.ServiceProvider.GetRequiredService<AgentPromptLoaderService>();
var agentsPath = Path.Combine(Directory.GetCurrentDirectory(), ".claude", "agents");
var loadedCount = await loaderService.LoadAgentPromptsAsync(agentsPath);
```

Or create an admin API endpoint:

```csharp
[HttpPost("api/admin/load-agent-prompts")]
public async Task<IActionResult> LoadAgentPrompts([FromServices] AgentPromptLoaderService loader)
{
    var agentsPath = Path.Combine(Directory.GetCurrentDirectory(), ".claude", "agents");
    var count = await loader.LoadAgentPromptsAsync(agentsPath);
    return Ok(new { loadedCount = count });
}
```

### 3. Update Agents to Use Templates

Modify existing agents (e.g., `ImplementationAgent`, `PlanningAgent`) to use templates:

**Before**:
```csharp
var systemPrompt = @"You are an expert software developer...";
```

**After**:
```csharp
public class ImplementationAgent : BaseAgent
{
    private readonly IAgentPromptService _promptService;

    public ImplementationAgent(
        ILogger<ImplementationAgent> logger,
        IClaudeClient claudeClient,
        IContextBuilder contextBuilder,
        ITicketRepository ticketRepository,
        IAgentPromptService promptService)  // Add this
        : base(logger)
    {
        _promptService = promptService;
        // ... other dependencies
    }

    protected override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct)
    {
        // Get prompt from template (with fallback to default)
        var promptTemplate = await _promptService.GetPromptTemplateAsync(
            "code-implementation-specialist",
            context.TenantId,
            ct
        );

        var systemPrompt = promptTemplate?.PromptContent ??
            @"You are an expert software developer..."; // Fallback

        // Rest of implementation...
    }
}
```

### 4. UI Integration

The prompts should be available in the PRFactory UI as preselectable options:

#### For Tenant Configuration:
- Add a settings page to manage tenant-specific templates
- Allow cloning system templates for customization
- Allow creating new templates from scratch

#### For Workflow Stages:
- In the ticket/workflow UI, add dropdowns to select prompts per stage:
  - **Analysis Stage**: Select from "Analysis" category templates
  - **Planning Stage**: Select from "Planning" category templates
  - **Implementation Stage**: Select from "Implementation" category templates
  - **Testing Stage**: Select from "Testing" category templates

#### Example UI Component Structure:
```tsx
// Tenant Settings -> Agent Prompts
<AgentPromptManagement>
  <SystemTemplates />
  <TenantTemplates>
    <TemplateList />
    <TemplateEditor />
    <CloneFromSystem />
  </TenantTemplates>
</AgentPromptManagement>

// Workflow Configuration
<WorkflowStageConfig>
  <StagePromptSelector
    stage="Implementation"
    category="Implementation"
    selectedTemplate={selectedImplementationTemplate}
    onSelect={handleTemplateSelect}
  />
</WorkflowStageConfig>
```

### 5. Extend Tenant Configuration

Consider adding a field to store selected templates per stage:

```csharp
public class TenantConfiguration
{
    // Existing properties...

    /// <summary>
    /// Selected prompt template for each workflow stage
    /// </summary>
    public Dictionary<string, Guid> StagePromptTemplates { get; set; } = new()
    {
        ["Analysis"] = Guid.Empty,
        ["Planning"] = Guid.Empty,
        ["Implementation"] = Guid.Empty,
        ["Testing"] = Guid.Empty
    };
}
```

## Benefits

1. **Flexibility**: Tenants can customize AI behavior per workflow stage
2. **Consistency**: System templates ensure baseline quality
3. **Experimentation**: Easy to A/B test different prompts
4. **Reusability**: Templates can be shared and cloned
5. **Version Control**: Prompts stored in `.claude/agents` are version-controlled
6. **UI Integration**: Dropdown selection in UI for non-technical users

## Usage Examples

### Clone and Customize a System Template

```bash
POST /api/agent-prompt-templates/{systemTemplateId}/clone?tenantId={tenantId}
```

Then edit via:

```bash
PUT /api/agent-prompt-templates/{clonedTemplateId}
{
  "promptContent": "Your customized prompt...",
  "description": "Custom implementation prompt for our team"
}
```

### Get Templates for a Workflow Stage

```bash
GET /api/agent-prompt-templates/category/Implementation?tenantId={tenantId}
```

Returns all implementation templates (system + tenant-specific).

### Use in Agent Code

```csharp
var template = await _promptService.GetPromptTemplateAsync(
    "code-implementation-specialist",
    tenantId,
    cancellationToken
);

if (template != null)
{
    _logger.LogInformation(
        "Using prompt template: {Name} (recommended model: {Model})",
        template.Name,
        template.RecommendedModel
    );
}
```

## Architecture Notes

This implementation follows the PRFactory architecture principles:

✅ **Multi-Tenant**: Templates are tenant-aware with fallback to system defaults
✅ **Flexible**: Easy to add new categories and templates
✅ **Clean Architecture**: Domain entities separate from infrastructure
✅ **Extensible**: New prompt sources can be added (e.g., from UI, API, external systems)
✅ **Type-Safe**: Strongly-typed entities and DTOs

## File Locations

**Domain**:
- `src/PRFactory.Domain/Entities/AgentPromptTemplate.cs`
- `src/PRFactory.Domain/Interfaces/IAgentPromptTemplateRepository.cs`

**Infrastructure**:
- `src/PRFactory.Infrastructure/Persistence/Repositories/AgentPromptTemplateRepository.cs`
- `src/PRFactory.Infrastructure/Persistence/Configurations/AgentPromptTemplateConfiguration.cs`
- `src/PRFactory.Infrastructure/Agents/Services/AgentPromptLoaderService.cs`
- `src/PRFactory.Infrastructure/Agents/Services/AgentPromptService.cs`
- `src/PRFactory.Infrastructure/Agents/Services/IAgentPromptService.cs`

**API**:
- `src/PRFactory.Api/Controllers/AgentPromptTemplatesController.cs`

**Agent Prompts**:
- `.claude/agents/*.md`
- `.claude/commands/*.md`

## Dependencies Added

- **YamlDotNet** (v16.2.0): For parsing YAML frontmatter in markdown files

## Testing Checklist

- [ ] Run database migration
- [ ] Load initial prompts from `.claude/agents` folder
- [ ] Test API endpoints (GET, POST, PUT, DELETE)
- [ ] Test cloning system templates
- [ ] Test tenant-specific template retrieval
- [ ] Update at least one agent to use templates
- [ ] Verify template selection in UI (when implemented)
- [ ] Test with multiple tenants
- [ ] Verify system templates cannot be modified/deleted

## Questions?

See:
- [CLAUDE.md](../CLAUDE.md) - Architecture vision
- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed architecture
- [WORKFLOW.md](WORKFLOW.md) - Workflow details
