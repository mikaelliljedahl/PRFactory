# Critical Production Blockers

> **Status**: 3 critical issues identified, 0 resolved
> **Last Updated**: 2025-11-09
> **Purpose**: Track production-blocking issues requiring resolution before deployment

---

## Overview

This document tracks **critical architectural and implementation issues** that prevent production deployment. These are not feature requests or enhancements - they are fundamental problems that must be resolved for the system to function in production.

**Severity Levels**:
- 🔴 **CRITICAL** - Production blocker, must be resolved before deployment
- 🟠 **HIGH** - Serious issue, should be resolved soon
- 🟡 **MEDIUM** - Important but not blocking

---

## 🔴 Issue 1: Claude Code CLI Authentication Model Incompatible with Server-Side Execution

**Identified**: 2025-11-09
**Source**: `/docs/reviews/ARCHITECTURE_REVIEW.md` (lines 45-89)
**Severity**: 🔴 CRITICAL - Production Blocker
**Status**: ❌ UNRESOLVED
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

The current architecture assumes Claude Code CLI can run in server-side worker processes, but the CLI requires interactive OAuth authentication that is incompatible with automated, unattended server execution.

**Technical Details**:
- Claude Code CLI uses OAuth device flow requiring human interaction
- Worker processes (PRFactory.Worker) cannot authenticate without user intervention
- Agents calling `ICliAgent.ExecuteAsync()` will fail in production
- No service account or API key authentication option exists for Claude Code CLI

**Impact**:
- 🔴 **RefinementGraph** - Cannot run AnalysisAgent (uses CLI)
- 🔴 **PlanningGraph** - Cannot run PlanningAgent (uses CLI)
- 🔴 **ImplementationGraph** - Cannot run ImplementationAgent (uses CLI)
- 🔴 **ALL AI-powered workflows blocked**

### Current Workarounds

None - this is a fundamental architectural incompatibility.

### Proposed Solutions

**Option 1: Switch to Claude API (RECOMMENDED)**
- Replace `ClaudeCodeCliAdapter` with direct Anthropic API client
- Requires API keys instead of OAuth
- Supports unattended server execution
- **Effort**: 2-3 weeks
- **Pros**: Native API support, better performance, service account compatible
- **Cons**: Different pricing model, API key management

**Option 2: Implement Proxy Authentication Service**
- Create authenticated proxy service that workers can call
- Proxy handles Claude Code CLI authentication on behalf of workers
- **Effort**: 3-4 weeks
- **Pros**: Keeps Claude Code CLI integration
- **Cons**: Complex architecture, adds single point of failure

**Option 3: Use Alternative LLM Provider**
- Switch to OpenAI, Azure OpenAI, or other provider with API key auth
- Complete `CodexCliAdapter` implementation
- **Effort**: 2-3 weeks
- **Pros**: Multiple provider options, API key compatible
- **Cons**: Loses Claude-specific features, requires new integration

### Recommended Action

**Adopt Option 1** - Switch to Claude API:
1. Install `Anthropic.SDK` NuGet package
2. Create `ClaudeApiAdapter : ICliAgent`
3. Update agent configurations to use API adapter
4. Test all workflows with API implementation
5. Update documentation

### Resolution Required Before

Production deployment cannot proceed without resolving this issue.

---

## 🔴 Issue 2: No Authentication Layer (API Completely Open)

**Identified**: 2025-11-09
**Source**: `/docs/reviews/ARCHITECTURE_REVIEW.md` (lines 112-135)
**Severity**: 🔴 CRITICAL - Security Vulnerability
**Status**: ❌ UNRESOLVED
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

The PRFactory API and Web application have no authentication layer. All endpoints are publicly accessible, and `StubCurrentUserService` returns a hardcoded demo user.

**Technical Details**:
- `StubCurrentUserService` always returns demo user (not environment-dependent stub)
- No OAuth integration
- No JWT validation
- No session management
- No authorization checks
- API endpoints unprotected

**Impact**:
- 🔴 **Security**: Anyone can access all data
- 🔴 **Compliance**: Cannot meet enterprise security requirements
- 🔴 **Multi-tenancy**: Cannot isolate tenant data without authentication
- 🔴 **Audit**: Cannot track who performed actions

### Current Workarounds

None in production - only acceptable for local development.

### Proposed Solutions

**Option 1: OAuth 2.0 / OpenID Connect (RECOMMENDED)**
- Integrate with identity provider (Auth0, Azure AD, Okta)
- Implement JWT token validation
- Replace `StubCurrentUserService` with real implementation
- **Effort**: 3-4 weeks
- **Pros**: Industry standard, enterprise-ready, supports SSO
- **Cons**: Requires external identity provider

