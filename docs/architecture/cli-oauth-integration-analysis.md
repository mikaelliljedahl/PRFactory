# PRFactory Claude Code CLI Integration & OAuth Analysis

**Analysis Date**: November 9, 2025
**Status**: CRITICAL GAPS IDENTIFIED

---

## Executive Summary

PRFactory has **foundational architecture** for Claude CLI integration and token management, but **NO functional OAuth flows** and **NO developer machine workflows**. The existing implementation is incomplete and would not work in production for external developer access.

**Critical Issues:**
1. ❌ ClaudeDesktopCliAdapter is **hypothetical, not real**
2. ❌ **Zero OAuth implementation** (no auth endpoints, no token generation)
3. ❌ **Unauthenticated API endpoints** (no API key or OAuth validation)
4. ❌ **No developer machine workflow** (only Jira webhook triggers)
5. ❌ **No CLI client** for local developers
6. ⚠️ Token management is encrypted but statically configured

---

## Part 1: Claude Code CLI Integration

### Current Implementation

#### ClaudeDesktopCliAdapter (Infrastructure/Agents/Adapters/ClaudeDesktopCliAdapter.cs)

**What Exists:**
- ✅ Adapter interface (`ICliAgent`) for CLI-based agents
- ✅ Headless CLI invocation structure:
  ```csharp
  claude --headless --project-path "/path/to/project" --prompt "Your prompt here"
  ```
- ✅ Response parsing (JSON file operations, metadata extraction)
- ✅ Streaming output support
- ✅ 5-minute default timeout for standard prompts
- ✅ 10-minute timeout for project context analysis
- ✅ Integration with DI container

**Critical Problem: It's Hypothetical**

From the source code comments (lines 54-58):
```
/// <remarks>
/// This is a hypothetical CLI interface. If the actual Claude Desktop CLI 
/// has a different interface, this adapter will need to be updated to match 
/// the actual CLI command structure.
///
/// For production use, verify the Claude Desktop CLI documentation and update 
/// this adapter accordingly.
/// </remarks>
```

**Reality Check:**
- There is **no Claude Desktop CLI** with `--headless` flag (as of November 2025)
- Claude Code is a VSCode extension/web interface, NOT a CLI tool
- The adapter assumes a CLI interface that **does not exist**
- The flag structure (`--headless --project-path --prompt`) is completely fictional

### Where CLI Adapter is Actually Used

#### Internal Use Only
The CLI adapter is used **inside server-side agents**, not for external developer access:

**AnalysisAgent** (Agents/AnalysisAgent.cs:91)
```csharp
var cliResponse = await _cliAgent.ExecuteWithProjectContextAsync(
    prompt,
    context.RepositoryPath!,
    cancellationToken
);
```
- Called by PRFactory server to analyze code
- Uses hypothetical CLI to run Claude against the codebase
- Results are stored in database

**PlanningAgent** (similar pattern)
- Uses CLI to generate implementation plans
- Called during RefinementGraph execution

**ImplementationAgent** (similar pattern)
- Uses CLI for optional code generation
- Only if `AutoImplementAfterPlanApproval` is enabled

**No External Developer Access:**
- ❌ No API endpoint exposes CLI execution
- ❌ No way for developers to invoke agents
- ❌ No webhook for developer-triggered workflows
- ❌ No developer authentication scheme

### Configuration Issues

**ClaudeDesktopCliOptions** (Configuration/ClaudeDesktopCliOptions.cs):
```csharp
public string ExecutablePath { get; set; } = "claude";  // Assumes CLI in PATH
public int DefaultTimeoutSeconds { get; set; } = 300;
public int ProjectContextTimeoutSeconds { get; set; } = 600;
public bool EnableVerboseLogging { get; set; } = false;
```

**Problems:**
- ❌ Hardcoded path "claude" - will fail if not in PATH
- ❌ No authentication configured for CLI
- ❌ No credential passing mechanism
- ❌ No error recovery for missing CLI tool
- ❌ Assumes local execution (not for remote developer machines)

