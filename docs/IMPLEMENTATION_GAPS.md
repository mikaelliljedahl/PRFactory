# Known Implementation Gaps

> **Purpose**: Track missing features, incomplete implementations, and technical debt
> **Last Updated**: 2025-11-09
> **Distinction**: These are gaps in completeness, NOT production blockers (see [CRITICAL_ISSUES.md](CRITICAL_ISSUES.md) for blockers)

---

## Overview

This document tracks **implementation gaps** - features that are incomplete or missing but do not block production deployment. These are prioritized based on risk, effort, and business value.

**Priority Levels**:
- 🔴 **CRITICAL** - Should be addressed before production (high risk if missing)
- 🟠 **HIGH** - Should be addressed soon (moderate risk)
- 🟡 **MEDIUM** - Important but not urgent
- 🟢 **LOW** - Nice to have, low risk

---

## 🔴 CRITICAL: Minimal Test Coverage

**Priority**: 🔴 CRITICAL
**Risk Level**: HIGH
**Current State**: 10% estimated coverage (151 tests pass)
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

While the project has 151 passing tests and the testing framework is properly configured (xUnit, Moq, EF Core InMemory), the test coverage is minimal. Critical components have zero test coverage.

**What's Missing**:
- ❌ **Agent Tests** - 0 tests for 17 agents (AnalysisAgent, PlanningAgent, etc.)
- ❌ **Graph Tests** - 0 tests for 4 graphs (RefinementGraph, PlanningGraph, ImplementationGraph, WorkflowOrchestrator)
- ❌ **Provider Tests** - 0 tests for 3 git platform providers (GitHub, Bitbucket, Azure DevOps)
- ❌ **E2E Workflow Tests** - 0 tests for complete workflows (ticket → refinement → planning → implementation)
- ⚠️ **Service Tests** - Partial coverage (151 tests exist, but scope unclear)

**What's Working**:
- ✅ Test framework configured (xUnit)
- ✅ Mocking configured (Moq)
- ✅ In-memory database configured (EF Core InMemory)
- ✅ 151 tests pass successfully
- ✅ Build includes test execution

### Impact

**Without proper test coverage**:
- Cannot confidently refactor code
- Cannot safely extend features
- High risk of regression bugs
- Difficult to onboard new developers
- Cannot verify agent behavior

**Risk Assessment**: HIGH - Production deployment is risky without tests, but technically possible if manual QA is thorough.

### Recommended Implementation Plan

**Phase 1: Agent Unit Tests (2 weeks)**
- Test each agent in isolation with mocked dependencies
- Test error handling, retry logic, timeout behavior
- Test state transitions and checkpoint creation
- **Deliverable**: 100+ agent tests

**Phase 2: Graph Integration Tests (2 weeks)**
- Test each graph's full execution path
- Test suspension/resume behavior
- Test parallel execution (e.g., GitPlan + JiraPost)
- Test loop-back logic (e.g., plan rejection)
- **Deliverable**: 50+ graph tests

**Phase 3: Provider Integration Tests (1 week)**
- Test GitHub, Bitbucket, Azure DevOps providers
- Mock HTTP responses (use WireMock or similar)
- Test retry policies and error handling
- **Deliverable**: 30+ provider tests

**Phase 4: E2E Workflow Tests (1 week)**
- Test complete workflows from trigger to completion
- Test human-in-the-loop scenarios
- Test error recovery and resumption
- **Deliverable**: 10+ E2E tests

**Total Estimated Effort**: 6 weeks

### Acceptance Criteria

- [ ] All 17 agents have unit tests (>80% coverage)
- [ ] All 4 graphs have integration tests (>70% coverage)
- [ ] All 3 providers have integration tests (>80% coverage)
- [ ] At least 10 E2E workflow tests exist
- [ ] Code coverage reports generated and reviewed
- [ ] All new code requires tests (enforced in PR reviews)

---

## 🟠 HIGH: Jira Integration Implementation Unclear

**Priority**: 🟠 HIGH
**Risk Level**: MEDIUM
**Current State**: Interfaces defined, implementation unclear
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

Jira integration is a core feature, but the actual implementation status is unclear from code exploration. Interfaces exist, but whether the full REST client is implemented and functional is uncertain.

**What's Defined**:
- ✅ `IJiraService` interface (PostCommentAsync, LinkPullRequestAsync, UpdateCustomFieldAsync, TransitionToStatusAsync)
- ✅ `JiraCommentParser` (parses @claude mentions)
- ✅ `JiraWebhookValidator` (validates webhook signatures)
- ✅ Refit configured for HTTP client

