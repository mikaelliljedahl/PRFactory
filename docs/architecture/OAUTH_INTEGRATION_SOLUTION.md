# OAuth Integration Solution for PRFactory

**Date**: 2025-11-09
**Status**: ✅ **SOLUTION IDENTIFIED** - Existing OAuth implementation in OrchestratorChat

---

## Executive Summary

PRFactory's authentication challenge can be solved by **porting the existing Anthropic OAuth implementation from OrchestratorChat**. This provides server-side authentication for Claude API access without requiring personal OAuth tokens from individual developers.

---

## The Solution: Anthropic OAuth (Already Implemented)

### What You Already Have in OrchestratorChat

**Repository**: https://github.com/mikaelliljedahl/OrchestratorChat

**Key Components**:

1. **IAnthropicOAuthService** - Complete OAuth flow implementation
   - `StartAuthAsync()` - Initiates OAuth flow with PKCE
   - `HandleCallbackAsync()` - Processes OAuth callback
   - `SubmitCodeAsync()` - Supports manual code submission
   - `GetStatusAsync()` - Returns connection status
   - `LogoutAsync()` - Clears tokens

2. **IOAuthStateStore** - CSRF protection with one-time state tokens
   - Server-side PKCE verifier storage
   - 10-minute expiration
   - Replay attack prevention

3. **ITokenStore** - OAuth token persistence
   - Access tokens
   - Refresh tokens
   - Expiration tracking

### OAuth Endpoints Used

```
Authorization: https://claude.ai/oauth/authorize
Token Exchange: https://console.anthropic.com/v1/oauth/token
Redirect URI: https://console.anthropic.com/oauth/code/callback
Client ID: 9d1c250a-e61b-44d9-88ed-5944d1962f5e
Scopes: org:create, user:*:profile, inference:*
```

### Security Features

- ✅ **PKCE** (Proof Key for Code Exchange) - prevents authorization code interception
- ✅ **Server-side verifier generation** - never trusts client-provided values
- ✅ **One-time state tokens** - prevents CSRF attacks
- ✅ **Automatic token expiration** - reduces security window
- ✅ **Comprehensive logging** - audit trail for security events

---

## How This Solves PRFactory's Problem

### Current Problem

PRFactory needs to authenticate with Claude to run AI workflows, but:
- ❌ Cannot use personal developer OAuth tokens (multi-tenant system)
- ❌ ClaudeCodeCliAdapter authentication mechanism unclear
- ❌ No OAuth infrastructure in PRFactory

### Solution

**Port OrchestratorChat OAuth to PRFactory**:

```
┌─────────────────────────────────────────────────────┐
│          PRFactory OAuth Flow (Per Tenant)          │
└─────────────────────────────────────────────────────┘

1. Tenant Admin initiates OAuth
   ├─> Web UI: "Connect Anthropic Account"
   └─> AnthropicOAuthService.StartAuthAsync()
       ├─> Generates PKCE verifier (server-side)
       ├─> Creates state token (CSRF protection)
       └─> Returns authorization URL

2. Tenant Admin authorizes
   ├─> Redirected to claude.ai/oauth/authorize
   ├─> Logs in with Anthropic account
   ├─> Grants permissions (org:create, user:*, inference:*)
   └─> Redirected back with authorization code

3. PRFactory exchanges code for tokens
   ├─> Validates state token (one-time use)
   ├─> Retrieves PKCE verifier
   ├─> Exchanges code for access/refresh tokens
   └─> Stores tokens encrypted in Tenant entity

4. Workflows use stored tokens
   ├─> AnalysisAgent fetches Tenant.OAuthAccessToken
   ├─> Makes API call to Anthropic with Bearer token
   └─> Token refresh handled automatically
```

---

## Architecture: Two Options

### Option A: Use OAuth Tokens with Anthropic API Directly (RECOMMENDED)

**How it works**:
1. Each Tenant has OAuth tokens stored in database (encrypted)
2. Agents use Anthropic Messages API with Bearer authentication
3. No CLI dependency - pure REST API calls

**Implementation**:

