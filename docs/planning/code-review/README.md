# Multi-Agent Code Review Implementation Plan

**Status**: 🔴 Not Implemented
**Priority**: P2 (High)
**Effort**: 2-3 weeks
**Dependencies**: Epic 2 (Multi-LLM Support)

---

## Overview

This feature enables **cross-provider AI code review**, where one LLM (e.g., GPT-4o) reviews code written by another LLM (e.g., Claude Sonnet). This provides quality improvement through diverse model perspectives, bias reduction, and cost optimization.

**Business Value**:
- **Quality Improvement**: Different models catch different issues
- **Bias Reduction**: Cross-model review reduces single-model blind spots
- **Cost Optimization**: Use expensive models (GPT-4o, Claude Opus) only for review, not implementation
- **Flexibility**: Customers choose code generator vs code reviewer independently

---

## Architecture Components

### 1. CodeReviewGraph
New workflow graph that executes after ImplementationGraph completes.

**File**: `/src/PRFactory.Infrastructure/Agents/Graphs/CodeReviewGraph.cs`
**Lines**: ~200
**Dependencies**: AgentGraphBase, WorkflowOrchestrator

**Flow**:
```
ImplementationGraph (Claude Sonnet) → Creates PR
  ↓
CodeReviewGraph (GPT-4o) → Reviews PR → Posts feedback to PR
  ↓
Issues Found?
  YES → ImplementationGraph (Claude) → Fixes code → CodeReviewGraph (retry)
  NO  → Post approval comment → Workflow complete
```

**Key Responsibilities**:
- Execute CodeReviewAgent with configured LLM provider
- Parse review results for critical issues vs suggestions
- Post review comments to PR (GitHub/Bitbucket/Azure DevOps)
- Loop back to ImplementationGraph if issues found (max 3 iterations)
- Track retry count and fail gracefully after max retries

---

### 2. CodeReviewAgent
Specialized agent that performs PR reviews using configurable LLM provider.

**File**: `/src/PRFactory.Infrastructure/Agents/Specialized/CodeReviewAgent.cs`
**Lines**: ~150
**Dependencies**: BaseAgent, ILlmProviderFactory, IPromptLoaderService, IGitPlatformService

**Responsibilities**:
- Fetch PR details from git platform (files changed, diffs, commits)
- Load implementation plan for context
- Build template variables (ticket info, PR details, code changes, testing coverage)
- Render Handlebars prompt template
- Execute LLM review with configured provider
- Parse review response into structured format
- Store review results in database

---

### 3. Handlebars Template System
Template engine for rendering prompts with dynamic context.

**File**: `/src/PRFactory.Infrastructure/Application/PromptLoaderService.cs` (enhancement)
**Lines**: ~100 (additions)
**Dependencies**: Handlebars.Net NuGet package

**New Method**: `RenderTemplate(agentName, providerName, promptType, templateVariables)`

**Custom Helpers**:
- `{{code language content}}` - Format code blocks
- `{{truncate text maxLength}}` - Truncate long content
- `{{filesize bytes}}` - Format file sizes

---

### 4. Prompt Templates
Provider-specific prompt templates with 20+ template variables.

**Directory Structure**:
```
/prompts/code-review/
├── anthropic/
│   ├── system.txt
│   └── user_template.hbs
├── openai/
│   ├── system.txt
│   └── user_template.hbs
└── google/
    ├── system.txt
    └── user_template.hbs
```

**Template Variables**: See [TEMPLATE_VARIABLES.md](./TEMPLATE_VARIABLES.md)

---

### 5. Configuration Enhancements
Tenant-level configuration for code review behavior.

**Domain Entity**: `/src/PRFactory.Domain/Entities/Tenant.cs` (TenantConfiguration)

**New Properties**:
- `EnableAutoCodeReview` (bool)
- `CodeReviewLlmProviderId` (Guid?)
- `ImplementationLlmProviderId` (Guid?)
- `MaxCodeReviewIterations` (int, default 3)
- `AutoApproveIfNoIssues` (bool, default false)
- `RequireHumanApprovalAfterReview` (bool, default true)

---

### 6. WorkflowOrchestrator Integration
Handle graph transitions for code review flow.

**File**: `/src/PRFactory.Infrastructure/Agents/Graphs/WorkflowOrchestrator.cs` (update)
**Lines**: ~50 (additions)

**New Transitions**:
- `ImplementationGraph` → `CodeReviewGraph` (on PRCreatedMessage, if enabled)
- `CodeReviewGraph` → `ImplementationGraph` (on issues found, within retry limit)
- `CodeReviewGraph` → Complete (on no issues or max retries reached)

---

### 7. Database Schema Changes
Store code review results and configuration.

**Migration**: `AddCodeReviewConfiguration`

**Tables Modified**:
- `TenantConfiguration` - Add code review settings columns
- `AgentPromptTemplates` - Add `PreferredLlmProviderId` column

**New Tables** (optional):
- `CodeReviewResults` - Store review history for audit trail

---

### 8. Message Types
New messages for code review workflow.

**File**: `/src/PRFactory.Infrastructure/Agents/Messages/AgentMessages.cs` (additions)