**What's Unclear**:
- ⚠️ Is `IJiraService` fully implemented or stubbed?
- ⚠️ Has the Jira REST client been tested against real Jira instance?
- ⚠️ Are all Jira API endpoints correctly mapped?
- ⚠️ Does authentication work (API token, OAuth)?

### Impact

**If Jira integration is incomplete**:
- Cannot post comments to tickets
- Cannot receive webhook triggers from Jira
- Core workflow (human-in-the-loop) broken
- Major feature gap

**Risk Assessment**: MEDIUM - This is a core feature, but the architecture is in place. Implementation may be complete and just not verified.

### Recommended Action

**Immediate (1-2 days)**:
1. Code review of Jira implementation:
   - Check if `JiraService` class exists and implements `IJiraService`
   - Verify all methods have real implementations (not stubs)
   - Check for integration tests

2. Manual testing:
   - Configure PRFactory with Jira API token
   - Trigger workflow from Jira webhook
   - Verify comments posted to Jira
   - Verify @claude mentions parsed correctly

3. Document findings:
   - Update this document with status
   - If gaps found, create detailed implementation task

**If gaps found (1-2 weeks)**:
- Implement missing Jira REST client methods
- Add integration tests against Jira sandbox
- Document Jira configuration requirements
- Test with real Jira Cloud instance

### Acceptance Criteria

- [ ] All `IJiraService` methods implemented (not stubbed)
- [ ] Integration tests for Jira client (mocked HTTP)
- [ ] Manual test against Jira Cloud successful
- [ ] Webhook validation tested with real Jira signature
- [ ] Documentation updated with Jira setup instructions

---

## 🟠 HIGH: OAuth Authentication Not Implemented

