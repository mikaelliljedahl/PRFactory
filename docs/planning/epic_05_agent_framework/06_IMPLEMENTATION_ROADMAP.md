# 06: Implementation Roadmap

**Document Purpose:** Week-by-week implementation timeline with milestones, deliverables, and approval gates.

**Total Duration:** 10-12 weeks
**Team Size:** 2 senior engineers
**Last Updated:** 2025-11-13

---

## Timeline Overview

```
Week 1-3: Foundation
Week 4-6: Agent Roles  
Week 7-8: Tool Ecosystem
Week 9-10: UI Integration
Week 11-12: Production Readiness
```

---

## Phase 1: Foundation (Weeks 1-3)

### Objectives
- Create infrastructure for Agent Framework integration
- Establish database schema and configuration system
- Implement core tool interfaces
- Set up observability

### Week 1: Project Setup & Core Interfaces

**Tasks:**
- [ ] Create `PRFactory.AgentTools` class library project
- [ ] Implement `ITool` interface (from Saturn)
- [ ] Implement `ToolBase` abstract class
- [ ] Implement `ToolExecutionContext` and `ToolExecutionResult`
- [ ] Create security utilities (`PathValidator`, `SsrfProtection`)
- [ ] Set up xUnit test project for tools

**Deliverables:**
- PRFactory.AgentTools.csproj
- Core interfaces with XML documentation
- Security utilities with unit tests

**Approval Gate:** Architecture review

---

### Week 2: Database Schema & Agent Framework SDK

**Tasks:**
- [ ] Create `AgentConfiguration` entity and DbSet
- [ ] Create `AgentExecutionLog` entity and DbSet
- [ ] Add migration for new tables
- [ ] Extend `Checkpoint` entity with AgentThread fields
- [ ] Add NuGet package: `Microsoft.SemanticKernel`
- [ ] Implement `AgentFactory` interface and class
- [ ] Implement `AgentConfigurationService`

**Deliverables:**
- Database migration scripts
- AgentConfiguration CRUD operations
- AgentFactory with DI registration

**Approval Gate:** Schema review, DBA approval

---

### Week 3: Tool Registry & First Tools

**Tasks:**
- [ ] Implement `ToolRegistry` with auto-discovery
- [ ] Implement `ReadFileTool` with security validations
- [ ] Implement `WriteFileTool` with atomic operations
- [ ] Implement `GrepTool` with regex support
- [ ] Implement `GlobTool` with pattern matching
- [ ] Set up OpenTelemetry integration
- [ ] Write comprehensive unit tests (80%+ coverage)

**Deliverables:**
- ToolRegistry with auto-discovery
- 4 file system tools (tested)
- OpenTelemetry integration

**Approval Gate:** Tool security review, penetration testing

---

## Phase 2: Agent Roles (Weeks 4-6)

### Objectives
- Implement first agent adapters for RefinementGraph and PlanningGraph
- Validate end-to-end workflow with Agent Framework agents
- Establish middleware patterns for multi-tenancy

### Week 4: AnalyzerAgent Implementation

**Tasks:**
- [ ] Implement `AnalysisAgentAdapter` (wraps AF AnalyzerAgent)
- [ ] Configure default `AnalyzerAgent` in database (seed data)
- [ ] Implement checkpoint save/restore for agent state
- [ ] Implement `GetJiraTicketTool` for Jira integration
- [ ] Implement `CodeSearchTool` for semantic search
- [ ] Write integration tests for AnalyzerAgent

**Deliverables:**
- AnalysisAgentAdapter integrated into RefinementGraph
- End-to-end test: Jira webhook → Analysis → Result

**Approval Gate:** Demo AnalyzerAgent vs current AnalysisAgent

---

### Week 5: PlannerAgent Implementation

**Tasks:**
- [ ] Implement `PlannerAgentAdapter` (wraps AF PlannerAgent)
- [ ] Configure default `PlannerAgent` in database (seed data)
- [ ] Implement structured output parsing (JSON plan format)
- [ ] Implement multi-turn conversation support (questions/answers)
- [ ] Add middleware: `TenantIsolationMiddleware`
- [ ] Add middleware: `TokenBudgetMiddleware`
- [ ] Write integration tests for PlannerAgent

**Deliverables:**
- PlannerAgentAdapter integrated into PlanningGraph
- Middleware chain for security and resource limits
- End-to-end test: Refinement → Planning → Structured plan

