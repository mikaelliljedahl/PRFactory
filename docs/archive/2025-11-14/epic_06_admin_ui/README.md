# Epic 06: Admin UI - Planning Documentation

> **Status**: Phase 1 Complete ✅ | Phases 2-5 Ready for Implementation 📋

This folder contains comprehensive planning documents for **Epic 06: Admin UI**, which provides a web-based administrative interface for PRFactory multi-tenant SaaS management.

---

## Table of Contents

- [Overview](#overview)
- [Phase Status Summary](#phase-status-summary)
- [Planning Documents](#planning-documents)
- [Implementation Order](#implementation-order)
- [Prerequisites](#prerequisites)
- [Total Effort Estimate](#total-effort-estimate)
- [Quick Navigation Guide](#quick-navigation-guide)
- [Related Documentation](#related-documentation)

---

## Overview

**Epic 06: Admin UI** delivers a complete administrative interface for PRFactory, enabling tenants to:

- **Manage Git repositories** (GitHub, Bitbucket, Azure DevOps) with secure credential storage
- **Configure LLM providers** (Anthropic, Z.ai, Minimax M2, OpenRouter, Together AI, Custom) with OAuth or API keys
- **Customize tenant settings** (workflow behavior, code review rules, LLM provider assignment)
- **Manage users and roles** (Owner, Admin, Member, Viewer) with RBAC enforcement

**Architecture**: Blazor Server with code-behind pattern, Clean Architecture service layer, multi-tenant isolation, AES-256-GCM encrypted credentials.

**Platform**: .NET 10, C# 13, Entity Framework Core 10, ASP.NET Core Identity, Radzen Blazor Components.

---

## Phase Status Summary

| Phase | Title | Status | Service Layer | UI Layer | Estimated Effort |
|-------|-------|--------|---------------|----------|------------------|
| **Phase 1** | Service Layer Foundation | ✅ Complete | ✅ Done | N/A | N/A |
| **Phase 2** | Repository Management | 📋 Ready | ✅ Done | 🔲 Pending | 10 days |
| **Phase 3** | LLM Provider Configuration | 📋 Ready | ✅ Done | 🔲 Pending | 12 days |
| **Phase 4** | Tenant Settings | 📋 Ready | ✅ Done | 🔲 Pending | 5 days |
| **Phase 5** | User Management | 📋 Ready | ✅ Done | 🔲 Pending | 8 days |

**Total Remaining Effort**: 35 days (5-8 weeks with parallel development)

---

## Planning Documents

### Phase 1: Service Layer Foundation (Complete ✅)

**Status**: Implemented and merged
**File**: See `/home/user/PRFactory/docs/planning/EPIC_06_ADMIN_UI.md` (master plan)

**Delivered**:
- ✅ `IRepositoryService` and implementation (CRUD, credential encryption)
- ✅ `ILlmProviderService` and implementation (multi-provider support, OAuth integration)
- ✅ `ITenantSettingsService` and implementation (settings management)
- ✅ `IUserManagementService` and implementation (role assignment, auto-provisioning)
- ✅ DTOs for all entities
- ✅ 112 unit tests (100% pass rate)

**Result**: Complete service layer ready for UI integration.

---

### Phase 2: Repository Management

**File**: [`phase_02_repository_management.md`](phase_02_repository_management.md) (2,385 lines)

**Scope**: UI for managing Git repositories (GitHub, Bitbucket, Azure DevOps)

**Pages**:
- Repository List (`/admin/repositories`) - Search, filter, statistics
- Create Repository (`/admin/repositories/create`) - Multi-step form with connection testing
- Edit Repository (`/admin/repositories/{id}/edit`) - Update credentials, test connection
- Repository Detail (`/admin/repositories/{id}`) - View config, connection status, statistics

**Key Components**:
- `RepositoryForm` - Unified create/edit form with provider-specific fields
- `RepositoryListItem` - Card-based display with connection status badges
- `RepositoryConnectionTest` - Real-time connection testing UI
- `RepositoryStatistics` - Active tickets, PRs, health metrics

**Key Features**:
- AES-256-GCM credential encryption (OAuth tokens, Personal Access Tokens)
- Real-time connection testing before save
- Provider-specific validation (GitHub: Personal Access Token, Bitbucket: App Password, Azure DevOps: PAT)
- RBAC enforcement (Owner/Admin can create/edit, Members can view)

**Testing**: 127 unit tests, 18 integration tests, 8 E2E tests (bUnit framework)

**Estimated Effort**: 10 days

---

### Phase 3: LLM Provider Configuration

**File**: [`phase_03_llm_provider_configuration.md`](phase_03_llm_provider_configuration.md) (1,479 lines)

**Scope**: UI for configuring LLM providers with OAuth and API key support

**Pages**:
- LLM Provider List (`/admin/llm-providers`) - View active providers, models
- Create Provider (`/admin/llm-providers/create`) - Multi-step wizard (select type → configure)
- Edit Provider (`/admin/llm-providers/{id}/edit`) - Update config, model overrides
- Provider Detail (`/admin/llm-providers/{id}`) - View config, connection status

**Supported Provider Types**:
1. **Anthropic Native OAuth** - OAuth 2.0 with Authorization Code flow
2. **Z.ai** - API key with custom model overrides
3. **Minimax M2** - API key with model selection
4. **OpenRouter** - API key with multi-model support
5. **Together AI** - API key with model configuration
6. **Custom** - Fully configurable endpoint and models

**Key Components**:
- `LlmProviderWizard` - Multi-step provider creation flow
- `LlmProviderTypeSelector` - Visual selection grid with provider cards
- `OAuthProviderForm` - OAuth configuration (Client ID, Secret, Redirect URI)
- `ApiKeyProviderForm` - API key configuration with endpoint URLs
- `ModelOverridesEditor` - JSON editor for custom model configurations
- `LlmProviderConnectionTest` - Test provider connection before save

**Key Features**:
- OAuth 2.0 integration for Anthropic Native (Authorization Code flow)
- AES-256-GCM encryption for API keys and OAuth credentials
- JSON-based model override configuration
- Connection testing before save
- Tenant-level LLM provider assignment (Phase 4 integration)

**Testing**: 134 unit tests, 22 integration tests, 10 E2E tests

**Estimated Effort**: 12 days

---

### Phase 4: Tenant Settings

**File**: [`phase_04_tenant_settings.md`](phase_04_tenant_settings.md) (1,076 lines)

**Scope**: Single-page tabbed interface for tenant configuration

**Page**:
- Tenant Settings (`/admin/settings`) - 4 tabs: General, Workflow, Code Review, LLM Providers

**Tabs**:

1. **General** (Read-only)
   - Tenant name, domain, created date
   - User statistics (total users, roles breakdown)
   - Repository/provider counts

2. **Workflow Settings** (Editable)
   - Auto-approve simple updates (bypass human approval)
   - Require planning approval (force review before implementation)
   - Max refinement questions (1-10 limit)
   - Implementation timeout (15-180 minutes)

3. **Code Review Settings** (Editable)
   - Enable automated code review (before PR creation)
   - Require security scan (vulnerability checks)
   - Max PR size (lines of code limit)
   - Auto-merge strategy (Never, On Approval, CI Pass)

4. **LLM Provider Assignment** (Editable)
   - Default provider (dropdown from tenant's providers)
   - Workflow-specific overrides (Refinement, Planning, Implementation, CodeReview)
   - Model override configuration

**Key Components**:
- `TenantSettingsPage` - Single-page tab navigator
- `GeneralSettingsTab` - Read-only overview
- `WorkflowSettingsTab` - Workflow behavior configuration
- `CodeReviewSettingsTab` - Code review rules
- `LlmProviderAssignmentTab` - LLM provider assignment

**Key Features**:
- Tabbed navigation with state persistence
- Inline editing with save confirmation
- Validation rules (e.g., timeout between 15-180 minutes)
- RBAC enforcement (only Owner can update settings)
- Real-time preview of changes before save

**Testing**: 98 unit tests, 15 integration tests, 6 E2E tests

**Estimated Effort**: 5 days

---

### Phase 5: User Management

**File**: [`phase_05_user_management.md`](phase_05_user_management.md) (1,634 lines)

**Scope**: User management with role assignment and RBAC enforcement

**Pages**:
- User List (`/admin/users`) - Search, filter by role, statistics
- Edit User (`/admin/users/{id}/edit`) - Role assignment with validation
- User Detail (`/admin/users/{id}`) - View profile, statistics, activity

**User Roles**:

| Role | Permissions | Can Manage |
|------|-------------|------------|
| **Owner** | Full admin access | Repositories, LLM Providers, Settings, Users (all roles) |
| **Admin** | Repository & provider management | Repositories, LLM Providers (cannot change settings or roles) |
| **Member** | Read-only access to admin UI | None (can view only) |
| **Viewer** | Read-only access (no admin UI) | None |

**Key Components**:
- `UserListItem` - Card-based user display with role badges
- `UserRoleEditor` - Role assignment dropdown with validation
- `UserStatistics` - Activity metrics (plans reviewed, comments, tickets)
- `UserActivityTimeline` - Recent user actions

**Key Features**:
- **Auto-provisioning from OAuth** (no manual user creation)
  - First user becomes Owner
  - Subsequent users become Members
- **Role hierarchy enforcement**
  - Owner can assign any role
  - Admin cannot change roles (view only)
  - Cannot remove the last Owner from a tenant
- **User statistics tracking**
  - Plans reviewed (approved/rejected counts)
  - Comments posted
  - Tickets assigned
- **RBAC enforcement** (only Owner can change roles)

**Business Rules**:
- ❌ Cannot remove the last Owner role from a tenant
- ❌ Cannot demote yourself if you are the last Owner
- ✅ Owner can assign multiple Owners for redundancy
- ✅ Admin can view users but not modify roles

**Testing**: 119 unit tests, 20 integration tests, 9 E2E tests

**Estimated Effort**: 8 days

---

## Implementation Order

**Recommended sequence** for implementing Phases 2-5:

```
Phase 2: Repository Management (10 days)
   ↓
Phase 3: LLM Provider Configuration (12 days)
   ↓
Phase 4: Tenant Settings (5 days) - Depends on Phase 3 for provider assignment
   ↓
Phase 5: User Management (8 days) - Can be done in parallel with earlier phases
```

**Parallel Development Opportunities**:
- Phase 2 and Phase 5 can be developed in parallel (no dependencies)
- Phase 3 must complete before Phase 4 (Tenant Settings assigns LLM providers)

**Critical Path**: Phase 2 → Phase 3 → Phase 4 (27 days)
**Optional Parallel**: Phase 5 (8 days, can overlap with Phase 2-3)

**Minimum Timeline**: 27 days (sequential)
**Optimal Timeline**: 5-6 weeks (with parallel development of Phase 5)

---

## Prerequisites

Before implementing any phase, ensure:

### Service Layer (Phase 1 - Complete ✅)

- ✅ `IRepositoryService` implemented (`PRFactory.Infrastructure/Application/RepositoryService.cs`)
- ✅ `ILlmProviderService` implemented (`PRFactory.Infrastructure/Application/LlmProviderService.cs`)
- ✅ `ITenantSettingsService` implemented (`PRFactory.Infrastructure/Application/TenantSettingsService.cs`)
- ✅ `IUserManagementService` implemented (`PRFactory.Infrastructure/Application/UserManagementService.cs`)
- ✅ All DTOs defined (`PRFactory.Core/Application/DTOs/`)
- ✅ Service layer tests passing (112 tests, 100% pass rate)

### Infrastructure

- ✅ **Entity Framework Core 10** - Database context and migrations
- ✅ **ASP.NET Core Identity** - Authentication with OAuth 2.0 (Microsoft Azure AD, Google Workspace)
- ✅ **Encryption Service** - AES-256-GCM for credentials (`IEncryptionService`)
- ✅ **Multi-Tenant Context** - Tenant isolation (`ICurrentTenantService`)
- ✅ **RBAC Policies** - Authorization policies for Owner/Admin/Member roles

### UI Framework

- ✅ **Blazor Server** (.NET 10) - Server-side rendering with SignalR
- ✅ **Radzen Blazor Components** - UI component library
- ✅ **Bootstrap 5** (CSS only, no JavaScript)
- ✅ **bUnit** - Blazor component testing framework

### Development Environment (Claude Code Web Only)

If working in Claude Code on the web, ensure:
- ✅ `.NET SDK 10` installed (via SessionStart hook)
- ✅ NuGet proxy configured (`source /tmp/dotnet-proxy-setup.sh`)
- ✅ Run `dotnet restore` and `dotnet build` successfully

---

## Total Effort Estimate

**By Phase**:

| Phase | Estimated Effort | Cumulative |
|-------|------------------|------------|
| Phase 1: Service Layer | ✅ Complete | ✅ Complete |
| Phase 2: Repository Management | 10 days | 10 days |
| Phase 3: LLM Provider Configuration | 12 days | 22 days |
| Phase 4: Tenant Settings | 5 days | 27 days |
| Phase 5: User Management | 8 days | 35 days |

**Total Remaining**: 35 days (5-8 weeks)

**Optimal Timeline** (with parallel development):
- Weeks 1-2: Phase 2 + Phase 5 (parallel)
- Weeks 3-4: Phase 3
- Week 5: Phase 4
- **Total: 5-6 weeks**

**Sequential Timeline** (no parallelization):
- **Total: 7 weeks**

---

## Quick Navigation Guide

### By Feature

| Feature | Phase | Document Link |
|---------|-------|---------------|
| Git repository management (GitHub, Bitbucket, Azure DevOps) | Phase 2 | [phase_02_repository_management.md](phase_02_repository_management.md) |
| LLM provider configuration (Anthropic, Z.ai, etc.) | Phase 3 | [phase_03_llm_provider_configuration.md](phase_03_llm_provider_configuration.md) |
| Tenant workflow and code review settings | Phase 4 | [phase_04_tenant_settings.md](phase_04_tenant_settings.md) |
| User role management (Owner, Admin, Member, Viewer) | Phase 5 | [phase_05_user_management.md](phase_05_user_management.md) |

### By Concern

| Concern | Where to Find It |
|---------|------------------|
| **Authentication & Authorization** | All phases - RBAC section |
| **Credential Encryption** | Phase 2 (Section 6.1), Phase 3 (Section 6.1) |
| **OAuth Integration** | Phase 3 (Section 2.2.1) |
| **Multi-Tenant Isolation** | All phases - Security section |
| **Component Architecture** | All phases - Section 3 (Component Specifications) |
| **Service Integration** | All phases - Section 4 (Service Layer Integration) |
| **Testing Strategy** | All phases - Section 6 (Testing Strategy) |
| **UI Mockups** | All phases - Section 9 (UI Mockups) |
| **Implementation Checklists** | All phases - Section 8 (Implementation Checklist) |

### By Role

| Role | Recommended Reading |
|------|---------------------|
| **Product Manager** | Section 1 (Overview) and Section 7 (Acceptance Criteria) in each phase |
| **Backend Developer** | Section 4 (Service Layer Integration) - Already implemented in Phase 1 |
| **Frontend Developer** | Section 2 (Page Specifications) and Section 3 (Component Specifications) |
| **QA Engineer** | Section 6 (Testing Strategy) and Section 8 (Implementation Checklist) |
| **UX Designer** | Section 9 (UI Mockups) and Section 2 (Page Specifications) |
| **Security Engineer** | Section 5 (Security & RBAC) in each phase |

---

## Related Documentation

### Core Documentation

- **Master Plan**: `/home/user/PRFactory/docs/planning/EPIC_06_ADMIN_UI.md` (1,150 lines)
  - Original Epic 06 plan with all 5 phases
  - High-level overview and goals
  - Phase 1 implementation details (service layer)

- **Implementation Status**: `/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md`
  - Current status: Phase 1 complete, Phases 2-5 ready for implementation
  - Test coverage: 2,136 tests (100% pass rate), 112 tests for Epic 06 service layer

- **Roadmap**: `/home/user/PRFactory/docs/ROADMAP.md`
  - Epic 06 listed as short-term priority (next 3 months)
  - Admin UI is a prerequisite for production deployment

### Architecture Documentation

- **Architecture**: `/home/user/PRFactory/docs/ARCHITECTURE.md`
  - Clean Architecture overview (Domain, Application, Infrastructure, UI layers)
  - Multi-tenant architecture and security model
  - Blazor Server architecture and service integration patterns

- **CLAUDE.md**: `/home/user/PRFactory/CLAUDE.md`
  - Section 4: Blazor UI Component Architecture (mandatory reading for frontend developers)
  - Code-behind pattern enforcement
  - NO JavaScript policy for Blazor Server
  - Approved UI libraries (Blazor + Radzen only)

### Setup and Development

- **Setup**: `/home/user/PRFactory/docs/SETUP.md`
  - Environment setup instructions
  - Database configuration
  - OAuth provider registration (Microsoft Azure AD, Google Workspace)

- **SessionStart Hook**: `/home/user/PRFactory/.claude/scripts/session-start.sh`
  - Automatic .NET 10 installation for Claude Code web sessions
  - NuGet proxy setup for HTTP authentication

---

## Implementation Checklist (High-Level)

Use this checklist to track overall progress across all phases:

### Phase 2: Repository Management

- [ ] Create `/admin/repositories` folder structure
- [ ] Implement Repository List page with search/filter
- [ ] Implement Create Repository page with multi-step form
- [ ] Implement Edit Repository page with credential updates
- [ ] Implement Repository Detail page with statistics
- [ ] Create `RepositoryForm`, `RepositoryListItem`, `RepositoryConnectionTest`, `RepositoryStatistics` components
- [ ] Write 127 unit tests (bUnit)
- [ ] Write 18 integration tests
- [ ] Write 8 E2E tests
- [ ] Update navigation menu with "Repositories" link
- [ ] **Estimated: 10 days**

### Phase 3: LLM Provider Configuration

- [ ] Create `/admin/llm-providers` folder structure
- [ ] Implement LLM Provider List page
- [ ] Implement Create Provider wizard (multi-step)
- [ ] Implement provider type selector (6 types)
- [ ] Implement OAuth provider form (Anthropic Native)
- [ ] Implement API key provider forms (Z.ai, Minimax M2, OpenRouter, Together AI, Custom)
- [ ] Implement Edit Provider page with model overrides
- [ ] Implement Provider Detail page
- [ ] Create `LlmProviderWizard`, `OAuthProviderForm`, `ApiKeyProviderForm`, `ModelOverridesEditor`, `LlmProviderConnectionTest` components
- [ ] Write 134 unit tests (bUnit)
- [ ] Write 22 integration tests
- [ ] Write 10 E2E tests
- [ ] Update navigation menu with "LLM Providers" link
- [ ] **Estimated: 12 days**

### Phase 4: Tenant Settings

- [ ] Create `/admin/settings` folder structure
- [ ] Implement Tenant Settings page with tab navigation
- [ ] Implement General Settings tab (read-only)
- [ ] Implement Workflow Settings tab (editable)
- [ ] Implement Code Review Settings tab (editable)
- [ ] Implement LLM Provider Assignment tab (editable)
- [ ] Create `GeneralSettingsTab`, `WorkflowSettingsTab`, `CodeReviewSettingsTab`, `LlmProviderAssignmentTab` components
- [ ] Write 98 unit tests (bUnit)
- [ ] Write 15 integration tests
- [ ] Write 6 E2E tests
- [ ] Update navigation menu with "Settings" link
- [ ] **Estimated: 5 days**

### Phase 5: User Management

- [ ] Create `/admin/users` folder structure
- [ ] Implement User List page with search/filter by role
- [ ] Implement Edit User page with role assignment
- [ ] Implement User Detail page with statistics
- [ ] Create `UserListItem`, `UserRoleEditor`, `UserStatistics`, `UserActivityTimeline` components
- [ ] Implement role hierarchy validation ("cannot remove last Owner")
- [ ] Write 119 unit tests (bUnit)
- [ ] Write 20 integration tests
- [ ] Write 9 E2E tests
- [ ] Update navigation menu with "Users" link
- [ ] **Estimated: 8 days**

### Final Integration

- [ ] Update main navigation menu with all Admin UI links
- [ ] Add RBAC enforcement to navigation (Owner/Admin see admin links, Members/Viewers don't)
- [ ] Test cross-phase integration (e.g., Tenant Settings → LLM Provider Assignment)
- [ ] Update documentation (IMPLEMENTATION_STATUS.md, ROADMAP.md)
- [ ] Deploy to staging environment for QA
- [ ] Obtain product owner approval for production deployment

---

## Questions or Issues?

If you encounter issues or have questions during implementation:

1. **Check the master plan**: `/home/user/PRFactory/docs/planning/EPIC_06_ADMIN_UI.md`
2. **Review architecture docs**: `/home/user/PRFactory/docs/ARCHITECTURE.md`
3. **Read CLAUDE.md**: `/home/user/PRFactory/CLAUDE.md` (Section 4: Blazor UI Component Architecture)
4. **Check implementation status**: `/home/user/PRFactory/docs/IMPLEMENTATION_STATUS.md`
5. **Consult the roadmap**: `/home/user/PRFactory/docs/ROADMAP.md`

For technical questions about:
- **Service layer**: All services are implemented - see `PRFactory.Infrastructure/Application/`
- **Blazor patterns**: See CLAUDE.md Section 4 (code-behind, component architecture, NO JavaScript)
- **Testing**: See bUnit examples in existing tests (`PRFactory.Tests/`)

---

**Ready to implement?** Start with [Phase 2: Repository Management](phase_02_repository_management.md) - the service layer is complete and ready for UI integration!
