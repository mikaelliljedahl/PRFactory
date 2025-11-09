# Implementation Quality Loop - Addendum: Multi-Agent Support & Complexity Tiers

**Status**: Draft Addendum
**Created**: 2025-11-09
**Updates**: Original design document with multi-agent configuration and complexity-based workflow selection
**Related**: [implementation-quality-loop.md](./implementation-quality-loop.md)

---

## Table of Contents

- [Overview](#overview)
- [Multi-Agent Architecture](#multi-agent-architecture)
- [Complexity Assessment in Planning Phase](#complexity-assessment-in-planning-phase)
- [Three-Tier Workflow System](#three-tier-workflow-system)
- [Agent Registry & Configuration](#agent-registry--configuration)
- [Workflow Execution Patterns](#workflow-execution-patterns)
- [Integration with Planning Phase](#integration-with-planning-phase)
- [Configuration Schema Updates](#configuration-schema-updates)
- [Database Schema Additions](#database-schema-additions)
- [Implementation Examples](#implementation-examples)

---

## Overview

This addendum addresses two critical requirements:

1. **Multi-Agent Support**: Enable using different agents for different roles (implementation vs evaluation vs code review)
2. **Complexity-Based Workflow Selection**: Automatically determine workflow tier in planning phase based on task complexity

### Key Changes from Original Design

| Aspect | Original Design | Updated Design |
|--------|----------------|----------------|
| Agent Architecture | Single Claude-based agent for all roles | Pluggable agents (Claude Code CLI, Codex CLI, etc.) |
| Workflow Complexity | One-size-fits-all quality loop | 3 tiers based on task complexity |
| Configuration | Feature flags only | Agent registry + complexity thresholds |
| Planning Phase | Passive (generates plan only) | Active (assesses complexity, selects workflow) |

---

## Multi-Agent Architecture

### Design Principle: Agent as Interface

Instead of hardcoding Claude AI for all roles, define agents as **pluggable implementations** of specific interfaces.

```csharp
/// <summary>
/// Base interface for all agent implementations
/// </summary>
public interface IAgentImplementation
{
    string Name { get; }
    string Provider { get; } // "Claude", "Codex", "Custom"
    AgentCapability Capabilities { get; }
    Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken ct);
}

/// <summary>
/// Capabilities an agent can support
/// </summary>
[Flags]
public enum AgentCapability
{
    None = 0,
    CodeImplementation = 1,
    QualityEvaluation = 2,
    CodeReview = 4,
    TestGeneration = 8,
    SecurityAnalysis = 16,
    PerformanceAnalysis = 32
}
```

### Agent Registry

Maintain a registry of available agent implementations per tenant:

```csharp
public class AgentRegistry
{
    private readonly Dictionary<string, IAgentImplementation> _agents = new();

    public void Register(string agentId, IAgentImplementation implementation)
    {
        _agents[agentId] = implementation;
    }

    public IAgentImplementation? GetAgent(string agentId)
    {
        return _agents.TryGetValue(agentId, out var agent) ? agent : null;
    }

    public IEnumerable<IAgentImplementation> GetAgentsByCapability(AgentCapability capability)
    {
        return _agents.Values.Where(a => a.Capabilities.HasFlag(capability));
    }
}
```

### Pre-Built Agent Implementations

#### 1. Claude Code CLI Agent

```csharp
public class ClaudeCodeCliAgent : IAgentImplementation
{
    public string Name => "Claude Code CLI";
    public string Provider => "Anthropic";
    public AgentCapability Capabilities =>
        AgentCapability.CodeImplementation |
        AgentCapability.TestGeneration;

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken ct)
    {
        // Execute Claude Code CLI in subprocess
        var processInfo = new ProcessStartInfo
        {
            FileName = "claude-code",
            Arguments = $"--task \"{request.TaskDescription}\" --files {string.Join(",", request.FilePaths)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(processInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        var errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(ct);

        return new AgentResponse
        {
            Success = process.ExitCode == 0,
            Output = output,
            Errors = errors,
            ModifiedFiles = ParseModifiedFiles(output)
        };
    }

    private List<string> ParseModifiedFiles(string output)
    {
        // Parse Claude Code CLI output to extract modified file paths
        // Format: "Modified: /path/to/file.cs"
        return output
            .Split('\n')
            .Where(line => line.StartsWith("Modified:"))
            .Select(line => line.Substring(10).Trim())
            .ToList();
    }
}
```

#### 2. Codex CLI Agent (Evaluation Specialist)

```csharp
public class CodexCliAgent : IAgentImplementation
{
    public string Name => "Codex CLI";
    public string Provider => "OpenAI";
    public AgentCapability Capabilities =>
        AgentCapability.QualityEvaluation |
        AgentCapability.CodeReview |
        AgentCapability.SecurityAnalysis;

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken ct)
    {
        // Execute Codex CLI for evaluation
        var processInfo = new ProcessStartInfo
        {
            FileName = "codex-cli",
            Arguments = $"review --files {string.Join(",", request.FilePaths)} --format json",
            RedirectStandardOutput = true
        };

        using var process = Process.Start(processInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync(ct);

        var evaluation = JsonSerializer.Deserialize<CodexEvaluation>(output);

        return new AgentResponse
        {
            Success = true,
            Output = output,
            EvaluationScore = evaluation?.OverallScore,
            Gaps = evaluation?.Issues.Select(i => new QualityGap
            {
                GapId = i.Id,
                Type = i.Category,
                Severity = i.Severity,
                Location = i.Location,
                Description = i.Message,
                RequiredFix = i.SuggestedFix
            }).ToList()
        };
    }
}
```

#### 3. Claude AI Agent (Direct API)

```csharp
public class ClaudeAiAgent : IAgentImplementation
{
    private readonly IClaudeApiClient _claudeClient;

    public string Name => "Claude AI (API)";
    public string Provider => "Anthropic";
    public AgentCapability Capabilities =>
        AgentCapability.CodeImplementation |
        AgentCapability.QualityEvaluation |
        AgentCapability.CodeReview |
        AgentCapability.TestGeneration;

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken ct)
    {
        var messages = new[]
        {
            new Message { Role = "user", Content = request.Prompt }
        };

        var response = await _claudeClient.CreateMessageAsync(new CreateMessageRequest
        {
            Model = "claude-sonnet-4-5-20250929",
            Messages = messages,
            MaxTokens = 8000,
            Temperature = 0.0
        }, ct);

        return new AgentResponse
        {
            Success = true,
            Output = response.Content[0].Text,
            TokensUsed = response.Usage.InputTokens + response.Usage.OutputTokens,
            Cost = CalculateCost(response.Usage)
        };
    }
}
```

### Agent Request/Response Models

```csharp
public class AgentRequest
{
    public string TaskId { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public List<string> FilePaths { get; set; } = new();
    public string? Prompt { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public AgentRole Role { get; set; } // Implementation, Evaluation, Review
}

public class AgentResponse
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? Errors { get; set; }
    public List<string> ModifiedFiles { get; set; } = new();
    public int? EvaluationScore { get; set; } // 1-100
    public List<QualityGap>? Gaps { get; set; }
    public int? TokensUsed { get; set; }
    public decimal? Cost { get; set; }
}

public enum AgentRole
{
    Implementation,
    Evaluation,
    CodeReview,
    TestGeneration
}
```

---

## Complexity Assessment in Planning Phase

### Overview

The **PlanningAgent** should assess task complexity during plan generation and recommend a workflow tier. This assessment becomes part of the approved plan.

### Complexity Dimensions

```csharp
public class ComplexityAssessment
{
    // Metrics (each scored 1-10)
    public int CodeVolumeComplexity { get; set; }      // Files/LOC to modify
    public int LogicalComplexity { get; set; }         // Algorithm complexity
    public int ArchitecturalImpact { get; set; }       // Cross-cutting changes
    public int TestingComplexity { get; set; }         // Test requirements
    public int RiskLevel { get; set; }                 // Security, performance, data

    // Calculated overall complexity (1-10)
    public decimal OverallComplexity =>
        (CodeVolumeComplexity * 0.20m +
         LogicalComplexity * 0.25m +
         ArchitecturalImpact * 0.25m +
         TestingComplexity * 0.15m +
         RiskLevel * 0.15m);

    // Recommended workflow tier
    public WorkflowTier RecommendedTier =>
        OverallComplexity switch
        {
            <= 3.0m => WorkflowTier.Simple,
            <= 7.0m => WorkflowTier.Standard,
            _       => WorkflowTier.Comprehensive
        };

    // Justification for human review
    public string Justification { get; set; } = string.Empty;
}
```

### Complexity Assessment Prompt (Added to PlanningAgent)

```markdown
## COMPLEXITY ASSESSMENT

After generating the implementation plan, assess task complexity across these dimensions:

1. **Code Volume Complexity (1-10)**
   - 1-3: Single file, <100 LOC
   - 4-6: 2-5 files, 100-500 LOC
   - 7-8: 6-15 files, 500-2000 LOC
   - 9-10: 15+ files, >2000 LOC

2. **Logical Complexity (1-10)**
   - 1-3: Simple CRUD, UI changes
   - 4-6: Business logic, validation rules
   - 7-8: Complex algorithms, state machines
   - 9-10: Distributed systems, concurrency, critical algorithms

3. **Architectural Impact (1-10)**
   - 1-3: Isolated to single component
   - 4-6: Affects 2-3 components
   - 7-8: Cross-cutting (affects 4+ components)
   - 9-10: Fundamental architecture changes

4. **Testing Complexity (1-10)**
   - 1-3: Simple unit tests only
   - 4-6: Unit + integration tests
   - 7-8: Complex test scenarios, mocking required
   - 9-10: E2E tests, performance tests, security tests

5. **Risk Level (1-10)**
   - 1-3: Low risk, easy to rollback
   - 4-6: Medium risk, data migrations involved
   - 7-8: High risk, security/auth changes
   - 9-10: Critical risk, payment/financial, data loss potential

**Output Format**:
```json
{
  "complexityAssessment": {
    "codeVolumeComplexity": 5,
    "logicalComplexity": 7,
    "architecturalImpact": 4,
    "testingComplexity": 6,
    "riskLevel": 3,
    "overallComplexity": 5.3,
    "recommendedTier": "Standard",
    "justification": "Moderate code changes with some logical complexity but isolated architectural impact"
  }
}
```

This assessment will determine the workflow tier used for implementation.
```

---

## Three-Tier Workflow System

### Tier 1: Simple (No Evaluation)

**Complexity Range**: 1.0 - 3.0
**Use Case**: Trivial changes (typo fixes, documentation, simple UI tweaks)

**Workflow**:
```
PlanApprovedMessage
  ↓
SingleImplementationAgent (ClaudeCodeCli)
  ↓
CompilationCheck (build only, no tests)
  ↓
GitCommit
  ↓
PullRequest + JiraPost (parallel)
  ↓
Completion
```

**Configuration**:
```csharp
public class SimpleTierConfig
{
    public string ImplementationAgentId { get; set; } = "claude-code-cli";
    public bool RequireCompilation { get; set; } = true;
    public bool RequireTests { get; set; } = false;
    public bool RequireEvaluation { get; set; } = false;
}
```

**Estimated Duration**: 2-5 minutes
**Estimated Cost**: $0.50 - $1.00

---

### Tier 2: Standard (Implementation + Evaluation)

**Complexity Range**: 3.1 - 7.0
**Use Case**: Typical feature development (new endpoints, business logic, database changes)

**Workflow**:
```
PlanApprovedMessage
  ↓
TaskDecomposition (optional, for 4+ file changes)
  ↓
ParallelImplementation (ClaudeCodeCli × N)
  ↓
CodeIntegration
  ↓
CompilationCheck
  ↓
TestExecution
  ↓
QualityEvaluation (Same agent as implementation: Claude)
  ↓
IterationDecision
  ├─ Score >= 90 → GitCommit → PR
  └─ Score < 90 → Loop back (max 3 iterations)
      ↓ (if max exceeded)
      Escalate to Human
```

**Configuration**:
```csharp
public class StandardTierConfig
{
    public string ImplementationAgentId { get; set; } = "claude-code-cli";
    public string EvaluationAgentId { get; set; } = "claude-ai-api"; // Same provider
    public bool EnableTaskDecomposition { get; set; } = true;
    public int MaxParallelTasks { get; set; } = 4;
    public bool RequireCompilation { get; set; } = true;
    public bool RequireTests { get; set; } = true;
    public int MinimumTestCoverage { get; set; } = 80;
    public int MinimumQualityScore { get; set; } = 90;
    public int MaxIterations { get; set; } = 3;
}
```

**Estimated Duration**: 8-15 minutes
**Estimated Cost**: $2.50 - $5.00

---

### Tier 3: Comprehensive (Implementation + Evaluation + Independent Code Review)

**Complexity Range**: 7.1 - 10.0
**Use Case**: Complex/critical features (authentication, payment processing, security changes)

**Workflow**:
```
PlanApprovedMessage
  ↓
TaskDecomposition (required)
  ↓
ParallelImplementation (ClaudeCodeCli × N)
  ↓
CodeIntegration
  ↓
CompilationCheck
  ↓
TestExecution
  ↓
SelfEvaluation (Implementation agent: Claude)
  ↓
IndependentCodeReview (Different agent: Codex)
  ↓
SecurityAnalysis (Optional: dedicated security agent)
  ↓
ConsolidatedEvaluation (merge scores)
  ↓
IterationDecision
  ├─ Both scores >= 90 → GitCommit → PR
  ├─ Either score < 90 → Loop back (max 5 iterations)
  └─ Critical issues → Immediate escalation
```

**Configuration**:
```csharp
public class ComprehensiveTierConfig
{
    public string ImplementationAgentId { get; set; } = "claude-code-cli";
    public string EvaluationAgentId { get; set; } = "claude-ai-api";
    public string CodeReviewAgentId { get; set; } = "codex-cli"; // Different provider
    public string? SecurityAnalysisAgentId { get; set; } = "snyk-agent"; // Optional

    public bool RequireTaskDecomposition { get; set; } = true;
    public int MaxParallelTasks { get; set; } = 6;
    public bool RequireCompilation { get; set; } = true;
    public bool RequireTests { get; set; } = true;
    public int MinimumTestCoverage { get; set; } = 90;
    public int MinimumSelfEvaluationScore { get; set; } = 90;
    public int MinimumCodeReviewScore { get; set; } = 90;
    public bool RequireSecurityAnalysis { get; set; } = true;
    public int MaxIterations { get; set; } = 5;

    // Consolidated scoring weights
    public decimal SelfEvaluationWeight { get; set; } = 0.40m;
    public decimal CodeReviewWeight { get; set; } = 0.50m;
    public decimal SecurityWeight { get; set; } = 0.10m;
}
```

**Estimated Duration**: 15-30 minutes
**Estimated Cost**: $5.00 - $12.00

---

## Agent Registry & Configuration

### Database Schema

```csharp
/// <summary>
/// Stores available agent implementations per tenant
/// </summary>
public class AgentRegistration
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string AgentId { get; private set; } = string.Empty; // "claude-code-cli"
    public string Name { get; private set; } = string.Empty; // "Claude Code CLI"
    public string Provider { get; private set; } = string.Empty; // "Anthropic"
    public AgentCapability Capabilities { get; private set; }
    public string ImplementationType { get; private set; } = string.Empty; // Full type name
    public Dictionary<string, object> Configuration { get; private set; } = new(); // JSON
    public bool IsEnabled { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Tenant Tenant { get; private set; } = null!;
}

/// <summary>
/// Stores workflow tier configuration per tenant
/// </summary>
public class WorkflowTierConfiguration
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public WorkflowTier Tier { get; private set; }

    // Complexity thresholds
    public decimal MinComplexity { get; private set; }
    public decimal MaxComplexity { get; private set; }

    // Agent assignments
    public string ImplementationAgentId { get; private set; } = string.Empty;
    public string? EvaluationAgentId { get; private set; }
    public string? CodeReviewAgentId { get; private set; }
    public string? SecurityAnalysisAgentId { get; private set; }

    // Workflow settings (JSON)
    public Dictionary<string, object> Settings { get; private set; } = new();

    // Navigation
    public Tenant Tenant { get; private set; } = null!;
}

public enum WorkflowTier
{
    Simple = 1,
    Standard = 2,
    Comprehensive = 3
}
```

### Configuration API

```csharp
public interface IWorkflowTierService
{
    /// <summary>
    /// Get recommended workflow tier based on complexity assessment
    /// </summary>
    Task<WorkflowTier> GetRecommendedTierAsync(Guid tenantId, ComplexityAssessment assessment);

    /// <summary>
    /// Get workflow configuration for a tier
    /// </summary>
    Task<WorkflowTierConfiguration> GetTierConfigurationAsync(Guid tenantId, WorkflowTier tier);

    /// <summary>
    /// Get agents assigned to a workflow tier
    /// </summary>
    Task<WorkflowAgents> GetTierAgentsAsync(Guid tenantId, WorkflowTier tier);
}

public class WorkflowAgents
{
    public IAgentImplementation ImplementationAgent { get; set; } = null!;
    public IAgentImplementation? EvaluationAgent { get; set; }
    public IAgentImplementation? CodeReviewAgent { get; set; }
    public IAgentImplementation? SecurityAnalysisAgent { get; set; }
}
```

---

## Workflow Execution Patterns

### Pattern 1: Simple Tier Execution

```csharp
public class SimpleTierExecutor : IWorkflowExecutor
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IBuildService _buildService;

    public async Task<WorkflowResult> ExecuteAsync(ImplementationContext context, CancellationToken ct)
    {
        var agent = _agentRegistry.GetAgent(context.Configuration.ImplementationAgentId);

        // Step 1: Implementation
        var implResult = await agent.ExecuteAsync(new AgentRequest
        {
            TaskDescription = context.Plan,
            FilePaths = context.FilesToModify,
            Role = AgentRole.Implementation
        }, ct);

        if (!implResult.Success)
            return WorkflowResult.Failed(implResult.Errors);

        // Step 2: Compilation check
        var buildResult = await _buildService.BuildAsync(context.RepositoryPath, ct);
        if (!buildResult.Success)
            return WorkflowResult.Failed($"Compilation failed: {buildResult.Errors}");

        // Step 3: Success (no evaluation)
        return WorkflowResult.Success(new WorkflowOutput
        {
            ModifiedFiles = implResult.ModifiedFiles,
            QualityScore = null, // No evaluation in Simple tier
            Iterations = 1
        });
    }
}
```

### Pattern 2: Standard Tier Execution (with Self-Evaluation)

```csharp
public class StandardTierExecutor : IWorkflowExecutor
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IBuildService _buildService;
    private readonly ITestRunner _testRunner;
    private const int MaxIterations = 3;

    public async Task<WorkflowResult> ExecuteAsync(ImplementationContext context, CancellationToken ct)
    {
        var implAgent = _agentRegistry.GetAgent(context.Configuration.ImplementationAgentId);
        var evalAgent = _agentRegistry.GetAgent(context.Configuration.EvaluationAgentId);

        for (int iteration = 1; iteration <= MaxIterations; iteration++)
        {
            // Step 1: Implementation
            var implResult = await implAgent.ExecuteAsync(new AgentRequest
            {
                TaskDescription = iteration == 1 ? context.Plan : context.GapFixes,
                FilePaths = context.FilesToModify,
                Role = AgentRole.Implementation
            }, ct);

            if (!implResult.Success)
                return WorkflowResult.Failed(implResult.Errors);

            // Step 2: Build
            var buildResult = await _buildService.BuildAsync(context.RepositoryPath, ct);
            if (!buildResult.Success)
            {
                context.GapFixes = $"Fix compilation errors:\n{buildResult.Errors}";
                continue; // Retry
            }

            // Step 3: Tests
            var testResult = await _testRunner.RunTestsAsync(context.RepositoryPath, ct);
            if (testResult.FailedCount > 0)
            {
                context.GapFixes = $"Fix failing tests:\n{testResult.FailureDetails}";
                continue; // Retry
            }

            // Step 4: Evaluation (same agent as implementation)
            var evalResult = await evalAgent.ExecuteAsync(new AgentRequest
            {
                TaskDescription = "Evaluate implementation quality",
                FilePaths = implResult.ModifiedFiles,
                Context = new Dictionary<string, object>
                {
                    ["originalPlan"] = context.Plan,
                    ["buildOutput"] = buildResult.Output,
                    ["testOutput"] = testResult.Output,
                    ["testCoverage"] = testResult.CoveragePercent
                },
                Role = AgentRole.Evaluation
            }, ct);

            // Step 5: Decision
            if (evalResult.EvaluationScore >= context.Configuration.MinimumQualityScore)
            {
                return WorkflowResult.Success(new WorkflowOutput
                {
                    ModifiedFiles = implResult.ModifiedFiles,
                    QualityScore = evalResult.EvaluationScore.Value,
                    Iterations = iteration
                });
            }

            // Prepare gap fixes for next iteration
            context.GapFixes = FormatGapFixes(evalResult.Gaps);
        }

        // Max iterations exceeded
        return WorkflowResult.Escalated("Max iterations exceeded", context.GapFixes);
    }
}
```

### Pattern 3: Comprehensive Tier Execution (Multi-Agent Review)

```csharp
public class ComprehensiveTierExecutor : IWorkflowExecutor
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IBuildService _buildService;
    private readonly ITestRunner _testRunner;
    private const int MaxIterations = 5;

    public async Task<WorkflowResult> ExecuteAsync(ImplementationContext context, CancellationToken ct)
    {
        var implAgent = _agentRegistry.GetAgent(context.Configuration.ImplementationAgentId);
        var evalAgent = _agentRegistry.GetAgent(context.Configuration.EvaluationAgentId);
        var reviewAgent = _agentRegistry.GetAgent(context.Configuration.CodeReviewAgentId);
        var securityAgent = context.Configuration.SecurityAnalysisAgentId != null
            ? _agentRegistry.GetAgent(context.Configuration.SecurityAnalysisAgentId)
            : null;

        for (int iteration = 1; iteration <= MaxIterations; iteration++)
        {
            // Step 1: Implementation
            var implResult = await implAgent.ExecuteAsync(new AgentRequest
            {
                TaskDescription = iteration == 1 ? context.Plan : context.GapFixes,
                FilePaths = context.FilesToModify,
                Role = AgentRole.Implementation
            }, ct);

            if (!implResult.Success)
                return WorkflowResult.Failed(implResult.Errors);

            // Step 2: Build
            var buildResult = await _buildService.BuildAsync(context.RepositoryPath, ct);
            if (!buildResult.Success)
            {
                context.GapFixes = $"Fix compilation errors:\n{buildResult.Errors}";
                continue;
            }

            // Step 3: Tests
            var testResult = await _testRunner.RunTestsAsync(context.RepositoryPath, ct);
            if (testResult.FailedCount > 0)
            {
                context.GapFixes = $"Fix failing tests:\n{testResult.FailureDetails}";
                continue;
            }

            // Step 4: Self-Evaluation (implementation agent evaluates itself)
            var evalResult = await evalAgent.ExecuteAsync(new AgentRequest
            {
                TaskDescription = "Evaluate implementation quality",
                FilePaths = implResult.ModifiedFiles,
                Context = BuildEvaluationContext(context, buildResult, testResult),
                Role = AgentRole.Evaluation
            }, ct);

            // Step 5: Independent Code Review (different agent)
            var reviewResult = await reviewAgent.ExecuteAsync(new AgentRequest
            {
                TaskDescription = "Perform independent code review",
                FilePaths = implResult.ModifiedFiles,
                Context = BuildEvaluationContext(context, buildResult, testResult),
                Role = AgentRole.CodeReview
            }, ct);

            // Step 6: Security Analysis (optional)
            AgentResponse? securityResult = null;
            if (securityAgent != null)
            {
                securityResult = await securityAgent.ExecuteAsync(new AgentRequest
                {
                    TaskDescription = "Analyze security vulnerabilities",
                    FilePaths = implResult.ModifiedFiles,
                    Role = AgentRole.CodeReview
                }, ct);

                // Critical security issues trigger immediate escalation
                if (securityResult.Gaps?.Any(g => g.Severity == "critical") == true)
                {
                    return WorkflowResult.Escalated(
                        "Critical security issues found",
                        FormatSecurityIssues(securityResult.Gaps));
                }
            }

            // Step 7: Consolidate scores
            var consolidatedScore = CalculateConsolidatedScore(
                evalResult.EvaluationScore!.Value,
                reviewResult.EvaluationScore!.Value,
                securityResult?.EvaluationScore,
                context.Configuration);

            // Step 8: Decision
            if (consolidatedScore >= context.Configuration.MinimumQualityScore)
            {
                return WorkflowResult.Success(new WorkflowOutput
                {
                    ModifiedFiles = implResult.ModifiedFiles,
                    QualityScore = consolidatedScore,
                    SelfEvaluationScore = evalResult.EvaluationScore.Value,
                    CodeReviewScore = reviewResult.EvaluationScore.Value,
                    SecurityScore = securityResult?.EvaluationScore,
                    Iterations = iteration
                });
            }

            // Merge gaps from all reviewers
            var allGaps = MergeGaps(evalResult.Gaps, reviewResult.Gaps, securityResult?.Gaps);
            context.GapFixes = FormatGapFixes(allGaps);
        }

        return WorkflowResult.Escalated("Max iterations exceeded", context.GapFixes);
    }

    private decimal CalculateConsolidatedScore(
        int selfEvalScore,
        int reviewScore,
        int? securityScore,
        ComprehensiveTierConfig config)
    {
        var totalWeight = config.SelfEvaluationWeight + config.CodeReviewWeight;
        var weightedScore =
            selfEvalScore * config.SelfEvaluationWeight +
            reviewScore * config.CodeReviewWeight;

        if (securityScore.HasValue && config.RequireSecurityAnalysis)
        {
            totalWeight += config.SecurityWeight;
            weightedScore += securityScore.Value * config.SecurityWeight;
        }

        return weightedScore / totalWeight;
    }
}
```

---

## Integration with Planning Phase

### Updated PlanningGraph

The `PlanningGraph` needs to output **both** the implementation plan **and** complexity assessment:

```csharp
public class PlanningGraph : AgentGraphBase<PlanningContext>
{
    protected override void ConfigureGraph(IGraphBuilder<PlanningContext> builder)
    {
        builder
            .StartWith<PlanGenerationAgent>()
            .Then<ComplexityAssessmentAgent>() // NEW: Assess complexity
            .ParallelExecution(
                b => b.AddAgent<GitPlanAgent>(),
                b => b.AddAgent<JiraPostPlanAgent>())
            .Then<AwaitApprovalAgent>()
            .HandleEvent<PlanApprovedEvent>(OnPlanApproved)
            .HandleEvent<PlanRejectedEvent>(OnPlanRejected);
    }

    private Task OnPlanApproved(PlanApprovedEvent evt)
    {
        // Pass complexity assessment to ImplementationGraph
        var message = new PlanApprovedMessage(
            evt.TicketId,
            evt.PlanMarkdown,
            Context.ComplexityAssessment); // Include assessment

        return PublishAsync(message);
    }
}
```

### ComplexityAssessmentAgent

```csharp
public class ComplexityAssessmentAgent : IAgent<PlanningContext>
{
    public async Task<AgentResult> ExecuteAsync(PlanningContext context, CancellationToken ct)
    {
        var prompt = $@"
You are a software complexity analyst. Analyze this implementation plan and assess its complexity.

# Implementation Plan

{context.PlanMarkdown}

# Repository Context

- Total files: {context.RepositoryStats.TotalFiles}
- Primary language: {context.RepositoryStats.PrimaryLanguage}
- Codebase size: {context.RepositoryStats.TotalLinesOfCode} LOC

# Assessment Criteria

Score each dimension 1-10:

1. **Code Volume Complexity**: Files and LOC to modify
2. **Logical Complexity**: Algorithm difficulty
3. **Architectural Impact**: Cross-cutting concerns
4. **Testing Complexity**: Test requirements
5. **Risk Level**: Security, data, performance risks

Output as JSON:

```json
{{
  ""codeVolumeComplexity"": 5,
  ""logicalComplexity"": 7,
  ""architecturalImpact"": 4,
  ""testingComplexity"": 6,
  ""riskLevel"": 3,
  ""justification"": ""Moderate code changes with some logical complexity but isolated architectural impact""
}}
```
";

        var response = await _claudeClient.CreateMessageAsync(new CreateMessageRequest
        {
            Model = "claude-sonnet-4-5-20250929",
            Messages = new[] { new Message { Role = "user", Content = prompt } },
            MaxTokens = 2000
        }, ct);

        var json = ExtractJson(response.Content[0].Text);
        var assessment = JsonSerializer.Deserialize<ComplexityAssessment>(json);

        context.ComplexityAssessment = assessment;

        _logger.LogInformation(
            "Complexity assessed: {OverallComplexity:F1}/10 → Tier {Tier}",
            assessment.OverallComplexity,
            assessment.RecommendedTier);

        return AgentResult.Success();
    }
}
```

### Updated ImplementationGraph

```csharp
public class EnhancedImplementationGraph : AgentGraphBase<ImplementationContext>
{
    private readonly IWorkflowTierService _tierService;

    protected override void ConfigureGraph(IGraphBuilder<ImplementationContext> builder)
    {
        builder
            .StartWith<WorkflowTierSelectionAgent>() // NEW: Select tier based on complexity
            .ConditionalExecution(
                condition: ctx => ctx.WorkflowTier == WorkflowTier.Simple,
                thenBranch: b => b.AddAgent<SimpleTierExecutor>(),
                elseBranch: b => b
                    .ConditionalExecution(
                        condition: ctx => ctx.WorkflowTier == WorkflowTier.Standard,
                        thenBranch: b2 => b2.AddAgent<StandardTierExecutor>(),
                        elseBranch: b2 => b2.AddAgent<ComprehensiveTierExecutor>()))
            .HandleResult<WorkflowResult.Success>(OnSuccess)
            .HandleResult<WorkflowResult.Escalated>(OnEscalated)
            .HandleResult<WorkflowResult.Failed>(OnFailed);
    }

    private async Task OnSuccess(WorkflowResult.Success result)
    {
        // Commit and create PR
        await _gitService.CommitAsync(Context.RepositoryId, result.ModifiedFiles);
        await _prService.CreatePullRequestAsync(Context.RepositoryId);
        await PublishAsync(new ImplementationCompletedMessage(Context.TicketId, result.QualityScore));
    }

    private async Task OnEscalated(WorkflowResult.Escalated result)
    {
        // Suspend workflow, notify human
        await _workflowService.SuspendAsync(Context.TicketId, result.Reason);
        await _jiraService.PostCommentAsync(Context.TicketId,
            $"Implementation needs human review. Reason: {result.Reason}\n\nGaps:\n{result.GapDetails}");
    }
}
```

---

## Configuration Schema Updates

### Extended TenantConfiguration

```csharp
public partial class TenantConfiguration
{
    // ... existing properties ...

    // Quality Loop Configuration
    public bool EnableQualityEvaluation { get; set; } = false; // Feature flag

    // Complexity thresholds (can override defaults)
    public decimal SimpleTierMaxComplexity { get; set; } = 3.0m;
    public decimal StandardTierMaxComplexity { get; set; } = 7.0m;

    // Allow manual tier override
    public bool AllowManualTierOverride { get; set; } = true;

    // Default agents (can be overridden per tier)
    public string DefaultImplementationAgentId { get; set; } = "claude-code-cli";
    public string DefaultEvaluationAgentId { get; set; } = "claude-ai-api";
    public string? DefaultCodeReviewAgentId { get; set; } = null; // Optional
    public string? DefaultSecurityAnalysisAgentId { get; set; } = null; // Optional
}
```

### Per-Tier Configuration Storage

Stored in `WorkflowTierConfiguration` table (see Database Schema section above).

---

## Database Schema Additions

### Migration: AddMultiAgentSupport

```sql
-- Agent Registry
CREATE TABLE AgentRegistrations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    AgentId NVARCHAR(100) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Provider NVARCHAR(100) NOT NULL,
    Capabilities INT NOT NULL, -- Flags enum
    ImplementationType NVARCHAR(500) NOT NULL,
    Configuration NVARCHAR(MAX), -- JSON
    IsEnabled BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_AgentRegistrations_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    CONSTRAINT UQ_AgentRegistrations_TenantAgent UNIQUE (TenantId, AgentId)
);

-- Workflow Tier Configuration
CREATE TABLE WorkflowTierConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Tier INT NOT NULL, -- 1=Simple, 2=Standard, 3=Comprehensive
    MinComplexity DECIMAL(4,2) NOT NULL,
    MaxComplexity DECIMAL(4,2) NOT NULL,
    ImplementationAgentId NVARCHAR(100) NOT NULL,
    EvaluationAgentId NVARCHAR(100),
    CodeReviewAgentId NVARCHAR(100),
    SecurityAnalysisAgentId NVARCHAR(100),
    Settings NVARCHAR(MAX), -- JSON
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,
    CONSTRAINT FK_WorkflowTierConfigurations_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    CONSTRAINT UQ_WorkflowTierConfigurations_TenantTier UNIQUE (TenantId, Tier)
);

-- Complexity Assessment (stored with plan)
ALTER TABLE Tickets ADD ComplexityAssessmentJson NVARCHAR(MAX);
ALTER TABLE Tickets ADD RecommendedWorkflowTier INT; -- 1/2/3
ALTER TABLE Tickets ADD ActualWorkflowTier INT; -- Can differ if manually overridden

-- Quality Evaluation (updated schema from original design)
CREATE TABLE QualityEvaluations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TicketId UNIQUEIDENTIFIER NOT NULL,
    IterationNumber INT NOT NULL,
    WorkflowTier INT NOT NULL,

    -- Agent-specific scores
    SelfEvaluationScore INT, -- 1-100, from implementation agent
    CodeReviewScore INT, -- 1-100, from code review agent
    SecurityScore INT, -- 1-100, from security agent

    -- Overall
    ConsolidatedScore INT NOT NULL, -- Weighted average
    Gaps NVARCHAR(MAX), -- JSON array of QualityGap
    Status NVARCHAR(50) NOT NULL, -- Approved, Rejected, Iteration
    CreatedAt DATETIME2 NOT NULL,

    CONSTRAINT FK_QualityEvaluations_Tickets FOREIGN KEY (TicketId) REFERENCES Tickets(Id)
);

CREATE INDEX IX_QualityEvaluations_Ticket ON QualityEvaluations(TicketId, IterationNumber);
```

---

## Implementation Examples

### Example 1: Registering Agents for a Tenant

```csharp
public class AgentRegistrationService
{
    public async Task SetupDefaultAgentsAsync(Guid tenantId)
    {
        // Register Claude Code CLI for implementation
        await RegisterAgentAsync(new AgentRegistration(
            tenantId: tenantId,
            agentId: "claude-code-cli",
            name: "Claude Code CLI",
            provider: "Anthropic",
            capabilities: AgentCapability.CodeImplementation | AgentCapability.TestGeneration,
            implementationType: typeof(ClaudeCodeCliAgent).AssemblyQualifiedName!,
            configuration: new Dictionary<string, object>
            {
                ["executablePath"] = "/usr/local/bin/claude-code",
                ["timeout"] = 600 // 10 minutes
            }));

        // Register Claude AI API for evaluation
        await RegisterAgentAsync(new AgentRegistration(
            tenantId: tenantId,
            agentId: "claude-ai-api",
            name: "Claude AI (API)",
            provider: "Anthropic",
            capabilities: AgentCapability.QualityEvaluation | AgentCapability.CodeReview,
            implementationType: typeof(ClaudeAiAgent).AssemblyQualifiedName!,
            configuration: new Dictionary<string, object>
            {
                ["apiKey"] = "sk-ant-...",
                ["model"] = "claude-sonnet-4-5-20250929"
            }));

        // Register Codex CLI for code review (optional)
        await RegisterAgentAsync(new AgentRegistration(
            tenantId: tenantId,
            agentId: "codex-cli",
            name: "Codex CLI",
            provider: "OpenAI",
            capabilities: AgentCapability.QualityEvaluation | AgentCapability.CodeReview,
            implementationType: typeof(CodexCliAgent).AssemblyQualifiedName!,
            configuration: new Dictionary<string, object>
            {
                ["executablePath"] = "/usr/local/bin/codex",
                ["apiKey"] = "sk-..."
            }));
    }
}
```

### Example 2: Configuring Workflow Tiers

```csharp
public class WorkflowTierSetupService
{
    public async Task SetupDefaultTiersAsync(Guid tenantId)
    {
        // Tier 1: Simple
        await CreateTierConfigAsync(new WorkflowTierConfiguration(
            tenantId: tenantId,
            tier: WorkflowTier.Simple,
            minComplexity: 0.0m,
            maxComplexity: 3.0m,
            implementationAgentId: "claude-code-cli",
            evaluationAgentId: null, // No evaluation
            settings: new SimpleTierConfig
            {
                RequireCompilation = true,
                RequireTests = false,
                RequireEvaluation = false
            }.ToDictionary()));

        // Tier 2: Standard
        await CreateTierConfigAsync(new WorkflowTierConfiguration(
            tenantId: tenantId,
            tier: WorkflowTier.Standard,
            minComplexity: 3.1m,
            maxComplexity: 7.0m,
            implementationAgentId: "claude-code-cli",
            evaluationAgentId: "claude-ai-api", // Same provider
            settings: new StandardTierConfig
            {
                EnableTaskDecomposition = true,
                MaxParallelTasks = 4,
                MinimumQualityScore = 90,
                MaxIterations = 3
            }.ToDictionary()));

        // Tier 3: Comprehensive
        await CreateTierConfigAsync(new WorkflowTierConfiguration(
            tenantId: tenantId,
            tier: WorkflowTier.Comprehensive,
            minComplexity: 7.1m,
            maxComplexity: 10.0m,
            implementationAgentId: "claude-code-cli",
            evaluationAgentId: "claude-ai-api",
            codeReviewAgentId: "codex-cli", // Different provider
            securityAnalysisAgentId: null, // Optional
            settings: new ComprehensiveTierConfig
            {
                MinimumSelfEvaluationScore = 90,
                MinimumCodeReviewScore = 90,
                MaxIterations = 5,
                SelfEvaluationWeight = 0.40m,
                CodeReviewWeight = 0.50m,
                SecurityWeight = 0.10m
            }.ToDictionary()));
    }
}
```

### Example 3: Workflow Execution with Multi-Agent

```csharp
// In EnhancedImplementationGraph
public class WorkflowTierSelectionAgent : IAgent<ImplementationContext>
{
    private readonly IWorkflowTierService _tierService;

    public async Task<AgentResult> ExecuteAsync(ImplementationContext context, CancellationToken ct)
    {
        // Get complexity assessment from planning phase
        var assessment = context.ComplexityAssessment;

        // Get recommended tier
        var tier = await _tierService.GetRecommendedTierAsync(context.TenantId, assessment);

        // Allow manual override if configured
        if (context.ManualTierOverride.HasValue && context.AllowManualTierOverride)
        {
            _logger.LogInformation(
                "Manual tier override: {Recommended} → {Actual}",
                tier,
                context.ManualTierOverride.Value);
            tier = context.ManualTierOverride.Value;
        }

        // Load tier configuration and agents
        var config = await _tierService.GetTierConfigurationAsync(context.TenantId, tier);
        var agents = await _tierService.GetTierAgentsAsync(context.TenantId, tier);

        // Store in context for next agents
        context.WorkflowTier = tier;
        context.TierConfiguration = config;
        context.Agents = agents;

        _logger.LogInformation(
            "Selected workflow tier {Tier} (complexity: {Complexity:F1}/10)",
            tier,
            assessment.OverallComplexity);

        return AgentResult.Success();
    }
}
```

---

## Summary

This addendum transforms the original implementation quality loop design into a **flexible, multi-agent, complexity-aware system**:

### Key Improvements

1. **Pluggable Agents**: Support Claude Code CLI, Codex CLI, or any custom agent implementation
2. **Complexity Assessment**: Planning phase automatically assesses task complexity
3. **3-Tier Workflows**: Simple (no eval), Standard (self-eval), Comprehensive (multi-agent review)
4. **Independent Review**: Different agents for implementation vs code review prevents bias
5. **Configurable**: Per-tenant agent registry and tier configuration
6. **Cost-Effective**: Simple tasks skip expensive evaluation; complex tasks get thorough review
7. **Production-Ready**: Robust error handling, escalation, monitoring

### Implementation Priority

**Phase 1 (Weeks 1-2)**: Foundation
- Implement `IAgentImplementation` interface
- Create `ClaudeCodeCliAgent` and `ClaudeAiAgent`
- Add complexity assessment to PlanningGraph
- Database schema for agent registry

**Phase 2 (Weeks 3-4)**: Tier Executors
- Implement `SimpleTierExecutor`
- Implement `StandardTierExecutor`
- Add tier selection logic

**Phase 3 (Weeks 5-6)**: Multi-Agent Review
- Implement `CodexCliAgent` (or similar)
- Implement `ComprehensiveTierExecutor`
- Consolidated scoring logic

**Phase 4 (Week 7)**: Production
- Configuration UI
- Monitoring dashboards
- Gradual rollout

This design provides a production-ready path to intelligent, automated code quality assurance while maintaining flexibility for different agent implementations and task complexities.
