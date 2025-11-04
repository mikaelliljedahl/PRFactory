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
             │                           │
             │ 1                         │ 1
             │                           │
             │ *                         │ *
             ▼                           ▼
┌───────────────────────────┐  ┌────────────────────────────────────────────┐
│      Repositories         │  │               Tickets                      │
├───────────────────────────┤  ├────────────────────────────────────────────┤
│ Id            Guid    PK  │  │ Id                     Guid       PK       │
│ TenantId      Guid    FK ─┼──┼─▶TenantId              Guid       FK       │
│ Name          string      │  │ RepositoryId           Guid       FK ◀─────┤
│ GitPlatform   string      │  │ TicketKey              string     UNIQUE   │
│ CloneUrl      string      │  │ TicketSystem           string              │
│               UNIQUE      │  │ Title                  string              │
│ DefaultBranch string      │  │ Description            string              │
│ AccessToken   string      │  │ State                  string (enum)       │
│               ENCRYPTED   │  │ CreatedAt              DateTime            │
│ CreatedAt     DateTime    │  │ UpdatedAt              DateTime?           │
│ UpdatedAt     DateTime?   │  │ CompletedAt            DateTime?           │
│ LastAccessedAt DateTime?  │  │ Questions              JSON[]              │
│ LocalPath     string?     │  │ Answers                JSON[]              │
│ IsActive      bool        │  │ PlanBranchName         string?             │
└───────────────────────────┘  │ PlanMarkdownPath       string?             │
                               │ PlanApprovedAt         DateTime?           │
                               │ ImplementationBranch   string?             │
                               │ PullRequestUrl         string?             │
                               │ PullRequestNumber      int?                │
                               │ RetryCount             int                 │
                               │ LastError              string?             │
                               │ Metadata               JSON                │
                               └────────────────────────────────────────────┘
                                              │
                                              │ 1
                                              │
                                              │ *
                                              ▼
                               ┌────────────────────────────────────────────┐
                               │           WorkflowEvents                   │
                               │           (TPH Inheritance)                │
                               ├────────────────────────────────────────────┤
                               │ Id              Guid       PK              │
                               │ TicketId        Guid       FK              │
                               │ OccurredAt      DateTime                   │
                               │ EventType       string (discriminator)     │
                               ├────────────────────────────────────────────┤
                               │ WorkflowStateChanged:                      │
                               │   - From            WorkflowState          │
                               │   - To              WorkflowState          │
                               │   - Reason          string?                │
                               ├────────────────────────────────────────────┤
                               │ QuestionAdded:                             │
                               │   - Question        (embedded)             │
                               ├────────────────────────────────────────────┤
                               │ AnswerAdded:                               │
                               │   - QuestionId      string                 │
                               │   - AnswerText      string                 │
                               ├────────────────────────────────────────────┤
                               │ PlanCreated:                               │
                               │   - BranchName      string                 │
                               ├────────────────────────────────────────────┤
                               │ PullRequestCreated:                        │
                               │   - PullRequestUrl  string                 │
                               │   - PullRequestNumber int                  │
                               └────────────────────────────────────────────┘
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

## Migration History

| Migration | Date | Description |
|-----------|------|-------------|
| 20251104000000_InitialCreate | 2025-11-04 | Initial schema with all tables, indexes, and relationships |

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
