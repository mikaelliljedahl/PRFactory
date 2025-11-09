# Database Seeding for Offline Development

## Overview

This implementation provides a complete database seeding system for PRFactory to enable offline/single-user development mode. The seeder automatically populates the database with realistic demo data when running in Development environment.

## Implementation Details

### Files Created

#### 1. Demo Data Helper Classes

**Location**: `/src/PRFactory.Infrastructure/Persistence/DemoData/`

- **DemoTenantData.cs** - Tenant configuration constants
  - Hardcoded demo tenant ID: `00000000-0000-0000-0000-000000000001`
  - Demo tenant name, Jira URL, API tokens, and Claude API key

- **DemoRepositoryData.cs** - Repository definitions
  - 3 repositories covering different platforms:
    - GitHub: `prfactory-demo`
    - Bitbucket: `enterprise-app`
    - Azure DevOps: `azure-project`

- **DemoTicketData.cs** - Ticket scenarios
  - 21 demo tickets covering all workflow states:
    - Triggered, Analyzing, TicketUpdateGenerated, TicketUpdateUnderReview
    - TicketUpdateRejected, TicketUpdateApproved, TicketUpdatePosted
    - QuestionsPosted, AwaitingAnswers, AnswersReceived
    - Planning, PlanPosted, PlanUnderReview, PlanApproved, PlanRejected
    - Implementing, ImplementationFailed, PRCreated, InReview
    - Completed, Failed, Cancelled

- **DemoPromptData.cs** - Agent prompt templates
  - 5 system prompt templates:
    - ticket-analyzer (Analysis category)
    - implementation-planner (Planning category)
    - code-reviewer (Review category)
    - test-generator (Testing category)
    - documentation-writer (Documentation category)

#### 2. Main Seeder Class

**Location**: `/src/PRFactory.Infrastructure/Persistence/DbSeeder.cs`

**Key Features**:
- **Idempotent**: Checks if demo tenant already exists before seeding
- **Encrypted Credentials**: Uses `IEncryptionService` to encrypt all tokens and API keys
- **Complete Workflow States**: Properly transitions tickets through valid state paths
- **Rich Timeline Data**: Adds workflow events, questions/answers, and ticket updates
- **Realistic Data**: Includes success criteria, acceptance criteria, and detailed descriptions

**Seeding Process**:
1. Check if demo tenant exists (idempotent operation)
2. Seed demo tenant with encrypted credentials
3. Seed 3 repositories (GitHub, Bitbucket, Azure DevOps)
4. Seed 21 tickets covering all workflow states
5. Add questions and answers for appropriate states
6. Add ticket updates for refinement workflow states
7. Add workflow events for timeline display
8. Seed 5 agent prompt templates

### Files Modified

#### 1. DependencyInjection.cs

**Location**: `/src/PRFactory.Infrastructure/DependencyInjection.cs`

**Changes**:
```csharp
// Register database seeder
services.AddScoped<DbSeeder>();
```

#### 2. Program.cs

**Location**: `/src/PRFactory.Web/Program.cs`

**Changes**:
```csharp
// Seed demo data in Development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbSeeder = scope.ServiceProvider.GetRequiredService<PRFactory.Infrastructure.Persistence.DbSeeder>();
    await dbSeeder.SeedAsync();
}
```

## Usage

### First Run

```bash
# Delete existing database (if any)
rm prfactory.db

# Run the Web application
dotnet run --project src/PRFactory.Web

# Expected console output:
# [INF] Checking if demo data needs to be seeded...
# [INF] Seeding demo data for offline development...
# [INF] Seeding demo tenant...
# [INF] Demo tenant seeded: Demo Tenant (ID: 00000000-0000-0000-0000-000000000001)
# [INF] Seeding demo repositories...
# [INF] Seeded repository: prfactory-demo (GitHub)
# [INF] Seeded repository: enterprise-app (Bitbucket)
# [INF] Seeded repository: azure-project (AzureDevOps)
# [INF] Seeding demo tickets...
# [INF] Seeded ticket: DEMO-001 - Add user authentication to dashboard (State: Triggered)
# ... (21 tickets total)
# [INF] Seeding agent prompt templates...
# [INF] Seeded prompt template: ticket-analyzer (Analysis)
# ... (5 templates total)
# [INF] Demo data seeding completed successfully
```

### Subsequent Runs

```bash
# Run the Web application again
dotnet run --project src/PRFactory.Web

# Expected console output:
# [INF] Checking if demo data needs to be seeded...
# [INF] Demo data already exists. Skipping seeding.
```

The seeder is **idempotent** - it only seeds data if the demo tenant doesn't exist.

### Resetting Demo Data

```bash
# Delete the database
rm prfactory.db

# Run the application (will re-seed)
dotnet run --project src/PRFactory.Web
```

## Seeded Data Details

### Demo Tenant

- **ID**: `00000000-0000-0000-0000-000000000001` (hardcoded for consistency)
- **Name**: Demo Tenant
- **Jira URL**: https://demo.atlassian.net
- **Configuration**:
  - AutoImplementAfterPlanApproval: `false`
  - MaxRetries: `3`
  - ClaudeModel: `claude-sonnet-4-5-20250929`
  - MaxTokensPerRequest: `8000`
  - EnableVerboseLogging: `true`

### Demo Repositories

1. **prfactory-demo** (GitHub)
   - Clone URL: https://github.com/demo/prfactory-demo.git
   - Default Branch: main

