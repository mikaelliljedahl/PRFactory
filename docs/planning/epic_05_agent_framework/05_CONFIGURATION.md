# 05: Database-Driven Agent Configuration

**Document Purpose:** Complete specification for database-driven agent configuration system, including schema, seed data, admin UI, and validation.

**Last Updated:** 2025-11-13

---

## Table of Contents

- [Database Schema](#database-schema)
- [Seed Data & Migrations](#seed-data--migrations)
- [Admin UI Specification](#admin-ui-specification)
- [Configuration Validation](#configuration-validation)
- [Tenant Configuration Model](#tenant-configuration-model)
- [Runtime Agent Creation](#runtime-agent-creation)

---

## Database Schema

### AgentConfiguration Table

**Full Schema:**
```sql
CREATE TABLE AgentConfigurations (
    -- Primary Key
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Multi-Tenant Isolation
    TenantId UNIQUEIDENTIFIER NOT NULL,
    
    -- Agent Identity
    AgentName NVARCHAR(100) NOT NULL,
    DisplayName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    
    -- Agent Behavior
    Instructions NVARCHAR(MAX) NOT NULL,  -- System prompt/persona
    EnabledTools NVARCHAR(MAX) NOT NULL,  -- JSON array: ["ReadFile", "Grep", ...]
    MaxTokens INT NOT NULL DEFAULT 8000,
    Temperature FLOAT NOT NULL DEFAULT 0.3,
    
    -- UI Settings
    StreamingEnabled BIT NOT NULL DEFAULT 1,
    ShowReasoningSteps BIT NOT NULL DEFAULT 1,
    
    -- Approval Settings
    RequiresApproval BIT NOT NULL DEFAULT 0,
    ApprovalPolicy NVARCHAR(50) NULL,  -- 'All', 'WriteOnly', 'None'
    
    -- Resource Limits
    MaxToolCallsPerRun INT NOT NULL DEFAULT 100,
    MaxExecutionTimeSeconds INT NOT NULL DEFAULT 300,  -- 5 minutes
    
    -- Feature Flags
    IsEnabled BIT NOT NULL DEFAULT 1,
    IsDefault BIT NOT NULL DEFAULT 0,  -- Default for tenant if multiple configs
    
    -- Audit
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(100) NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    
    -- Constraints
    CONSTRAINT FK_AgentConfig_Tenant 
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_AgentConfig_TenantAgent 
        UNIQUE (TenantId, AgentName),
    CONSTRAINT CK_AgentConfig_Temperature 
        CHECK (Temperature >= 0.0 AND Temperature <= 1.0),
    CONSTRAINT CK_AgentConfig_MaxTokens 
        CHECK (MaxTokens > 0 AND MaxTokens <= 200000)
);

-- Indexes
CREATE INDEX IX_AgentConfig_TenantId 
    ON AgentConfigurations(TenantId);
    
CREATE INDEX IX_AgentConfig_TenantEnabled 
    ON AgentConfigurations(TenantId, IsEnabled) 
    WHERE IsEnabled = 1;
```

### AgentExecutionLog Table

**Full Schema:**
```sql
CREATE TABLE AgentExecutionLogs (
    -- Primary Key
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Multi-Tenant Isolation
    TenantId UNIQUEIDENTIFIER NOT NULL,
    
    -- Context
    TicketId UNIQUEIDENTIFIER NOT NULL,
    CheckpointId UNIQUEIDENTIFIER NULL,
    
    -- Agent Information
    AgentConfigId UNIQUEIDENTIFIER NOT NULL,
    AgentName NVARCHAR(100) NOT NULL,
    
    -- Tool Information
    ToolName NVARCHAR(100) NULL,  -- NULL if no tool used
    ToolInput NVARCHAR(MAX) NULL,
    ToolOutput NVARCHAR(MAX) NULL,
    
    -- Token Usage
    InputTokens INT NOT NULL DEFAULT 0,
    OutputTokens INT NOT NULL DEFAULT 0,
    TotalTokens AS (InputTokens + OutputTokens) PERSISTED,
    
    -- Performance
    DurationMs INT NOT NULL,
    
    -- Status
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    ErrorType NVARCHAR(100) NULL,  -- 'Timeout', 'Security', 'Tool', 'LLM'
    
    -- Audit
    ExecutedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Constraints
    CONSTRAINT FK_AgentLog_Tenant 
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AgentLog_Ticket 
        FOREIGN KEY (TicketId) REFERENCES Tickets(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AgentLog_AgentConfig 
        FOREIGN KEY (AgentConfigId) REFERENCES AgentConfigurations(Id) ON DELETE NO ACTION
);

-- Indexes for Performance
CREATE INDEX IX_AgentLog_TenantId 
    ON AgentExecutionLogs(TenantId);
    
CREATE INDEX IX_AgentLog_TicketId 
    ON AgentExecutionLogs(TicketId);
    
CREATE INDEX IX_AgentLog_ExecutedAt 
    ON AgentExecutionLogs(ExecutedAt DESC);
    
CREATE INDEX IX_AgentLog_TenantTicket 
    ON AgentExecutionLogs(TenantId, TicketId, ExecutedAt DESC);
```

### Checkpoint Table Extension

**Add columns to existing Checkpoint table:**
```sql
-- Migration: Extend Checkpoint for Agent Framework
ALTER TABLE Checkpoints ADD AgentThreadId NVARCHAR(200) NULL;
ALTER TABLE Checkpoints ADD ConversationHistory NVARCHAR(MAX) NULL;
ALTER TABLE Checkpoints ADD AgentState NVARCHAR(MAX) NULL;

CREATE INDEX IX_Checkpoint_AgentThread 
    ON Checkpoints(AgentThreadId) 
    WHERE AgentThreadId IS NOT NULL;
```

---

## Seed Data & Migrations

### Migration Script: 20250113_AddAgentFramework.sql

```sql
-- =============================================================================
-- Migration: 20250113_AddAgentFramework
-- Description: Add Agent Framework support (AgentConfigurations, Execution Logs)
-- Dependencies: Tenants table must exist
-- =============================================================================

BEGIN TRANSACTION;

-- 1. Create AgentConfigurations table
CREATE TABLE AgentConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    AgentName NVARCHAR(100) NOT NULL,
    DisplayName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    Instructions NVARCHAR(MAX) NOT NULL,
    EnabledTools NVARCHAR(MAX) NOT NULL,
    MaxTokens INT NOT NULL DEFAULT 8000,
    Temperature FLOAT NOT NULL DEFAULT 0.3,
    StreamingEnabled BIT NOT NULL DEFAULT 1,
    ShowReasoningSteps BIT NOT NULL DEFAULT 1,
    RequiresApproval BIT NOT NULL DEFAULT 0,
    ApprovalPolicy NVARCHAR(50) NULL,
    MaxToolCallsPerRun INT NOT NULL DEFAULT 100,
    MaxExecutionTimeSeconds INT NOT NULL DEFAULT 300,
    IsEnabled BIT NOT NULL DEFAULT 1,
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(100) NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    
    CONSTRAINT FK_AgentConfig_Tenant 
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_AgentConfig_TenantAgent 
        UNIQUE (TenantId, AgentName),
    CONSTRAINT CK_AgentConfig_Temperature 
        CHECK (Temperature >= 0.0 AND Temperature <= 1.0),
    CONSTRAINT CK_AgentConfig_MaxTokens 
        CHECK (MaxTokens > 0 AND MaxTokens <= 200000)
);

CREATE INDEX IX_AgentConfig_TenantId ON AgentConfigurations(TenantId);
CREATE INDEX IX_AgentConfig_TenantEnabled ON AgentConfigurations(TenantId, IsEnabled) WHERE IsEnabled = 1;

-- 2. Create AgentExecutionLogs table
CREATE TABLE AgentExecutionLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    TicketId UNIQUEIDENTIFIER NOT NULL,
    CheckpointId UNIQUEIDENTIFIER NULL,
    AgentConfigId UNIQUEIDENTIFIER NOT NULL,
    AgentName NVARCHAR(100) NOT NULL,
    ToolName NVARCHAR(100) NULL,
    ToolInput NVARCHAR(MAX) NULL,
    ToolOutput NVARCHAR(MAX) NULL,
    InputTokens INT NOT NULL DEFAULT 0,
    OutputTokens INT NOT NULL DEFAULT 0,
    TotalTokens AS (InputTokens + OutputTokens) PERSISTED,
    DurationMs INT NOT NULL,
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    ErrorType NVARCHAR(100) NULL,
    ExecutedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_AgentLog_Tenant 
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AgentLog_Ticket 
        FOREIGN KEY (TicketId) REFERENCES Tickets(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AgentLog_AgentConfig 
        FOREIGN KEY (AgentConfigId) REFERENCES AgentConfigurations(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_AgentLog_TenantId ON AgentExecutionLogs(TenantId);
CREATE INDEX IX_AgentLog_TicketId ON AgentExecutionLogs(TicketId);
CREATE INDEX IX_AgentLog_ExecutedAt ON AgentExecutionLogs(ExecutedAt DESC);
CREATE INDEX IX_AgentLog_TenantTicket ON AgentExecutionLogs(TenantId, TicketId, ExecutedAt DESC);

-- 3. Extend Checkpoints table
ALTER TABLE Checkpoints ADD AgentThreadId NVARCHAR(200) NULL;
ALTER TABLE Checkpoints ADD ConversationHistory NVARCHAR(MAX) NULL;
ALTER TABLE Checkpoints ADD AgentState NVARCHAR(MAX) NULL;

CREATE INDEX IX_Checkpoint_AgentThread ON Checkpoints(AgentThreadId) WHERE AgentThreadId IS NOT NULL;

COMMIT TRANSACTION;
GO
```

### Seed Data: Default Agent Configurations

```sql
-- =============================================================================
-- Seed Data: Default Agent Configurations for All Tenants
-- Run after migration: 20250113_AddAgentFramework
-- =============================================================================

-- Insert default configurations for each existing tenant
INSERT INTO AgentConfigurations (
    TenantId, 
    AgentName, 
    DisplayName, 
    Description, 
    Instructions, 
    EnabledTools, 
    MaxTokens, 
    Temperature, 
    RequiresApproval, 
    ApprovalPolicy,
    IsEnabled,
    IsDefault
)
SELECT 
    t.Id AS TenantId,
    'AnalyzerAgent' AS AgentName,
    'Analyzer Agent' AS DisplayName,
    'Analyzes codebases for impact, dependencies, and risks' AS Description,
    'You are a senior software architect analyzing codebases for impact assessment. Be thorough but concise. Always identify risks and dependencies.

Steps:
1. Use GetJiraTicket to fetch full ticket details
2. Use CodeSearch to find related classes/methods
3. Use Grep to search for relevant code patterns
4. Use ReadFile to examine key files

Provide:
- Impact assessment (Low/Medium/High)
- List of affected files
- Technical dependencies
- Implementation risks
- Clarifying questions for user (if requirements unclear)' AS Instructions,
    '["ReadFile","Grep","Glob","CodeSearch","GetJiraTicket"]' AS EnabledTools,
    8000 AS MaxTokens,
    0.3 AS Temperature,
    0 AS RequiresApproval,
    'None' AS ApprovalPolicy,
    1 AS IsEnabled,
    1 AS IsDefault
FROM Tenants t;

-- PlannerAgent
INSERT INTO AgentConfigurations (
    TenantId, AgentName, DisplayName, Description, Instructions, EnabledTools, 
    MaxTokens, Temperature, RequiresApproval, ApprovalPolicy, IsEnabled, IsDefault
)
SELECT 
    t.Id,
    'PlannerAgent',
    'Planner Agent',
    'Creates detailed implementation plans with task decomposition',
    'You are a technical lead creating implementation plans. Break work into small, testable tasks. Identify risks and dependencies. Provide realistic estimates.

Review refined requirements and analysis, then:
1. Decompose work into discrete tasks
2. Estimate effort and complexity per task
3. Identify implementation order and dependencies
4. Assess technical risks

Output format (JSON):
{
  "tasks": [...],
  "implementationApproach": "...",
  "testingStrategy": "...",
  "estimatedTotalEffort": "..."
}',
    '["ReadFile","Grep","CodeSearch","GetJiraTicket"]',
    8000,
    0.3,
    1,  -- Requires approval
    'All',
    1,
    1
FROM Tenants t;

-- CodeExecutorAgent
INSERT INTO AgentConfigurations (
    TenantId, AgentName, DisplayName, Description, Instructions, EnabledTools, 
    MaxTokens, Temperature, RequiresApproval, ApprovalPolicy, IsEnabled, IsDefault
)
SELECT 
    t.Id,
    'CodeExecutorAgent',
    'Code Executor Agent',
    'Executes implementation plans with code generation and testing',
    'You are a senior software engineer implementing code changes. Follow the approved plan exactly. Write clean, tested code. Run tests after changes. Commit with clear messages.

For each task:
1. Use ReadFile to read existing code
2. Generate updated code
3. Use WriteFile to save changes (atomic)
4. Use RunTests to validate
5. If tests fail, analyze and fix
6. Use GitCommit when task complete',
    '["ReadFile","WriteFile","Grep","Glob","CodeSearch","ExecuteShell","RunTests","GitCommit"]',
    16000,  -- Higher token limit for code generation
    0.2,    -- Lower temperature for determinism
    1,      -- Requires approval for writes
    'WriteOnly',
    1,
    1
FROM Tenants t;

-- ReviewerAgent
INSERT INTO AgentConfigurations (
    TenantId, AgentName, DisplayName, Description, Instructions, EnabledTools, 
    MaxTokens, Temperature, RequiresApproval, ApprovalPolicy, IsEnabled, IsDefault
)
SELECT 
    t.Id,
    'ReviewerAgent',
    'Reviewer Agent',
    'Reviews code for quality, security, and best practices',
    'You are a senior code reviewer focusing on security, quality, and best practices. Be constructive and specific. Identify risks and suggest improvements.

Review categories:
- Security: SQL injection, XSS, auth issues, credentials, directory traversal
- Quality: Readability, naming, error handling, edge cases, performance
- Testing: Unit tests exist, coverage adequate (80%+), edge cases tested

Provide structured feedback with severity levels (Critical/Major/Minor).',
    '["ReadFile","Grep","GetGitDiff","GetJiraTicket"]',
    8000,
    0.3,
    0,  -- No approval needed for read-only review
    'None',
    1,
    1
FROM Tenants t;

GO
```

### Rollback Script

```sql
-- =============================================================================
-- Rollback: 20250113_AddAgentFramework
-- WARNING: This will delete all agent configurations and execution logs!
-- =============================================================================

BEGIN TRANSACTION;

-- Drop indexes
DROP INDEX IF EXISTS IX_Checkpoint_AgentThread ON Checkpoints;
DROP INDEX IF EXISTS IX_AgentLog_TenantTicket ON AgentExecutionLogs;
DROP INDEX IF EXISTS IX_AgentLog_ExecutedAt ON AgentExecutionLogs;
DROP INDEX IF EXISTS IX_AgentLog_TicketId ON AgentExecutionLogs;
DROP INDEX IF EXISTS IX_AgentLog_TenantId ON AgentExecutionLogs;
DROP INDEX IF EXISTS IX_AgentConfig_TenantEnabled ON AgentConfigurations;
DROP INDEX IF EXISTS IX_AgentConfig_TenantId ON AgentConfigurations;

-- Drop tables
DROP TABLE IF EXISTS AgentExecutionLogs;
DROP TABLE IF EXISTS AgentConfigurations;

-- Remove columns from Checkpoints
ALTER TABLE Checkpoints DROP COLUMN IF EXISTS AgentThreadId;
ALTER TABLE Checkpoints DROP COLUMN IF EXISTS ConversationHistory;
ALTER TABLE Checkpoints DROP COLUMN IF EXISTS AgentState;

COMMIT TRANSACTION;
GO
```

---

## Admin UI Specification

### Page: `/admin/agent-configuration`

**Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│  Agent Configuration Management                              │
├─────────────────────────────────────────────────────────────┤
│  Tenant: [Acme Corp ▼]                    [+ New Agent]     │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ AnalyzerAgent                  ✓ Enabled   [Edit]    │  │
│  │ Analyzes codebases for impact...                      │  │
│  │ Tools: ReadFile, Grep, Glob, CodeSearch              │  │
│  │ Max Tokens: 8000  |  Temperature: 0.3                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ PlannerAgent                   ✓ Enabled   [Edit]    │  │
│  │ Creates implementation plans...                       │  │
│  │ Tools: ReadFile, Grep, CodeSearch                    │  │
│  │ Max Tokens: 8000  |  Temperature: 0.3                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ CodeExecutorAgent              ✓ Enabled   [Edit]    │  │
│  │ Executes code implementation...                       │  │
│  │ Tools: ReadFile, WriteFile, GitCommit (8 total)      │  │
│  │ Max Tokens: 16000  |  Temperature: 0.2  ⚠️ Approval  │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ ReviewerAgent                  ✓ Enabled   [Edit]    │  │
│  │ Reviews code quality...                               │  │
│  │ Tools: ReadFile, Grep, GetGitDiff                    │  │
│  │ Max Tokens: 8000  |  Temperature: 0.3                │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Edit Agent Configuration Modal

```razor
<Modal Title="Edit Agent: @AgentName" Size="Large">
    <EditForm Model="@config" OnValidSubmit="SaveConfiguration">
        <DataAnnotationsValidator />
        
        <Tabs>
            <Tab Title="Basic Settings">
                <FormField Label="Display Name">
                    <InputText @bind-Value="config.DisplayName" class="form-control" />
                </FormField>
                
                <FormField Label="Description">
                    <InputTextArea @bind-Value="config.Description" class="form-control" rows="2" />
                </FormField>
                
                <FormField Label="System Instructions">
                    <InputTextArea @bind-Value="config.Instructions" class="form-control" rows="10" />
                    <small class="text-muted">This is the agent's persona and behavior guidelines</small>
                </FormField>
            </Tab>
            
            <Tab Title="Tools & Permissions">
                <h6>Select Enabled Tools:</h6>
                <div class="row">
                    @foreach (var tool in AllAvailableTools)
                    {
                        <div class="col-md-6 mb-2">
                            <div class="form-check">
                                <input type="checkbox" 
                                       class="form-check-input" 
                                       id="tool-@tool.Name"
                                       checked="@IsToolEnabled(tool.Name)"
                                       @onchange="e => ToggleTool(tool.Name, e.Value)" />
                                <label class="form-check-label" for="tool-@tool.Name">
                                    <strong>@tool.Name</strong>
                                    <br /><small class="text-muted">@tool.Description</small>
                                </label>
                            </div>
                        </div>
                    }
                </div>
            </Tab>
            
            <Tab Title="LLM Settings">
                <FormField Label="Max Tokens">
                    <InputNumber @bind-Value="config.MaxTokens" class="form-control" />
                    <small class="text-muted">Maximum tokens per agent execution (1-200000)</small>
                </FormField>
                
                <FormField Label="Temperature">
                    <InputNumber @bind-Value="config.Temperature" class="form-control" step="0.1" />
                    <small class="text-muted">0.0 = deterministic, 1.0 = creative (0.0-1.0)</small>
                </FormField>
                
                <div class="form-check">
                    <InputCheckbox @bind-Value="config.StreamingEnabled" class="form-check-input" id="streaming" />
                    <label class="form-check-label" for="streaming">
                        Enable streaming responses (real-time updates)
                    </label>
                </div>
            </Tab>
            
            <Tab Title="Approval & Limits">
                <div class="form-check mb-3">
                    <InputCheckbox @bind-Value="config.RequiresApproval" class="form-check-input" id="approval" />
                    <label class="form-check-label" for="approval">
                        Require human approval for actions
                    </label>
                </div>
                
                @if (config.RequiresApproval)
                {
                    <FormField Label="Approval Policy">
                        <InputSelect @bind-Value="config.ApprovalPolicy" class="form-control">
                            <option value="All">Approve all actions</option>
                            <option value="WriteOnly">Approve write operations only</option>
                            <option value="None">No approval required</option>
                        </InputSelect>
                    </FormField>
                }
                
                <FormField Label="Max Tool Calls Per Run">
                    <InputNumber @bind-Value="config.MaxToolCallsPerRun" class="form-control" />
                    <small class="text-muted">Prevent infinite loops (1-1000)</small>
                </FormField>
                
                <FormField Label="Max Execution Time (seconds)">
                    <InputNumber @bind-Value="config.MaxExecutionTimeSeconds" class="form-control" />
                    <small class="text-muted">Timeout for agent execution (1-600)</small>
                </FormField>
            </Tab>
        </Tabs>
        
        <div class="d-flex gap-2 mt-3">
            <LoadingButton Type="submit" Icon="check">Save Configuration</LoadingButton>
            <button type="button" class="btn btn-secondary" @onclick="Cancel">Cancel</button>
        </div>
    </EditForm>
</Modal>
```

---

## Configuration Validation

### Validation Rules

**C# Model with Data Annotations:**
```csharp
public class AgentConfiguration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    [Required]
    [StringLength(100)]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Agent name must be alphanumeric")]
    public string AgentName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MinLength(50, ErrorMessage = "Instructions must be at least 50 characters")]
    public string Instructions { get; set; } = string.Empty;
    
    [Required]
    [ValidateToolNames]
    public string[] EnabledTools { get; set; } = Array.Empty<string>();
    
    [Range(1, 200000)]
    public int MaxTokens { get; set; } = 8000;
    
    [Range(0.0, 1.0)]
    public float Temperature { get; set; } = 0.3f;
    
    [Range(1, 1000)]
    public int MaxToolCallsPerRun { get; set; } = 100;
    
    [Range(1, 600)]
    public int MaxExecutionTimeSeconds { get; set; } = 300;
    
    // ... other properties
}
```

**Custom Validator for Tool Names:**
```csharp
public class ValidateToolNamesAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string[] toolNames)
            return new ValidationResult("EnabledTools must be a string array");
        
        var toolRegistry = (IToolRegistry)validationContext.GetService(typeof(IToolRegistry))!;
        var validTools = toolRegistry.GetAllTools().Select(t => t.Name).ToHashSet();
        
        var invalidTools = toolNames.Except(validTools).ToList();
        
        if (invalidTools.Any())
        {
            return new ValidationResult(
                $"Invalid tool names: {string.Join(", ", invalidTools)}. " +
                $"Valid tools: {string.Join(", ", validTools)}");
        }
        
        return ValidationResult.Success;
    }
}
```

---

## Tenant Configuration Model

### Configuration Inheritance

**Default Configuration (System-Wide):**
```json
{
  "system": {
    "defaultMaxTokens": 8000,
    "defaultTemperature": 0.3,
    "defaultApprovalPolicy": "None",
    "allowedTools": ["all"],
    "maxToolCallsPerRun": 100
  }
}
```

**Tenant-Level Override:**
```
Tenant: Acme Corp
├── AnalyzerAgent (custom instructions, custom tools)
├── PlannerAgent (custom instructions, default tools)
├── CodeExecutorAgent (DISABLED - tenant preference)
└── ReviewerAgent (default configuration)
```

**Fallback Logic:**
```csharp
public async Task<AgentConfiguration> GetEffectiveConfigurationAsync(
    Guid tenantId, string agentName)
{
    // 1. Try tenant-specific configuration
    var tenantConfig = await _repo.GetByAgentNameAsync(tenantId, agentName);
    if (tenantConfig != null && tenantConfig.IsEnabled)
        return tenantConfig;
    
    // 2. Fall back to system default
    var systemConfig = await _repo.GetSystemDefaultAsync(agentName);
    if (systemConfig != null)
        return systemConfig;
    
    // 3. No configuration found
    throw new AgentConfigurationNotFoundException(tenantId, agentName);
}
```

---

## Runtime Agent Creation

**See 01_ARCHITECTURE.md for AgentFactory implementation details.**

**Quick Reference:**
```csharp
// Load configuration from database
var config = await _configService.GetConfigurationAsync(tenantId, "AnalyzerAgent");

// Get tools based on config
var tools = _toolRegistry.GetTools(tenantId, config.EnabledTools);

// Create agent with config
var agent = _chatClient.CreateAIAgent(
    instructions: config.Instructions,
    tools: tools,
    maxTokens: config.MaxTokens,
    temperature: config.Temperature);

// Apply middleware
return agent
    .WithMiddleware(TenantIsolationMiddleware)
    .WithMiddleware(TokenBudgetMiddleware)
    .WithMiddleware(AuditLoggingMiddleware);
```

---

## Next Steps

1. **Run migration** - Apply 20250113_AddAgentFramework.sql to database
2. **Seed default configs** - Run seed data script for all tenants
3. **Implement Admin UI** - Create `/admin/agent-configuration` page
4. **Test configuration loading** - Verify AgentFactory reads from database
5. **User acceptance testing** - Validate UI with tenant admins

**See:** `01_ARCHITECTURE.md` for AgentFactory details and `04_IMPLEMENTATION_ROADMAP.md` for timeline.
