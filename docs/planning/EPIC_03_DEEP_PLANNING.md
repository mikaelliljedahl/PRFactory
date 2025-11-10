# Epic 3: Deeper Planning Phase (MetaGPT-Inspired)

**Status:** 🔴 Not Started
**Priority:** P2 (Important)
**Effort:** 2-3 weeks
**Dependencies:** Epic 1 (Team Review), Epic 2 (Multi-LLM)

---

## Strategic Goal

Make Phase 2 (Planning) the core value proposition of PRFactory. Generate comprehensive, multi-artifact plans that simulate a full development team (PM, Architect, QA, Tech Lead).

**Current State:** `cli plan` generates a single implementation plan file.

**Target State:** `cli plan` generates a directory of artifacts (user stories, API design, database schema, test cases, implementation steps).

**Inspiration:** MetaGPT's multi-agent approach where different LLM personas simulate different team roles.

---

## Success Criteria

✅ **Must Have:**
- `cli plan` generates 5 artifacts: user stories, API design, database schema, test cases, implementation steps
- Multi-step orchestration with different LLM personas (PM → Architect → QA → Tech Lead)
- `cli revise` command for iterative plan refinement
- Web UI displays all plan artifacts (not just single file)

✅ **Nice to Have:**
- Plan versioning (track revisions)
- Plan diff viewer (show changes between versions)
- Additional artifact types (sequence diagrams, deployment guide, monitoring plan)

---

## Implementation Plan

### 1. Multi-Artifact Generation

**Output Directory Structure:**

```
workspace/{ticket-id}/plan/
├── 01-user_stories.md
├── 02-api_design.yml
├── 03-database_schema.sql
├── 04-test_cases.md
└── 05-implementation_steps.md
```

**Orchestrator Logic:**

```csharp
public class PlanOrchestrator
{
    public async Task<PlanArtifacts> GeneratePlanAsync(Guid ticketId)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        var codebaseContext = await _contextBuilder.BuildContextAsync(ticket.RepositoryId);

        // Step 1: Product Manager persona → User Stories
        var userStories = await GenerateUserStoriesAsync(ticket, codebaseContext);
        SaveArtifact("01-user_stories.md", userStories);

        // Step 2: Software Architect persona → API Design + Database Schema
        var apiDesign = await GenerateApiDesignAsync(ticket, userStories, codebaseContext);
        var dbSchema = await GenerateDatabaseSchemaAsync(ticket, userStories, codebaseContext);
        SaveArtifact("02-api_design.yml", apiDesign);
        SaveArtifact("03-database_schema.sql", dbSchema);

        // Step 3: QA Engineer persona → Test Cases
        var testCases = await GenerateTestCasesAsync(ticket, userStories, apiDesign, dbSchema);
        SaveArtifact("04-test_cases.md", testCases);

        // Step 4: Tech Lead persona → Implementation Steps
        var implSteps = await GenerateImplementationStepsAsync(
            ticket, userStories, apiDesign, dbSchema, testCases, codebaseContext);
        SaveArtifact("05-implementation_steps.md", implSteps);

        return new PlanArtifacts
        {
            UserStories = userStories,
            ApiDesign = apiDesign,
            DatabaseSchema = dbSchema,
            TestCases = testCases,
            ImplementationSteps = implSteps
        };
    }
}
```

**Prompt Templates:**

`/prompts/plan/anthropic/pm_user_stories.txt`:
```
You are a Product Manager analyzing a ticket and writing user stories.

<ticket>
{ticket_description}
</ticket>

Generate user stories in the format:
- As a [persona], I want [feature], so that [benefit]
- Acceptance criteria for each story
- Edge cases and non-functional requirements
```

`/prompts/plan/anthropic/architect_api_design.txt`:
```
You are a Software Architect designing API endpoints.

<user_stories>
{user_stories}
</user_stories>

<codebase_context>
{codebase_files}
</codebase_context>

Generate an OpenAPI specification (YAML) for the required endpoints.
Include: paths, methods, request/response schemas, status codes, error handling.
```