**New Messages**:
- `ReviewCodeMessage` - Trigger code review
- `CodeReviewCompleteMessage` - Review finished
- `FixCodeIssuesMessage` - Fix issues found in review
- `PostCommentsMessage` - Post review comments to PR
- `ApprovalMessage` - Post approval comment

---

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- [ ] Install Handlebars.Net NuGet package
- [ ] Enhance PromptLoaderService with `RenderTemplate()` method
- [ ] Register custom Handlebars helpers
- [ ] Create message types for code review workflow
- [ ] Database migration for configuration fields
- [ ] Update TenantConfiguration entity

### Phase 2: Agent & Graph Implementation (Week 2)
- [ ] Implement CodeReviewAgent
- [ ] Implement CodeReviewGraph
- [ ] Create prompt templates for all providers (Anthropic, OpenAI, Google)
- [ ] Implement review result parser
- [ ] Implement PR comment posting logic
- [ ] Unit tests for CodeReviewAgent
- [ ] Unit tests for CodeReviewGraph

### Phase 3: Integration & UI (Week 3)
- [ ] Integrate CodeReviewGraph into WorkflowOrchestrator
- [ ] Add configuration UI in `/admin/agent-configuration`
- [ ] Display review results in ticket detail page
- [ ] Add review feedback to PR comments
- [ ] Integration tests (cross-provider review scenarios)
- [ ] End-to-end workflow tests
- [ ] Documentation updates

---

## Testing Strategy

### Unit Tests
- **CodeReviewAgent**: Template rendering, provider selection, result parsing
- **CodeReviewGraph**: Graph execution flow, retry logic, transitions
- **PromptLoaderService**: Handlebars rendering, custom helpers, variable substitution

### Integration Tests
- **Cross-Provider Review**: GPT-4 reviews Claude Sonnet code
- **Retry Loop**: Verify max 3 iterations with FixCodeIssuesMessage
- **Multi-Platform**: Test on GitHub, Bitbucket, Azure DevOps

### End-to-End Tests
- **Full Workflow**: Ticket creation → Implementation → Code Review → Fix → Approval → Complete
- **Configuration**: Verify tenant settings are respected
- **Error Handling**: Test failures in review agent, graph transitions

---

## Success Criteria

### Functional Requirements
- ✅ CodeReviewGraph executes after ImplementationGraph when enabled
- ✅ Different LLM providers can be used for implementation vs review
- ✅ Review results posted as comments to PR
- ✅ Up to 3 retry iterations for code fixes
- ✅ Human approval still required after AI review
- ✅ Works across GitHub, Bitbucket, Azure DevOps

### Non-Functional Requirements
- ✅ Review execution completes within 5 minutes (typical PR)
- ✅ Template rendering handles 1000+ line diffs
- ✅ Review results stored for audit trail
- ✅ Graceful degradation if review provider unavailable
- ✅ Clear error messages in UI when review fails

### Quality Requirements
- ✅ 80% test coverage for new code
- ✅ Zero critical SonarCloud issues
- ✅ All acceptance criteria met (13 items in Epic 2, Section 7.10)
- ✅ Documentation complete (API docs, user manual, architecture diagrams)

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **LLM provider rate limits** | High | Implement exponential backoff, queue reviews during rate limit |
| **Large PR diffs exceed token limits** | Medium | Truncate diffs, review changed files in batches |
| **Review quality varies by model** | Medium | Allow model selection per tenant, collect feedback metrics |
| **Infinite review loops** | High | Hard limit of 3 iterations, timeout after 30 minutes |
| **Cost explosion from reviews** | High | Track token usage, set monthly budget alerts, allow disabling |

---

## Related Documents

- [TEMPLATE_VARIABLES.md](./TEMPLATE_VARIABLES.md) - Complete list of Handlebars variables
- [IMPLEMENTATION_TASKS.md](./IMPLEMENTATION_TASKS.md) - Detailed task breakdown
- [TESTING_PLAN.md](./TESTING_PLAN.md) - Comprehensive testing scenarios
- [PROMPT_EXAMPLES.md](./PROMPT_EXAMPLES.md) - Sample prompt templates
- [EPIC_02_MULTI_LLM.md](../EPIC_02_MULTI_LLM.md) - Parent epic

---

## Questions & Decisions

### Open Questions
1. Should we support partial PR reviews (review only specific files)?
2. How to handle review conflicts when multiple models disagree?
3. Should review agent have access to git history beyond current PR?
4. What's the fallback behavior if Handlebars template missing?

### Design Decisions
- ✅ **Decision 1**: Use Handlebars.Net (not Scriban) for template engine - simpler syntax, better .NET support
- ✅ **Decision 2**: Store review results in database - needed for audit trail and analytics
- ✅ **Decision 3**: Hard limit of 3 review iterations - prevent infinite loops, balance quality vs cost
- ✅ **Decision 4**: Require human approval even after AI review - maintain human oversight

---

**Next Steps**:
1. Review this plan with team
2. Break down Phase 1 into specific tickets
3. Set up feature branch for development
4. Begin implementation starting with PromptLoaderService enhancement