```csharp
// Updated Tenant entity
public class Tenant
{
    public string? OAuthAccessToken { get; private set; }      // Encrypted
    public string? OAuthRefreshToken { get; private set; }     // Encrypted
    public DateTime? OAuthTokenExpiresAt { get; private set; }
    public string[] OAuthScopes { get; private set; } = [];

    public void SetOAuthTokens(string accessToken, string refreshToken, DateTime expiresAt, string[] scopes)
    {
        OAuthAccessToken = accessToken;
        OAuthRefreshToken = refreshToken;
        OAuthTokenExpiresAt = expiresAt;
        OAuthScopes = scopes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearOAuthTokens()
    {
        OAuthAccessToken = null;
        OAuthRefreshToken = null;
        OAuthTokenExpiresAt = null;
        OAuthScopes = [];
        UpdatedAt = DateTime.UtcNow;
    }
}

// AnthropicApiClient using OAuth tokens
public class AnthropicApiClient : IClaudeAgent
{
    private readonly HttpClient _httpClient;
    private readonly ITokenRefreshService _tokenRefresh;

    public async Task<string> AnalyzeCodebaseAsync(
        Guid tenantId,
        string prompt,
        string codebaseContext,
        CancellationToken ct)
    {
        // Get fresh access token (refresh if expired)
        var accessToken = await _tokenRefresh.GetValidAccessTokenAsync(tenantId, ct);

        var request = new
        {
            model = "claude-sonnet-4-5-20250929",
            max_tokens = 8000,
            messages = new[]
            {
                new { role = "user", content = prompt + "\n\n" + codebaseContext }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(request)
        };

        // Use OAuth access token
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(ct);
        return result.Content[0].Text;
    }
}
```

**Pros**:
- ✅ No CLI dependency
- ✅ Works in Docker containers
- ✅ Token refresh handled automatically
- ✅ Multi-tenant (each tenant has own OAuth tokens)
- ✅ Known OAuth implementation (already working in OrchestratorChat)

