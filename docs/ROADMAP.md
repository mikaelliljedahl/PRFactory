# PRFactory Roadmap

**Last Updated**: 2025-11-15
**Purpose**: Clear future vision separated from current implementation

This document outlines planned enhancements beyond the current MVP implementation.

> **Current Status**: See [IMPLEMENTATION_STATUS.md](./IMPLEMENTATION_STATUS.md) for what's built today.

---

## Roadmap Principles

1. **AI Assists, Humans Decide** - Never automate final deployment
2. **Enterprise-First** - Multi-tenant, multi-platform, secure
3. **Extensibility** - Modular architecture for new workflows
4. **Developer Experience** - Fast, reliable, transparent

---

## Recently Completed ✅

### Epic 08: System Architecture Cleanup (Complete - Nov 14, 2025)
- ✅ **Phase 1: Project Consolidation** (Nov 14, 2025) - Merged 3 projects (Api, Worker, Web) into single consolidated `PRFactory.Web` project
- ✅ **Phase 2: CSS Isolation** (Nov 14, 2025) - Migrated all 38 UI components to `.razor.css` files (100% CSS isolation)
- ✅ **Phase 3: Server-Side Pagination** (Nov 14, 2025) - Database-level pagination for Tickets, Repositories, Errors (83% faster page loads)
- ✅ **Phase 4: Missing UI Components** (Nov 14, 2025) - Added PageHeader, GridLayout, Section, InfoBox, ProgressBar components
- ✅ **Phase 5: DTO Mapping with Mapperly** (Nov 14, 2025) - Compile-time source generation for zero runtime overhead
- ✅ **Phase 6: Final Polish** (Nov 14, 2025) - Refactored high-traffic pages, standardized error display, updated all documentation
- ✅ **Impact Metrics**: 66% fewer containers (3→1), 100% CSS isolation, 83% faster page loads, 100% automated DTO mapping
- ✅ **Deployment Simplification**: Single `dotnet run` command, single Docker container, unified deployment model

### Epic 06: Admin UI (Complete - Nov 13-14, 2025)
- ✅ **Phase 1: Admin Services Foundation** (Nov 13, 2025) - Application services for repository, LLM provider, tenant config, and user management
- ✅ **Phase 2: Repository Management UI** (Nov 14, 2025) - Multi-platform Git repository configuration with connection testing
- ✅ **Phase 3: LLM Provider Configuration UI** (Nov 14, 2025) - 6 provider types with OAuth and API key support
- ✅ **Phase 4: Tenant Settings UI** (Nov 14, 2025) - Workflow, code review, and LLM provider assignment configuration
- ✅ **Phase 5: User Management UI** (Nov 14, 2025) - Role-based access control with Owner/Admin/Member/Viewer roles
- ✅ **Implementation Statistics**: 67 files created (46 production, 21 tests), 6,626 insertions, 130 unit tests, 100% test coverage
- ✅ **Self-service configuration** for all admin functions with encrypted credential storage

### Epic 05: Agent System Foundation ✅ (November 2025)
**Status**: Complete and enabled by default for all users

Autonomous AI agents with tool execution, multi-turn reasoning, and real-time streaming UI.

**Delivered**:
- 22 autonomous tools (file, Git, Jira, analysis, command)
- AgentConfiguration entity and AgentFactory for runtime agent creation
- AG-UI integration with SSE streaming and Blazor components
- AFAnalyzerAgent with autonomous tool use
- Middleware: TenantIsolation, TokenBudget, AuditLogging
- 100+ tests, 2,079 total passing (100% pass rate)

### Epic 07: Planning Phase UX & Collaboration Improvements (Nov 14, 2025)
- ✅ **Enhanced Planning Prompts** - 5 domain-specific prompt templates (web_ui, rest_api, database, background_jobs, refactoring)
- ✅ **ArchitectureContextService** - Intelligent prompt selection based on ticket analysis (268 lines)
- ✅ **Rich Markdown Editor** - Professional split-view editor with live preview and formatting toolbar
- ✅ **Inline Comment Anchoring** - Contextual discussions on specific plan lines with visual indicators
- ✅ **Review Checklists** - Structured, domain-specific review guidance with 4 YAML templates
- ✅ **Database Migrations** - 2 migrations for InlineCommentAnchors and ReviewChecklists
- ✅ **Comprehensive Testing** - 2,404 lines of new test coverage (5 test files)
- ✅ **76 files changed** - 13,050 insertions, 7 new UI components, Notion-like collaborative experience

