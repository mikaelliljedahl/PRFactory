# Implementation Quality Loop - Design Document

**Status**: Draft  
**Created**: 2025-11-09  
**Author**: AI Agent Analysis  
**Purpose**: Design a built-in quality evaluation and iteration loop for PRFactory's Phase 3 (Code Implementation)

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Background](#background)
- [Design Principles](#design-principles)
- [Architecture Overview](#architecture-overview)
- [Graph Architecture Decision](#graph-architecture-decision)
- [Agent Specifications](#agent-specifications)
- [Workflow Stages](#workflow-stages)
- [Prompt Templates](#prompt-templates)
- [Configuration Schema](#configuration-schema)
- [Database Schema](#database-schema)
- [Integration Strategy](#integration-strategy)
- [Error Handling & Edge Cases](#error-handling--edge-cases)
- [Success Metrics & Monitoring](#success-metrics--monitoring)
- [Phased Implementation Roadmap](#phased-implementation-roadmap)
- [Appendices](#appendices)

---

## Executive Summary

This document designs a **quality-driven implementation workflow** for PRFactory that:

1. **Decomposes** approved implementation plans into parallelizable tasks
2. **Executes** multiple code implementation agents simultaneously
3. **Evaluates** implementation quality using AI-powered code review
4. **Iterates** automatically on quality gaps until reaching approval threshold (90+ score)
5. **Escalates** to human review if max iterations exceeded or critical issues found

**Key Innovation**: This transforms PRFactory from a linear "plan → implement → PR" workflow into an intelligent "plan → parallel implement → evaluate → iterate → PR" cycle that produces higher quality code with fewer human review cycles.

**Alignment with CLAUDE.md**:
- Extends existing ImplementationGraph (not replacing)
- Uses parallel execution pattern (already proven in PlanningGraph)
- Follows multi-graph architecture and checkpointing patterns
- Maintains Clean Architecture separation
- Supports tenant-level configuration

---

## Background

### Current Phase 3 Limitations

The existing `ImplementationGraph` has these limitations:

1. **Single-threaded execution**: One `ImplementationAgent` does all coding sequentially
2. **No quality validation**: Code goes straight to PR without automated checks
3. **No iteration**: Compilation/test failures require human intervention
4. **Manual decomposition**: Implementation plan is a single large task

**Current Flow:**
```
PlanApprovedMessage → ImplementationAgent → GitCommitAgent → (PullRequestAgent + JiraPostAgent parallel) → CompletionAgent
```

### Observed High-Quality Pattern

Recent work showed a superior pattern:

1. **Planning Agent** → Break down work into specific, testable tasks
2. **4 Parallel Code-Implementation Agents** → Implement all tasks simultaneously
3. **Evaluation-Specialist Agent** → Score quality (99/100), identify gaps
4. **Iteration** → Fix gaps until reaching 100% score

**Result**: All tests passing, 100% coverage, excellent code quality, no human rework needed.

### Goal

Embed this pattern into PRFactory so every implementation automatically benefits from:
- Intelligent task decomposition
- Parallel execution for speed
- AI-powered quality evaluation
- Automated iteration on gaps
- Higher code quality at PR creation time

---

## Design Principles

### 1. Extend, Don't Replace

**Principle**: Build upon existing `ImplementationGraph` rather than replacing it.

**Rationale**:
- Existing graph has proven patterns (checkpointing, parallel execution)
- Backward compatibility for tenants with auto-implementation disabled
- Gradual rollout via feature flags

**Approach**: Replace single `ImplementationAgent` with multi-stage workflow.

### 2. Parallel by Default

**Principle**: Leverage parallelization whenever tasks are independent.

**Rationale**:
- PRFactory already uses parallel execution (GitPlan + JiraPost)
- Modern AI APIs support high concurrency
- Significantly reduces wall-clock time

**Examples**:
- Multiple `CodeImplementationAgent` instances run simultaneously
- Compilation + Test Execution can run in parallel
- GitCommit + JiraPost remain parallel (existing pattern)

### 3. Quality-Driven Iteration

**Principle**: Don't proceed until quality threshold met or max iterations exceeded.

**Rationale**:
- Automated iteration is cheaper than human review cycles
- Higher quality code at PR creation time reduces review burden
- AI evaluation is consistent and comprehensive

**Threshold**: 90/100 score (configurable per tenant)  
**Max Iterations**: 3 (configurable per tenant)

### 4. Fail-Safe with Human Escalation

**Principle**: Suspend workflow and notify humans if automated iteration fails.

**Rationale**:
- Some problems require human judgment (architectural decisions, business logic)
- Max iterations prevent infinite loops
- Maintains human oversight for critical failures

**Escalation Triggers**:
- Max iterations exceeded without reaching quality threshold
- Compilation failures after 3 attempts
- Test failures indicating plan issues (not implementation bugs)

### 5. Tenant-Level Configuration

**Principle**: All new features must be configurable per-tenant.

**Rationale**:
- PRFactory is multi-tenant SaaS
- Different customers have different quality standards and risk tolerance
- Gradual rollout to production tenants

---

## Architecture Overview

### High-Level Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         ENHANCED IMPLEMENTATION GRAPH                           │
└─────────────────────────────────────────────────────────────────────────────────┘

 INPUT: PlanApprovedMessage (from PlanningGraph)
   │
   ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 1: Task Planning                                                       │
│ Agent: TaskPlanningAgent                                                     │
│ Input: Approved plan markdown + codebase context                            │
│ Output: Task breakdown JSON (task ID, description, files, dependencies)     │
│ Checkpoint: task_plan_generated                                             │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 2: Dependency Analysis                                                 │
│ Agent: DependencyAnalysisAgent                                               │
│ Input: Task breakdown JSON                                                   │
│ Output: Parallelization strategy (task groups, execution order)             │
│ Checkpoint: dependency_analysis_complete                                     │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 3: Parallel Implementation [LOOP START]                                │
│ Agents: Multiple CodeImplementationAgent instances (up to MaxParallelTasks) │
│ Input: Individual task specifications                                        │
│ Output: Code changes per task (modified/created files)                      │
│ Checkpoint: implementation_iteration_N_complete                              │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 4: Code Integration                                                    │
│ Agent: CodeIntegrationAgent                                                  │
│ Input: All code changes from parallel agents                                │
│ Output: Integrated codebase (merge conflicts resolved)                      │
│ Checkpoint: code_integrated                                                  │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 5: Build Validation                                                    │
│ Agent: CompilationAgent                                                      │
│ Input: Integrated codebase                                                   │
│ Output: Build success/failure + compiler errors                             │
│ Checkpoint: build_validated                                                  │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 6: Test Execution                                                      │
│ Agent: TestExecutionAgent                                                    │
│ Input: Compiled code                                                         │
│ Output: Test results (pass/fail counts, coverage %)                         │
│ Checkpoint: tests_executed                                                   │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 7: Quality Evaluation                                                  │
│ Agent: QualityEvaluationAgent                                                │
│ Input: Code + build results + test results + original plan                  │
│ Output: Quality score (1-100) + dimension scores + gap analysis             │
│ Checkpoint: quality_evaluated_iteration_N                                    │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 8: Iteration Decision                                                  │
│ Agent: IterationCoordinatorAgent                                             │
│ Input: Quality evaluation + iteration count                                 │
│ Decision Logic:                                                              │
│   - Score >= MinQualityScore (90) → APPROVE → Continue to Stage 9          │
│   - Score < MinQualityScore AND iterations < MaxIterations (3) → ITERATE   │
│     └─→ Generate gap fix instructions → Loop back to Stage 3               │
│   - Score < MinQualityScore AND iterations >= MaxIterations → ESCALATE     │
│     └─→ Suspend workflow, post to Jira, await human intervention           │
│ Checkpoint: iteration_decision_N                                             │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │ [If APPROVE]
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 9: Git Commit                                                          │
│ Agent: GitCommitAgent (existing)                                             │
│ Input: Approved implementation                                               │
│ Output: Committed changes on implementation branch                          │
│ Checkpoint: code_committed                                                   │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 10: PR Creation & Jira Post (PARALLEL - existing pattern)             │
│ Agents: PullRequestAgent + JiraPostAgent (existing, run in parallel)        │
│ Input: Committed code                                                        │
│ Output: PR URL + Jira comment with quality report                           │
│ Checkpoint: pr_created                                                       │
└──────────────┬───────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ STAGE 11: Completion                                                         │
│ Agent: CompletionAgent (existing)                                            │
│ Output: WorkflowCompletedMessage                                             │
│ Checkpoint: completed                                                        │
└──────────────────────────────────────────────────────────────────────────────┘

 OUTPUT: WorkflowCompletedMessage → WorkflowOrchestrator marks workflow complete
```

### Iteration Loop Detail

```
┌─────────────────────────────────────────────────────────────────┐
│                    ITERATION LOOP                               │
└─────────────────────────────────────────────────────────────────┘

  [Stage 3: Parallel Implementation]
              │
              ▼
  [Stage 4: Code Integration]
              │
              ▼
  [Stage 5: Build Validation]
              │
              ▼
  [Stage 6: Test Execution]
              │
              ▼
  [Stage 7: Quality Evaluation]
              │
              ▼
  [Stage 8: Iteration Decision]
              │
              ├─→ [APPROVE: Score >= 90] → Continue to Stage 9 (Git Commit)
              │
              ├─→ [ITERATE: Score < 90 AND iteration < 3]
              │       │
              │       └─→ Generate GapFixInstructions
              │           └─→ Loop back to Stage 3 (Parallel Implementation)
              │               with context:
              │                 - Previous implementation
              │                 - Quality gaps to fix
              │                 - Failed tests
              │                 - Compiler errors
              │
              └─→ [ESCALATE: Score < 90 AND iteration >= 3]
                      └─→ Suspend workflow
                          Post to Jira: "Quality threshold not reached after 3 iterations"
                          Await human intervention (@claude mention for retry or manual fix)
```

---

## Graph Architecture Decision

### Option A: Extend Existing ImplementationGraph ✅ RECOMMENDED

**Description**: Replace the single `ImplementationAgent` call in `ImplementationGraph.cs` with a multi-stage workflow that includes task planning, parallel implementation, evaluation, and iteration.

**Changes Required**:
- Add 7 new agent types: `TaskPlanningAgent`, `DependencyAnalysisAgent`, `CodeImplementationAgent`, `CodeIntegrationAgent`, `CompilationAgent`, `TestExecutionAgent`, `QualityEvaluationAgent`, `IterationCoordinatorAgent`
- Modify `ExecuteCoreAsync` in `ImplementationGraph` to orchestrate new stages
- Add iteration loop logic within the graph
- Extend checkpoint system to track iterations

**Pros**:
- ✅ Minimal architectural changes
- ✅ Backward compatible (conditional execution based on `EnableQualityEvaluation` flag)
- ✅ Reuses existing checkpoint and resumption infrastructure
- ✅ Aligns with CLAUDE.md principle: "Extend existing patterns"
- ✅ No changes required to `WorkflowOrchestrator`
- ✅ Existing `PlanApprovedMessage` input remains unchanged

**Cons**:
- ❌ Makes `ImplementationGraph` more complex (but manageable with helper methods)
- ❌ Single file will be larger (can mitigate with partial classes or helper services)

**Code Structure**:
```csharp
// ImplementationGraph.cs (extended)
protected override async Task<GraphExecutionResult> ExecuteCoreAsync(
    IAgentMessage inputMessage,
    GraphContext context,
    CancellationToken cancellationToken)
{
    // Check if quality evaluation enabled
    var qualityConfig = await GetQualityConfigurationAsync(context.TicketId, cancellationToken);
    
    if (qualityConfig.EnableQualityEvaluation)
    {
        return await ExecuteWithQualityLoopAsync(inputMessage, context, qualityConfig, cancellationToken);
    }
    else
    {
        // Original behavior: single ImplementationAgent
        return await ExecuteLegacyImplementationAsync(inputMessage, context, cancellationToken);
    }
}

private async Task<GraphExecutionResult> ExecuteWithQualityLoopAsync(...)
{
    // Stage 1: Task Planning
    var taskPlan = await ExecuteAgentAsync<TaskPlanningAgent>(...);
    
    // Stage 2: Dependency Analysis
    var dependencies = await ExecuteAgentAsync<DependencyAnalysisAgent>(...);
    
    // ITERATION LOOP
    for (int iteration = 1; iteration <= qualityConfig.MaxIterations; iteration++)
    {
        // Stage 3: Parallel Implementation
        var implementations = await ExecuteParallelImplementationAsync(taskPlan, dependencies, context);
        
        // Stage 4: Code Integration
        var integrated = await ExecuteAgentAsync<CodeIntegrationAgent>(implementations, ...);
        
        // Stage 5: Build Validation
        var buildResult = await ExecuteAgentAsync<CompilationAgent>(integrated, ...);
        
        // Stage 6: Test Execution
        var testResult = await ExecuteAgentAsync<TestExecutionAgent>(buildResult, ...);
        
        // Stage 7: Quality Evaluation
        var evaluation = await ExecuteAgentAsync<QualityEvaluationAgent>(testResult, ...);
        
        // Stage 8: Iteration Decision
        if (evaluation.OverallScore >= qualityConfig.MinimumQualityScore)
        {
            // APPROVE: Continue to git commit
            break;
        }
        else if (iteration < qualityConfig.MaxIterations)
        {
            // ITERATE: Prepare gap fix instructions for next iteration
            context.State["gap_fix_instructions"] = evaluation.GenerateFixInstructions();
            await SaveCheckpointAsync(context, $"iterating_{iteration}", "IterationCoordinator");
            continue; // Loop back
        }
        else
        {
            // ESCALATE: Max iterations exceeded
            return await EscalateToHumanAsync(context, evaluation, cancellationToken);
        }
    }
    
    // Stage 9: Git Commit (existing)
    var committed = await ExecuteAgentAsync<GitCommitAgent>(...);
    
    // Stage 10: PR + Jira (parallel, existing)
    var prTask = ExecuteAgentAsync<PullRequestAgent>(...);
    var jiraTask = ExecuteAgentAsync<JiraPostAgent>(...);
    await Task.WhenAll(prTask, jiraTask);
    
    // Stage 11: Completion (existing)
    return await ExecuteAgentAsync<CompletionAgent>(...);
}
```

### Option B: Create New QualityAssuranceGraph

**Description**: Create a separate `QualityAssuranceGraph` that runs after `ImplementationGraph`.

**Pros**:
- Clean separation of concerns
- `ImplementationGraph` remains simple
- Easy to enable/disable QA workflow independently

**Cons**:
- ❌ Requires changes to `WorkflowOrchestrator` (new graph transition)
- ❌ More complex resumption logic (tracking which graph to resume)
- ❌ Iteration loop requires re-running `ImplementationGraph` (less efficient)
- ❌ Not aligned with CLAUDE.md: ImplementationGraph is already conditional

### Option C: Replace ImplementationGraph with EnhancedImplementationGraph

**Description**: Deprecate existing `ImplementationGraph` and create new implementation from scratch.

**Pros**:
- Clean slate design

**Cons**:
- ❌ Breaking change for existing tenants
- ❌ Violates CLAUDE.md principle: "Extend, don't replace"
- ❌ Loses existing checkpoint/resumption logic
- ❌ Requires migration of existing in-flight workflows

### DECISION: Option A - Extend Existing ImplementationGraph ✅

**Rationale**:
1. Minimal architectural changes (no WorkflowOrchestrator modifications)
2. Backward compatible via feature flag
3. Aligns with CLAUDE.md guidelines
4. Proven pattern (PlanningGraph already uses conditional logic for retries)
5. Single graph owns entire implementation → easier reasoning about state

---

## Agent Specifications

### 1. TaskPlanningAgent

**Purpose**: Decomposes approved implementation plan into parallelizable, testable tasks.

**Inputs**:
- `PlanApprovedMessage` (contains plan markdown via context)
- Codebase context (file structure, patterns)
- Repository path

**Outputs**:
- `TaskPlanGeneratedMessage` containing JSON:
  ```json
  {
    "tasks": [
      {
        "task_id": "task_001",
        "description": "Implement User entity with domain logic",
        "files_to_modify": ["src/Domain/Entities/User.cs"],
        "files_to_create": [],
        "dependencies": [],
        "test_requirements": "Unit tests for User entity methods",
        "acceptance_criteria": ["User.Create() validates inputs", "User.Deactivate() sets IsActive=false"]
      },
      {
        "task_id": "task_002",
        "description": "Create UserRepository interface and implementation",
        "files_to_modify": [],
        "files_to_create": [
          "src/Domain/Interfaces/IUserRepository.cs",
          "src/Infrastructure/Persistence/UserRepository.cs"
        ],
        "dependencies": ["task_001"],
        "test_requirements": "Repository integration tests",
        "acceptance_criteria": ["GetByIdAsync returns user", "SaveAsync persists changes"]
      }
    ],
    "total_tasks": 2,
    "estimated_parallelism": 1
  }
  ```

**Checkpoint**: `task_plan_generated`

**Error Handling**:
- If plan parsing fails → Retry up to 3 times with error context
- If no tasks generated → Fail workflow with error message

**Prompt Template**: See [Prompt Templates](#prompt-templates) section

---

### 2. DependencyAnalysisAgent

**Purpose**: Analyzes task dependencies and generates parallelization strategy.

**Inputs**:
- `TaskPlanGeneratedMessage` (task breakdown JSON)

**Outputs**:
- `DependencyAnalysisCompleteMessage` containing:
  ```json
  {
    "execution_groups": [
      {
        "group_id": 1,
        "parallel_tasks": ["task_001", "task_003", "task_005"],
        "description": "Independent tasks: entities and interfaces"
      },
      {
        "group_id": 2,
        "parallel_tasks": ["task_002", "task_004"],
        "depends_on_groups": [1],
        "description": "Implementations depending on interfaces"
      }
    ],
    "max_parallelism": 3,
    "estimated_duration_seconds": 120
  }
  ```

**Checkpoint**: `dependency_analysis_complete`

**Logic**:
1. Build dependency graph from task dependencies
2. Perform topological sort
3. Group tasks into execution waves (tasks in same wave have no inter-dependencies)
4. Limit parallelism to `MaxParallelTasks` config (default 4)

**Error Handling**:
- Circular dependencies → Fail workflow with error details
- Empty execution groups → Use sequential execution (all tasks in separate groups)

---

### 3. CodeImplementationAgent (Parallel Execution)

**Purpose**: Implements a single task from the task plan.

**Inputs**:
- Task specification JSON (single task from `TaskPlanGeneratedMessage`)
- Codebase context
- Repository path
- Gap fix instructions (if iteration > 1)

**Outputs**:
- `TaskImplementedMessage` containing:
  ```json
  {
    "task_id": "task_001",
    "status": "completed",
    "modified_files": {
      "src/Domain/Entities/User.cs": "<full file content>"
    },
    "created_files": {
      "tests/Domain/UserTests.cs": "<full file content>"
    },
    "summary": "Implemented User entity with Create(), Deactivate(), and validation logic. Added 12 unit tests covering all methods and edge cases."
  }
  ```

**Parallelization**:
- **Multiple instances** run concurrently (up to `MaxParallelTasks`)
- Each instance handles one task
- Execution group determines which tasks run in parallel

**Checkpoint**: None (individual agents don't checkpoint; CodeIntegrationAgent checkpoints after all complete)

**Error Handling**:
- Agent execution timeout → Retry task with extended timeout
- Invalid file paths → Mark task as failed, continue with other tasks
- If any task fails → Entire iteration fails, evaluation agent scores it low

**Prompt Template**: See [Prompt Templates](#prompt-templates) section

---

### 4. CodeIntegrationAgent

**Purpose**: Integrates code changes from all parallel CodeImplementationAgent instances, resolves conflicts.

**Inputs**:
- List of `TaskImplementedMessage` from all parallel agents

**Outputs**:
- `CodeIntegratedMessage` containing:
  ```json
  {
    "merged_files": {
      "src/Domain/Entities/User.cs": "<final merged content>",
      "tests/Domain/UserTests.cs": "<final merged content>"
    },
    "conflicts_resolved": 2,
    "integration_summary": "Merged 5 tasks, resolved 2 conflicts in imports"
  }
  ```

**Checkpoint**: `code_integrated`

**Logic**:
1. Collect all modified/created files from parallel agents
2. Detect conflicts (multiple agents modifying same file)
3. Use AI to resolve conflicts:
   - Parse file structure (ASTs if available)
   - Merge non-conflicting changes
   - For conflicting changes, use context to determine best merge
4. Write merged files to repository

**Error Handling**:
- Unresolvable conflicts → Mark as integration failure, score will be low in evaluation
- File system errors → Retry with exponential backoff

---

### 5. CompilationAgent

**Purpose**: Validates that integrated code compiles successfully.

**Inputs**:
- `CodeIntegratedMessage` (merged codebase)
- Repository path

**Outputs**:
- `BuildValidatedMessage` containing:
  ```json
  {
    "build_status": "success",
    "compiler_errors": [],
    "compiler_warnings": ["CS8618: Non-nullable field 'Name' must contain..."],
    "build_duration_ms": 4523
  }
  ```

**Checkpoint**: `build_validated`

**Execution**:
```bash
cd {repository_path}
dotnet build --no-restore > build.log 2>&1
```

**Output Parsing**:
- Parse `build.log` for errors/warnings
- Extract error codes, file paths, line numbers
- Format for AI consumption

**Error Handling**:
- Build timeout (5 minutes) → Mark as build failure
- Process execution errors → Retry once, then mark as failure

---

### 6. TestExecutionAgent

**Purpose**: Runs all tests and collects results + coverage metrics.

**Inputs**:
- `BuildValidatedMessage` (compiled code)
- Repository path

**Outputs**:
- `TestsExecutedMessage` containing:
  ```json
  {
    "test_status": "partial_pass",
    "total_tests": 47,
    "passed_tests": 43,
    "failed_tests": 4,
    "skipped_tests": 0,
    "test_duration_ms": 8234,
    "coverage_percent": 87.3,
    "failed_test_details": [
      {
        "test_name": "User_Deactivate_ShouldSetIsActiveFalse",
        "error_message": "Expected: False, Actual: True",
        "stack_trace": "at UserTests.cs:line 45"
      }
    ]
  }
  ```

**Checkpoint**: `tests_executed`

**Execution**:
```bash
cd {repository_path}
dotnet test --no-build --collect:"XPlat Code Coverage" --logger:"json;LogFilePath=test-results.json"
```

**Output Parsing**:
- Parse `test-results.json` (JUnit XML or xUnit format)
- Extract coverage report (if available)
- Format failed test details for AI

**Error Handling**:
- Test timeout (10 minutes) → Mark as test failure
- No tests found → Not an error (score will reflect in evaluation)
- Coverage tool errors → Continue without coverage data

---

### 7. QualityEvaluationAgent

**Purpose**: Evaluates implementation quality across multiple dimensions and generates actionable gap analysis.

**Inputs**:
- Original implementation plan markdown
- `CodeIntegratedMessage` (implemented code)
- `BuildValidatedMessage` (compilation results)
- `TestsExecutedMessage` (test results)
- Iteration number

**Outputs**:
- `QualityEvaluationCompleteMessage` containing:
  ```json
  {
    "overall_score": 87,
    "dimension_scores": {
      "compilation": 100,
      "test_pass_rate": 91,
      "test_coverage": 87,
      "code_quality": 85,
      "plan_alignment": 90
    },
    "gaps": [
      {
        "gap_id": "gap_001",
        "type": "test_failure",
        "severity": "high",
        "location": "UserTests.cs:45",
        "description": "User_Deactivate_ShouldSetIsActiveFalse test failing",
        "required_fix": "Update User.Deactivate() to set IsActive = false",
        "estimated_effort": "low"
      },
      {
        "gap_id": "gap_002",
        "type": "coverage_gap",
        "severity": "medium",
        "location": "UserRepository.cs",
        "description": "GetAllActiveAsync method not covered by tests",
        "required_fix": "Add integration test for GetAllActiveAsync",
        "estimated_effort": "medium"
      }
    ],
    "approval_recommendation": "iterate",
    "summary": "Implementation is 87% complete. 4 test failures need fixing. Code quality is good but test coverage below target (87% vs 90% target)."
  }
  ```

**Checkpoint**: `quality_evaluated_iteration_{N}`

**Scoring Logic**:

1. **Compilation Score (0-100)**:
   - Build success = 100
   - Build failure = 0 (cannot proceed)

2. **Test Pass Rate (0-100)**:
   - Formula: `(passed_tests / total_tests) * 100`
   - 100% = perfect score
   - < 80% = significant issues

3. **Test Coverage (0-100)**:
   - Formula: `coverage_percent` (from coverage report)
   - Target: 80% (configurable)
   - < 60% = poor coverage

4. **Code Quality (0-100)**:
   - AI evaluates:
     - Follows repository patterns
     - Clean code principles
     - Proper error handling
     - Documentation quality
     - No code smells
   - Subjective but consistent

5. **Plan Alignment (0-100)**:
   - AI evaluates:
     - All plan requirements implemented
     - No missing features
     - No out-of-scope additions
     - Acceptance criteria met

**Overall Score Calculation**:
```
overall_score = (
    compilation * 0.20 +
    test_pass_rate * 0.30 +
    test_coverage * 0.20 +
    code_quality * 0.15 +
    plan_alignment * 0.15
)
```

**Approval Recommendation**:
- `overall_score >= 90` → "approve"
- `80 <= overall_score < 90` → "iterate" (fixable gaps)
- `overall_score < 80` → "escalate" (major issues, may need plan revision)

**Error Handling**:
- AI evaluation timeout → Retry with reduced context
- Invalid JSON response → Retry with structured output instructions
- After 3 retries → Default to conservative score (70) and iterate

**Prompt Template**: See [Prompt Templates](#prompt-templates) section

---

### 8. IterationCoordinatorAgent

**Purpose**: Decides whether to approve implementation, iterate with fixes, or escalate to human.

**Inputs**:
- `QualityEvaluationCompleteMessage`
- Current iteration number
- Max iterations (from config)
- Quality threshold (from config)

**Outputs**:
- `IterationDecisionMessage` containing:
  ```json
  {
    "decision": "iterate",
    "reason": "Score 87/100 below threshold 90. 4 test failures fixable.",
    "next_action": "generate_gap_fixes",
    "gap_fix_instructions": {
      "iteration_number": 2,
      "priority_gaps": ["gap_001", "gap_002"],
      "context_for_agents": "Focus on fixing test failures in User.Deactivate() and improving coverage for UserRepository.GetAllActiveAsync"
    }
  }
  ```

**Checkpoint**: `iteration_decision_{N}`

**Decision Logic**:
```python
def decide(evaluation, iteration, max_iterations, min_score):
    if evaluation.overall_score >= min_score:
        return APPROVE
    elif iteration < max_iterations:
        if evaluation.approval_recommendation == "escalate":
            return ESCALATE  # Major issues, don't iterate
        else:
            return ITERATE
    else:
        return ESCALATE  # Max iterations exceeded
```

**Actions by Decision**:

1. **APPROVE**:
   - Save final quality report to context
   - Continue to Stage 9 (Git Commit)
   - Include quality metrics in commit message and PR description

2. **ITERATE**:
   - Generate gap fix instructions from evaluation gaps
   - Add to context for next iteration
   - Loop back to Stage 3 (Parallel Implementation) with:
     - Previous implementation (as baseline)
     - Gap fix instructions (prioritized list)
     - Failed tests (for targeted fixing)
   - Increment iteration counter
   - Checkpoint state

3. **ESCALATE**:
   - Suspend workflow (set `is_suspended = true` in context)
   - Post detailed report to Jira with:
     - Quality evaluation summary
     - All gap details
     - Iteration history
     - Instructions: "@claude retry" to restart or manual fix required
   - Return `GraphExecutionResult.Suspended("escalated_to_human", ...)`
   - Await human `@claude` mention to resume

**Error Handling**:
- Invalid decision → Default to ESCALATE (safe choice)

---

## Workflow Stages

### Detailed Stage Breakdown

#### Stage 1: Task Planning
**Input**: PlanApprovedMessage  
**Agent**: TaskPlanningAgent  
**Execution Time**: ~30-60 seconds (AI call)  
**Checkpoint**: `task_plan_generated`  

**Success Criteria**:
- At least 1 task generated
- All tasks have valid file paths
- Dependencies reference valid task IDs

**Failure Modes**:
- Plan markdown parsing fails → Retry with error context
- No tasks generated → Fail workflow

---

#### Stage 2: Dependency Analysis
**Input**: TaskPlanGeneratedMessage  
**Agent**: DependencyAnalysisAgent  
**Execution Time**: <5 seconds (graph algorithm)  
**Checkpoint**: `dependency_analysis_complete`  

**Success Criteria**:
- No circular dependencies
- At least 1 execution group

**Failure Modes**:
- Circular dependency detected → Fail workflow with details
- Empty execution plan → Use sequential fallback

---

#### Stage 3: Parallel Implementation [ITERATION LOOP START]
**Input**: Task plan + dependency analysis + (gap fix instructions if iteration > 1)  
**Agents**: Multiple CodeImplementationAgent instances  
**Execution Time**: ~60-180 seconds per wave (depends on task complexity)  
**Parallel Execution**: Up to `MaxParallelTasks` (default 4) agents run concurrently  
**Checkpoint**: None (integrated in Stage 4)  

**Execution Pattern**:
```csharp
// Execute tasks wave by wave
foreach (var executionGroup in dependencyAnalysis.ExecutionGroups)
{
    var tasksInGroup = executionGroup.ParallelTasks;
    
    // Create agent execution tasks
    var agentTasks = tasksInGroup.Select(taskId => 
        ExecuteAgentAsync<CodeImplementationAgent>(
            taskSpec: GetTaskSpec(taskId),
            context: context,
            cancellationToken: cancellationToken
        )
    ).ToList();
    
    // Execute all tasks in group concurrently
    var results = await Task.WhenAll(agentTasks);
    
    // Collect results for integration
    implementationResults.AddRange(results);
}
```

**Success Criteria**:
- All tasks complete without exceptions
- At least 50% of tasks produce valid code changes

**Failure Modes**:
- Task timeout → Continue with other tasks, mark task as failed
- All tasks fail → Iteration fails, quality score will be low

---

#### Stage 4: Code Integration
**Input**: All TaskImplementedMessage results  
**Agent**: CodeIntegrationAgent  
**Execution Time**: ~20-40 seconds (AI call for conflict resolution)  
**Checkpoint**: `code_integrated`  

**Success Criteria**:
- All files written to repository
- No unresolved conflicts

**Failure Modes**:
- Merge conflicts unresolvable → Mark as integration failure (quality score reflects)
- File I/O errors → Retry with backoff

---

#### Stage 5: Build Validation
**Input**: CodeIntegratedMessage  
**Agent**: CompilationAgent  
**Execution Time**: ~10-60 seconds (depends on project size)  
**Checkpoint**: `build_validated`  

**Success Criteria**:
- `dotnet build` exits with code 0
- No compilation errors (warnings acceptable)

**Failure Modes**:
- Build timeout → Mark as build failure
- Compilation errors → Captured in evaluation, will trigger iteration

---

#### Stage 6: Test Execution
**Input**: BuildValidatedMessage  
**Agent**: TestExecutionAgent  
**Execution Time**: ~30-300 seconds (depends on test suite size)  
**Checkpoint**: `tests_executed`  

**Success Criteria**:
- Test execution completes (even if tests fail)
- Results parseable

**Failure Modes**:
- Test timeout → Mark as test failure
- Test execution crashes → Captured in evaluation

---

#### Stage 7: Quality Evaluation
**Input**: All previous stage results + original plan  
**Agent**: QualityEvaluationAgent  
**Execution Time**: ~30-90 seconds (AI analysis)  
**Checkpoint**: `quality_evaluated_iteration_{N}`  

**Success Criteria**:
- Score generated (1-100)
- Gap analysis provided

**Failure Modes**:
- AI timeout → Retry
- After retries → Default score 70, recommend iteration

---

#### Stage 8: Iteration Decision [LOOP POINT]
**Input**: QualityEvaluationCompleteMessage + config  
**Agent**: IterationCoordinatorAgent  
**Execution Time**: <1 second (logic-based)  
**Checkpoint**: `iteration_decision_{N}`  

**Decision Paths**:
1. **APPROVE** → Continue to Stage 9
2. **ITERATE** → Loop back to Stage 3 with gap fix context
3. **ESCALATE** → Suspend workflow, post to Jira

**Success Criteria**:
- Valid decision made

**Failure Modes**:
- Invalid decision → Default to ESCALATE

---

#### Stage 9: Git Commit
**Input**: Approved implementation  
**Agent**: GitCommitAgent (existing)  
**Execution Time**: ~5-10 seconds  
**Checkpoint**: `code_committed`  

**Commit Message Enhancement**:
```
feat: Implement user management feature (PROJ-123)

Implementation Details:
- Added User entity with domain logic
- Created UserRepository for persistence
- Added 47 unit tests with 92% coverage

Quality Metrics:
✓ Build: Success
✓ Tests: 47/47 passing (100%)
✓ Coverage: 92%
✓ Quality Score: 95/100
✓ Iterations: 2

Plan: see docs/plans/PROJ-123-plan.md
```

---

#### Stage 10: PR Creation & Jira Post (Parallel)
**Input**: Committed code  
**Agents**: PullRequestAgent + JiraPostAgent (existing)  
**Execution Time**: ~10-20 seconds (parallel)  
**Checkpoint**: `pr_created`  

**PR Description Enhancement**:
```markdown
# Implementation: User Management Feature

Closes: PROJ-123

## Summary
Implemented user management with full CRUD operations and role-based access control.

## Quality Report
- **Overall Score**: 95/100 ✅
- **Build Status**: ✅ Success
- **Tests**: 47/47 passing (100%) ✅
- **Coverage**: 92% ✅
- **Iterations**: 2 (improved test coverage from 87% to 92%)

## Changes
- Added `User` entity with domain validation
- Created `IUserRepository` and EF Core implementation
- Added 47 unit + integration tests
- Updated documentation

## Testing
All tests passing. Verified on local environment.

## Plan
Implementation follows approved plan: [PROJ-123-plan.md](link)
```

**Jira Comment Format**:
```
🤖 *PRFactory Implementation Complete*

✅ *Pull Request Created*: [PR #123](link)

📊 *Quality Metrics*:
• Build: ✅ Success
• Tests: 47/47 passing (100%)
• Coverage: 92%
• Overall Quality: 95/100

🔄 *Iterations*: 2
• Iteration 1: Score 87/100 (4 test failures)
• Iteration 2: Score 95/100 (all tests passing, improved coverage)

👉 *Next Steps*: Review and merge PR #123
```

---

#### Stage 11: Completion
**Input**: PR created  
**Agent**: CompletionAgent (existing)  
**Checkpoint**: `completed`  

**Metrics Logged**:
- Total duration (plan approval → PR creation)
- Iteration count
- Final quality score
- Token usage per iteration

---

## Prompt Templates

### TaskPlanningAgent Prompt Template

```markdown
You are an expert software architect and task planner. Your job is to decompose an approved implementation plan into specific, parallelizable tasks.

## Context

**Repository**: {repository_name}  
**Language/Framework**: {detected_language} / {detected_framework}  
**Ticket**: {ticket_key} - {ticket_title}

**Approved Implementation Plan**:
```markdown
{approved_plan_markdown}
```

**Codebase Structure**:
```
{file_tree}
```

**Existing Patterns** (examples of similar implementations):
```
{pattern_examples}
```

---

## Your Task

Break down the implementation plan into **specific, granular tasks** that can be implemented independently and in parallel where possible.

For each task, provide:

1. **task_id**: Unique identifier (e.g., "task_001", "task_002")
2. **description**: Clear, actionable description of what needs to be implemented
3. **files_to_modify**: List of existing files this task will modify
4. **files_to_create**: List of new files this task will create
5. **dependencies**: List of task_ids that must complete before this task (empty array if no dependencies)
6. **test_requirements**: What tests are needed for this task
7. **acceptance_criteria**: List of conditions that must be true when task is complete

---

## Guidelines

**Task Granularity**:
- Each task should be completable in < 15 minutes of coding time
- Aim for 5-20 tasks depending on plan complexity
- Tasks should be focused on a single responsibility

**Dependency Management**:
- Only specify dependencies when task B truly needs output from task A
- Prefer independent tasks (no dependencies) to maximize parallelism
- Example: Interface creation (task_001) must happen before implementation (task_002)

**Test Requirements**:
- Every task that adds business logic MUST include test requirements
- Be specific: "unit tests", "integration tests", "edge case tests"

**File Paths**:
- Use absolute paths relative to repository root
- Follow existing project structure conventions
- Example: "src/Domain/Entities/User.cs"

---

## Output Format

Respond with valid JSON only (no markdown, no explanations):

```json
{
  "tasks": [
    {
      "task_id": "task_001",
      "description": "Create User entity with domain validation",
      "files_to_modify": [],
      "files_to_create": ["src/Domain/Entities/User.cs"],
      "dependencies": [],
      "test_requirements": "Unit tests for User.Create(), User.Deactivate(), and input validation",
      "acceptance_criteria": [
        "User.Create() validates non-empty name and email",
        "User.Deactivate() sets IsActive to false",
        "User.ToString() returns formatted string"
      ]
    },
    {
      "task_id": "task_002",
      "description": "Create IUserRepository interface in domain layer",
      "files_to_modify": [],
      "files_to_create": ["src/Domain/Interfaces/IUserRepository.cs"],
      "dependencies": ["task_001"],
      "test_requirements": "No tests needed (interface only)",
      "acceptance_criteria": [
        "Interface defines GetByIdAsync, SaveAsync, DeleteAsync methods",
        "Methods return Task<T> with proper nullability annotations"
      ]
    }
  ],
  "total_tasks": 2,
  "estimated_parallelism": 1.5
}
```

**estimated_parallelism**: Average number of tasks that can run concurrently (1.0 = fully sequential, 5.0 = 5 tasks in parallel)

---

## Important

- **Do not** include build, test, or deployment tasks (those are handled by the workflow)
- **Do not** include tasks for updating documentation (PRFactory does this)
- **Focus** on code implementation tasks only
- **Be specific** about files and acceptance criteria
```

---

### CodeImplementationAgent Prompt Template

```markdown
You are an expert {language} developer implementing a specific task as part of a larger feature.

## Context

**Repository**: {repository_name}  
**Language/Framework**: {detected_language} / {detected_framework}  
**Ticket**: {ticket_key} - {ticket_title}

**Overall Implementation Plan** (for context):
```markdown
{approved_plan_markdown}
```

**Your Specific Task**:
```json
{task_json}
```

{#if iteration > 1}
**Previous Iteration**: This is iteration {iteration}. The previous implementation had quality issues.

**Quality Gaps to Fix**:
```json
{gap_fix_instructions}
```

**Previous Implementation** (for reference):
```
{previous_implementation_files}
```
{/if}

**Codebase Context**:

Relevant existing files:
```
{relevant_file_contents}
```

Project patterns to follow:
```
{pattern_examples}
```

---

## Your Task

Implement **only** the task specified above. Do not implement other tasks from the plan.

### Requirements

1. **Follow Existing Patterns**:
   - Use the same coding style as existing files
   - Follow naming conventions (e.g., PascalCase for classes, camelCase for variables)
   - Use similar project structure

2. **Implement Tests**:
   - Write comprehensive tests as specified in test_requirements
   - Cover happy path, edge cases, and error conditions
   - Use existing test framework patterns

3. **Handle Edge Cases**:
   - Validate inputs
   - Handle null/empty cases
   - Provide meaningful error messages

4. **Documentation**:
   - Add XML comments for public methods
   - Include inline comments for complex logic

{#if iteration > 1}
5. **Fix Quality Gaps**:
   - Address all issues from gap_fix_instructions
   - Pay special attention to failed tests
   - Improve coverage if below target
{/if}

---

## Output Format

Respond with valid JSON only:

```json
{
  "task_id": "task_001",
  "status": "completed",
  "modified_files": {
    "src/Domain/Entities/User.cs": "<FULL FILE CONTENT>",
    "tests/Domain/UserTests.cs": "<FULL FILE CONTENT>"
  },
  "created_files": {},
  "summary": "Implemented User entity with Create(), Deactivate(), and validation logic. Added 12 unit tests covering all methods including edge cases for null/empty inputs."
}
```

**Important**:
- Include **complete file contents**, not diffs
- Do not use placeholders like "// ... existing code ..."
- Include all imports, namespaces, and formatting
```

---

### QualityEvaluationAgent Prompt Template

```markdown
You are an expert code reviewer evaluating the quality of an AI-generated implementation.

## Context

**Repository**: {repository_name}  
**Language/Framework**: {detected_language} / {detected_framework}  
**Ticket**: {ticket_key} - {ticket_title}

**Original Implementation Plan**:
```markdown
{approved_plan_markdown}
```

**Implementation** (Iteration {iteration}):

Modified/Created Files:
```
{implemented_file_contents}
```

**Build Results**:
```
{compilation_output}
```
- **Status**: {build_status}
- **Errors**: {compiler_error_count}
- **Warnings**: {compiler_warning_count}

**Test Results**:
```
{test_output}
```
- **Total**: {total_tests}
- **Passed**: {passed_tests}
- **Failed**: {failed_tests}
- **Coverage**: {coverage_percent}%

---

## Your Task

Evaluate this implementation across 5 dimensions and provide a comprehensive quality report.

### Evaluation Dimensions

#### 1. Compilation (0-100)
- **100**: Build succeeds with no errors
- **0**: Build fails with compilation errors

Score: ___

#### 2. Test Pass Rate (0-100)
- **100**: All tests passing
- **80-99**: Most tests passing, minor failures
- **60-79**: Significant test failures
- **<60**: Major test failures

Formula: `(passed_tests / total_tests) * 100`

Score: ___

#### 3. Test Coverage (0-100)
- **100**: 90%+ coverage
- **80-99**: 70-89% coverage
- **60-79**: 50-69% coverage
- **<60**: <50% coverage or no coverage data

Score: ___

#### 4. Code Quality (0-100)
Evaluate:
- ✓ Follows repository patterns and conventions
- ✓ Clean code principles (SOLID, DRY, KISS)
- ✓ Proper error handling
- ✓ Meaningful variable names
- ✓ Appropriate comments/documentation
- ✗ Code smells (long methods, God classes, etc.)

Score: ___

#### 5. Plan Alignment (0-100)
Evaluate:
- ✓ All requirements from plan implemented
- ✓ Acceptance criteria met
- ✓ No missing features
- ✗ Out-of-scope additions
- ✗ Deviations from plan without justification

Score: ___

---

### Gap Analysis

For each issue found, create a gap entry:

```json
{
  "gap_id": "gap_001",
  "type": "test_failure | coverage_gap | compilation_error | code_smell | missing_feature",
  "severity": "high | medium | low",
  "location": "file.cs:line or method name",
  "description": "Clear description of the issue",
  "required_fix": "Specific, actionable fix instructions",
  "estimated_effort": "low | medium | high"
}
```

**Prioritization**:
- **High severity**: Blocks functionality, compilation errors, critical test failures
- **Medium severity**: Non-critical test failures, coverage gaps, code quality issues
- **Low severity**: Minor code smells, documentation gaps

---

### Approval Recommendation

Based on overall score:
- **Score >= 90**: Recommend "approve" (high quality, ready for PR)
- **80 <= Score < 90**: Recommend "iterate" (fixable issues)
- **Score < 80**: Recommend "escalate" (major issues, may need plan revision)

---

## Output Format

Respond with valid JSON only:

```json
{
  "overall_score": 87,
  "dimension_scores": {
    "compilation": 100,
    "test_pass_rate": 91,
    "test_coverage": 87,
    "code_quality": 85,
    "plan_alignment": 90
  },
  "gaps": [
    {
      "gap_id": "gap_001",
      "type": "test_failure",
      "severity": "high",
      "location": "UserTests.cs:User_Deactivate_ShouldSetIsActiveFalse",
      "description": "Test expects User.IsActive to be false after Deactivate(), but it remains true",
      "required_fix": "Update User.Deactivate() method to set this.IsActive = false",
      "estimated_effort": "low"
    },
    {
      "gap_id": "gap_002",
      "type": "coverage_gap",
      "severity": "medium",
      "location": "UserRepository.cs:GetAllActiveAsync",
      "description": "Method GetAllActiveAsync has no test coverage",
      "required_fix": "Add integration test: UserRepository_GetAllActiveAsync_ReturnsOnlyActiveUsers",
      "estimated_effort": "medium"
    }
  ],
  "approval_recommendation": "iterate",
  "summary": "Implementation is 87% complete. Build succeeds. 4 test failures need fixing (User.Deactivate logic error). Code quality is good overall. Test coverage at 87% is close to 90% target. Recommend iteration to fix test failures and add missing coverage."
}
```

**Overall Score Calculation**:
```
overall_score = (
  compilation * 0.20 +
  test_pass_rate * 0.30 +
  test_coverage * 0.20 +
  code_quality * 0.15 +
  plan_alignment * 0.15
)
```
```

---

## Configuration Schema

### Extended TenantConfiguration

Add new properties to existing `TenantConfiguration` class:

```csharp
// PRFactory.Domain/Entities/Tenant.cs

public class TenantConfiguration
{
    // ... existing properties ...
    
    /// <summary>
    /// Whether to enable quality evaluation loop in Phase 3 implementation
    /// </summary>
    public bool EnableQualityEvaluation { get; set; } = false;
    
    /// <summary>
    /// Minimum overall quality score (1-100) required to approve implementation
    /// </summary>
    public int MinimumQualityScore { get; set; } = 90;
    
    /// <summary>
    /// Maximum number of quality iteration attempts before escalating to human
    /// </summary>
    public int MaxQualityIterations { get; set; } = 3;
    
    /// <summary>
    /// Enable parallel implementation of independent tasks
    /// </summary>
    public bool EnableParallelImplementation { get; set; } = true;
    
    /// <summary>
    /// Maximum number of parallel code implementation agents
    /// </summary>
    public int MaxParallelTasks { get; set; } = 4;
    
    /// <summary>
    /// Require tests for all code changes
    /// </summary>
    public bool RequireTests { get; set; } = true;
    
    /// <summary>
    /// Minimum test coverage percentage (0-100) required
    /// </summary>
    public int MinimumTestCoverage { get; set; } = 80;
    
    /// <summary>
    /// Enable verbose quality evaluation reports in Jira comments
    /// </summary>
    public bool VerboseQualityReports { get; set; } = false;
    
    /// <summary>
    /// Dimension score weights for overall quality calculation
    /// </summary>
    public QualityScoreWeights ScoreWeights { get; set; } = new();
}

/// <summary>
/// Weights for quality score dimensions (must sum to 1.0)
/// </summary>
public class QualityScoreWeights
{
    public decimal Compilation { get; set; } = 0.20m;
    public decimal TestPassRate { get; set; } = 0.30m;
    public decimal TestCoverage { get; set; } = 0.20m;
    public decimal CodeQuality { get; set; } = 0.15m;
    public decimal PlanAlignment { get; set; } = 0.15m;
    
    public decimal Sum => Compilation + TestPassRate + TestCoverage + CodeQuality + PlanAlignment;
    
    public void Validate()
    {
        if (Math.Abs(Sum - 1.0m) > 0.01m)
            throw new InvalidOperationException($"Quality score weights must sum to 1.0 (current sum: {Sum})");
    }
}
```

### Configuration Validation

Add validation in `TenantApplicationService`:

```csharp
public async Task<TenantConfiguration> UpdateTenantConfigurationAsync(
    Guid tenantId,
    TenantConfiguration newConfig)
{
    // Validate quality configuration
    if (newConfig.EnableQualityEvaluation)
    {
        if (newConfig.MinimumQualityScore < 50 || newConfig.MinimumQualityScore > 100)
            throw new ArgumentException("MinimumQualityScore must be between 50 and 100");
            
        if (newConfig.MaxQualityIterations < 1 || newConfig.MaxQualityIterations > 10)
            throw new ArgumentException("MaxQualityIterations must be between 1 and 10");
            
        if (newConfig.MaxParallelTasks < 1 || newConfig.MaxParallelTasks > 10)
            throw new ArgumentException("MaxParallelTasks must be between 1 and 10");
            
        if (newConfig.MinimumTestCoverage < 0 || newConfig.MinimumTestCoverage > 100)
            throw new ArgumentException("MinimumTestCoverage must be between 0 and 100");
            
        newConfig.ScoreWeights.Validate();
    }
    
    // ... rest of update logic ...
}
```

### Default Configuration Profiles

Provide preset configuration profiles:

```csharp
public static class QualityConfigurationProfiles
{
    public static TenantConfiguration Conservative => new()
    {
        EnableQualityEvaluation = true,
        MinimumQualityScore = 95,
        MaxQualityIterations = 5,
        EnableParallelImplementation = false, // Sequential for stability
        MaxParallelTasks = 1,
        RequireTests = true,
        MinimumTestCoverage = 90
    };
    
    public static TenantConfiguration Balanced => new()
    {
        EnableQualityEvaluation = true,
        MinimumQualityScore = 90,
        MaxQualityIterations = 3,
        EnableParallelImplementation = true,
        MaxParallelTasks = 4,
        RequireTests = true,
        MinimumTestCoverage = 80
    };
    
    public static TenantConfiguration Aggressive => new()
    {
        EnableQualityEvaluation = true,
        MinimumQualityScore = 85,
        MaxQualityIterations = 2,
        EnableParallelImplementation = true,
        MaxParallelTasks = 8,
        RequireTests = true,
        MinimumTestCoverage = 70
    };
    
    public static TenantConfiguration Disabled => new()
    {
        EnableQualityEvaluation = false // Use legacy single-agent implementation
    };
}
```

---

## Database Schema

### New Entities

#### 1. ImplementationTask

Tracks individual tasks decomposed from the implementation plan.

```csharp
// PRFactory.Domain/Entities/ImplementationTask.cs

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a single implementation task decomposed from an approved plan.
/// Used for parallel implementation and progress tracking.
/// </summary>
public class ImplementationTask
{
    /// <summary>
    /// Unique identifier in PRFactory
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Ticket this task belongs to
    /// </summary>
    public Guid TicketId { get; private set; }
    
    /// <summary>
    /// Task identifier from task plan (e.g., "task_001")
    /// </summary>
    public string TaskId { get; private set; } = string.Empty;
    
    /// <summary>
    /// Human-readable task description
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    
    /// <summary>
    /// Files this task will modify (JSON array)
    /// </summary>
    public string FilesToModify { get; private set; } = "[]";
    
    /// <summary>
    /// Files this task will create (JSON array)
    /// </summary>
    public string FilesToCreate { get; private set; } = "[]";
    
    /// <summary>
    /// Task IDs this task depends on (JSON array)
    /// </summary>
    public string Dependencies { get; private set; } = "[]";
    
    /// <summary>
    /// Test requirements for this task
    /// </summary>
    public string TestRequirements { get; private set; } = string.Empty;
    
    /// <summary>
    /// Acceptance criteria (JSON array)
    /// </summary>
    public string AcceptanceCriteria { get; private set; } = "[]";
    
    /// <summary>
    /// Current status of the task
    /// </summary>
    public TaskStatus Status { get; private set; }
    
    /// <summary>
    /// Execution group ID (for parallel execution)
    /// </summary>
    public int? ExecutionGroup { get; private set; }
    
    /// <summary>
    /// Implementation result (JSON with file contents)
    /// </summary>
    public string? ImplementationResult { get; private set; }
    
    /// <summary>
    /// Error message if task failed
    /// </summary>
    public string? ErrorMessage { get; private set; }
    
    /// <summary>
    /// When task was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// When task implementation started
    /// </summary>
    public DateTime? StartedAt { get; private set; }
    
    /// <summary>
    /// When task implementation completed
    /// </summary>
    public DateTime? CompletedAt { get; private set; }
    
    /// <summary>
    /// Navigation property to ticket
    /// </summary>
    public Ticket? Ticket { get; private set; }
    
    private ImplementationTask() { }
    
    public static ImplementationTask Create(
        Guid ticketId,
        string taskId,
        string description,
        List<string> filesToModify,
        List<string> filesToCreate,
        List<string> dependencies,
        string testRequirements,
        List<string> acceptanceCriteria)
    {
        return new ImplementationTask
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            TaskId = taskId,
            Description = description,
            FilesToModify = System.Text.Json.JsonSerializer.Serialize(filesToModify),
            FilesToCreate = System.Text.Json.JsonSerializer.Serialize(filesToCreate),
            Dependencies = System.Text.Json.JsonSerializer.Serialize(dependencies),
            TestRequirements = testRequirements,
            AcceptanceCriteria = System.Text.Json.JsonSerializer.Serialize(acceptanceCriteria),
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void Start(int executionGroup)
    {
        Status = TaskStatus.InProgress;
        ExecutionGroup = executionGroup;
        StartedAt = DateTime.UtcNow;
    }
    
    public void Complete(string implementationResult)
    {
        Status = TaskStatus.Completed;
        ImplementationResult = implementationResult;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void Fail(string errorMessage)
    {
        Status = TaskStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
    
    public List<string> GetFilesToModify() =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(FilesToModify) ?? new();
    
    public List<string> GetFilesToCreate() =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(FilesToCreate) ?? new();
    
    public List<string> GetDependencies() =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(Dependencies) ?? new();
    
    public List<string> GetAcceptanceCriteria() =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(AcceptanceCriteria) ?? new();
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
```

**EF Core Configuration**:
```csharp
// PRFactory.Infrastructure/Persistence/EntityConfigurations/ImplementationTaskConfiguration.cs

public class ImplementationTaskConfiguration : IEntityTypeConfiguration<ImplementationTask>
{
    public void Configure(EntityTypeBuilder<ImplementationTask> builder)
    {
        builder.ToTable("ImplementationTasks");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.TaskId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(1000);
        
        builder.Property(t => t.FilesToModify)
            .IsRequired()
            .HasColumnType("jsonb"); // PostgreSQL JSON
        
        builder.Property(t => t.FilesToCreate)
            .IsRequired()
            .HasColumnType("jsonb");
        
        builder.Property(t => t.Dependencies)
            .IsRequired()
            .HasColumnType("jsonb");
        
        builder.Property(t => t.AcceptanceCriteria)
            .IsRequired()
            .HasColumnType("jsonb");
        
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>();
        
        builder.HasOne(t => t.Ticket)
            .WithMany()
            .HasForeignKey(t => t.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(t => new { t.TicketId, t.TaskId });
        builder.HasIndex(t => t.Status);
    }
}
```

---

#### 2. QualityEvaluation

Stores quality evaluation results for each iteration.

```csharp
// PRFactory.Domain/Entities/QualityEvaluation.cs

namespace PRFactory.Domain.Entities;

/// <summary>
/// Represents a quality evaluation performed on an implementation iteration.
/// </summary>
public class QualityEvaluation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Ticket this evaluation belongs to
    /// </summary>
    public Guid TicketId { get; private set; }
    
    /// <summary>
    /// Iteration number (1, 2, 3, ...)
    /// </summary>
    public int IterationNumber { get; private set; }
    
    /// <summary>
    /// Overall quality score (1-100)
    /// </summary>
    public int OverallScore { get; private set; }
    
    /// <summary>
    /// Compilation score (0-100)
    /// </summary>
    public int CompilationScore { get; private set; }
    
    /// <summary>
    /// Test pass rate score (0-100)
    /// </summary>
    public int TestPassRateScore { get; private set; }
    
    /// <summary>
    /// Test coverage score (0-100)
    /// </summary>
    public int TestCoverageScore { get; private set; }
    
    /// <summary>
    /// Code quality score (0-100)
    /// </summary>
    public int CodeQualityScore { get; private set; }
    
    /// <summary>
    /// Plan alignment score (0-100)
    /// </summary>
    public int PlanAlignmentScore { get; private set; }
    
    /// <summary>
    /// Quality gaps identified (JSON)
    /// </summary>
    public string Gaps { get; private set; } = "[]";
    
    /// <summary>
    /// Evaluation decision: approve, iterate, escalate
    /// </summary>
    public EvaluationDecision Decision { get; private set; }
    
    /// <summary>
    /// Summary of evaluation
    /// </summary>
    public string Summary { get; private set; } = string.Empty;
    
    /// <summary>
    /// Build status: success, failure
    /// </summary>
    public string BuildStatus { get; private set; } = string.Empty;
    
    /// <summary>
    /// Number of tests passed
    /// </summary>
    public int TestsPassed { get; private set; }
    
    /// <summary>
    /// Number of tests failed
    /// </summary>
    public int TestsFailed { get; private set; }
    
    /// <summary>
    /// Test coverage percentage
    /// </summary>
    public decimal? CoveragePercent { get; private set; }
    
    /// <summary>
    /// When evaluation was performed
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Navigation property to ticket
    /// </summary>
    public Ticket? Ticket { get; private set; }
    
    private QualityEvaluation() { }
    
    public static QualityEvaluation Create(
        Guid ticketId,
        int iterationNumber,
        int overallScore,
        Dictionary<string, int> dimensionScores,
        List<QualityGap> gaps,
        EvaluationDecision decision,
        string summary,
        string buildStatus,
        int testsPassed,
        int testsFailed,
        decimal? coveragePercent)
    {
        return new QualityEvaluation
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            IterationNumber = iterationNumber,
            OverallScore = overallScore,
            CompilationScore = dimensionScores.GetValueOrDefault("compilation", 0),
            TestPassRateScore = dimensionScores.GetValueOrDefault("test_pass_rate", 0),
            TestCoverageScore = dimensionScores.GetValueOrDefault("test_coverage", 0),
            CodeQualityScore = dimensionScores.GetValueOrDefault("code_quality", 0),
            PlanAlignmentScore = dimensionScores.GetValueOrDefault("plan_alignment", 0),
            Gaps = System.Text.Json.JsonSerializer.Serialize(gaps),
            Decision = decision,
            Summary = summary,
            BuildStatus = buildStatus,
            TestsPassed = testsPassed,
            TestsFailed = testsFailed,
            CoveragePercent = coveragePercent,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public List<QualityGap> GetGaps() =>
        System.Text.Json.JsonSerializer.Deserialize<List<QualityGap>>(Gaps) ?? new();
}

public enum EvaluationDecision
{
    Approve,
    Iterate,
    Escalate
}

/// <summary>
/// Represents a quality gap found during evaluation
/// </summary>
public record QualityGap(
    string GapId,
    QualityGapType Type,
    QualityGapSeverity Severity,
    string Location,
    string Description,
    string RequiredFix,
    string EstimatedEffort
);

public enum QualityGapType
{
    TestFailure,
    CoverageGap,
    CompilationError,
    CodeSmell,
    MissingFeature
}

public enum QualityGapSeverity
{
    Low,
    Medium,
    High
}
```

**EF Core Configuration**:
```csharp
// PRFactory.Infrastructure/Persistence/EntityConfigurations/QualityEvaluationConfiguration.cs

public class QualityEvaluationConfiguration : IEntityTypeConfiguration<QualityEvaluation>
{
    public void Configure(EntityTypeBuilder<QualityEvaluation> builder)
    {
        builder.ToTable("QualityEvaluations");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Gaps)
            .IsRequired()
            .HasColumnType("jsonb");
        
        builder.Property(e => e.Decision)
            .IsRequired()
            .HasConversion<string>();
        
        builder.HasOne(e => e.Ticket)
            .WithMany()
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(e => new { e.TicketId, e.IterationNumber });
        builder.HasIndex(e => e.Decision);
        builder.HasIndex(e => e.CreatedAt);
    }
}
```

---

### Database Migration

```bash
# Add migration
dotnet ef migrations add AddImplementationQualityLoop \
  --project src/PRFactory.Infrastructure \
  --startup-project src/PRFactory.Api

# Update database
dotnet ef database update \
  --project src/PRFactory.Infrastructure \
  --startup-project src/PRFactory.Api
```

**Migration SQL** (PostgreSQL):
```sql
-- Create ImplementationTasks table
CREATE TABLE "ImplementationTasks" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "TicketId" uuid NOT NULL,
    "TaskId" varchar(50) NOT NULL,
    "Description" varchar(1000) NOT NULL,
    "FilesToModify" jsonb NOT NULL,
    "FilesToCreate" jsonb NOT NULL,
    "Dependencies" jsonb NOT NULL,
    "TestRequirements" text NOT NULL,
    "AcceptanceCriteria" jsonb NOT NULL,
    "Status" varchar(50) NOT NULL,
    "ExecutionGroup" int NULL,
    "ImplementationResult" text NULL,
    "ErrorMessage" text NULL,
    "CreatedAt" timestamp NOT NULL,
    "StartedAt" timestamp NULL,
    "CompletedAt" timestamp NULL,
    CONSTRAINT "FK_ImplementationTasks_Tickets" FOREIGN KEY ("TicketId") 
        REFERENCES "Tickets" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ImplementationTasks_TicketId_TaskId" 
    ON "ImplementationTasks" ("TicketId", "TaskId");
CREATE INDEX "IX_ImplementationTasks_Status" 
    ON "ImplementationTasks" ("Status");

-- Create QualityEvaluations table
CREATE TABLE "QualityEvaluations" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "TicketId" uuid NOT NULL,
    "IterationNumber" int NOT NULL,
    "OverallScore" int NOT NULL,
    "CompilationScore" int NOT NULL,
    "TestPassRateScore" int NOT NULL,
    "TestCoverageScore" int NOT NULL,
    "CodeQualityScore" int NOT NULL,
    "PlanAlignmentScore" int NOT NULL,
    "Gaps" jsonb NOT NULL,
    "Decision" varchar(50) NOT NULL,
    "Summary" text NOT NULL,
    "BuildStatus" varchar(50) NOT NULL,
    "TestsPassed" int NOT NULL,
    "TestsFailed" int NOT NULL,
    "CoveragePercent" decimal(5,2) NULL,
    "CreatedAt" timestamp NOT NULL,
    CONSTRAINT "FK_QualityEvaluations_Tickets" FOREIGN KEY ("TicketId") 
        REFERENCES "Tickets" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_QualityEvaluations_TicketId_IterationNumber" 
    ON "QualityEvaluations" ("TicketId", "IterationNumber");
CREATE INDEX "IX_QualityEvaluations_Decision" 
    ON "QualityEvaluations" ("Decision");
CREATE INDEX "IX_QualityEvaluations_CreatedAt" 
    ON "QualityEvaluations" ("CreatedAt");
```

---

## Integration Strategy

### 1. WorkflowOrchestrator Integration

**No changes required** - `ImplementationGraph` already managed by orchestrator.

Existing transition logic remains:
```csharp
case "PlanningGraph":
    if (result.OutputMessage is PlanApprovedEvent planApproved)
    {
        // Transition to ImplementationGraph
        workflowState.CurrentGraph = "ImplementationGraph";
        var implementationResult = await _implementationGraph.ExecuteAsync(
            new PlanApprovedMessage(ticketId, ...),
            cancellationToken
        );
        await HandleGraphResultAsync(workflowState, implementationResult, cancellationToken);
    }
    break;
```

**New suspension point** in ImplementationGraph:
- When max iterations exceeded, graph returns `GraphExecutionResult.Suspended("escalated_to_human", ...)`
- WorkflowOrchestrator marks workflow as `Suspended`
- Posts Jira comment with "@claude retry" instructions

**Resume flow**:
```csharp
// New message type for retry
public record QualityEscalationResumeMessage(
    Guid TicketId,
    string Action // "retry" or "skip_quality_check"
) : IAgentMessage;

// ImplementationGraph.ResumeCoreAsync
protected override async Task<GraphExecutionResult> ResumeCoreAsync(
    IAgentMessage resumeMessage,
    GraphContext context,
    CancellationToken cancellationToken)
{
    if (resumeMessage is QualityEscalationResumeMessage escResume)
    {
        if (escResume.Action == "retry")
        {
            // Reset iteration counter, retry from current state
            context.State["iteration_count"] = 0;
            return await ContinueQualityLoopAsync(context, cancellationToken);
        }
        else if (escResume.Action == "skip_quality_check")
        {
            // Bypass quality check, proceed to git commit
            return await ExecuteLegacyImplementationAsync(context, cancellationToken);
        }
    }
    
    throw new InvalidOperationException("Cannot resume from this state");
}
```

---

### 2. JiraIntegration Enhancement

**New comment formats** for quality loop stages:

#### 2a. Quality Evaluation Posted (Each Iteration)

```markdown
🤖 **PRFactory Quality Check - Iteration {iteration}**

📊 **Overall Score**: {score}/100

**Dimension Scores**:
• Build: {compilation}/100 {status_icon}
• Tests: {test_pass_rate}/100 ({tests_passed}/{tests_total} passing)
• Coverage: {test_coverage}/100 ({coverage_percent}%)
• Code Quality: {code_quality}/100
• Plan Alignment: {plan_alignment}/100

{#if decision == "approve"}
✅ **Decision**: Approved - Proceeding to create pull request
{/if}

{#if decision == "iterate"}
🔄 **Decision**: Iterating - Fixing {gap_count} quality gaps
**Top Issues**:
{#each high_priority_gaps}
• [{severity}] {description}
{/each}
{/if}

{#if decision == "escalate"}
⚠️ **Decision**: Escalated to Human Review

**Reason**: {escalation_reason}

**Outstanding Issues**:
{#each gaps}
• [{severity}] {description} ({location})
{/each}

**Actions**:
• Review implementation details: [View Code]({branch_url})
• To retry quality check: @claude retry
• To skip quality check and create PR anyway: @claude skip quality check
{/if}
```

#### 2b. Iteration Update

```markdown
🔄 **PRFactory - Starting Iteration {iteration}**

**Fixing**: {gap_count} quality gaps from previous evaluation

**Focus Areas**:
{#each priority_gaps}
• {description}
{/each}

_Estimated time: ~2-3 minutes_
```

---

### 3. GitIntegration Enhancement

**Branch naming convention**:
```
feat/PROJ-123-user-management-impl-iter2
                                    ^^^^
                                    Iteration number
```

**Commit messages**:
```
feat: Implement user management (iteration 2)

Quality improvements:
- Fixed User.Deactivate() test failures
- Improved test coverage from 87% to 92%
- Resolved merge conflicts in UserRepository

Evaluation:
- Build: ✅ Success
- Tests: 47/47 passing (100%)
- Coverage: 92%
- Overall Score: 95/100

Plan: docs/plans/PROJ-123-plan.md
```

**Tag commits with metadata**:
```csharp
// Add custom metadata to commits
var commitMetadata = new Dictionary<string, string>
{
    ["prfactory.quality.overall_score"] = "95",
    ["prfactory.quality.iteration"] = "2",
    ["prfactory.quality.decision"] = "approved"
};

// Include in commit message trailer
```

---

### 4. API Integration (for External Clients)

**New API endpoints** for monitoring quality loop:

```csharp
// PRFactory.Api/Controllers/QualityController.cs

[ApiController]
[Route("api/tickets/{ticketId}/quality")]
public class QualityController : ControllerBase
{
    private readonly IQualityEvaluationRepository _qualityRepo;
    
    /// <summary>
    /// Get all quality evaluations for a ticket
    /// </summary>
    [HttpGet("evaluations")]
    public async Task<ActionResult<List<QualityEvaluationDto>>> GetEvaluations(Guid ticketId)
    {
        var evaluations = await _qualityRepo.GetByTicketIdAsync(ticketId);
        return Ok(evaluations.Select(e => MapToDto(e)));
    }
    
    /// <summary>
    /// Get latest quality evaluation for a ticket
    /// </summary>
    [HttpGet("evaluations/latest")]
    public async Task<ActionResult<QualityEvaluationDto>> GetLatestEvaluation(Guid ticketId)
    {
        var evaluation = await _qualityRepo.GetLatestByTicketIdAsync(ticketId);
        if (evaluation == null)
            return NotFound();
        return Ok(MapToDto(evaluation));
    }
    
    /// <summary>
    /// Retry quality loop after escalation
    /// </summary>
    [HttpPost("retry")]
    public async Task<ActionResult> RetryQualityLoop(Guid ticketId)
    {
        await _workflowOrchestrator.ResumeWorkflowAsync(
            ticketId,
            new QualityEscalationResumeMessage(ticketId, "retry")
        );
        return Accepted();
    }
    
    /// <summary>
    /// Skip quality check and proceed to PR creation
    /// </summary>
    [HttpPost("skip")]
    public async Task<ActionResult> SkipQualityCheck(Guid ticketId)
    {
        await _workflowOrchestrator.ResumeWorkflowAsync(
            ticketId,
            new QualityEscalationResumeMessage(ticketId, "skip_quality_check")
        );
        return Accepted();
    }
}
```

---

### 5. Web UI Integration

**New pages/components**:

#### 5a. Quality Dashboard Page

```
/tickets/{ticketId}/quality
```

**Features**:
- Quality trend chart (scores across iterations)
- Iteration timeline
- Gap details table
- Retry/Skip action buttons

#### 5b. TicketDetail Page Enhancements

**Add quality metrics section**:
```razor
@if (ticket.State == WorkflowState.Implementing)
{
    <Card Title="Implementation Quality">
        @if (latestEvaluation != null)
        {
            <QualityScoreGauge Score="@latestEvaluation.OverallScore" />
            <IterationProgress Current="@currentIteration" Max="@maxIterations" />
            
            @if (latestEvaluation.Decision == EvaluationDecision.Escalate)
            {
                <AlertMessage Type="Warning" Message="Quality threshold not reached. Human review required." />
                <ButtonGroup>
                    <LoadingButton OnClick="@RetryQualityLoop">Retry Quality Check</LoadingButton>
                    <LoadingButton OnClick="@SkipQualityCheck" Variant="secondary">Skip & Create PR</LoadingButton>
                </ButtonGroup>
            }
        }
        else
        {
            <EmptyState Message="Quality evaluation in progress..." />
        }
    </Card>
}
```

---

## Error Handling & Edge Cases

### 1. Compilation Failures

**Scenario**: Code doesn't compile after integration.

**Handling**:
1. **CompilationAgent** captures compiler errors
2. **QualityEvaluationAgent** scores compilation dimension as 0
3. **IterationCoordinatorAgent** decides to iterate (if iterations remaining)
4. **Gap fix instructions** include full compiler error details
5. **Next iteration** focuses on fixing compilation errors first

**Escalation Trigger**: If compilation still fails after 3 iterations, escalate.

**Example Gap**:
```json
{
  "gap_id": "gap_001",
  "type": "compilation_error",
  "severity": "high",
  "location": "User.cs:42",
  "description": "CS0246: The type or namespace name 'InvalidOperationException' could not be found",
  "required_fix": "Add 'using System;' to User.cs",
  "estimated_effort": "low"
}
```

---

### 2. Test Failures Indicating Plan Issues

**Scenario**: Tests fail not due to implementation bugs, but because the plan itself is flawed.

**Handling**:
1. **QualityEvaluationAgent** detects pattern: multiple test failures across unrelated areas
2. **Approval recommendation**: "escalate" (even if iterations remaining)
3. **IterationCoordinatorAgent** respects recommendation and escalates immediately
4. **Jira comment** suggests: "Test failures may indicate plan issues. Consider revising plan or providing clarification."

**Human Options**:
- "@claude regenerate plan" → Restart from PlanningGraph
- "@claude retry with context: [clarification]" → Retry implementation with additional context
- Manual fix

---

### 3. Merge Conflicts Between Parallel Tasks

**Scenario**: Two parallel tasks modify overlapping code sections.

**Handling**:
1. **CodeIntegrationAgent** detects conflict
2. Uses AI to resolve:
   - If changes are complementary (e.g., different methods in same class) → Merge both
   - If changes conflict (e.g., both modify same method) → Use task priority or ask AI to create composite
3. If unresolvable → Mark as integration failure
4. **QualityEvaluationAgent** penalizes code quality dimension
5. **Next iteration** reduces parallelism (fewer tasks per wave)

**Fallback**: If conflicts persist after 2 iterations, switch to sequential execution.

---

### 4. Timeout Scenarios

**Agent Execution Timeout** (e.g., CodeImplementationAgent takes >10 minutes):
- Middleware catches timeout
- Retry task once with extended timeout (20 minutes)
- If still times out → Mark task as failed
- Continue with other tasks
- Evaluation will score low due to missing task implementation

**Graph Execution Timeout** (entire graph takes >30 minutes):
- Checkpoint current state
- Suspend workflow with timeout error
- Post to Jira: "Implementation timed out. Manual intervention required."
- Human can review progress and decide: retry, modify plan, or manual implementation

---

### 5. Max Iterations Exceeded

**Scenario**: Quality score still below threshold after MaxIterations.

**Handling**:
1. **IterationCoordinatorAgent** detects `iteration >= MaxIterations`
2. Decision: ESCALATE
3. Workflow suspended
4. **Jira post**:
   ```
   ⚠️ Quality threshold not reached after {MaxIterations} iterations
   
   Latest Score: {score}/100 (target: {MinimumQualityScore}/100)
   
   Outstanding Issues:
   - [High] {issue1}
   - [Medium] {issue2}
   
   Options:
   1. @claude retry - Reset iteration counter and try again
   2. @claude adjust threshold {new_score} - Lower quality threshold temporarily
   3. Manual fix required - Review code and fix issues manually
   ```

**Human Actions**:
- **Retry**: Resets iteration counter, tries again (useful if issue was transient)
- **Adjust threshold**: Temporarily lower quality bar for this ticket only
- **Manual fix**: Checkout branch, fix issues, push, resume workflow

---

### 6. External Dependency Failures

**Scenario**: dotnet restore fails due to NuGet outage, or git operations fail.

**Handling**:
1. **Retry with exponential backoff** (3 attempts)
2. If persistent failure → Mark as infrastructure failure
3. Suspend workflow with specific error
4. **Jira post**: "Infrastructure issue: {error}. PRFactory will automatically retry in 5 minutes."
5. **Background job** retries workflow after delay

---

### 7. No Tests Generated

**Scenario**: Implementation tasks don't generate any tests.

**Handling**:
1. **TestExecutionAgent** detects zero tests
2. **QualityEvaluationAgent** scores test dimensions as 0
3. If `RequireTests` config is true → Automatic iteration with instruction: "Add comprehensive tests"
4. If `RequireTests` is false → Warning logged, score reflects lack of tests

---

### 8. Coverage Tool Unavailable

**Scenario**: Code coverage collection fails or tool not installed.

**Handling**:
1. **TestExecutionAgent** logs warning
2. Continue without coverage data
3. **QualityEvaluationAgent** skips test coverage dimension (recalculates weights)
4. Warning added to evaluation summary

---

### 9. AI Evaluation Inconsistency

**Scenario**: AI gives different scores for same code across iterations.

**Mitigation**:
1. Include **deterministic data** in evaluation prompt:
   - Exact test counts
   - Coverage percentages
   - Compiler error counts
2. Use **structured output** (JSON schema) to enforce consistency
3. **Temperature = 0** for evaluation agent
4. If score variance > 10 points between iterations with no code changes → Log warning, use average

---

### 10. Circular Dependencies in Task Plan

**Scenario**: Task A depends on B, B depends on C, C depends on A.

**Handling**:
1. **DependencyAnalysisAgent** detects cycle during topological sort
2. Fail workflow immediately (don't attempt implementation)
3. **Jira post**: "Task plan has circular dependencies: {task_ids}. Manual task plan revision required."
4. Human must revise plan or provide clarification

---

## Success Metrics & Monitoring

### Key Performance Indicators (KPIs)

#### 1. Implementation Success Rate
**Definition**: Percentage of implementations that reach quality threshold without escalation.

**Formula**: 
```
success_rate = (approved_implementations / total_implementations) * 100
```

**Target**: 85%+ (indicates good plan quality and implementation capability)

**Tracking**:
```sql
SELECT 
    DATE_TRUNC('day', created_at) as date,
    COUNT(*) as total,
    COUNT(*) FILTER (WHERE decision = 'Approve') as approved,
    ROUND(COUNT(*) FILTER (WHERE decision = 'Approve')::numeric / COUNT(*) * 100, 2) as success_rate
FROM quality_evaluations
WHERE iteration_number = (
    SELECT MAX(iteration_number) 
    FROM quality_evaluations qe2 
    WHERE qe2.ticket_id = quality_evaluations.ticket_id
)
GROUP BY DATE_TRUNC('day', created_at)
ORDER BY date DESC;
```

---

#### 2. Average Iterations to Approval
**Definition**: Mean number of quality loop iterations before approval.

**Formula**:
```
avg_iterations = SUM(iteration_number) / COUNT(tickets_with_approval)
```

**Target**: <2.0 (most implementations pass within 1-2 iterations)

**Tracking**:
```sql
SELECT 
    AVG(iteration_number) as avg_iterations,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY iteration_number) as median_iterations,
    MAX(iteration_number) as max_iterations
FROM quality_evaluations
WHERE decision = 'Approve';
```

---

#### 3. Time to Quality (Plan → Approved Implementation)
**Definition**: Duration from plan approval to implementation approval.

**Formula**:
```
time_to_quality = implementation_approved_at - plan_approved_at
```

**Target**: <10 minutes (with parallel execution)

**Tracking**:
```sql
SELECT 
    t.ticket_key,
    t.plan_approved_at,
    qe.created_at as implementation_approved_at,
    EXTRACT(EPOCH FROM (qe.created_at - t.plan_approved_at)) / 60 as minutes_to_quality
FROM tickets t
JOIN quality_evaluations qe ON qe.ticket_id = t.id
WHERE qe.decision = 'Approve'
ORDER BY qe.created_at DESC
LIMIT 100;
```

---

#### 4. Parallel Execution Efficiency
**Definition**: Ratio of tasks executed in parallel vs. total tasks.

**Formula**:
```
parallel_efficiency = (tasks_executed_in_parallel / total_tasks) * 100
```

**Target**: >60% (indicates effective task decomposition)

**Tracking**:
```sql
WITH task_parallelism AS (
    SELECT 
        ticket_id,
        COUNT(*) as total_tasks,
        COUNT(DISTINCT execution_group) as total_groups,
        MAX(group_size) as max_parallel
    FROM (
        SELECT 
            ticket_id,
            execution_group,
            COUNT(*) as group_size
        FROM implementation_tasks
        GROUP BY ticket_id, execution_group
    ) group_counts
    GROUP BY ticket_id
)
SELECT 
    AVG(max_parallel::float / NULLIF(total_tasks, 0)) * 100 as avg_parallel_efficiency
FROM task_parallelism;
```

---

#### 5. Cost per Implementation (Claude API Tokens)
**Definition**: Average tokens consumed from plan approval to implementation approval.

**Formula**:
```
cost_per_impl = SUM(tokens_per_iteration) / COUNT(implementations)
```

**Target**: <500k tokens per implementation (cost-effective)

**Tracking**:
```csharp
// Log tokens in AgentContext after each agent execution
context.Metadata["tokens_used"] = new TokenUsage
{
    TaskPlanning = 50000,
    CodeImplementation = 200000, // sum of all parallel agents
    QualityEvaluation = 80000,
    Total = 330000
};

// Aggregate per ticket
SELECT 
    t.ticket_key,
    SUM((m.metadata->>'tokens_used')::int) as total_tokens,
    ROUND(SUM((m.metadata->>'tokens_used')::int) / 1000000.0 * 3, 2) as estimated_cost_usd
FROM tickets t
JOIN quality_evaluations qe ON qe.ticket_id = t.id
-- Assuming token cost: ~$3 per 1M tokens (Claude Sonnet 4.5)
GROUP BY t.ticket_key
ORDER BY total_tokens DESC;
```

---

#### 6. Quality Score Distribution
**Definition**: Distribution of final quality scores across all implementations.

**Target**: 80%+ of implementations scoring 90+

**Tracking**:
```sql
SELECT 
    CASE 
        WHEN overall_score >= 95 THEN '95-100 (Excellent)'
        WHEN overall_score >= 90 THEN '90-94 (Good)'
        WHEN overall_score >= 80 THEN '80-89 (Fair)'
        ELSE '<80 (Poor)'
    END as score_range,
    COUNT(*) as count,
    ROUND(COUNT(*)::numeric / SUM(COUNT(*)) OVER () * 100, 2) as percentage
FROM quality_evaluations
WHERE decision = 'Approve'
GROUP BY score_range
ORDER BY score_range DESC;
```

---

### Monitoring Dashboards

#### Dashboard 1: Quality Loop Health

**Metrics**:
- Success rate (last 7 days): 87%
- Avg iterations: 1.8
- Avg time to quality: 8.2 minutes
- Parallel efficiency: 65%

**Visualizations**:
- Line chart: Success rate over time
- Histogram: Iterations to approval distribution
- Heatmap: Quality scores by dimension

---

#### Dashboard 2: Agent Performance

**Metrics per agent**:
- TaskPlanningAgent: Avg execution time, success rate
- CodeImplementationAgent: Avg execution time, failure rate
- QualityEvaluationAgent: Avg execution time, score consistency

**Alerts**:
- Agent timeout rate > 5% → Investigate AI performance
- Agent failure rate > 10% → Check prompt templates

---

#### Dashboard 3: Cost Analysis

**Metrics**:
- Total tokens consumed (daily)
- Cost per implementation
- Cost breakdown by agent type
- Cost trends (increasing/decreasing)

**Budget Alerts**:
- Daily token usage exceeds budget → Throttle parallel execution
- Cost per implementation > $5 → Review prompt efficiency

---

### Alerting Rules

```yaml
alerts:
  - name: HighEscalationRate
    condition: escalation_rate > 20%
    severity: warning
    message: "More than 20% of implementations are escalating to humans. Review quality thresholds or improve agent prompts."
  
  - name: LowSuccessRate
    condition: success_rate < 70%
    severity: critical
    message: "Implementation success rate below 70%. Immediate investigation required."
  
  - name: HighIterationCount
    condition: avg_iterations > 3
    severity: warning
    message: "Average iterations exceeding 3. Review gap fix instructions effectiveness."
  
  - name: SlowImplementation
    condition: avg_time_to_quality > 15 minutes
    severity: info
    message: "Implementation taking longer than target. Consider increasing parallelism or optimizing agents."
  
  - name: HighCost
    condition: daily_token_cost > $500
    severity: warning
    message: "Daily token cost exceeding budget. Review implementation volume and agent efficiency."
```

---

### A/B Testing Framework

To validate quality loop effectiveness, implement A/B testing:

```csharp
public class QualityLoopExperiment
{
    public async Task<ExperimentResult> RunExperimentAsync(int sampleSize)
    {
        // Group A: Quality loop enabled
        var groupA = await RunWithQualityLoopAsync(sampleSize / 2);
        
        // Group B: Legacy single-agent implementation
        var groupB = await RunLegacyImplementationAsync(sampleSize / 2);
        
        return new ExperimentResult
        {
            GroupA = new GroupMetrics
            {
                SuccessRate = groupA.SuccessRate,
                AvgTimeToComplete = groupA.AvgTime,
                AvgQualityScore = groupA.AvgQualityScore,
                HumanReviewCycles = groupA.HumanReviewCycles
            },
            GroupB = new GroupMetrics
            {
                SuccessRate = groupB.SuccessRate,
                AvgTimeToComplete = groupB.AvgTime,
                AvgQualityScore = groupB.AvgQualityScore,
                HumanReviewCycles = groupB.HumanReviewCycles
            },
            StatisticalSignificance = CalculateSignificance(groupA, groupB)
        };
    }
}
```

**Hypothesis**: Quality loop will:
- ✓ Increase implementation success rate by 20%+
- ✓ Reduce human review cycles by 30%+
- ✓ Increase initial PR quality scores (from human reviewers) by 15%+
- ✗ Increase time to PR creation by ~50% (acceptable tradeoff)

---

## Phased Implementation Roadmap

### Phase 1: Foundation (Weeks 1-2)

**Goal**: Establish core quality evaluation infrastructure without full loop.

**Deliverables**:
1. ✅ Database schema (ImplementationTask, QualityEvaluation entities)
2. ✅ Configuration schema (TenantConfiguration extensions)
3. ✅ QualityEvaluationAgent implementation (evaluation only, no iteration)
4. ✅ CompilationAgent + TestExecutionAgent implementations
5. ✅ Integrate into ImplementationGraph (quality check after implementation, log only)

**Success Criteria**:
- Quality evaluations logged for all implementations
- No impact on existing workflow (evaluation runs in background)
- Metrics dashboard shows evaluation scores

**Code Changes**:
```csharp
// ImplementationGraph.cs - Phase 1
protected override async Task<GraphExecutionResult> ExecuteCoreAsync(...)
{
    // ... existing implementation ...
    
    var implemented = await ExecuteAgentAsync<ImplementationAgent>(...);
    
    // PHASE 1: Add quality evaluation (log only, don't block)
    if (_config.EnableQualityEvaluation)
    {
        _ = Task.Run(async () => 
        {
            var buildResult = await ExecuteAgentAsync<CompilationAgent>(...);
            var testResult = await ExecuteAgentAsync<TestExecutionAgent>(...);
            var evaluation = await ExecuteAgentAsync<QualityEvaluationAgent>(...);
            await SaveEvaluationToDatabase(evaluation);
        });
    }
    
    // Continue with existing flow (git commit, PR, etc.)
    // ...
}
```

**Testing**:
- Run on 10 test tickets
- Verify evaluations logged correctly
- Compare evaluation scores with human review scores (validation)

---

### Phase 2: Iteration Loop (Weeks 3-4)

**Goal**: Add iteration capability with gap fixing.

**Deliverables**:
1. ✅ IterationCoordinatorAgent implementation
2. ✅ Iteration loop logic in ImplementationGraph
3. ✅ Gap fix instruction generation
4. ✅ Retry logic for compilation/test failures
5. ✅ Escalation to human when max iterations exceeded

**Success Criteria**:
- Implementations automatically iterate on test failures
- Quality scores improve across iterations
- Escalation rate <20%
- Human approval not required for iterations

**Code Changes**:
```csharp
// ImplementationGraph.cs - Phase 2
protected override async Task<GraphExecutionResult> ExecuteCoreAsync(...)
{
    for (int iteration = 1; iteration <= maxIterations; iteration++)
    {
        var implemented = await ExecuteAgentAsync<ImplementationAgent>(...);
        var buildResult = await ExecuteAgentAsync<CompilationAgent>(...);
        var testResult = await ExecuteAgentAsync<TestExecutionAgent>(...);
        var evaluation = await ExecuteAgentAsync<QualityEvaluationAgent>(...);
        
        if (evaluation.OverallScore >= minScore)
        {
            break; // Approved
        }
        else if (iteration < maxIterations)
        {
            // Prepare gap fix instructions for next iteration
            context.State["gap_fix_instructions"] = evaluation.GenerateFixInstructions();
            continue; // ITERATE
        }
        else
        {
            // ESCALATE
            return await EscalateToHumanAsync(context, evaluation);
        }
    }
    
    // Continue with git commit, PR, etc.
    // ...
}
```

**Testing**:
- Manually inject test failures → Verify automatic iteration
- Inject compiler errors → Verify fix in next iteration
- Exceed max iterations → Verify escalation to Jira

---

### Phase 3: Task Decomposition & Parallelization (Weeks 5-6)

**Goal**: Add task planning and parallel implementation.

**Deliverables**:
1. ✅ TaskPlanningAgent implementation
2. ✅ DependencyAnalysisAgent implementation
3. ✅ CodeImplementationAgent (parallel-aware)
4. ✅ CodeIntegrationAgent (merge conflict resolution)
5. ✅ Parallel execution orchestration
6. ✅ ImplementationTask entity tracking

**Success Criteria**:
- Task plans generated for all implementations
- 50%+ parallelism achieved on average
- Merge conflicts resolved automatically in 80%+ cases
- Time to implementation reduced by 30%+

**Code Changes**:
```csharp
// ImplementationGraph.cs - Phase 3
protected override async Task<GraphExecutionResult> ExecuteCoreAsync(...)
{
    // NEW: Task planning stage
    var taskPlan = await ExecuteAgentAsync<TaskPlanningAgent>(...);
    var dependencies = await ExecuteAgentAsync<DependencyAnalysisAgent>(...);
    
    for (int iteration = 1; iteration <= maxIterations; iteration++)
    {
        // NEW: Parallel implementation
        var implementations = await ExecuteParallelImplementationAsync(taskPlan, dependencies);
        
        // NEW: Code integration
        var integrated = await ExecuteAgentAsync<CodeIntegrationAgent>(implementations);
        
        var buildResult = await ExecuteAgentAsync<CompilationAgent>(integrated);
        var testResult = await ExecuteAgentAsync<TestExecutionAgent>(buildResult);
        var evaluation = await ExecuteAgentAsync<QualityEvaluationAgent>(...);
        
        // ... iteration logic (same as Phase 2) ...
    }
}
```

**Testing**:
- Test on complex features requiring 10+ tasks
- Verify parallel execution (monitor CPU usage)
- Inject merge conflicts → Verify resolution
- Compare time vs. Phase 2 (should be faster)

---

### Phase 4: Polish & Production Rollout (Week 7)

**Goal**: Production-ready with monitoring, documentation, and gradual rollout.

**Deliverables**:
1. ✅ Monitoring dashboards (Grafana/Prometheus)
2. ✅ Alert rules configured
3. ✅ Web UI components (quality dashboard, retry actions)
4. ✅ API endpoints for external integrations
5. ✅ Documentation (user guide, troubleshooting)
6. ✅ A/B testing framework
7. ✅ Gradual tenant rollout plan

**Rollout Strategy**:

**Week 7.1** - Internal Testing:
- Enable for PRFactory's own repository
- Run for 1 week, gather feedback
- Fix any bugs discovered

**Week 7.2** - Beta Tenants:
- Enable for 2-3 friendly tenants (opt-in)
- Monitor metrics closely
- Gather user feedback

**Week 7.3** - Gradual Rollout:
- Enable for 25% of tenants
- Monitor success rate, escalation rate
- If metrics healthy → Continue

**Week 7.4** - Full Rollout:
- Enable for all tenants (default: on)
- Provide opt-out option via config

**Rollback Plan**:
- If success rate <70% or escalation rate >30% → Disable for all tenants
- Investigate root cause (prompt quality, AI performance, etc.)
- Fix issues in controlled environment
- Re-attempt rollout

---

## Appendices

### Appendix A: Message Type Reference

New message types introduced:

```csharp
// Task planning messages
public record TaskPlanGeneratedMessage(
    Guid TicketId,
    List<TaskSpec> Tasks,
    int TotalTasks,
    decimal EstimatedParallelism
) : IAgentMessage;

public record DependencyAnalysisCompleteMessage(
    Guid TicketId,
    List<ExecutionGroup> ExecutionGroups,
    int MaxParallelism
) : IAgentMessage;

// Implementation messages
public record TaskImplementedMessage(
    Guid TicketId,
    string TaskId,
    TaskStatus Status,
    Dictionary<string, string> ModifiedFiles,
    Dictionary<string, string> CreatedFiles,
    string Summary
) : IAgentMessage;

public record CodeIntegratedMessage(
    Guid TicketId,
    Dictionary<string, string> MergedFiles,
    int ConflictsResolved,
    string IntegrationSummary
) : IAgentMessage;

// Validation messages
public record BuildValidatedMessage(
    Guid TicketId,
    string BuildStatus,
    List<CompilerError> CompilerErrors,
    List<CompilerWarning> CompilerWarnings,
    int BuildDurationMs
) : IAgentMessage;

public record TestsExecutedMessage(
    Guid TicketId,
    string TestStatus,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    List<FailedTestDetail> FailedTestDetails,
    decimal? CoveragePercent
) : IAgentMessage;

// Quality evaluation messages
public record QualityEvaluationCompleteMessage(
    Guid TicketId,
    int IterationNumber,
    int OverallScore,
    Dictionary<string, int> DimensionScores,
    List<QualityGap> Gaps,
    EvaluationDecision Decision,
    string Summary
) : IAgentMessage;

public record IterationDecisionMessage(
    Guid TicketId,
    int IterationNumber,
    IterationAction Action,
    string Reason,
    GapFixInstructions? GapFixInstructions
) : IAgentMessage;

// Resume messages
public record QualityEscalationResumeMessage(
    Guid TicketId,
    string Action // "retry" or "skip_quality_check"
) : IAgentMessage;
```

---

### Appendix B: Agent Timeout Configuration

Recommended timeouts for each agent:

| Agent | Default Timeout | Max Timeout | Retry Count |
|-------|----------------|-------------|-------------|
| TaskPlanningAgent | 120s | 300s | 3 |
| DependencyAnalysisAgent | 30s | 60s | 2 |
| CodeImplementationAgent | 180s | 600s | 2 |
| CodeIntegrationAgent | 60s | 180s | 3 |
| CompilationAgent | 120s | 300s | 2 |
| TestExecutionAgent | 300s | 600s | 1 |
| QualityEvaluationAgent | 120s | 300s | 3 |
| IterationCoordinatorAgent | 10s | 30s | 1 |

**Timeout Behavior**:
- On timeout → Retry with extended timeout (2x default)
- After max retries → Mark operation as failed
- Failed operations reflected in quality evaluation score

---

### Appendix C: Estimated Token Consumption

Per-agent token usage estimates (Claude Sonnet 4.5):

| Agent | Input Tokens | Output Tokens | Total | Cost per Call |
|-------|-------------|---------------|-------|--------------|
| TaskPlanningAgent | ~40k | ~10k | ~50k | $0.15 |
| CodeImplementationAgent | ~50k | ~15k | ~65k | $0.20 |
| CodeIntegrationAgent | ~60k | ~20k | ~80k | $0.24 |
| QualityEvaluationAgent | ~70k | ~10k | ~80k | $0.24 |

**Total per iteration**:
- Task planning: $0.15
- Parallel implementation (4 agents): $0.80
- Integration: $0.24
- Validation (build + tests): $0
- Evaluation: $0.24
- **Total per iteration**: ~$1.43

**For typical 2-iteration workflow**: ~$2.86 per implementation

**Monthly costs** (assuming 100 implementations/day):
- Daily: $286
- Monthly: $8,580

**Cost optimization strategies**:
- Reduce context size (smart file selection)
- Use caching for repeated prompts
- Lower max parallel tasks (trades speed for cost)

---

### Appendix D: Testing Strategy

#### Unit Tests

```csharp
// PRFactory.Tests/Agents/QualityEvaluationAgentTests.cs

public class QualityEvaluationAgentTests
{
    [Fact]
    public async Task EvaluateAsync_AllTestsPassing_ReturnsHighScore()
    {
        // Arrange
        var agent = new QualityEvaluationAgent(...);
        var context = CreateContext(
            buildStatus: "success",
            testsPassed: 47,
            testsFailed: 0,
            coverage: 92m
        );
        
        // Act
        var result = await agent.ExecuteAsync(context);
        
        // Assert
        var evaluation = ParseEvaluation(result);
        Assert.True(evaluation.OverallScore >= 90);
        Assert.Equal(EvaluationDecision.Approve, evaluation.Decision);
    }
    
    [Fact]
    public async Task EvaluateAsync_CompilationFailure_ReturnsZeroCompilationScore()
    {
        // Arrange
        var agent = new QualityEvaluationAgent(...);
        var context = CreateContext(buildStatus: "failure");
        
        // Act
        var result = await agent.ExecuteAsync(context);
        
        // Assert
        var evaluation = ParseEvaluation(result);
        Assert.Equal(0, evaluation.DimensionScores["compilation"]);
        Assert.Equal(EvaluationDecision.Iterate, evaluation.Decision);
    }
}
```

#### Integration Tests

```csharp
// PRFactory.Tests/Graphs/ImplementationGraphQualityLoopTests.cs

public class ImplementationGraphQualityLoopTests
{
    [Fact]
    public async Task ExecuteAsync_QualityLoopEnabled_IteratesOnTestFailures()
    {
        // Arrange
        var graph = CreateImplementationGraph(enableQualityLoop: true);
        var inputMessage = new PlanApprovedMessage(...);
        
        // Simulate test failures in first iteration
        MockImplementationAgent(testsPassed: 40, testsFailed: 7);
        
        // Simulate test fixes in second iteration
        MockImplementationAgent(testsPassed: 47, testsFailed: 0);
        
        // Act
        var result = await graph.ExecuteAsync(inputMessage);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("completed", result.State);
        
        // Verify 2 iterations occurred
        var evaluations = await GetEvaluations(inputMessage.TicketId);
        Assert.Equal(2, evaluations.Count);
        Assert.True(evaluations[1].OverallScore > evaluations[0].OverallScore);
    }
    
    [Fact]
    public async Task ExecuteAsync_MaxIterationsExceeded_EscalatesToHuman()
    {
        // Arrange
        var graph = CreateImplementationGraph(
            enableQualityLoop: true,
            maxIterations: 3,
            minScore: 90
        );
        
        // Simulate persistent test failures across all iterations
        MockImplementationAgent(testsPassed: 40, testsFailed: 7);
        
        // Act
        var result = await graph.ExecuteAsync(...);
        
        // Assert
        Assert.True(result.IsSuccess); // Suspension is not a failure
        Assert.Equal("escalated_to_human", result.State);
        
        // Verify 3 evaluations logged
        var evaluations = await GetEvaluations(...);
        Assert.Equal(3, evaluations.Count);
        
        // Verify Jira comment posted
        var jiraComments = await GetJiraComments(...);
        Assert.Contains("Quality threshold not reached after 3 iterations", 
            jiraComments.Last().Body);
    }
}
```

#### End-to-End Tests

```csharp
// PRFactory.Tests/E2E/QualityLoopE2ETests.cs

[Collection("E2E")]
public class QualityLoopE2ETests
{
    [Fact]
    public async Task CompleteWorkflow_WithQualityLoop_CreatesHighQualityPR()
    {
        // Arrange
        var testTicket = await CreateTestTicket(
            plan: SamplePlans.UserManagement,
            enableQualityLoop: true
        );
        
        // Act - Start workflow
        var workflowId = await _orchestrator.StartWorkflowAsync(
            new TriggerTicketMessage(testTicket.Id, ...)
        );
        
        // Wait for completion (with timeout)
        var result = await WaitForWorkflowCompletion(workflowId, timeoutSeconds: 600);
        
        // Assert
        Assert.Equal(WorkflowStatus.Completed, result.Status);
        
        // Verify PR created
        var ticket = await _ticketRepo.GetByIdAsync(testTicket.Id);
        Assert.NotNull(ticket.PullRequestUrl);
        
        // Verify quality evaluation
        var finalEvaluation = await _qualityRepo.GetLatestByTicketIdAsync(testTicket.Id);
        Assert.True(finalEvaluation.OverallScore >= 90);
        
        // Verify PR description includes quality metrics
        var prDetails = await GetPullRequestDetails(ticket.PullRequestUrl);
        Assert.Contains("Quality Score:", prDetails.Description);
        Assert.Contains("Build Status: ✅", prDetails.Description);
    }
}
```

---

### Appendix E: Troubleshooting Guide

**Problem**: Quality evaluations always score low (<70) even for good code.

**Solution**:
1. Check QualityEvaluationAgent prompt template (may be too strict)
2. Review score weights configuration (may be unbalanced)
3. Verify test execution results (may have false failures)
4. Temporarily enable verbose logging: `EnableVerboseLogging = true`

---

**Problem**: Iterations not improving quality score.

**Solution**:
1. Review gap fix instructions (may be too vague)
2. Check if CodeImplementationAgent is receiving gap context
3. Verify iteration counter incrementing correctly
4. Examine previous iteration context in agent input

---

**Problem**: High escalation rate (>30%).

**Solution**:
1. Lower `MinimumQualityScore` temporarily (e.g., 85 instead of 90)
2. Increase `MaxQualityIterations` (e.g., 5 instead of 3)
3. Review failed implementations for common patterns
4. Improve prompt templates based on common failure modes

---

**Problem**: Merge conflicts during parallel execution.

**Solution**:
1. Reduce `MaxParallelTasks` (e.g., 2 instead of 4)
2. Improve task decomposition (better file isolation)
3. Enhance CodeIntegrationAgent conflict resolution prompt

---

**Problem**: Slow implementation (>15 minutes).

**Solution**:
1. Increase `MaxParallelTasks` if CPU/memory allow
2. Optimize CompilationAgent (use incremental builds)
3. Optimize TestExecutionAgent (run tests in parallel)
4. Reduce context size in agent prompts (fewer files)

---

## Summary

This design document provides a comprehensive blueprint for implementing a **quality-driven, iterative implementation loop** in PRFactory's Phase 3. Key innovations:

1. **Task Decomposition**: Intelligent breakdown of plans into parallelizable tasks
2. **Parallel Execution**: Multiple CodeImplementationAgent instances run concurrently
3. **AI-Powered Quality Evaluation**: Comprehensive code review across 5 dimensions
4. **Automated Iteration**: Fixes quality gaps automatically without human intervention
5. **Fail-Safe Escalation**: Suspends workflow and notifies humans when threshold not met

**Alignment with CLAUDE.md**:
- ✅ Extends existing ImplementationGraph (Option A)
- ✅ Uses proven parallel execution pattern (PlanningGraph)
- ✅ Maintains Clean Architecture separation
- ✅ Supports tenant-level configuration
- ✅ Includes comprehensive error handling
- ✅ Provides phased rollout plan (4 phases, 7 weeks)

**Expected Outcomes**:
- 85%+ implementation success rate
- <2 average iterations to approval
- 30% reduction in human review cycles
- Higher quality PRs (fewer bugs, better tests, higher coverage)

**Next Steps**:
1. Review and approve design document
2. Create implementation tickets for Phase 1
3. Allocate resources (2 developers, 7 weeks)
4. Begin Phase 1 development

---

**Document Status**: Draft → Awaiting Review  
**Last Updated**: 2025-11-09  
**Authors**: AI Agent Analysis based on PRFactory architecture study
