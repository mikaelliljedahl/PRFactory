# PRFactory Database Schema

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                          Tenants                             │
├─────────────────────────────────────────────────────────────┤
│ Id                    Guid       PK                          │
│ Name                  string     UNIQUE                      │
│ JiraUrl               string                                 │
│ JiraApiToken          string     ENCRYPTED                   │
│ ClaudeApiKey          string     ENCRYPTED                   │
│ IsActive              bool                                   │
│ CreatedAt             DateTime                               │
│ UpdatedAt             DateTime?                              │
│ Configuration         JSON                                   │
│   - AutoImplementAfterPlanApproval                           │
│   - MaxRetries                                               │
│   - ClaudeModel                                              │
│   - MaxTokensPerRequest                                      │
│   - ApiTimeoutSeconds                                        │
│   - EnableVerboseLogging                                     │
│   - CustomPromptTemplates                                    │
└─────────────────────────────────────────────────────────────┘
   │         │                    │              │
   │ 1       │ 1                  │ 1            │ 1
   │         │                    │              │
   │ *       │ *                  │ *            │ *
   │         │                    │              │
   ▼         ▼                    ▼              ▼
  ┌──────────────────┐  ┌────────────┐  ┌──────────────────┐  ┌──────────────────────┐
  │  AgentConfig     │  │Repositories│  │     Tickets      │  │ TenantLlmProviders   │
  │  (Epic 05)       │  │            │  │                  │  │ (Epic 06)            │
  │  [FK: TenantId]  │  │[FK:TenantId]  │ [FK: TenantId]   │  │ [FK: TenantId]       │
  │                  │  │            │  │ [FK: RepoId]     │  │                      │
  └──────────────────┘  │            │  │ [FK: LlmProv...]─┼──┘                      │
                        │            │  │                  │                          │
                        └────────────┘  │                  │                          │
                                        │                  │                          │
                                        ▼                  ▼                          │
                               ┌────────────────────────────────────────────────┐    │
                               │           WorkflowEvents                       │    │
                               │           (TPH Inheritance)                    │    │
                               ├────────────────────────────────────────────────┤    │
                               │ Id              Guid       PK                  │    │
                               │ TicketId        Guid       FK                  │    │
                               │ OccurredAt      DateTime                       │    │
                               │ EventType       string (discriminator)         │    │
                               ├────────────────────────────────────────────────┤    │
                               │ [WorkflowStateChanged, QuestionAdded, etc.]    │    │
                               └────────────────────────────────────────────────┘    │
                                                      │                              │
                                                      ▼                              │
                               ┌────────────────────────────────────────────────┐    │
                               │              PlanReviews                       │◀───┘
                               ├────────────────────────────────────────────────┤
                               │ Id              Guid       PK                  │
                               │ TicketId        Guid       FK                  │
                               │ ReviewStatus    string                         │
                               │ CreatedAt       DateTime                       │
                               │ UpdatedAt       DateTime?                      │
                               └────────────────────────────────────────────────┘
                                         │
                                         │ 1
                                         │ *
                                         ▼
                        ┌────────────────────────────────────┐
                        │      ReviewChecklists              │
                        │         (Epic 07)                  │
                        ├────────────────────────────────────┤
                        │ Id           Guid        PK        │
                        │ PlanReviewId Guid        FK        │
                        │ TemplateName string              │
                        │ CreatedAt    DateTime             │
                        └────────────────────────────────────┘
                                    │
                                    │ 1
                                    │ *
                                    ▼
                        ┌────────────────────────────────────┐
                        │       ChecklistItems               │
                        │         (Epic 07)                  │
                        ├────────────────────────────────────┤
                        │ Id           Guid        PK        │
                        │ ChecklistId  Guid        FK        │
                        │ Category     string                │
                        │ Title        string                │
                        │ Description  string                │
                        │ Severity     string (enum)         │
                        │ IsChecked    bool                  │
                        │ CheckedAt    DateTime?             │
                        │ SortOrder    int                   │
                        └────────────────────────────────────┘

                        ┌────────────────────────────────────┐
                        │    InlineCommentAnchors            │
                        │         (Epic 07)                  │
                        ├────────────────────────────────────┤
                        │ Id              Guid       PK      │
                        │ ReviewCommentId Guid       FK      │
                        │ StartLine       int                │
                        │ EndLine         int                │
                        │ TextSnippet     string (200 max)   │
                        │ CreatedAt       DateTime           │
                        └────────────────────────────────────┘