2. **enterprise-app** (Bitbucket)
   - Clone URL: https://bitbucket.org/demo/enterprise-app.git
   - Default Branch: master

3. **azure-project** (Azure DevOps)
   - Clone URL: https://dev.azure.com/demo/azure-project/_git/main
   - Default Branch: main

### Demo Tickets

Each ticket includes:
- **Basic Info**: Ticket key, title, description
- **Workflow State**: Current state in the workflow
- **Questions** (for appropriate states): 4 clarifying questions covering requirements, technical, edge-cases, and UX
- **Answers** (for AwaitingAnswers+): Detailed answers to each question
- **Ticket Update** (for refinement states):
  - Refined title and description
  - 5 success criteria (Functional, Technical, Testing, UX, Performance)
  - Detailed acceptance criteria
  - Version tracking
  - Approval/rejection status
- **Workflow Events**: Timeline of state transitions
- **Plan Information** (for Planning+ states): Branch name and plan path
- **Pull Request** (for PRCreated+ states): PR URL and number

### Demo Agent Prompt Templates

All templates are **system templates** (available to all tenants):

1. **ticket-analyzer** (Analysis)
   - Analyzes ticket requirements and identifies missing information
   - Recommended model: claude-sonnet-4-5-20250929

2. **implementation-planner** (Planning)
   - Creates detailed implementation plans from refined requirements
   - Recommended model: claude-sonnet-4-5-20250929

3. **code-reviewer** (Review)
   - Reviews code changes for quality and best practices
   - Recommended model: claude-sonnet-4-5-20250929

4. **test-generator** (Testing)
   - Generates comprehensive test cases for implementations
   - Recommended model: claude-sonnet-4-5-20250929

5. **documentation-writer** (Documentation)
   - Creates clear technical documentation
   - Recommended model: claude-sonnet-4-5-20250929

## Key Implementation Details

### Hardcoded Tenant ID

The demo tenant uses a hardcoded ID for consistency across database resets. This is achieved using reflection:

```csharp
var tenant = Tenant.Create(/* ... */);
var idProperty = typeof(Tenant).GetProperty("Id");
idProperty?.SetValue(tenant, DemoTenantData.DemoTenantId);
```

### Credential Encryption

All credentials (Jira tokens, Git tokens, Claude API keys) are encrypted using `IEncryptionService`:

```csharp
var encryptedJiraToken = _encryptionService.Encrypt(DemoTenantData.JiraApiToken);
var encryptedClaudeKey = _encryptionService.Encrypt(DemoTenantData.ClaudeApiKey);
```

### State Transitions

Tickets are properly transitioned through valid state paths using the domain entity's state machine:

```csharp
private List<WorkflowState> GetStateTransitionPath(WorkflowState targetState)
{
    // Returns the complete path from Triggered to target state
    // Ensures all transitions are valid according to domain rules
}
```

### Workflow Events

Events are automatically added by the `Ticket.TransitionTo()` method. Additional events (PlanCreated, PullRequestCreated) are added for advanced states.

## Testing

### Verify Seeded Data via Web UI

1. Navigate to `/tickets` - should show 21 demo tickets
2. Click any ticket to see details:
   - Timeline with workflow events
   - Questions and answers (for appropriate states)
   - Ticket updates (for refinement states)
   - Success criteria and acceptance criteria

### Verify via Database

```bash
# Install SQLite CLI tool
sudo apt-get install sqlite3

# Query demo data
sqlite3 prfactory.db

# Check tenant
SELECT * FROM Tenants WHERE Id = '00000000-0000-0000-0000-000000000001';

# Check repositories
SELECT Name, GitPlatform FROM Repositories;

# Check tickets by state
SELECT TicketKey, Title, State FROM Tickets ORDER BY State;

# Check ticket updates
SELECT * FROM TicketUpdates;

# Check workflow events
SELECT * FROM WorkflowEvents ORDER BY OccurredAt;

# Check prompt templates
SELECT Name, Category FROM AgentPromptTemplates;
```

## Troubleshooting

### Database Already Seeded

**Issue**: Running the application shows "Demo data already exists. Skipping seeding."

**Solution**: Delete the database and run again:
```bash
rm prfactory.db
dotnet run --project src/PRFactory.Web
```

### Encryption Key Not Configured

**Issue**: Error "Encryption key not configured"

**Solution**: Ensure `appsettings.Development.json` has an encryption key:
```json
{
  "Encryption": {
    "Key": "your-base64-encryption-key-here"
  }
}
```

Generate a key if needed:
```bash
dotnet run --project src/PRFactory.Web -- generate-encryption-key
```

## Future Enhancements

Potential improvements to the seeding system:

1. **Custom Seed Profiles**: Allow loading different seed profiles (minimal, complete, stress-test)
2. **Seed from JSON**: Load demo data from JSON configuration files
3. **CLI Command**: Add `dotnet run -- seed-database` command
4. **Seed Reset**: Add `dotnet run -- reset-database` command
5. **Production Seed**: Seed initial system data (not demo data) for production deployments
6. **Tenant Templates**: Provide template tenants with different configurations

## Architecture Notes

This implementation follows PRFactory's architectural principles:

- **Clean Architecture**: Seeding logic is in Infrastructure layer
- **Domain Entities**: Uses entity factory methods (Tenant.Create(), Repository.Create(), etc.)
- **Encryption**: All credentials properly encrypted at rest
- **Idempotency**: Safe to run multiple times without duplicating data
- **Development Only**: Automatically runs only in Development environment
- **Logging**: Comprehensive structured logging of seeding operations
