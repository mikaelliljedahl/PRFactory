# 03: Specialized Agent Roles

**Document Purpose:** Define the four specialized agent roles aligned with PRFactory workflows, including their responsibilities, tool permissions, and implementation patterns.

**Last Updated:** 2025-11-13

---

## Agent Role Overview

PRFactory workflows are enhanced by four specialized agent roles, each optimized for a specific phase:

| Agent Role | Workflow Phase | Primary Responsibility | Key Tools |
|-----------|----------------|----------------------|-----------|
| **AnalyzerAgent** | RefinementGraph | Codebase analysis, impact assessment | ReadFile, Grep, Glob, CodeSearch, GetJiraTicket |
| **PlannerAgent** | PlanningGraph | Implementation planning, task decomposition | ReadFile, Grep, CodeSearch, GetJiraTicket |
| **CodeExecutorAgent** | ImplementationGraph | Code generation, testing, validation | ReadFile, WriteFile, Grep, ExecuteShell, RunTests, GitCommit |
| **ReviewerAgent** | CodeReviewGraph | Code quality review, security analysis | ReadFile, Grep, GetGitDiff, GetJiraTicket |

---

## 1. AnalyzerAgent (Refinement Phase)

### Purpose
Understand ticket requirements and analyze codebase impact. Provides foundation for planning and implementation.

### Responsibilities
- Fetch Jira ticket details and parse requirements
- Search codebase for related code (impacted files, dependencies)
- Assess implementation impact (low/medium/high)
- Identify risks and technical dependencies
- Generate clarifying questions for user

### Tool Permissions (Read-Only)
```json
{
  "enabledTools": [
    "GetJiraTicket",
    "ReadFile",
    "Grep",
    "Glob",
    "CodeSearch"
  ]
}
```

### Sample Configuration
```json
{
  "agentName": "AnalyzerAgent",
  "instructions": "You are a senior software architect analyzing codebases for impact assessment. Be thorough but concise. Always identify risks and dependencies.",
  "maxTokens": 8000,
  "temperature": 0.3,
  "streamingEnabled": true,
  "requiresApproval": false
}
```

### Adapter Implementation
```csharp
public class AnalysisAgentAdapter : BaseAgent
{
    private readonly IAgentFactory _agentFactory;

    public override async Task<IAgentMessage> ExecuteAsync(IAgentMessage input)
    {
        var analysisInput = (AnalysisRequestMessage)input;

        var agent = await _agentFactory.CreateAgentAsync(
            Context.TenantId, "AnalyzerAgent");

        var prompt = $@"
Analyze Jira ticket: {analysisInput.TicketKey}

Use these steps:
1. Use GetJiraTicket to fetch full ticket details
2. Use CodeSearch to find related classes/methods
3. Use Grep to search for relevant code patterns
4. Use ReadFile to examine key files

Provide:
- Impact assessment (Low/Medium/High)
- List of affected files
- Technical dependencies
- Implementation risks
- Clarifying questions for user (if requirements unclear)
";

        var result = await agent.RunAsync(prompt);

        return new AnalysisCompleteMessage(
            Impact: ParseImpact(result.Output),
            RelatedFiles: ParseFiles(result.Output),
            Risks: ParseRisks(result.Output),
            Questions: ParseQuestions(result.Output));
    }
}
```

---

## 2. PlannerAgent (Planning Phase)

### Purpose
Create detailed implementation plan based on refined requirements. Decompose work into tasks with estimates.

### Responsibilities
- Review refined requirements and analysis
- Decompose work into discrete tasks
- Estimate effort and complexity per task
- Identify implementation order and dependencies
- Assess technical risks
- Generate implementation approach

### Tool Permissions (Read-Only)
```json
{
  "enabledTools": [
    "GetJiraTicket",
    "ReadFile",
    "Grep",
    "CodeSearch"
  ]
}
```

### Sample Configuration
```json
{
  "agentName": "PlannerAgent",
  "instructions": "You are a technical lead creating implementation plans. Break work into small, testable tasks. Identify risks and dependencies. Provide realistic estimates.",
  "maxTokens": 8000,
  "temperature": 0.3,
  "streamingEnabled": true,
  "requiresApproval": true
}
```

### Output Format
```json
{
  "tasks": [
    {
      "title": "Update UserService.ValidateToken method",
      "description": "Add expiration check to JWT token validation",
      "files": ["src/Services/UserService.cs"],
      "estimate": "2 hours",
      "complexity": "Medium",
      "risks": ["Breaking change if consumers don't handle expired tokens"]
    }
  ],
  "implementationApproach": "Add token expiration validation before claims verification",
  "testingStrategy": "Unit tests for expired/valid tokens, integration test for auth flow",
  "estimatedTotalEffort": "1 day"
}
```

---

## 3. CodeExecutorAgent (Implementation Phase)

### Purpose
Execute approved implementation plan. Generate code, run tests, commit changes.

### Responsibilities
- Read existing code to understand context
- Generate code changes per plan
- Write updated files (atomic operations)
- Run tests to validate changes
- Commit changes with descriptive messages
- Handle test failures with fixes

### Tool Permissions (Read-Write)
```json
{
  "enabledTools": [
    "ReadFile",
    "WriteFile",
    "Grep",
    "Glob",
    "CodeSearch",
    "ExecuteShell",
    "RunTests",
    "GitCommit"
  ]
}
```