---

## Part 2: OAuth Flow Design

### Current OAuth Status

**ZERO OAuth Implementation Found**

Grep results:
```
/docs/SETUP.md:  Allow OAuth via GitHub for login (future work)
/docs/REFINEMENT_ENHANCEMENT_PLAN.md:  "Should we use OAuth or username/password?"
```

No actual code for:
- ❌ OAuth endpoints (`/oauth/authorize`, `/oauth/callback`, `/oauth/token`)
- ❌ OAuth token generation
- ❌ Token refresh mechanisms
- ❌ Authorization code flow
- ❌ Client credentials
- ❌ Scope management
- ❌ Token expiration handling

### What COULD Work: Token Management Infrastructure

**The system HAS encrypted credential storage**, which is good foundational work:

**AesEncryptionService** (Persistence/Encryption/AesEncryptionService.cs)
- ✅ AES-256-GCM encryption (authenticated encryption)
- ✅ Random nonce generation (12 bytes)
- ✅ Authentication tags (16 bytes)
- ✅ Base64 encoding for storage
- ✅ Proper error handling

**Token Storage Locations:**
1. **Tenant.JiraApiToken** - encrypted at rest
2. **Tenant.ClaudeApiKey** - encrypted at rest
3. **Repository.AccessToken** - encrypted at rest (git PAT)

**But:** All tokens are **statically configured via environment variables or appsettings.json**

### Missing OAuth Components

#### 1. No Authorization Server

Need:
```
POST /oauth/authorize
- Redirect to provider
- Request user consent
- Return auth code

POST /oauth/token
- Exchange code for access token
- Handle token refresh
- Return JWT + refresh token

POST /oauth/token/refresh
- Refresh expired tokens
- Rotate refresh tokens
```

#### 2. No Client Application Registration