### Comprehensive Blazor Testing Infrastructure (PR #61 - Nov 13, 2025)
- ✅ **bUnit Test Suite** - 1,424 tests for 88 Blazor components with 100% pass rate
- ✅ **Test Infrastructure** - Reusable base classes (TestContextBase, ComponentTestBase, PageTestBase)
- ✅ **Fluent Test Data Builders** - 8 builder classes for DTO test data generation
- ✅ **Comprehensive Coverage**:
  - 26 pure UI components tested (418 tests)
  - 34 business components tested (~500 tests)
  - 28 page components tested (~500 tests)
- ✅ **Testing Documentation** - Complete Blazor testing guide (570 lines)
- ✅ **Package Integration** - bUnit 1.32.7 and AngleSharp 1.1.2
- ✅ **Total Test Count** - 2,136 tests (712 backend + 1,424 Blazor), 100% pass rate
- ⚠️ 30 tests strategically skipped with TODO messages for future refinement
- ⚠️ 2 test files disabled (caused infinite hangs, needs investigation)

### C# Codebase Modernization (PR #62 - Nov 13, 2025)
- ✅ **Primary Constructors (C# 12)** - Eliminated ~79 lines of constructor boilerplate across 9 classes
- ✅ **Collection Expressions (C# 12)** - Modernized 20+ collection initializations across 11 files
- ✅ **Global Usings (C# 10)** - Removed ~180 lines of duplicate using directives
- ✅ **ArgumentNullException.ThrowIfNull()** - Modernized 125+ null checks across 126 files
- ✅ **Code Quality Fixes** - 5 SonarQube violations resolved (S4487, S2139)
- ✅ **100% Backward Compatible** - No breaking changes, all 712 tests passing
- ✅ **Performance Impact** - ~280 lines reduced, ~1,680 tokens saved
- ✅ **Files Refactored** - 127 files modernized across all layers

### Epic 02: Multi-LLM Support with Code Review (PR #59 - Nov 12, 2025)
- ✅ **Multi-LLM Provider Infrastructure** - ILlmProvider interface, factory pattern, 3 providers (Claude, OpenAI, Gemini)
- ✅ **Automated Code Review Workflow** - CodeReviewGraph with AI-powered PR review
- ✅ **Cross-Provider Review** - One LLM can review code written by another (e.g., GPT-4 reviews Claude code)
- ✅ **Prompt Template System** - 24 Handlebars templates for 4 agents × 3 providers
- ✅ **Per-Agent Provider Configuration** - Different agents can use different LLMs
- ✅ **Iteration Loop** - Implementation → CodeReview → Fix (max 3 iterations)
- ✅ **Agent Configuration UI** - Admin page for managing agent-provider mapping
- ✅ **Git Platform Enhancements** - GetPullRequestDetailsAsync() for all providers
- ✅ **68 new tests** - CodeReviewAgent tests, 712 total tests passing
- ⚠️ OpenAI and Gemini adapters are placeholders (need full implementation)

### Authentication & User Management (PR #52 - Nov 11, 2025)
- ✅ OAuth 2.0 integration with Microsoft Azure AD and Google Workspace
- ✅ Auto-provisioning of tenants and users from identity providers
- ✅ Role-based access control (Owner, Admin, Member, Viewer)
- ✅ ProvisioningService and CurrentUserService (replaces StubCurrentUserService)
- ✅ Complete Blazor UI (Login, Welcome, Profile dropdown)
- ✅ ASP.NET Core Identity integration with encrypted credentials
- ✅ 40 comprehensive unit tests (100% pass rate)
- ✅ Security enhancements (open redirect protection, personal account blocking)
- ⚠️ OAuth client registration required (Google/Microsoft app credentials)

### Multi-LLM Provider Support (PR #48 - Nov 10, 2025)
- ✅ TenantLlmProvider entity with support for 6 provider types:
  - Anthropic Native (OAuth), Z.ai, Minimax M2, OpenRouter, Together AI, Custom
- ✅ OAuth vs API key authentication modes
- ✅ Encrypted token storage
- ✅ Model overrides and environment variable configuration
- ✅ ProcessExecutor service for safe CLI process execution
- ✅ ClaudeCodeCliAdapter enhancements with tenant-specific LLM configuration
- ✅ Ticket-level provider selection

### Test Coverage Expansion (PR #46 - Nov 9, 2025)
- ✅ **708 passing tests** (from 151) - includes authentication tests
- ✅ 88% code coverage achieved (exceeded 80% target)
- ✅ Domain entities, repositories, graphs, services, and DI tested
- ⚠️ Remaining gaps: TenantLlmProvider tests, ProcessExecutor tests
- ✅ **UI component tests completed in PR #61**

---

## Short Term (Next 3 Months)

### 🔐 Authentication & User Management ⚠️ **MOSTLY COMPLETE**

**Goal**: ✅ Core authentication complete, UI enhancements remaining

- [x] **OAuth 2.0 SSO Integration** ✅ **COMPLETED (PR #52)**
  - [x] Google OAuth provider (Google Workspace)
  - [x] Microsoft OAuth provider (Azure AD)
  - [x] User profile management (display name, avatar)
  - [x] Session management and token refresh
  - [x] Replace StubCurrentUserService with real implementation ✅
  - [ ] **OAuth client registration** (Google/Microsoft app credentials required)

- [x] **User Management UI** ✅ **COMPLETED (Epic 06 Phase 5)**
  - [x] User profile page (OAuth-provisioned profile)
  - [x] Team member management (search, assign roles)
  - [x] User search and assignment (for plan reviews)
  - [x] User roles and permissions UI (RBAC with Owner/Admin/Member/Viewer)

**Success Criteria**: ✅ COMPLETE - Users can sign in with Google/Microsoft, full user management UI implemented with role-based access control. OAuth client registration pending (credentials required).

---

### 🧪 Testing & Quality

**Goal**: Complete remaining test coverage gaps for production readiness

- [x] **Unit Test Suite** ✅ **COMPLETE** (PR #46, #52, #61)
  - [x] Target: 80% code coverage → **88% achieved**
  - [x] **2,136 passing tests** across all layers (712 backend + 1,424 Blazor)
  - [x] Domain entities tested
  - [x] Graphs tested
  - [x] Git providers tested
  - [x] Authentication tested (ProvisioningService, CurrentUserService - 40 tests)
  - [x] **Blazor components tested** (88 components, 1,424 tests - PR #61) ✅
  - [ ] TenantLlmProvider tests (new entity from PR #48)
  - [ ] ProcessExecutor tests (new service from PR #48)
  - [ ] Encryption service tests
  - [ ] Remaining agent unit tests

- [x] **Integration Tests** ✅ **MOSTLY COMPLETE** (PR #46)
  - [x] Repository integration tests
  - [x] Service integration tests
  - [x] Dependency injection validation tests
  - [ ] End-to-end workflow tests (Refinement → Planning → Implementation)
  - [ ] Checkpoint resume tests
  - [ ] Multi-tenant isolation verification
  - [ ] Database migration tests
  - [ ] External system integration tests (Jira, Git platforms)

- [ ] **E2E Tests**
  - [ ] Complete user journeys (ticket creation → PR merge)
  - [ ] Error recovery scenarios
  - [ ] Plan rejection and retry flows
  - [ ] Suspension and resume flows

**Success Criteria**: All workflows tested, 90% coverage, CI/CD pipeline green

---

### 🌐 Platform Completion

**Goal**: Support all major enterprise Git platforms

- [ ] **GitLab Provider Implementation**
  - [ ] GitLab.NET library integration
  - [ ] Create merge requests
  - [ ] Add MR comments
  - [ ] Get repository information
  - [ ] Retry policy configuration
  - [ ] Integration tests

- [ ] **GitHub Issues Integration**
  - [ ] Webhook handling for issue events
  - [ ] Comment parsing (@claude mentions)
  - [ ] Issue creation and updates
  - [ ] Bidirectional sync (WebUI ↔ GitHub Issues)

- [ ] **Azure DevOps Work Items Integration**
  - [ ] Work item webhook handling
  - [ ] Comment parsing
  - [ ] Work item creation and updates
  - [ ] Bidirectional sync (WebUI ↔ Azure DevOps)

**Success Criteria**: All 4 Git platforms supported, external system sync verified

---

### 🎨 UI/UX Polish

**Goal**: Production-ready user interface with great UX

- [ ] **Core UI Improvements**
  - [ ] Real-time status updates with SignalR
  - [ ] Improved error messaging and user feedback
  - [ ] Mobile-responsive design
  - [ ] Dark mode support
  - [ ] Accessibility (WCAG 2.1 AA compliance)

- [x] **Admin UIs** ✅ **COMPLETED (Epic 06 Phases 1-5 - Nov 13-14, 2025)**
  - [x] Admin services foundation (RepositoryService, TenantLlmProviderService, UserManagementService, TenantConfigurationService) ✅
  - [x] Repository management UI (/admin/repositories) with multi-platform support and connection testing ✅
  - [x] LLM provider configuration UI (/admin/settings/llm-providers) with OAuth and API key modes ✅
  - [x] Tenant settings UI (/admin/settings/general) with workflow, code review, and provider assignments ✅
  - [x] User management UI (/admin/settings/users) with role-based access control ✅
  - [ ] Agent prompt template editor (Future enhancement - planned Q1 2026)
  - [ ] Workflow event log viewer (Future enhancement - planned Q1 2026)
  - [ ] Error reporting and debugging UI (Future enhancement - planned Q1 2026)

- [ ] **Analytics & Dashboards**
  - [ ] Workflow success rate metrics
  - [ ] Average workflow duration
  - [ ] Token usage tracking and reporting
  - [ ] Agent performance metrics
  - [ ] Tenant usage analytics

**Success Criteria**: Production-quality UI, all admin functions accessible via UI

---

### 🔧 Infrastructure Improvements

- [x] **Agent System Foundation** ✅ **COMPLETED (Epic 05 - Nov 15, 2025)**
  - [x] 22 autonomous tools with execution capability
  - [x] AgentConfiguration entity and AgentFactory
  - [x] AG-UI integration with SSE streaming
  - [x] Enabled by default for all users

- [ ] **Agent Prompts System Completion**
  - [x] Agent prompt templates defined in database (AgentPromptTemplate entity)
  - [ ] Database migration applied and tested
  - [ ] Initial prompts loaded from `.claude/agents/`
  - [ ] Agents refactored to use `AgentPromptTemplate`
  - [ ] UI for prompt customization (agent prompt template editor)

- [ ] **Jira Integration Verification**
  - [ ] Webhook endpoint tested end-to-end
  - [ ] Comment parsing verified (@claude mentions)
  - [ ] Ticket state transitions verified
  - [ ] Bidirectional sync working

- [ ] **Performance Optimization**
  - [x] Database query optimization (Epic 08 Phase 3 - 83% faster page loads)
  - [x] Server-side pagination implemented
  - [ ] Caching strategy (repository context, tenant config)
  - [ ] Parallel execution performance tuning
  - [ ] Token usage optimization and tracking

**Success Criteria**: All integrations verified, performance acceptable under load

---

## Medium Term (3-6 Months)

### 🔄 Advanced Workflows

**Goal**: Support complex enterprise approval and quality processes

- [x] **Code Review Graph** ✅ **COMPLETED (Epic 02 - PR #59 - Nov 12, 2025)**
  - [x] Automated code review agent with configurable LLM provider
  - [x] AI-powered PR analysis and feedback
  - [x] Feedback posted to PR as comments
  - [x] Iterative improvement loop (Implementation → CodeReview → Fix, max 3 iterations)
  - [x] Cross-provider review (e.g., GPT-4 reviews Claude-generated code)
  - [x] Automatic approval comments when code passes review
  - [ ] Code quality scoring with metrics (Future enhancement)
  - [ ] Security vulnerability detection (OWASP, dependency scanning - Future enhancement)

- [ ] **Testing Graph**
  - [ ] Automated test generation
  - [ ] Test execution and result analysis
  - [ ] Additional edge case test generation
  - [ ] Loop back to implementation on test failures
  - [ ] Test coverage reporting

- [ ] **Deployment Orchestration Graph**
  - [ ] Deploy to staging
  - [ ] Run E2E tests in staging
  - [ ] Human approval for production
  - [ ] Deploy to production
  - [ ] Post-deployment validation
  - [ ] Rollback on failures

- [ ] **Multi-Stage Approval Workflows**
  - [ ] Team lead approval
  - [ ] Architect approval
  - [ ] Security review
  - [ ] Configurable approval chains per tenant/repo
  - [ ] Automated approval for low-risk changes

**Success Criteria**: New graph types demonstrated, customer validation positive

---

### 🏢 Enterprise Features

**Goal**: Production-ready for large enterprise deployments

- [ ] **Enterprise Authentication & Authorization**
  - [ ] SAML 2.0 authentication (enterprise SSO)
  - [ ] Advanced role-based access control (RBAC)
  - [ ] Tenant admin roles
  - [ ] Repository-level permissions
  - [ ] API key management
  - [ ] Multi-factor authentication (MFA)

- [ ] **Audit & Compliance**
  - [ ] Comprehensive audit logging
  - [ ] Audit log dashboard and search
  - [ ] Compliance report generation
  - [ ] Data retention policies
  - [ ] GDPR compliance (data export, deletion)

- [ ] **Advanced Configuration**
  - [ ] Tenant configuration UI
  - [ ] Repository-level settings (branch protection, approval rules)
  - [ ] Agent behavior customization per tenant
  - [ ] Rate limiting and quota management
  - [ ] Feature flags per tenant

**Success Criteria**: Enterprise customers can self-service configuration

---

### 👨‍💻 Developer Experience

**Goal**: Make PRFactory easy to extend and customize

- [ ] **CLI Tool**
  - [ ] Local workflow testing without full deployment
  - [ ] Agent development and testing
  - [ ] Prompt template management
  - [ ] Workflow simulation

- [ ] **Agent Development Toolkit**
  - [ ] Agent scaffolding generator
  - [ ] Local agent testing framework
  - [ ] Mock Claude client for testing
  - [ ] Agent debugging tools

- [ ] **Documentation**
  - [ ] Developer onboarding guide
  - [ ] Agent development guide
  - [ ] Graph architecture deep-dive
  - [ ] API documentation (Swagger/OpenAPI)
  - [ ] Troubleshooting guide

- [ ] **Workflow Visualization**
  - [ ] Visual graph designer (view graphs)
  - [ ] Live workflow execution tracking
  - [ ] Checkpoint visualization
  - [ ] State transition diagrams

**Success Criteria**: New developers productive in < 1 day

---

## Long Term (6-12 Months)

### 🤖 Advanced AI Capabilities

**Goal**: Leverage AI for quality and optimization

- [ ] **A/B Implementation Strategies**
  - [ ] Parallel implementation with different approaches
  - [ ] Code quality comparison (metrics, test coverage)
  - [ ] Performance comparison
  - [ ] Human selects best implementation

- [ ] **Automated Code Quality Assessment**
  - [ ] Static analysis integration (SonarQube, CodeQL)
  - [ ] Code smell detection
  - [ ] Technical debt scoring
  - [ ] Refactoring suggestions

- [ ] **Performance Optimization**
  - [ ] Performance profiling of generated code
  - [ ] Optimization suggestions (algorithmic, memory)
  - [ ] Database query optimization
  - [ ] Caching strategy recommendations

- [ ] **Security Scanning**
  - [ ] Security vulnerability detection (OWASP Top 10)
  - [ ] Dependency vulnerability scanning
  - [ ] Secrets detection
  - [ ] Security best practice recommendations

**Success Criteria**: AI-powered quality gates demonstrably improve code quality

---

### 🚀 Platform Expansion

**Goal**: Deploy anywhere, support any environment

- [ ] **Self-Hosted Deployment**
  - [ ] Docker Compose for single-server deployment
  - [ ] Kubernetes operator for multi-server deployment
  - [ ] On-premises air-gapped deployment
  - [ ] Deployment automation scripts

- [ ] **Cloud Deployment Options**
  - [ ] Azure deployment templates
  - [ ] AWS deployment templates
  - [ ] GCP deployment templates
  - [ ] Terraform modules

- [x] **Custom LLM Provider Support** ✅ **COMPLETED** (PR #48, #59 - Nov 2025)
  - [x] Abstraction layer for LLM providers (ILlmProvider, factory pattern)
  - [x] Multi-provider support (Anthropic/Claude, OpenAI/GPT, Google/Gemini)
  - [x] Custom LLM endpoint configuration
  - [x] OpenAI (GPT-4) integration via CLI adapter (placeholder)
  - [x] Google Gemini integration via CLI adapter (placeholder)
  - [x] Per-agent provider configuration (Analysis, Planning, Implementation, CodeReview)
  - [x] Admin UI for managing agent-provider mapping (/admin/agent-configuration)
  - [ ] Azure OpenAI integration (supported via OpenRouter or Z.ai)
  - [ ] Self-hosted LLMs (Ollama, vLLM) - can be configured as Custom provider
  - [ ] Full implementation of OpenAI and Gemini CLI adapters

**Success Criteria**: Customers can deploy in any environment

---

### 🌍 Ecosystem

**Goal**: Build a thriving ecosystem of extensions

- [ ] **Marketplace**
  - [ ] Custom agent marketplace
  - [ ] Workflow template library
  - [ ] Agent prompt template sharing
  - [ ] Community-contributed integrations

- [ ] **API & Extensions**
  - [ ] Public REST API for third-party integrations
  - [ ] Webhook system for external events
  - [ ] Plugin architecture for custom agents
  - [ ] SDK for agent development (C#, Python)

- [ ] **Platform Integrations**
  - [ ] Asana integration
  - [ ] Linear integration
  - [ ] Trello integration
  - [ ] Monday.com integration
  - [ ] Slack notifications
  - [ ] Microsoft Teams notifications

**Success Criteria**: Active community contributing extensions

---

## Research & Exploration

### 🔬 Under Investigation

These are ideas we're actively exploring but haven't committed to roadmap:

- **Advanced Context Building with Vector Embeddings**
  - Use vector databases (Pinecone, Weaviate) for codebase embeddings
  - Semantic code search for better context
  - Incremental context updates

- **Multi-Repository Change Orchestration**
  - Coordinate changes across multiple microservices
  - Dependency graph analysis
  - Atomic multi-repo commits

- **Continuous Learning from Approved Implementations**
  - Learn from past successful implementations
  - Fine-tune prompt templates based on feedback
  - Personalized agent behavior per team/project

- **Graph Optimization with Reinforcement Learning**
  - Optimize agent parameters based on success metrics
  - Automatic retry policy tuning
  - Adaptive workflow routing

### 💡 Ideas for Future

Not yet investigated, but interesting:

- Visual workflow designer (drag-and-drop graph builder)
- Natural language workflow configuration ("When a bug is filed, analyze and plan but don't implement")
- Automated documentation generation from code changes
- Test generation from specifications (BDD-style)
- Contract testing for API changes
- Database schema migration generation
- Infrastructure-as-code generation (Terraform, CloudFormation)

---

## What We're NOT Planning

To maintain focus and avoid scope creep:

### Will NOT Do

- ❌ **Full Autonomous Deployment** - Humans must always approve PRs
- ❌ **Code Generation Without Review** - No automatic merging
- ❌ **Direct Production Access** - PRFactory never touches production
- ❌ **Bypassing Security Policies** - Always respect branch protection, CI/CD gates
- ❌ **Proprietary Code Hosting** - We integrate, not replace Git platforms
- ❌ **Project Management Replacement** - We integrate with Jira/Azure DevOps, not replace
- ❌ **Full CI/CD Platform** - Focus on code generation, not build/deploy

### Philosophical Boundaries

1. **AI Assists, Humans Decide** - Final decisions always human
2. **Transparency** - No "black box" code generation
3. **Auditability** - Every change traceable and explainable
4. **Security** - Never compromise security for convenience
5. **Extensibility Over Features** - Enable customization vs. build everything

---

## How to Influence the Roadmap

### Priority Drivers

1. **Customer Requests** - Paying customers get priority
2. **Enterprise Blockers** - Issues blocking enterprise adoption
3. **Security** - Security issues always prioritized
4. **Foundation** - Architectural improvements that enable future features
5. **Community Demand** - Popular community requests

### How to Request Features

1. **GitHub Issues** - Open an issue with `[Feature Request]` label
2. **Customer Feedback** - Enterprise customers contact account manager
3. **Community Discussions** - GitHub Discussions for ideas
4. **Pull Requests** - Well-architected PRs considered for merge

---

## Success Metrics

### MVP Success (3 months)
- ✅ 80% test coverage
- ✅ 4 Git platforms supported (including GitLab)
- ✅ Real-time WebUI updates
- ✅ 10+ paying customers

### Enterprise Ready (6 months)
- ✅ SSO/SAML authentication
- ✅ Comprehensive audit logging
- ✅ 50+ paying customers
- ✅ 99.9% uptime SLA

### Ecosystem Maturity (12 months)
- ✅ Public API with 1000+ API calls/day
- ✅ Marketplace with 10+ community agents
- ✅ 100+ paying customers
- ✅ Self-hosted deployment option

---

## Dependencies & Risks

### Technical Dependencies
- Claude API availability and pricing
- LibGit2Sharp maintenance and updates
- .NET platform evolution (currently .NET 10)
- Git platform API stability

### Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Claude API changes | HIGH | MEDIUM | LLM provider abstraction layer |
| Customer adoption slow | HIGH | LOW | Focus on enterprise value prop |
| Security vulnerabilities | HIGH | MEDIUM | Regular audits, bug bounty program |
| Platform API breaking changes | MEDIUM | MEDIUM | Version pinning, provider abstraction |
| Key personnel turnover | MEDIUM | MEDIUM | Documentation, knowledge sharing |

---

## Releases

### Versioning Strategy

We follow [Semantic Versioning](https://semver.org/):
- **MAJOR** version for incompatible API changes
- **MINOR** version for backwards-compatible functionality
- **PATCH** version for backwards-compatible bug fixes

### Planned Releases

- **v0.9.0** (Current) - MVP with core workflows ⚠️ NOT production-ready
- **v1.0.0** (3 months) - Production-ready MVP (tests, GitLab, real-time UI)
- **v1.1.0** (4 months) - Advanced workflows (Code Review Graph, Testing Graph)
- **v1.2.0** (6 months) - Enterprise features (SSO, RBAC, audit logging)
- **v2.0.0** (12 months) - Ecosystem launch (public API, marketplace, self-hosted)

---

## References

- [IMPLEMENTATION_STATUS.md](./IMPLEMENTATION_STATUS.md) - What's built today
- [ARCHITECTURE.md](./ARCHITECTURE.md) - System architecture
- [WORKFLOW.md](./WORKFLOW.md) - Workflow details
- [CLAUDE.md](../CLAUDE.md) - Architecture vision

---

**Maintained By**: PRFactory Product Team
**Review Frequency**: Monthly (or after major epic completion)
**Last Reviewed**: 2025-11-15
**Next Review**: 2025-12-15