**Cons**:
- ❌ No file operations (Claude can't edit files directly)
- ❌ Must send codebase context explicitly (no automatic indexing)
- ❌ Token limits (need to select relevant files)

---

### Option B: Use OAuth Tokens with Claude Code CLI

**Research Question**: Can Claude Code CLI accept OAuth tokens via environment variable?

**Test**:
```bash
# Does this work?
export ANTHROPIC_OAUTH_TOKEN="eyJhbGciOiJIUzI1NiIs..."
claude --print "Analyze this codebase"
```

**If YES**:
```csharp
public async Task<CliAgentResponse> ExecuteWithProjectContextAsync(
    Guid tenantId,
    string prompt,
    string projectPath,
    CancellationToken cancellationToken = default)
{
    // Get fresh access token
    var accessToken = await _tokenRefresh.GetValidAccessTokenAsync(tenantId, cancellationToken);

    var arguments = new List<string>
    {
        "--print",
        prompt,
        "--output-format", "json",
        "--allowedTools", "Read,Bash,Edit",
        "--permission-mode", "acceptEdits"
    };

    // Set OAuth token as environment variable
    var envVars = new Dictionary<string, string>
    {
        { "ANTHROPIC_OAUTH_TOKEN", accessToken }
    };

    var result = await _processExecutor.ExecuteAsync(
        _options.ExecutablePath,
        arguments,
        workingDirectory: projectPath,
        environmentVariables: envVars,
        timeoutSeconds: _options.ProjectContextTimeoutSeconds,
        cancellationToken: cancellationToken);

    return ParseCliResponse(result);
}
```

**Pros (if this works)**:
- ✅ Claude Code has full file editing capabilities
- ✅ Automatic codebase indexing
- ✅ Multi-tenant (each workflow uses tenant's OAuth token)
- ✅ Known OAuth implementation

**Cons**:
- ⚠️ Need to verify CLI accepts OAuth tokens (not documented)
- ⚠️ Requires Claude Code installation in Docker
- ⚠️ May require Claude Code Pro subscription

---

## Implementation Plan

### Phase 1: Port OAuth Infrastructure (3-4 days)

**Tasks**:

1. **Copy OAuth services from OrchestratorChat** (1 day)
   - `IAnthropicOAuthService` → `PRFactory.Core/Authentication/`
   - `AnthropicOAuthService` → `PRFactory.Infrastructure/Authentication/`
   - `IOAuthStateStore` → `PRFactory.Core/Authentication/`
   - `OAuthStateStore` → `PRFactory.Infrastructure/Authentication/`

2. **Add OAuth entities to Tenant** (1 day)
   - `OAuthAccessToken` (encrypted)
   - `OAuthRefreshToken` (encrypted)
   - `OAuthTokenExpiresAt`
   - `OAuthScopes`
   - Migration to add columns

3. **Add token refresh service** (1 day)
   - `ITokenRefreshService` interface
   - Automatically refreshes expired tokens
   - Updates Tenant entity with new tokens

4. **Add OAuth UI to Web** (1 day)
   - Tenant settings page
   - "Connect Anthropic Account" button
   - OAuth callback handler
   - Connection status display

### Phase 2: Update Agents to Use OAuth (2-3 days)

**Choose Option A or Option B**:

**Option A: Anthropic API** (2 days)
1. Create `AnthropicApiClient` with OAuth Bearer tokens
2. Update `AnalysisAgent`, `PlanningAgent`, `ImplementationAgent`
3. Build codebase context manually
4. Test end-to-end workflows

**Option B: CLI with OAuth** (3 days if successful)
1. Research: Test Claude Code CLI with OAuth tokens
2. Update `ClaudeCodeCliAdapter` to pass OAuth token
3. Update `ProcessExecutor` to support environment variables
4. Test end-to-end workflows

### Phase 3: Testing & Validation (2-3 days)

1. **OAuth flow testing**
   - Test with multiple tenants
   - Verify token refresh works
   - Test token expiration handling

2. **Workflow testing**
   - Create test tickets
   - Verify AI-powered analysis/planning/implementation
   - Test error handling

3. **Security testing**
   - Verify encrypted token storage
   - Test PKCE flow
   - Validate state token one-time use

**Total Effort**: 1-2 weeks

---

## Configuration Changes

### appsettings.json

```json
{
  "Anthropic": {
    "OAuth": {
      "ClientId": "9d1c250a-e61b-44d9-88ed-5944d1962f5e",
      "AuthorizationEndpoint": "https://claude.ai/oauth/authorize",
      "TokenEndpoint": "https://console.anthropic.com/v1/oauth/token",
      "RedirectUri": "https://your-prfactory.com/oauth/callback",
      "Scopes": ["org:create", "user:*:profile", "inference:*"]
    },
    "Api": {
      "BaseUrl": "https://api.anthropic.com/v1",
      "DefaultModel": "claude-sonnet-4-5-20250929",
      "MaxTokens": 8000,
      "TimeoutSeconds": 300
    }
  },
  "ClaudeCodeCli": {
    "ExecutablePath": "claude",
    "UseOAuthTokens": true  // Use OAuth instead of API keys
  }
}
```

### Database Migration

```csharp
public class AddOAuthToTenant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OAuthAccessToken",
            table: "Tenants",
            type: "TEXT",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OAuthRefreshToken",
            table: "Tenants",
            type: "TEXT",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "OAuthTokenExpiresAt",
            table: "Tenants",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OAuthScopes",
            table: "Tenants",
            type: "TEXT",
            nullable: false,
            defaultValue: "[]");
    }
}
```

---

## Multi-Tenant OAuth Model

### How It Works

```
Tenant A
  ├── Admin: alice@example.com
  ├── OAuth Flow: Alice authorizes with her Anthropic account
  ├── Tokens: Stored encrypted in Tenant A
  └── Workflows: Use Tenant A's OAuth tokens

Tenant B
  ├── Admin: bob@example.com
  ├── OAuth Flow: Bob authorizes with his Anthropic account
  ├── Tokens: Stored encrypted in Tenant B
  └── Workflows: Use Tenant B's OAuth tokens
```

**Benefits**:
- ✅ Each tenant has independent Anthropic authorization
- ✅ Token costs/usage tracked per tenant
- ✅ Tenant can revoke access independently
- ✅ No shared credentials across tenants

**Token Lifecycle**:
1. **Initial Authorization**: Tenant admin authorizes once
2. **Token Storage**: Access + refresh tokens encrypted in database
3. **Automatic Refresh**: When access token expires, refresh token gets new one
4. **Re-authorization**: If refresh token expires, admin must re-authorize

---

## Security Improvements

### OAuth vs API Keys

| Aspect | API Keys (Current) | OAuth (Proposed) |
|--------|-------------------|------------------|
| **Revocation** | Manual, requires new key | Instant via Anthropic dashboard |
| **Expiration** | Never (unless manually rotated) | Access tokens expire (auto-refresh) |
| **Scopes** | Full access | Granular permissions |
| **Audit Trail** | No visibility | Anthropic tracks usage |
| **Shared Accounts** | Everyone uses same key | Per-tenant authorization |

### PKCE Benefits

**PKCE (Proof Key for Code Exchange)** prevents authorization code interception:

1. **Code Verifier**: Random secret generated server-side (never sent to client)
2. **Code Challenge**: Hash of verifier sent to authorization endpoint
3. **Token Exchange**: Original verifier sent to token endpoint
4. **Validation**: Anthropic verifies challenge matches verifier

**Attack Prevention**:
- If attacker intercepts authorization code, they lack the verifier
- Cannot exchange code for tokens without server-side verifier
- Eliminates need for client secrets

---

## Comparison: OAuth vs API Keys

### Use OAuth If:
- ✅ Need per-tenant authorization and usage tracking
- ✅ Want token expiration and automatic refresh
- ✅ Need granular permission scopes
- ✅ Want revocation without changing configuration
- ✅ Multi-tenant SaaS application

### Use API Keys If:
- ✅ Simple single-tenant deployment
- ✅ Background jobs with no user interaction
- ✅ Service-to-service authentication
- ✅ Don't need per-user tracking

**For PRFactory**: **OAuth is the better choice** because it's a multi-tenant SaaS application where each tenant should control their own Anthropic authorization.

---

## Updated Architecture Review Conclusions

### Critical Findings (Revised)

1. ✅ **OAuth solution exists** - OrchestratorChat has working Anthropic OAuth implementation
2. 🔴 **No API authentication** - All endpoints publicly accessible (unchanged)
3. 🔴 **Secrets logged to files** - All API keys exposed in logs (unchanged)
4. 🔴 **Hardcoded tenant ID** - Multi-tenant isolation completely broken (unchanged)
5. 🟡 **SQLite limits scalability** - Cannot run multiple Worker instances (unchanged)

### Immediate Actions Required (Updated)

**Before ANY deployment**:
1. ✅ Remove `GetDebugView()` secret logging (10 min)
2. ✅ Implement tenant context resolution (1 day)
3. ✅ Add API authentication middleware (3-4 days)
4. ✅ **Port OAuth from OrchestratorChat** (3-4 days) ← NEW
5. ✅ **Choose Option A (API) or Option B (CLI)** (2-3 days) ← NEW

**Estimated time to minimum viable production**: **2-3 weeks**

---

## Recommended Next Steps

### Week 1: OAuth Integration
1. Port OAuth services from OrchestratorChat
2. Add OAuth columns to Tenant entity
3. Build OAuth UI in Web app
4. Test OAuth flow end-to-end

### Week 2: Agent Updates + Security Fixes
1. Implement AnthropicApiClient with OAuth (Option A)
2. Update agents to use OAuth tokens
3. Fix critical security issues (secret logging, tenant context, API auth)
4. Test complete workflows

### Week 3: Testing + Production Prep
1. End-to-end workflow testing
2. Token refresh testing
3. Multi-tenant testing
4. Security audit
5. Production deployment

---

## Questions for Implementation

1. **Which option do you prefer?**
   - Option A: Anthropic API with OAuth (recommended, known to work)
   - Option B: Claude Code CLI with OAuth (needs verification)

2. **OAuth scopes needed?**
   - Current OrchestratorChat uses: `org:create`, `user:*:profile`, `inference:*`
   - Are these sufficient for PRFactory?

3. **Token storage?**
   - Current: In-memory cache (OAuthStateStore)
   - PRFactory: Database (encrypted in Tenant entity)
   - Is this acceptable?

4. **Redirect URI?**
   - OrchestratorChat: `https://console.anthropic.com/oauth/code/callback`
   - PRFactory: `https://your-domain.com/oauth/callback`?

5. **Multi-tenant authorization**
   - Who authorizes OAuth per tenant? (Tenant admin?)
   - UI for connecting Anthropic account?

---

**END OF OAUTH INTEGRATION SOLUTION**