```

## Relationships

### Tenant → Repositories (1:N)
- **Type**: One-to-Many
- **Delete Behavior**: Cascade
- **Description**: Each tenant can have multiple repositories. When a tenant is deleted, all its repositories are deleted.

### Tenant → Tickets (1:N)
- **Type**: One-to-Many
- **Delete Behavior**: Restrict
- **Description**: Each tenant can have multiple tickets. Tickets are protected from cascade deletion.

### Repository → Tickets (1:N)
- **Type**: One-to-Many
- **Delete Behavior**: Restrict
- **Description**: Each repository can have multiple tickets. Tickets are protected from cascade deletion.

### Ticket → WorkflowEvents (1:N)
- **Type**: One-to-Many
- **Delete Behavior**: Cascade
- **Description**: Each ticket has multiple workflow events tracking its lifecycle. When a ticket is deleted, all its events are deleted.

### Tenant → AgentConfiguration (1:N) [Epic 05]
- **Type**: One-to-Many
- **Delete Behavior**: Cascade
- **Description**: Each tenant can have multiple agent configurations. Agents are tenant-isolated for configuration and permission control. When a tenant is deleted, all their agent configurations are deleted.

### Tenant → TenantLlmProvider (1:N) [Epic 06]
- **Type**: One-to-Many
- **Delete Behavior**: Cascade
- **Description**: Each tenant can have multiple LLM provider configurations for multi-provider support. Enables switching between Claude, Z.ai, Minimax M2, OpenRouter, Together AI, etc.

### Ticket → TenantLlmProvider (N:1) [Epic 06]
- **Type**: Many-to-One
- **Delete Behavior**: Set Null
- **Description**: A ticket can be associated with a specific LLM provider, allowing different tickets to use different providers. Null means the tenant's default provider is used.

### PlanReview → ReviewChecklist (1:N) [Epic 07]
- **Type**: One-to-Many
- **Delete Behavior**: Cascade
- **Description**: Each plan review can have one or more checklists (for different review templates). When a plan review is deleted, all its checklists are deleted.

### ReviewChecklist → ChecklistItem (1:N) [Epic 07]
- **Type**: One-to-Many
- **Delete Behavior**: Cascade
- **Description**: Each checklist contains multiple items representing review criteria. Items are maintained in sort order.

### ReviewComment → InlineCommentAnchor (1:1) [Epic 07]
- **Type**: One-to-One
- **Delete Behavior**: Cascade
- **Description**: Each review comment can have an anchor linking it to specific lines in a plan/document. Enables inline commenting on implementation details.

## Indexes

### Tenants
- `IX_Tenants_Name` - UNIQUE index on Name
- `IX_Tenants_IsActive` - Index on IsActive for filtering active tenants

### Repositories
- `IX_Repositories_TenantId` - Index for tenant-based queries
- `IX_Repositories_CloneUrl` - UNIQUE index on CloneUrl
- `IX_Repositories_GitPlatform` - Index for platform-based queries

### Tickets
- `IX_Tickets_TicketKey` - UNIQUE index on TicketKey
- `IX_Tickets_TenantId` - Index for tenant-based queries
- `IX_Tickets_RepositoryId` - Index for repository-based queries
- `IX_Tickets_State` - Index for state-based queries
- `IX_Tickets_State_TenantId` - Composite index for state + tenant queries
- `IX_Tickets_CreatedAt` - Index for date-based queries

### WorkflowEvents
- `IX_WorkflowEvents_TicketId` - Index for ticket event history
- `IX_WorkflowEvents_OccurredAt` - Index for temporal queries
- `IX_WorkflowEvents_EventType` - Index for event type filtering

### AgentConfiguration [Epic 05]
- `IX_AgentConfigurations_TenantId` - Index for tenant-based queries
- `IX_AgentConfigurations_TenantId_AgentName` - UNIQUE composite index ensuring one config per agent per tenant

### TenantLlmProvider [Epic 06]
- `IX_TenantLlmProviders_TenantId` - Index for tenant-based queries
- `IX_TenantLlmProviders_ProviderType` - Index for provider type filtering
- `IX_TenantLlmProviders_TenantId_IsDefault` - Composite index for finding default provider per tenant
- `IX_TenantLlmProviders_TenantId_IsActive` - Composite index for querying active providers per tenant

### ReviewChecklist [Epic 07]
- `IX_ReviewChecklists_PlanReviewId` - UNIQUE index linking one checklist per plan review

### ChecklistItem [Epic 07]
- `IX_ChecklistItems_ReviewChecklistId` - Index for items within a checklist
- `IX_ChecklistItems_ReviewChecklistId_SortOrder` - Composite index for ordered item retrieval

### InlineCommentAnchor [Epic 07]
- `IX_InlineCommentAnchors_ReviewCommentId` - UNIQUE index for one-to-one relationship with ReviewComment
- `IX_InlineCommentAnchors_LineRange` - Composite index on StartLine/EndLine for range queries

## JSON Fields

### Tenant.Configuration
Stored as JSON with the following structure:
```json
{
  "AutoImplementAfterPlanApproval": false,
  "MaxRetries": 3,
  "ClaudeModel": "claude-sonnet-4-5-20250929",
  "MaxTokensPerRequest": 8000,
  "ApiTimeoutSeconds": 300,
  "EnableVerboseLogging": false,
  "CustomPromptTemplates": {}
}
```

### Ticket.Questions
Array of questions stored as JSON:
```json
[
  {
    "Id": "guid-string",
    "Text": "Question text here?",
    "Category": "requirements",
    "CreatedAt": "2025-11-04T12:00:00Z"
  }
]
```

### Ticket.Answers
Array of answers stored as JSON:
```json
[
  {
    "QuestionId": "guid-string",
    "Text": "Answer text here",
    "AnsweredAt": "2025-11-04T12:30:00Z"
  }
]
```

### Ticket.Metadata
Dictionary stored as JSON:
```json
{
  "customKey1": "value1",
  "customKey2": 123,
  "customKey3": true
}
```

## Encrypted Fields

The following fields are encrypted at rest using AES-256-GCM:

1. **Tenant.JiraApiToken** - Jira API authentication token
2. **Tenant.ClaudeApiKey** - Claude API key for AI operations
3. **Repository.AccessToken** - Git repository personal access token

All encrypted fields are stored as Base64-encoded strings with embedded nonce and authentication tag.

## Workflow States

Tickets can be in one of the following states:

```
Triggered → Analyzing → QuestionsPosted → AwaitingAnswers → AnswersReceived
    ↓                                                              ↓
  Failed                                                        Planning
                                                                   ↓
                                            PlanRejected ← PlanPosted → PlanUnderReview
                                                  ↑                            ↓
                                                  └────────────────────── PlanApproved
                                                                               ↓
                                                                          Implementing
                                                                          ↙         ↘
                                                             ImplementationFailed  PRCreated
                                                                    ↓                  ↓
                                                                  Failed           InReview
                                                                                       ↓
                                                                                  Completed