Missing:
```csharp
public class OAuthClient
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }  // Encrypted
    public string[] RedirectUris { get; set; }
    public string[] Scopes { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

#### 3. No Authorization Code Management

Missing:
```csharp
public class AuthorizationCode
{
    public string Code { get; set; }  // Secure random
    public string UserId { get; set; }
    public DateTime ExpiresAt { get; set; }  // 10-15 minutes
    public string[] Scopes { get; set; }
    public string RedirectUri { get; set; }
}
```

#### 4. No Token Entity

Missing:
```csharp
public class Token
{
    public string AccessToken { get; set; }  // JWT
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string[] Scopes { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
```

---

## Part 3: Developer Machine Workflows

### Current State: No Developer Machine Integration

**The system ONLY supports Jira webhook triggers:**

From WebhookController.cs:
```csharp
[HttpPost("jira")]
public async Task<IActionResult> ReceiveJiraWebhook([FromBody] JiraWebhookPayload payload)
{
    // Only way to trigger workflows
}
```

**Flow:**
```
Jira Issue Created/Updated
    ↓
Jira Webhook → /api/webhooks/jira
    ↓
PRFactory processes event
    ↓
Agent graphs execute
```

**Missing: Developer-triggered workflows**
- ❌ No CLI client to trigger from local machine
- ❌ No API endpoint for manual trigger (with auth)
- ❌ No way to pass parameters from developer
- ❌ No feedback mechanism to developer machine

### What Would Be Needed

#### 1. CLI Client

```bash
# Would need something like:
prfactory workflow trigger \
  --ticket-key JIRA-123 \
  --token $PAT \
  --server https://prfactory.example.com

# Or for Claude Code integration:
prfactory claude analyze \
  --repo /path/to/repo \
  --token $CLAUDE_TOKEN
```

**Current Status:** ❌ Does not exist

#### 2. Developer API Endpoints

```
POST /api/developer/workflows
  Authorization: Bearer <dev-token>
  {
    "ticketKey": "JIRA-123",
    "parameters": {...}
  }

GET /api/developer/workflows/{id}/status
  Authorization: Bearer <dev-token>

WebSocket /api/developer/workflows/{id}/stream
  Authorization: Bearer <dev-token>
  (for real-time updates)
```

**Current Status:** ❌ Not implemented (API controllers exist but are TODOs)

#### 3. Authentication for Developer Access

```csharp
// MISSING:
public class DeveloperToken
{
    public string Id { get; set; }
    public string Token { get; set; }  // PAT format: prfactory_pat_xxx
    public Guid DeveloperId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string[] Scopes { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
```

**Current Status:** ❌ Not implemented

---

## Part 4: API Authentication Status

### Critical Finding: NO API AUTHENTICATION

**All API endpoints are unauthenticated:**

**WebhookController.cs:**
```csharp
[HttpPost("jira")]
public async Task<IActionResult> ReceiveJiraWebhook([FromBody] JiraWebhookPayload payload)
{
    // Zero [Authorize] attribute
    // Only validation: webhook HMAC signature
}
```

**TicketController.cs:**
```csharp
[HttpPost]
public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
{
    // Zero [Authorize] attribute
    // Anyone can create tickets!
}

[HttpGet("{id}")]
public async Task<IActionResult> GetTicketStatus(string id)
{
    // Zero [Authorize] attribute
}

[HttpPost("{id}/approve-plan")]
public async Task<IActionResult> ApprovePlan(string id, [FromBody] ApprovePlanRequest request)
{
    // Zero [Authorize] attribute
    // Anyone can approve plans!
}
```

**TicketUpdatesController.cs:**
- All endpoints unauthenticated

**AgentPromptTemplatesController.cs:**
- All endpoints unauthenticated

**Security Risk:** 🔴 CRITICAL
- No tenant isolation at API layer
- No user authentication
- No API key validation
- No JWT validation
- No rate limiting
- No access control

### What's Configured

**Jira Webhook Authentication (Partial):**
```csharp
// JiraWebhookAuthenticationMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    if (context.Request.Path.StartsWithSegments("/api/webhooks/jira"))
    {
        // Validates HMAC-SHA256 signature
        // X-Hub-Signature header required
    }
}
```

**Good:** ✅ HMAC validation prevents spoofed Jira webhooks
**Bad:** ❌ Only applies to Jira webhooks, not general API access

---

## Part 5: Token Management Architecture

### What Works

**1. Encryption Infrastructure** ✅
- AES-256-GCM encryption service
- Proper nonce generation
- Authentication tags
- Base64 encoding
- Good error handling

**2. Token Storage Locations** ✅
- Tenant.JiraApiToken (encrypted)
- Tenant.ClaudeApiKey (encrypted)
- Repository.AccessToken (encrypted)

**3. Configuration Sources** ✅
- Environment variables
- User secrets
- appsettings.json
- appsettings.Development.json

### What's Missing

#### 1. No Token Lifecycle Management

Missing:
```csharp
public enum TokenStatus
{
    Active,
    Expired,
    Revoked,
    Rotated
}

public class TokenRotationPolicy
{
    public TimeSpan ExpirationInterval { get; set; }
    public bool AutoRotate { get; set; }
    public int MaxTokenAge { get; set; }
    public int MaxTokensPerUser { get; set; }
}
```

#### 2. No Token Refresh Mechanism

Current: Tokens never expire or refresh
Needed:
- Automatic rotation (e.g., monthly)
- Manual revocation endpoint
- Token version tracking
- Audit trail

#### 3. No Personal Access Token (PAT) System

Missing:
```csharp
public class PersonalAccessToken
{
    public string Id { get; set; }
    public string Token { get; set; }  // prfactory_pat_xxx
    public string Name { get; set; }   // "My Dev Machine"
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
```

#### 4. No Token Scopes

All tokens have full access. Need:
```
user:read
user:write
workflow:trigger
workflow:read
ticket:read
ticket:write
repository:read
repository:write
```

#### 5. No Token Audit Trail

Missing:
```
Token created: 2025-11-09 10:00:00 by admin
Token rotated: 2025-11-09 12:00:00 (system)
Token last used: 2025-11-09 11:30:00 from 192.168.1.1
Token revoked: 2025-11-09 15:00:00 by owner
```

---

## Part 6: Architectural Gaps

### Gap 1: No Developer Identity

**Problem:** How do developers authenticate?

Current: Via Jira webhook (no developer identity needed)
Needed:
```csharp
public class Developer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public List<PersonalAccessToken> Tokens { get; set; }
    public List<AuthorizedDevice> Devices { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### Gap 2: No Device Management

**Problem:** How do we track developer machines and tokens?

Needed:
```csharp
public class AuthorizedDevice
{
    public Guid Id { get; set; }
    public Guid DeveloperId { get; set; }
    public string DeviceName { get; set; }
    public string DeviceId { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public bool IsRevoked { get; set; }
}
```

### Gap 3: No Rate Limiting

**Problem:** Anyone can hammer the API

Needed:
```csharp
public class RateLimitPolicy
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int ConcurrentRequests { get; set; } = 10;
    public TimeSpan BanDurationOnExceed { get; set; } = TimeSpan.FromHours(1);
}
```

### Gap 4: No API Versioning

**Problem:** Breaking changes will affect all developers

Needed:
```
GET /api/v1/tickets
GET /api/v2/tickets
Accept-Version: 1.0
X-API-Version: 2.0
```

### Gap 5: No Audit Logging for API Access

**Problem:** No visibility into who's doing what

Needed:
```csharp
public class ApiAuditLog
{
    public Guid Id { get; set; }
    public Guid? DeveloperId { get; set; }
    public string Endpoint { get; set; }
    public string Method { get; set; }
    public int ResponseStatus { get; set; }
    public DateTime RequestedAt { get; set; }
    public string IpAddress { get; set; }
    public Dictionary<string, object> RequestData { get; set; }
}
```

---

## Part 7: Configuration Issues

### appsettings.json Problems

**Current (src/PRFactory.Api/appsettings.json):**
```json
{
  "Claude": {
    "ApiKey": "",
    "Model": "claude-sonnet-4-5-20250929",
    "MaxTokens": 8000
  },
  "ClaudeDesktopCli": {
    "ExecutablePath": "claude",
    "DefaultTimeoutSeconds": 300
  },
  "Git": {
    "GitHub": {
      "Token": ""
    },
    "Bitbucket": {
      "Token": ""
    },
    "AzureDevOps": {
      "Token": ""
    }
  }
}
```

**Problems:**
1. ❌ **Static tokens** - no way to generate/rotate them
2. ❌ **No OAuth config** - no client_id, client_secret, endpoints
3. ❌ **No developer flow config** - no PAT settings
4. ❌ **No API auth config** - no JWT settings, no API key rules
5. ❌ **Insecure defaults** - empty strings, hardcoded model names

---

## Part 8: Functional Gaps Summary

| Feature | Status | Gap Severity |
|---------|--------|-------------|
| **Hypothetical Claude CLI** | ❌ Non-functional | CRITICAL |
| **OAuth 2.0 Implementation** | ❌ Missing | CRITICAL |
| **Developer PAT System** | ❌ Missing | CRITICAL |
| **API Authentication** | ❌ Missing | CRITICAL |
| **Developer Identity** | ❌ Missing | CRITICAL |
| **CLI Client Tool** | ❌ Missing | HIGH |
| **Token Refresh** | ❌ Missing | HIGH |
| **Rate Limiting** | ❌ Missing | HIGH |
| **Device Management** | ❌ Missing | MEDIUM |
| **Audit Logging** | ❌ Partial | MEDIUM |
| **Encrypted Storage** | ✅ Complete | N/A |
| **Webhook Auth (HMAC)** | ✅ Complete | N/A |

---

## Part 9: What Would Be Needed for Production

### Phase 1: Basic Developer Authentication (2-3 weeks)

1. **Personal Access Token System**
   - Entity for storing PATs
   - Generation/revocation endpoints
   - Token validation middleware
   - Scope management

2. **API Authentication Middleware**
   - Bearer token validation
   - Tenant isolation
   - Rate limiting
   - Audit logging

3. **Developer Identity**
   - Developer entity
   - Device tracking
   - Session management

### Phase 2: OAuth 2.0 Flow (3-4 weeks)

1. **OAuth Server Implementation**
   - Authorization endpoint
   - Token endpoint
   - Refresh flow
   - Client registration

2. **Integration Points**
   - GitHub OAuth (for developers)
   - Jira OAuth (for webhooks)
   - Azure AD (for enterprises)

### Phase 3: Developer Tooling (2-3 weeks)

1. **CLI Client**
   - Workflow triggering
   - Status checking
   - Log streaming
   - Token management

2. **SDK for Developers**
   - .NET client library
   - Python client library
   - JavaScript/Node client library

### Phase 4: Production Hardening (1-2 weeks)

1. **Security Enhancements**
   - CSP headers
   - CSRF protection
   - TLS enforcement
   - Secret scanning

2. **Monitoring**
   - Token usage metrics
   - Auth failure tracking
   - Rate limit violations
   - Audit log analysis

---

## Part 10: Architecture Recommendations

### Recommended OAuth Flow

**For Developers (PAT-based):**
```
Developer generates PAT on web UI
    ↓
CLI/SDK uses: Authorization: Bearer prfactory_pat_xxx
    ↓
API validates token scope
    ↓
Request allowed if valid
```

**For integrations (OAuth 2.0 Authorization Code):**
```
Developer clicks "Authorize App"
    ↓
Browser → /oauth/authorize
    ↓
User consents on consent screen
    ↓
Browser → redirect_uri?code=xxx
    ↓
App backend calls /oauth/token with code
    ↓
App receives access_token + refresh_token
    ↓
App uses token for API calls
```

### Recommended CLI Structure

```bash
# Installation
curl -fsSL https://install.prfactory.io | sh

# Configuration
prfactory config set --server https://prfactory.example.com
prfactory auth login --provider github
prfactory token create --name "dev-machine-1" --expires 90d

# Usage
prfactory workflow trigger JIRA-123
prfactory workflow status JIRA-123
prfactory workflow watch JIRA-123
prfactory logs tail JIRA-123

# Token management
prfactory token list
prfactory token revoke <token-id>
prfactory token rotate
```

---

## Conclusion

PRFactory has **solid foundational infrastructure** (encryption, token storage, webhook auth) but **zero functional OAuth and developer machine workflows**. The Claude CLI adapter is **completely hypothetical** and won't work without significant changes.

### Key Takeaways

1. **ClaudeDesktopCliAdapter is non-functional** - assumes a CLI that doesn't exist
2. **No OAuth implementation** - not a single endpoint or flow implemented
3. **API endpoints are unauthenticated** - critical security issue
4. **No developer machine workflow** - only Jira webhooks supported
5. **Token management is static** - no generation, rotation, or refresh

### Immediate Actions Needed

1. **Clarify Claude integration strategy** - Remove hypothetical CLI adapter or implement real integration
2. **Implement API authentication** - Add OAuth/PAT system ASAP
3. **Add rate limiting** - Prevent abuse of unauthenticated endpoints
4. **Create developer documentation** - How will external devs authenticate?
5. **Design OAuth flow** - Support for GitHub, Jira, enterprise OAuth

### Questions for Product Owner

1. How should developers authenticate? OAuth? PATs? Both?
2. Should Claude Code integration be local CLI or remote API?
3. What's the target developer experience (Jenkins-like webhooks vs CLI commands)?
4. Do we need multi-provider OAuth or just internal PATs?
5. What's the timeline for API authentication implementation?