`/prompts/plan/anthropic/architect_db_schema.txt`:
```
You are a Database Architect designing schema changes.

<user_stories>
{user_stories}
</user_stories>

<existing_schema>
{current_database_schema}
</existing_schema>

Generate SQL DDL statements for:
- New tables (CREATE TABLE)
- Schema changes (ALTER TABLE)
- Indexes
- Foreign keys
- Migration considerations
```

`/prompts/plan/anthropic/qa_test_cases.txt`:
```
You are a QA Engineer writing test cases.

<user_stories>
{user_stories}
</user_stories>

<api_design>
{api_design}
</api_design>

Generate test cases covering:
- Happy path scenarios
- Edge cases
- Error handling
- Integration test scenarios
- Performance test scenarios
```

`/prompts/plan/anthropic/techlead_implementation.txt`:
```
You are a Tech Lead creating implementation steps.

<user_stories>
{user_stories}
</user_stories>

<api_design>
{api_design}
</api_design>

<database_schema>
{database_schema}
</database_schema>

<test_cases>
{test_cases}
</test_cases>

<codebase_context>
{codebase_files}
</codebase_context>

Generate detailed implementation steps:
1. Files to create/modify (with exact paths)
2. Classes and methods to add
3. Configuration changes
4. Migration scripts
5. Test implementation guidance
```

---

### 2. CLI `revise` Command

**Purpose:** Iterative plan refinement via natural language instructions.

**Usage:**

```bash
# Initial plan generation
cli plan --issue PROJ-123
# Generates: workspace/PROJ-123/plan/

# Review in Web UI, find issue
# User instruction: "Add rate limiting to the API endpoints"

# Revise plan with instruction
cli revise --plan-dir workspace/PROJ-123/plan/ \
  --instruction "Add rate limiting to the API endpoints"

# CLI updates relevant artifacts (02-api_design.yml, 05-implementation_steps.md)
```

**Implementation:**

```csharp
public class ReviseCommand
{
    [Option("--plan-dir", Required = true)]
    public string PlanDirectory { get; set; }

    [Option("--instruction", Required = true)]
    public string Instruction { get; set; }

    public async Task<int> ExecuteAsync()
    {
        // Load all plan artifacts
        var artifacts = LoadAllArtifacts(PlanDirectory);

        // Determine which artifacts need updating
        var prompt = $@"
You are a software architect revising a plan based on feedback.

<current_plan>
{artifacts.ToMarkdown()}
</current_plan>

<revision_instruction>
{Instruction}
</revision_instruction>

Identify which artifacts need updating and generate revised content.
Output format:
FILE: 02-api_design.yml
[revised content]

FILE: 05-implementation_steps.md
[revised content]
";

        var response = await _llmProvider.SendMessageAsync(prompt);

        // Parse response, update only changed files
        var updates = ParseFileUpdates(response.Content);

        foreach (var update in updates)
        {
            var filePath = Path.Combine(PlanDirectory, update.FileName);
            File.WriteAllText(filePath, update.Content);
            Console.WriteLine($"Updated: {update.FileName}");
        }

        return 0;
    }
}
```

---

### 3. Web UI Multi-Artifact Viewer

**Update:** `/PRFactory.Web/Pages/Plans/Detail.razor`