```

Terminal states: **Completed**, **Cancelled**, **Failed**

## Sample Queries

### Get all active tickets for a tenant
```sql
SELECT * FROM Tickets
WHERE TenantId = ?
  AND State NOT IN ('Completed', 'Cancelled', 'Failed')
ORDER BY UpdatedAt DESC;
```

### Get repositories by platform
```sql
SELECT * FROM Repositories
WHERE GitPlatform = ?
ORDER BY Name;
```

### Get workflow event history for a ticket
```sql
SELECT * FROM WorkflowEvents
WHERE TicketId = ?
ORDER BY OccurredAt ASC;
```

### Get state distribution for a tenant
```sql
SELECT State, COUNT(*) as Count
FROM Tickets
WHERE TenantId = ?
GROUP BY State;
```

## New Entities (Epic 05, 06, 07)

### AgentConfiguration [Epic 05 - Agent System Configuration]

**Purpose**: Stores tenant-specific agent configuration instead of relying on appsettings-based configuration. Enables per-tenant agent customization.

**Table**: `AgentConfigurations`

**Schema**:
```
Id                  Guid        PRIMARY KEY
TenantId            Guid        FOREIGN KEY -> Tenants(Id) CASCADE
AgentName           string      (max 100) - e.g., "AnalyzerAgent", "PlannerAgent"
Instructions        string      System prompt/instructions for the agent
EnabledTools        string      JSON array of enabled tool names
MaxTokens           int         Default: 8000
Temperature         float       Default: 0.3 (0.0=deterministic, 1.0=creative)
StreamingEnabled    bool        Default: true - Enable streaming responses
RequiresApproval    bool        Default: false - Require human approval before execution
CreatedAt           DateTime    Timestamp when created
UpdatedAt           DateTime    Timestamp when last updated
```

**Unique Constraints**:
- Composite unique index: (TenantId, AgentName) - One configuration per agent per tenant

**Use Cases**:
- Store agent-specific prompts per tenant
- Configure token limits per agent per tenant
- Enable/disable specific tools per tenant
- Require approval workflows for certain agents

**Sample JSON (EnabledTools)**:
```json
["ReadFile", "Grep", "Write", "Edit", "Bash"]
```

---

### TenantLlmProvider [Epic 06 - Multi-LLM Provider Support]

**Purpose**: Enables tenants to use multiple LLM providers (Claude, Z.ai, Minimax M2, OpenRouter, Together AI, custom). Supports both OAuth (Anthropic) and API key authentication.

**Table**: `TenantLlmProviders`

**Schema**:
```
Id                          Guid        PRIMARY KEY
TenantId                    Guid        FOREIGN KEY -> Tenants(Id) CASCADE
Name                        string      (max 200) - Display name (e.g., "Production Claude", "Dev Minimax")
ProviderType                int         Enum (1=AnthropicNative, 2=ZAi, 3=MinimaxM2, 4=OpenRouter, 5=TogetherAI, 99=Custom)
UsesOAuth                   bool        true=OAuth flow, false=API key authentication
EncryptedApiToken           string      (max 2000, ENCRYPTED) - API key or OAuth token
ApiBaseUrl                  string      (max 500, nullable) - Custom API endpoint (null for native Anthropic)
TimeoutMs                   int         Default: 300000 (5 minutes) - Request timeout
DefaultModel                string      (max 100) - Default model name (e.g., "claude-sonnet-4-5-20250929")
DisableNonEssentialTraffic  bool        Default: false - For proxy providers
ModelOverrides              string      JSON dict - Model tier overrides (Minimax M2 specific)
IsActive                    bool        Default: true - Whether this provider can be used
IsDefault                   bool        Default: false - Default provider for this tenant
CreatedAt                   DateTime    When created
UpdatedAt                   DateTime    When last updated
OAuthTokenRefreshedAt       DateTime    When OAuth token was last refreshed
```

**Supported Provider Types**:
- `AnthropicNative` (1) - Native Claude API with OAuth
- `ZAi` (2) - Z.ai unified API (Claude, GPT-4, Gemini)
- `MinimaxM2` (3) - Minimax M2 via Anthropic-compatible API
- `OpenRouter` (4) - OpenRouter (100+ models)
- `TogetherAI` (5) - Together AI (fast inference)
- `Custom` (99) - User-specified base URL

**Unique Constraints**:
- Composite index: (TenantId, IsDefault) - Only one default per tenant

**Use Cases**:
- Store multiple provider credentials per tenant
- Switch between providers per ticket (via Ticket.LlmProviderId)
- Fallback provider if primary provider fails
- Cost optimization (use cheaper model for simple tasks)
- Provider-specific model tuning (Minimax M2 model overrides)
- OAuth token management for Anthropic integration

**Sample ModelOverrides (Minimax M2)**:
```json
{
  "small_fast_model": "MiniMax-M2",
  "default_sonnet_model": "MiniMax-M2",
  "large_model": "MiniMax-M2"
}
```

**Relationships**:
- Referenced by Tickets.LlmProviderId (many tickets can use one provider)

---

### ReviewChecklist [Epic 07 - Review Checklist Templates]

**Purpose**: Structured checklists for plan review evaluation. Ensures consistent review quality across plan reviews with domain-specific criteria.

**Table**: `ReviewChecklists`

**Schema**:
```
Id              Guid        PRIMARY KEY
PlanReviewId    Guid        FOREIGN KEY -> PlanReviews(Id) CASCADE (UNIQUE)
TemplateName    string      (max 100) - Name of checklist template
CreatedAt       DateTime    When created
```

**Unique Constraints**:
- Unique index on PlanReviewId - One checklist per plan review

**Key Methods**:
- `CompletionPercentage()` - Calculate completion %
- `AllRequiredItemsChecked()` - Validate all required items are checked

**Use Cases**:
- Enforce consistency in plan reviews
- Track review progress with completion percentage
- Ensure required criteria are met before approval
- Support multiple checklist templates per domain

**Relationships**:
- Contains many ChecklistItems (1:N)

---

### ChecklistItem [Epic 07 - Individual Checklist Items]

**Purpose**: Individual items within a review checklist. Each item represents a review criterion that can be marked complete.

**Table**: `ChecklistItems`

**Schema**:
```
Id                  Guid        PRIMARY KEY
ReviewChecklistId   Guid        FOREIGN KEY -> ReviewChecklists(Id) CASCADE
Category            string      (max 100) - Grouping (e.g., "Security", "Performance", "Code Quality")
Title               string      (max 255) - Item title
Description         string      (max 1000) - Detailed description
Severity            string      (max 20) - "required" or "recommended"
IsChecked           bool        Whether reviewer has checked this item
CheckedAt           DateTime    When item was marked complete
SortOrder           int         Ordering within checklist (0-based)
```

**Severity Types**:
- `required` - Must be checked before approval
- `recommended` - Nice to have, doesn't block approval

**Use Cases**:
- Define review criteria for different plan types
- Track which reviewer actions completed review requirements
- Support mandatory and optional checklist items
- Maintain consistent review process across team

---

### InlineCommentAnchor [Epic 07 - Comment Anchoring to Plan Lines]

**Purpose**: Anchors review comments to specific lines in a plan document. Enables inline commenting on implementation details with precise line references.

**Table**: `InlineCommentAnchors`

**Schema**:
```
Id              Guid        PRIMARY KEY
ReviewCommentId Guid        FOREIGN KEY -> ReviewComments(Id) CASCADE (UNIQUE)
StartLine       int         Starting line number (1-based)
EndLine         int         Ending line number (1-based)
TextSnippet     string      (max 200) - Snippet of anchored text for context
CreatedAt       DateTime    When anchor was created
```

**Validation Rules**:
- StartLine >= 1 (1-based line numbering)
- EndLine >= StartLine
- TextSnippet must be non-empty and <= 200 chars

**Key Methods**:
- `CoversLine(int)` - Check if anchor covers a specific line
- `GetLineRangeDisplay()` - Returns "10-15" or "10" for single line

**Use Cases**:
- Link review comments to specific plan sections
- Display comments inline with plan markdown
- Track which parts of a plan were reviewed
- Jump to commented lines during review process

**Relationships**:
- One-to-one with ReviewComments

## Migration History

| Migration | Date | Description |
|-----------|------|-------------|
| 20251104000000_InitialCreate | 2025-11-04 | Initial schema with Tenants, Repositories, Tickets, WorkflowEvents |
| 20251107200959_AddTicketSourceAndExternalTracking | 2025-11-07 | Added source/external tracking to tickets |
| 20251107223234_AddWorkflowStateStore | 2025-11-07 | Added workflow state persistence |
| 20251107223500_AddCheckpointEntity | 2025-11-07 | Added checkpoint for workflow resumption |
| 20251108000000_AddTicketUpdates | 2025-11-08 | Added TicketUpdates entity |
| 20251109000000_AddErrorLogTable | 2025-11-09 | Added ErrorLog table |
| 20251109150000_AddTicketPlatformSupport | 2025-11-09 | Added multi-ticket-platform support |
| 20251110000000_AddTenantLlmProvider | 2025-11-10 | Added TenantLlmProvider for multi-LLM support (Epic 06) |
| 20251111000001_AddCodeReviewConfiguration | 2025-11-11 | Added code review configuration |
| 20251114173447_AddReviewChecklists | 2025-11-14 | Added ReviewChecklist, ChecklistItem, InlineCommentAnchor entities (Epic 07); Identity/Auth tables; AgentConfiguration (Epic 05) |
| 20251114174119_AddInlineCommentAnchors | 2025-11-14 | Refinements to InlineCommentAnchor foreign key relationships |

## Database Size Estimates

For planning purposes, here are rough size estimates per record:

- **Tenant**: ~1KB per record (with encrypted tokens and configuration)
- **Repository**: ~500 bytes per record (with encrypted access token)
- **Ticket**: ~2-5KB per record (depending on questions/answers/metadata)
- **WorkflowEvent**: ~200-500 bytes per event

Example capacity planning:
- 100 tenants = ~100KB
- 1,000 repositories = ~500KB
- 10,000 tickets with 50,000 events = ~35MB
- **Total for small installation**: ~36MB

For larger installations with 100,000 tickets, expect 350-500MB of database storage.

## Backup Recommendations

1. **Frequency**: Daily full backups, hourly incremental backups
2. **Retention**: Keep 30 days of daily backups, 7 days of hourly backups
3. **Critical Tables**: Prioritize Tenants and Tickets (Repositories can be re-registered)
4. **Encryption Keys**: Store encryption key backups separately in secure key vault
5. **Point-in-Time Recovery**: Enable transaction log backups for SQLite/SQL Server

## Performance Optimization

### Query Optimization
- Use `.AsNoTracking()` for read-only queries
- Leverage indexes for filtering and sorting
- Use `.Include()` for eager loading relationships
- Consider pagination for large result sets

### Index Usage
All common query patterns are covered by existing indexes:
- Ticket lookups by key (unique)
- State-based filtering (indexed)
- Tenant-based filtering (indexed)
- Date range queries (indexed on CreatedAt)
- Event history (indexed on TicketId)

### Scaling Considerations
- Current schema supports multi-tenancy
- Consider table partitioning for Tickets if > 1M records
- Consider archiving completed tickets after 90 days
- Monitor index fragmentation and rebuild regularly