**Approval Gate:** Demo PlannerAgent output quality

---

### Week 6: Testing & Refinement

**Tasks:**
- [ ] Comprehensive E2E testing (Jira → Refinement → Planning)
- [ ] Performance benchmarking (latency, token usage)
- [ ] Security testing (cross-tenant isolation, path traversal)
- [ ] Fix bugs and edge cases
- [ ] Update documentation
- [ ] Train team on new architecture

**Deliverables:**
- Test report (E2E, performance, security)
- Bug fixes
- Team training materials

**Approval Gate:** Engineering leadership sign-off for Phase 3

---

## Phase 3: Tool Ecosystem (Weeks 7-8)

### Objectives
- Complete tool implementations across all categories
- Security hardening and validation
- Tool execution audit logging

### Week 7: Complete Tool Implementations

**Tasks:**
- [ ] Implement Git tools: `CommitTool`, `CreateBranchTool`, `GetDiffTool`
- [ ] Implement Jira tools: `AddCommentTool`, `TransitionTicketTool`
- [ ] Implement command tools: `ExecuteShellTool`, `RunTestsTool`
- [ ] Implement web tools: `WebFetchTool` (with SSRF protection)
- [ ] Implement `DependencyMapTool` for code analysis
- [ ] Write unit tests for all tools (80%+ coverage)

**Deliverables:**
- 15+ tools implemented and tested
- Comprehensive tool test suite

**Approval Gate:** Tool inventory review

---

### Week 8: Security Hardening & Audit Logging

**Tasks:**
- [ ] Implement `AuditLoggingMiddleware`
- [ ] Add tool execution logging to `AgentExecutionLog` table
- [ ] Security review: All tools validated for path traversal, SSRF, injection
- [ ] Add resource limits: File size, timeout, rate limiting
- [ ] Performance optimization: Tool execution caching
- [ ] Admin UI: Tool execution logs viewer

**Deliverables:**
- Complete audit trail for all tool executions
- Security validation report
- Admin UI for log viewing

**Approval Gate:** Security team sign-off

---

## Phase 4: UI Integration (Weeks 9-10)

### Objectives
- AG-UI protocol implementation
- Blazor components for agent interaction
- Real-time streaming and follow-up questions

### Week 9: AG-UI Protocol & Streaming

**Tasks:**
- [ ] Implement AG-UI HTTP endpoint (`/api/agent/chat`)
- [ ] Implement Server-Sent Events (SSE) streaming
- [ ] Create `AgentChatService` for message handling
- [ ] Create Blazor component: `AgentChat.razor`
- [ ] Create Blazor component: `AgentMessage.razor`
- [ ] Implement real-time streaming UI (partial responses)

**Deliverables:**
- AG-UI endpoint with SSE streaming
- Blazor components for agent chat

**Approval Gate:** Demo streaming UI

---

### Week 10: Follow-Up Questions & Approval Gates

**Tasks:**
- [ ] Implement follow-up question flow (agent asks, user answers)
- [ ] Create Blazor component: `AgentFollowUpQuestion.razor`
- [ ] Create Blazor component: `AgentApprovalGate.razor`
- [ ] Update workflow orchestrator for user interaction pauses
- [ ] Implement approval gate UI (approve/reject proposed actions)
- [ ] UX polish and refinement

**Deliverables:**
- Interactive agent UI with follow-up questions
- Approval gate UI for human-in-the-loop
- User acceptance testing results

**Approval Gate:** UX team sign-off

---

## Phase 5: Production Readiness (Weeks 11-12)

### Objectives
- Complete remaining agent roles
- End-to-end testing
- Production deployment

### Week 11: CodeExecutorAgent & ReviewerAgent

**Tasks:**
- [ ] Implement `CodeExecutorAgentAdapter` for ImplementationGraph
- [ ] Implement `ReviewerAgentAdapter` for CodeReviewGraph
- [ ] Configure default agents in database (seed data)
- [ ] End-to-end testing: Jira → Refinement → Planning → Implementation → Review → PR
- [ ] Performance optimization (parallel tool execution, caching)
- [ ] Documentation: Architecture, API, admin guide

**Deliverables:**
- All 4 agent roles implemented
- Complete E2E workflow tested
- Documentation complete

**Approval Gate:** E2E demo to stakeholders

---

### Week 12: Production Deployment