### Sample Configuration
```json
{
  "agentName": "CodeExecutorAgent",
  "instructions": "You are a senior software engineer implementing code changes. Follow the approved plan exactly. Write clean, tested code. Run tests after changes. Commit with clear messages.",
  "maxTokens": 16000,
  "temperature": 0.2,
  "streamingEnabled": true,
  "requiresApproval": true
}
```

### Execution Pattern
```csharp
public class CodeExecutorAgentAdapter : BaseAgent
{
    public override async Task<IAgentMessage> ExecuteAsync(IAgentMessage input)
    {
        var implInput = (ImplementationRequestMessage)input;

        var agent = await _agentFactory.CreateAgentAsync(
            Context.TenantId, "CodeExecutorAgent");

        var prompt = $@"
Execute implementation plan for ticket {implInput.TicketKey}:

{JsonSerializer.Serialize(implInput.Plan, new JsonSerializerOptions { WriteIndented = true })}

For each task:
1. Use ReadFile to read existing code
2. Generate updated code
3. Use WriteFile to save changes (atomic)
4. Use RunTests to validate
5. If tests fail, analyze and fix
6. Use GitCommit when task complete

Provide:
- Code changes made
- Test results
- Commit hashes
";

        var result = await agent.RunAsync(prompt);

        return new ImplementationCompleteMessage(
            ChangedFiles: ParseChangedFiles(result.Output),
            TestResults: ParseTestResults(result.Output),
            CommitHashes: ParseCommitHashes(result.Output));
    }
}
```

---

## 4. ReviewerAgent (Code Review Phase)

### Purpose
Review code changes for quality, security, and best practices. Provide feedback before PR creation.

### Responsibilities
- Review git diff of changes
- Check code quality (readability, maintainability)
- Identify security issues (SQL injection, XSS, etc.)
- Verify tests exist and are adequate
- Suggest improvements
- Approve or request changes

### Tool Permissions (Read-Only)
```json
{
  "enabledTools": [
    "ReadFile",
    "Grep",
    "GetGitDiff",
    "GetJiraTicket"
  ]
}
```

### Sample Configuration
```json
{
  "agentName": "ReviewerAgent",
  "instructions": "You are a senior code reviewer focusing on security, quality, and best practices. Be constructive and specific. Identify risks and suggest improvements.",
  "maxTokens": 8000,
  "temperature": 0.3,
  "streamingEnabled": true,
  "requiresApproval": false
}
```

### Review Categories
```json
{
  "security": {
    "checks": [
      "SQL injection vulnerabilities",
      "XSS vulnerabilities",
      "Authentication/authorization issues",
      "Credential exposure",
      "Directory traversal"
    ]
  },
  "quality": {
    "checks": [
      "Code readability",
      "Naming conventions",
      "Error handling",
      "Edge cases covered",
      "Performance concerns"
    ]
  },
  "testing": {
    "checks": [
      "Unit tests exist",
      "Test coverage adequate (80%+)",
      "Edge cases tested",
      "Integration tests if needed"
    ]
  }
}
```

---

## Prompt Engineering Best Practices

### 1. Clear Instructions
```csharp
var instructions = @"
You are a [ROLE]. Your task is to [PRIMARY RESPONSIBILITY].

Your approach:
1. [STEP 1]
2. [STEP 2]
3. [STEP 3]

You have access to these tools:
- [TOOL 1]: [DESCRIPTION]
- [TOOL 2]: [DESCRIPTION]

Provide output in this format:
[OUTPUT FORMAT]
";
```

### 2. Constrained Outputs
```csharp
var prompt = @"
Analyze the ticket and provide JSON output ONLY (no markdown):

{
  ""impact"": ""Low|Medium|High"",
  ""affectedFiles"": [""file1.cs"", ""file2.cs""],
  ""risks"": [""risk1"", ""risk2""],
  ""questions"": [""question1"", ""question2""]
}
";
```

### 3. Few-Shot Examples
```csharp
var prompt = @"
Generate a commit message for these changes.

Example:
Input: Added JWT token expiration validation
Output: feat(auth): Add token expiration validation to UserService

Example:
Input: Fixed null reference exception in payment processing
Output: fix(payments): Handle null customer in ProcessPayment method

Input: {changes}
Output:
";
```

---

## Agent Configuration Per Tenant

### Default Configuration (Seed Data)
```csharp
public static class AgentConfigurationSeeder
{
    public static List<AgentConfiguration> GetDefaultConfigurations(Guid tenantId)
    {
        return new List<AgentConfiguration>
        {
            new AgentConfiguration
            {
                TenantId = tenantId,
                AgentName = "AnalyzerAgent",
                Instructions = "You are a senior software architect...",
                EnabledTools = new[] { "ReadFile", "Grep", "Glob", "CodeSearch", "GetJiraTicket" },
                MaxTokens = 8000,
                Temperature = 0.3f,
                StreamingEnabled = true,
                RequiresApproval = false
            },
            // ... other agents
        };
    }
}
```

### Customization Per Tenant
Tenants can customize:
- Agent instructions (persona, tone, focus areas)
- Tool permissions (enable/disable specific tools)
- Token budget (cost control)
- Temperature (creativity vs. determinism)
- Approval requirements (human-in-the-loop gates)

---

## Next Steps

1. **Review agent roles** with product team
2. **Finalize tool permissions** per role
3. **Write detailed prompts** for each agent
4. **Implement adapters** starting with AnalyzerAgent
5. **Test end-to-end** with real Jira tickets

**See:** `04_UI_INTEGRATION.md` for AG-UI integration patterns.