```razor
<Card Title="Plan Artifacts" Icon="folder">
    <RadzenTabStrip>
        <Tabs>
            <RadzenTabsItem Text="User Stories">
                <MarkdownViewer Content="@plan.UserStories" />
            </RadzenTabsItem>

            <RadzenTabsItem Text="API Design">
                <CodeEditor Content="@plan.ApiDesign" Language="yaml" ReadOnly="true" />
            </RadzenTabsItem>

            <RadzenTabsItem Text="Database Schema">
                <CodeEditor Content="@plan.DatabaseSchema" Language="sql" ReadOnly="true" />
            </RadzenTabsItem>

            <RadzenTabsItem Text="Test Cases">
                <MarkdownViewer Content="@plan.TestCases" />
            </RadzenTabsItem>

            <RadzenTabsItem Text="Implementation Steps">
                <MarkdownViewer Content="@plan.ImplementationSteps" />
            </RadzenTabsItem>
        </Tabs>
    </RadzenTabStrip>
</Card>

<Card Title="Revise Plan" Icon="pencil">
    <FormField Label="Revision Instruction">
        <InputTextArea @bind-Value="revisionInstruction" rows="3"
            placeholder="e.g., Add rate limiting to the API endpoints" />
    </FormField>

    <LoadingButton OnClick="HandleRevise" IsLoading="@isRevising" Icon="arrow-repeat">
        Revise Plan
    </LoadingButton>
</Card>
```

---

### 4. Database Schema Updates

**Modify:** `Plans` table to store multiple artifacts

```sql
ALTER TABLE Plans
ADD UserStories NVARCHAR(MAX) NULL,
    ApiDesign NVARCHAR(MAX) NULL,
    DatabaseSchema NVARCHAR(MAX) NULL,
    TestCases NVARCHAR(MAX) NULL,
    ImplementationSteps NVARCHAR(MAX) NULL,
    Version INT NOT NULL DEFAULT 1;

-- Plan versioning table (optional, for nice-to-have)
CREATE TABLE PlanVersions (
    VersionID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanID UNIQUEIDENTIFIER NOT NULL,
    Version INT NOT NULL,
    UserStories NVARCHAR(MAX),
    ApiDesign NVARCHAR(MAX),
    DatabaseSchema NVARCHAR(MAX),
    TestCases NVARCHAR(MAX),
    ImplementationSteps NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT FK_PlanVersions_Plans FOREIGN KEY (PlanID) REFERENCES Plans(Id)
);
```

---

## Acceptance Criteria

### CLI Commands
- [ ] `cli plan` generates 5 artifacts in directory structure
- [ ] Orchestrator executes 4-step persona workflow (PM → Architect → QA → Tech Lead)
- [ ] Each artifact uses appropriate prompt template
- [ ] `cli revise` updates only relevant artifacts based on instruction
- [ ] Revision history tracked (optional: plan versioning)

### Prompt Templates
- [ ] PM user stories prompt created
- [ ] Architect API design prompt created
- [ ] Architect database schema prompt created
- [ ] QA test cases prompt created
- [ ] Tech Lead implementation steps prompt created
- [ ] Prompts work with multiple LLM providers (Epic 2 dependency)

### Web UI
- [ ] Plan detail page displays all 5 artifacts in tabs
- [ ] Syntax highlighting for YAML (API design) and SQL (schema)
- [ ] Markdown rendering for user stories, test cases, implementation steps
- [ ] "Revise Plan" UI with natural language input
- [ ] Plan version history viewer (optional)

### Database
- [ ] `Plans` table updated with artifact columns
- [ ] `PlanVersions` table created (optional)
- [ ] EF Core migrations generated

---

## Migration Path

### Week 1: Orchestrator & Prompts
- Create `PlanOrchestrator` class
- Create 5 prompt templates for each persona
- Implement multi-step generation logic
- Test with Claude provider

### Week 2: CLI & Storage
- Implement `cli revise` command
- Update `Plan` entity with artifact properties
- Update database schema
- Test end-to-end plan generation and revision

### Week 3: Web UI
- Build multi-artifact viewer component
- Add tab-based UI for artifacts
- Implement revise functionality in UI
- Test full workflow (generate → review → revise → approve)

---

## Related Epics

- **Epic 1 (Team Review):** Comments can reference specific artifacts
- **Epic 2 (Multi-LLM):** Orchestrator works with any LLM provider
- **Epic 4 (Diff Viewer):** Code validation compares against all plan artifacts

---

**Next Steps:**
1. Validate multi-artifact approach with stakeholders
2. Create tickets for orchestrator, prompts, UI
3. Start with Week 1 (orchestrator and prompts)