**Option 2: Built-in ASP.NET Core Identity**
- Use ASP.NET Core Identity with local user database
- Implement cookie-based authentication
- **Effort**: 2-3 weeks
- **Pros**: No external dependencies, simpler setup
- **Cons**: No SSO, requires custom user management UI

### Recommended Action

**Adopt Option 1** - OAuth 2.0 with Auth0:
1. Create Auth0 tenant or equivalent
2. Install `Microsoft.AspNetCore.Authentication.OpenIdConnect` package
3. Implement `ICurrentUserService` reading from JWT claims
4. Add `[Authorize]` attributes to all API controllers
5. Add Blazor authentication components
6. Test multi-tenant user isolation

### Resolution Required Before

Production deployment - cannot launch without authentication.

---

## 🔴 Issue 3: Multi-Tenant Isolation Not Enforced in Application Layer

**Identified**: 2025-11-09
**Source**: `/docs/reviews/ARCHITECTURE_REVIEW.md` (lines 137-168)
**Severity**: 🔴 CRITICAL - Data Leak Risk
**Status**: ❌ UNRESOLVED
**GitHub Issue**: [TBD - Create issue]
**Owner**: [TBD - Assign owner]

### Problem Description

While EF Core has global query filters for `TenantId`, the application layer does not enforce tenant isolation. Services can access data across tenants if `TenantId` is manually overridden or bypassed.

**Technical Details**:
- EF Core global filters apply `WHERE TenantId = @currentTenantId` automatically
- However, services can pass any `TenantId` to repositories
- No middleware enforces current tenant context
- `ICurrentUserService` stub does not validate tenant membership
- Risk of cross-tenant data access bugs

**Impact**:
- 🔴 **Data Leak**: Bug in service layer could expose tenant A's data to tenant B
- 🔴 **Compliance**: Fails SOC2/ISO27001 tenant isolation requirements
- 🔴 **Trust**: Cannot guarantee customer data isolation

### Current Workarounds

None - relying solely on developer discipline is insufficient.

### Proposed Solutions

**Option 1: Tenant Context Middleware + Service Validation (RECOMMENDED)**
- Add `ITenantContext` scoped service resolved from JWT claims
- Create middleware that sets `ITenantContext.CurrentTenantId` from user claims
- Update all service methods to validate `TenantId` matches `ITenantContext.CurrentTenantId`
- Throw `UnauthorizedAccessException` on mismatch
- **Effort**: 2-3 weeks
- **Pros**: Defense in depth, explicit validation, audit trail
- **Cons**: Requires updating all services

**Option 2: Repository-Level Enforcement**
- Override `SaveChanges` to validate all entities have correct `TenantId`
- Prevent queries without tenant filter
- **Effort**: 1-2 weeks
- **Pros**: Centralized enforcement
- **Cons**: Database-level only, doesn't catch logic bugs

### Recommended Action

**Adopt Option 1** - Tenant Context Middleware:
1. Create `ITenantContext` interface and implementation
2. Add middleware to extract tenant from user claims
3. Update service constructors to inject `ITenantContext`
4. Add validation to all service methods:
   ```csharp
   public async Task<Ticket> GetTicketAsync(Guid ticketId, Guid tenantId)
   {
       if (tenantId != _tenantContext.CurrentTenantId)
           throw new UnauthorizedAccessException("Cross-tenant access denied");
       // ...
   }
   ```
5. Add integration tests for cross-tenant access attempts

### Resolution Required Before

Production deployment - critical for data security compliance.

---

## Resolution Tracking

| Issue | Priority | Effort | Target Resolution | Status |
|-------|----------|--------|-------------------|--------|
| #1: CLI Authentication | P0 | 2-3 weeks | [TBD] | ❌ Not Started |
| #2: No Authentication Layer | P0 | 3-4 weeks | [TBD] | ❌ Not Started |
| #3: Multi-Tenant Isolation | P0 | 2-3 weeks | [TBD] | ❌ Not Started |

**Total Estimated Effort**: 7-10 weeks of work

---

## Related Documents

- [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) - Current implementation completeness
- [IMPLEMENTATION_GAPS.md](IMPLEMENTATION_GAPS.md) - Known gaps that are not blockers
- [/docs/reviews/ARCHITECTURE_REVIEW.md](reviews/ARCHITECTURE_REVIEW.md) - Full architectural review
- [/docs/reviews/SECURITY_REVIEW.md](reviews/SECURITY_REVIEW.md) - Security vulnerabilities

---

## Update Log

| Date | Change | Updated By |
|------|--------|------------|
| 2025-11-09 | Initial document created with 3 critical issues | Documentation Audit |