**Priority**: 🟠 HIGH
**Risk Level**: HIGH (but not a blocker - see CRITICAL_ISSUES.md #2)
**Current State**: StubCurrentUserService placeholder
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

User authentication is implemented as a stub (`StubCurrentUserService`), which always returns a hardcoded demo user. This prevents real multi-tenant isolation and user management.

**Current State**:
- ✅ `ICurrentUserService` interface defined
- ✅ `User` entity exists in database
- ✅ Architecture supports user claims
- ❌ OAuth not integrated
- ❌ Login/logout flows missing
- ❌ Session management missing
- ❌ User registration missing

**Related Critical Issue**:
- This is related to [CRITICAL_ISSUES.md #2: No Authentication Layer](CRITICAL_ISSUES.md#-issue-2-no-authentication-layer-api-completely-open)
- Authentication is a **production blocker**
- This gap tracks the specific OAuth implementation task

### Impact

**Without OAuth**:
- Cannot identify users
- Cannot implement role-based access control (RBAC)
- Cannot track who performed actions (audit log incomplete)
- Cannot implement user-specific features (notifications, preferences)

**Risk Assessment**: HIGH - Required for production, but not blocking initial deployment if using internal-only deployment with VPN/network isolation.

### Recommended Implementation Plan

See [CRITICAL_ISSUES.md #2](CRITICAL_ISSUES.md#-issue-2-no-authentication-layer-api-completely-open) for full implementation plan.

**Summary**:
1. Choose identity provider (Auth0, Azure AD, Okta)
2. Install OpenID Connect middleware
3. Implement `ICurrentUserService` from JWT claims
4. Add `[Authorize]` attributes to controllers
5. Implement Blazor authentication UI
6. Test user flows

**Estimated Effort**: 3-4 weeks

---

## 🟡 MEDIUM: Web UI - Tenant & Repository Configuration Pages Missing

**Priority**: 🟡 MEDIUM
**Risk Level**: LOW (can configure via database initially)
**Current State**: Core UI implemented, admin pages missing
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

The Blazor Web UI has core pages for tickets and dashboard, but admin configuration pages for tenants and repositories are missing. Currently, these must be configured directly in the database.

**What's Implemented**:
- ✅ Dashboard (statistics, recent activity)
- ✅ Ticket list and detail pages
- ✅ Ticket creation wizard
- ✅ Team review components (plan approval, commenting)
- ✅ Pure UI component library (`/UI/*`)
- ✅ Business components (`/Components/*`)

**What's Missing**:
- ❌ Tenant management pages (list, create, edit, delete)
- ❌ Repository configuration pages (list, create, edit, delete)
- ❌ Repository connection wizard (test git access, configure credentials)
- ❌ Jira integration configuration UI
- ❌ Agent prompt template editor
- ❌ User management UI (assign users to tenants, manage permissions)

### Impact

**Without admin UI**:
- Must configure tenants/repos via SQL or seed data
- Difficult to onboard new customers
- Cannot easily test git connections
- No way to update Jira credentials without database access

**Risk Assessment**: LOW - Can launch with database configuration initially. Admin UI can be added post-launch.

### Recommended Implementation Plan

**Phase 1: Repository Configuration (1 week)**
- Create `/Repositories/Index.razor` (list repositories)
- Create `/Repositories/Create.razor` (add new repository)
- Create `/Repositories/Edit.razor` (edit repository settings)
- Test git connection button (validate credentials)

**Phase 2: Tenant Management (1 week)**
- Create `/Tenants/Index.razor` (list tenants - admin only)
- Create `/Tenants/Create.razor` (create new tenant)
- Create `/Tenants/Edit.razor` (edit tenant configuration)
- Configure feature flags, token limits, etc.

**Phase 3: Integration Configuration (1 week)**
- Create Jira configuration page (API URL, credentials, webhook secret)
- Create GitHub/Bitbucket/Azure DevOps configuration pages
- Test integration buttons (validate API access)

**Phase 4: Agent Prompt Editor (1 week)**
- Create prompt template list page
- Create prompt template editor (markdown with YAML frontmatter)
- Test prompt rendering
- Version control for prompts

**Total Estimated Effort**: 4 weeks

### Acceptance Criteria

- [ ] Can create/edit repositories via Web UI
- [ ] Can test git connections from UI
- [ ] Can create/edit tenants via Web UI (admin only)
- [ ] Can configure Jira integration via UI
- [ ] Can edit agent prompt templates via UI
- [ ] All configuration changes reflected in database

---

## 🟡 MEDIUM: Agent Prompt Templates Not Wired Into Agents

**Priority**: 🟡 MEDIUM
**Risk Level**: LOW (agents use hardcoded prompts currently)
**Current State**: Infrastructure complete, agents not using templates
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

The agent prompt template system is fully implemented (`AgentPromptService`, `AgentPromptLoaderService`, YAML frontmatter support), but individual agents still use hardcoded prompts instead of loading from templates.

**What's Implemented**:
- ✅ `IAgentPromptService` interface and implementation
- ✅ `AgentPromptLoaderService` loads prompts from markdown files
- ✅ 6 prompt templates in `.claude/agents/` directory
- ✅ YAML frontmatter parsing
- ✅ Tenant-specific prompt customization support

**What's Missing**:
- ❌ Agents don't call `IAgentPromptService.GetPromptAsync()`
- ❌ Agents use hardcoded strings instead of templates
- ❌ No dynamic prompt loading in agent execution

**Example (Current - Hardcoded)**:
```csharp
// AnalysisAgent.cs (current - incorrect)
public class AnalysisAgent : BaseAgent
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        string prompt = "Analyze this codebase..."; // HARDCODED
        var result = await _cliAgent.ExecuteAsync(prompt, ct);
    }
}
```

**Example (Target - Template-Driven)**:
```csharp
// AnalysisAgent.cs (should be)
public class AnalysisAgent : BaseAgent
{
    private readonly IAgentPromptService _promptService;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var prompt = await _promptService.GetPromptAsync(
            "analysis-agent",
            new { RepositoryPath = _workflowState.WorkspacePath },
            ct);
        var result = await _cliAgent.ExecuteAsync(prompt.Content, ct);
    }
}
```

### Impact

**Without template integration**:
- Cannot customize prompts per tenant
- Must rebuild to change agent behavior
- Cannot A/B test prompts
- Prompt versioning not tracked

**Risk Assessment**: LOW - Current hardcoded prompts work. Template system is "nice to have" for flexibility.

### Recommended Implementation Plan

**Phase 1: Update Base Agents (1 week)**
- Inject `IAgentPromptService` into `BaseAgent`
- Update each of 17 agents to load prompts from templates
- Ensure backward compatibility if template not found

**Phase 2: Template Migration (3 days)**
- Extract hardcoded prompts to `.claude/agents/` directory
- Create markdown files with YAML frontmatter
- Define template variables for each agent

**Phase 3: Testing (2 days)**
- Test all agents with new template system
- Verify tenant-specific prompt overrides work
- Test prompt caching

**Total Estimated Effort**: 2 weeks

### Acceptance Criteria

- [ ] All 17 agents load prompts from `IAgentPromptService`
- [ ] All prompts exist as markdown files in `.claude/agents/`
- [ ] Tenant-specific prompt overrides tested
- [ ] No hardcoded prompts remain in agent code
- [ ] Prompt template documentation updated

---

## 🟢 LOW: GitLab Provider Not Implemented

**Priority**: 🟢 LOW
**Risk Level**: LOW (only needed if customers use GitLab)
**Current State**: Interface defined, not implemented
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

GitLab is the 4th major git platform, but the provider is not implemented. Architecture supports it, and GitLab.NET library is available.

**Current Platform Support**:
- ✅ GitHub (Octokit SDK)
- ✅ Bitbucket (REST API)
- ✅ Azure DevOps (Official SDK)
- ❌ GitLab (planned)

### Impact

**Without GitLab provider**:
- Cannot support customers using GitLab
- Missing parity with competitors (e.g., Agor.live supports GitLab)

**Risk Assessment**: LOW - GitLab has ~30% market share vs. GitHub's ~60%. Not critical for initial launch.

### Recommended Implementation Plan

**Prerequisites**:
- Review GitLab API documentation
- Install `GitLabApiClient` NuGet package
- Create GitLab test account/project

**Implementation (1 week)**:
1. Create `GitLabProvider : IGitPlatformProvider`
2. Implement required methods:
   - `CreatePullRequestAsync` (GitLab calls them "Merge Requests")
   - `AddPullRequestCommentAsync`
   - `GetRepositoryInfoAsync`
3. Configure Polly retry policy
4. Handle GitLab-specific authentication (Personal Access Token, OAuth)
5. Add integration tests

**Total Estimated Effort**: 1 week

### Acceptance Criteria

- [ ] `GitLabProvider` implements `IGitPlatformProvider`
- [ ] Can create merge request on GitLab
- [ ] Can add comments to merge request
- [ ] Retry policy configured
- [ ] Integration tests pass
- [ ] Documentation updated

---

## 🟢 LOW: CodexCliAdapter Stub (Future OpenAI Support)

**Priority**: 🟢 LOW
**Risk Level**: NONE (not needed for current functionality)
**Current State**: Placeholder stub
**GitHub Issue**: None (tracked in ROADMAP.md)
**Owner**: [TBD when prioritized]

### Problem Description

`CodexCliAdapter` exists as a placeholder for future OpenAI Codex support, but is not implemented. This is intentional - OpenAI support is a future enhancement, not a current requirement.

**Current State**:
- ✅ `ICliAgent` interface (LLM-agnostic)
- ✅ `ClaudeCodeCliAdapter` fully implemented
- 🚧 `CodexCliAdapter` is stub only

### Impact

None - this is a future feature. Not needed for production launch.

### Recommended Action

**Do Nothing** - Track in [ROADMAP.md](ROADMAP.md) for future prioritization. Complete this only if:
1. Customer requests OpenAI support
2. Pricing/performance comparison favors OpenAI
3. Claude API integration proves insufficient

---

## Summary Table

| Gap | Priority | Risk | Effort | Target Resolution |
|-----|----------|------|--------|-------------------|
| **Test Coverage** | 🔴 CRITICAL | HIGH | 6 weeks | Before v1.0 release |
| **Jira Integration Verification** | 🟠 HIGH | MEDIUM | 1-2 weeks | Before v1.0 release |
| **OAuth Implementation** | 🟠 HIGH | HIGH | 3-4 weeks | Before production deployment (see CRITICAL_ISSUES.md) |
| **Admin UI (Tenants/Repos)** | 🟡 MEDIUM | LOW | 4 weeks | Post-launch (v1.1) |
| **Agent Prompt Templates** | 🟡 MEDIUM | LOW | 2 weeks | Post-launch (v1.1) |
| **GitLab Provider** | 🟢 LOW | LOW | 1 week | Post-launch (v1.2) |
| **CodexCliAdapter** | 🟢 LOW | NONE | TBD | Future (v2.0?) |

**Total Estimated Effort for Pre-Launch Gaps**: 10-12 weeks

---

## Related Documents

- [CRITICAL_ISSUES.md](CRITICAL_ISSUES.md) - Production blocking issues
- [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) - Current implementation completeness
- [ROADMAP.md](ROADMAP.md) - Future enhancements and features
- [/docs/reviews/ARCHITECTURE_REVIEW.md](reviews/ARCHITECTURE_REVIEW.md) - Full architectural review

---

## Update Log

| Date | Change | Updated By |
|------|--------|------------|
| 2025-11-09 | Initial document created with 7 implementation gaps | Documentation Audit |