**Tasks:**
- [ ] Create feature flags: `EnableAgentFrameworkAnalyzer`, `EnableAgentFrameworkPlanner`, etc.
- [ ] Deploy to staging environment
- [ ] Staging validation with real Jira tickets
- [ ] Create deployment runbook
- [ ] Train customer success team
- [ ] Deploy to production (pilot tenant only)
- [ ] Monitor production metrics (latency, token usage, errors)

**Deliverables:**
- Production deployment
- Feature flags for gradual rollout
- Monitoring dashboards
- Training materials

**Approval Gate:** Production readiness review, go-live decision

---

## Milestones

| Milestone | Week | Description |
|-----------|------|-------------|
| M1: Foundation Complete | 3 | Tools library + DB schema + AF SDK integrated |
| M2: First Agent Live | 4 | AnalyzerAgent working in RefinementGraph |
| M3: Planning Agents Live | 6 | Both AnalyzerAgent and PlannerAgent production-ready |
| M4: Complete Tool Ecosystem | 8 | 15+ tools implemented, security validated |
| M5: UI Integration Complete | 10 | AG-UI streaming, follow-up questions, approval gates |
| M6: Production Deployment | 12 | All agents deployed to production with feature flags |

---

## Success Criteria

### Phase 1 (Week 3)
- [x] PRFactory.AgentTools project created with core interfaces
- [x] 5+ tools implemented and tested (80%+ coverage)
- [x] Database schema deployed
- [x] OpenTelemetry integration working

### Phase 2 (Week 6)
- [x] AnalyzerAgent produces better analysis than current
- [x] PlannerAgent generates structured plans
- [x] Token usage < 2x current approach
- [x] Latency < 5s added per workflow stage
- [x] Multi-tenant isolation verified

### Phase 3 (Week 8)
- [x] 15+ tools implemented
- [x] Security validation passes (no vulnerabilities)
- [x] Tool audit logs working
- [x] Performance benchmarks acceptable

### Phase 4 (Week 10)
- [x] AG-UI streaming works in Blazor
- [x] Follow-up questions functional
- [x] User feedback positive (user acceptance testing)

### Phase 5 (Week 12)
- [x] All 4 agent roles deployed
- [x] E2E workflow tested (Jira → PR)
- [x] Feature flags enabled for pilot tenant
- [x] Production monitoring active

---

## Risk Mitigation

| Risk | Week | Mitigation |
|------|------|------------|
| Agent Framework SDK breaking changes | 1-3 | Pin to stable version, subscribe to release notes |
| Token costs exceed budget | 4-6 | Implement TokenBudgetMiddleware, use smaller models for simple tasks |
| Agent output quality issues | 4-6 | Extensive prompt engineering, few-shot examples, constrained outputs |
| Security vulnerabilities in tools | 7-8 | Security review, penetration testing, multiple validation layers |
| UX issues with streaming | 9-10 | User testing, iterative refinement, fallback to non-streaming |
| Production incidents | 12 | Feature flags for quick rollback, comprehensive monitoring, runbooks |

---

## Dependencies & Blockers

### External Dependencies
- Microsoft Agent Framework SDK availability
- Azure OpenAI / Anthropic API access
- Jira sandbox environment for testing

### Internal Dependencies
- Database migration approval (Week 2)
- Security team review (Week 8)
- UX team sign-off (Week 10)
- Production deployment window (Week 12)

---

## Team Allocation

### Engineers (2 Full-Time)
- **Engineer 1 (Backend Focus):** Tools library, agent adapters, middleware
- **Engineer 2 (Full-Stack):** Database schema, UI integration, E2E testing

### Part-Time Support
- **Architect (10%):** Design reviews, technical guidance
- **Security Engineer (10%):** Security reviews, penetration testing
- **UX Designer (10%):** AG-UI design, user testing
- **DevOps (10%):** Deployment, monitoring, infrastructure

---

## Next Steps

1. ✅ **Detailed plans created** - All implementation documents ready
2. ⏳ **Stakeholder review** - Present roadmap to engineering leadership
3. ⏳ **Resource allocation** - Assign 2 engineers for 10-12 weeks
4. ⏳ **Kick off Week 1** - Create PRFactory.AgentTools project
5. ⏳ **Weekly status updates** - Progress tracking and blocker resolution

**Status:** ✅ **Ready for kickoff**
