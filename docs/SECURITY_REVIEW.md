# PRFactory Security Review - Comprehensive Findings

## Executive Summary

**Critical Issues Found: 3**  
**High Severity Issues: 6**  
**Medium Severity Issues: 4**  
**Low Severity Issues: 3**

The most pressing security concerns are the lack of API authentication, multi-tenant isolation gaps, and secrets exposed in debug logging. These must be addressed before production deployment.

---

## CRITICAL SEVERITY

### 1. Configuration Debug View Logs Secrets (Critical)
**File:** `/home/user/PRFactory/src/PRFactory.Api/Program.cs:226`  
**Severity:** CRITICAL  
**Type:** Secrets Exposure

```csharp
Log.Information("Configuration loaded from: {ConfigurationSource}",
    builder.Configuration.GetDebugView());
```

**Issue:**
- `GetDebugView()` returns the entire configuration including all secrets (API tokens, database connection strings, encryption keys)
- This is logged at Information level and persists in log files
- Logs are stored in `/logs/` directory and retained for 30 days

**Impact:**
- Complete credential compromise if logs are accessed
- Attackers can extract API keys for Claude, Jira, GitHub, Bitbucket, Azure DevOps
- Database credentials, webhook secrets, and encryption configuration exposed

**Remediation:**
```csharp
// REMOVE GetDebugView() logging
Log.Information("Configuration loaded successfully");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
```

---

### 2. No API Authentication (Critical)
**File:** `/home/user/PRFactory/src/PRFactory.Api/Program.cs:197-199`  
**Severity:** CRITICAL  
**Type:** Missing Authentication

**Current State:**
- ALL API endpoints unprotected except Jira webhooks
- `// app.UseAuthentication()` and `// app.UseAuthorization()` are commented out
- Anyone with network access can:
  - Create tickets via `POST /api/tickets`
  - Approve/reject plans
  - List all tickets
  - Submit answers
  - Query ticket status

**Endpoints at Risk:**
```
POST   /api/tickets                      - Create ticket (NO AUTH)
GET    /api/tickets                      - List all tickets (NO AUTH)
GET    /api/tickets/{id}                 - Get ticket status (NO AUTH)
POST   /api/tickets/{id}/approve-plan    - Approve plan (NO AUTH)
POST   /api/tickets/{id}/reject-plan     - Reject plan (NO AUTH)
POST   /api/tickets/{id}/answers         - Submit answers (NO AUTH)
GET    /api/tickets/{id}/questions       - Get questions (NO AUTH)
GET    /api/tickets/{id}/events          - Get events (NO AUTH)
```

**Remediation:**
```csharp
// Enable authentication/authorization
builder.Services.AddAuthentication().AddBearerToken();
builder.Services.AddAuthorization();

// In middleware pipeline (after CORS, before endpoints)
app.UseAuthentication();
app.UseAuthorization();

// Protect endpoints
[Authorize]
public class TicketController : ControllerBase { ... }
```

---

### 3. Multi-Tenant Context Hardcoded to Demo Tenant (Critical)
**File:** `/home/user/PRFactory/src/PRFactory.Infrastructure/Application/TenantContext.cs:19-30`  
**Severity:** CRITICAL  
**Type:** Multi-Tenant Isolation Bypass

```csharp
private static readonly Guid DemoTenantId = new Guid("00000000-0000-0000-0000-000000000001");

public Guid GetCurrentTenantId()
{
    // Returns same tenant for ALL requests!
    return DemoTenantId;
}
```

**Issue:**
- ALL users access the same hardcoded demo tenant
- No tenant isolation - all data is shared
- Repositories, tickets, credentials mixed across all users
- Multi-tenant architecture completely bypassed

**Impact:**
- Tenant A can access Tenant B's repositories
- All API keys, GitHub tokens, Jira credentials shared
- Workspace files accessible by any user (same TenantId)
- Complete data breach in multi-tenant deployment

**Remediation:**
```csharp
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentTenantId()
    {
        // Extract from JWT claims, API key, or HTTP header
        var claim = _httpContextAccessor.HttpContext?.User
            .FindFirst("tenant_id");
        
        if (claim != null && Guid.TryParse(claim.Value, out var tenantId))
            return tenantId;
            
        throw new UnauthorizedAccessException("Tenant ID not found in request");
    }
}
```

---

## HIGH SEVERITY

### 4. Path Traversal Risk in File Operations (High)
**File:** `/home/user/PRFactory/src/PRFactory.Infrastructure/Claude/ContextBuilder.cs:267-276`  
**Severity:** HIGH  
**Type:** Path Traversal

```csharp
private async Task<string?> TryReadFileAsync(string repoPath, string relativePath)
{
    var fullPath = Path.Combine(repoPath, relativePath);
    if (!File.Exists(fullPath))
        return null;

    try
    {
        return await File.ReadAllTextAsync(fullPath);  // NO PATH VALIDATION!
    }
    catch (Exception ex) { ... }
}
```

**Vulnerability:**
- No validation that `fullPath` is within `repoPath`
- Attacker-controlled repository content can include `../../../etc/passwd`
- Combined with code execution, can read arbitrary files

**Attack Scenario:**
```
Repository uploaded with file: `../../../var/lib/prfactory/encryption-key.txt`
repoPath = "/var/prfactory/workspace/repo123"
relativePath = "../../encryption-key.txt"
fullPath = Path.Combine(...) = "/var/prfactory/encryption-key.txt"  ✗ UNSAFE
```

**Remediation:**
```csharp
private async Task<string?> TryReadFileAsync(string repoPath, string relativePath)
{
    var fullPath = Path.GetFullPath(Path.Combine(repoPath, relativePath));
    var baseRepoPath = Path.GetFullPath(repoPath);
    
    // Ensure fullPath is within repoPath
    if (!fullPath.StartsWith(baseRepoPath + Path.DirectorySeparatorChar))
    {
        _logger.LogWarning("Path traversal attempt detected: {AttemptedPath}", relativePath);
        return null;  // Silently reject
    }
    
    if (!File.Exists(fullPath))
        return null;

    return await File.ReadAllTextAsync(fullPath);
}
```

**Also affects:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/LocalGitService.cs:126`
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/GitPlanAgent.cs:85`

---

### 5. Missing Input Validation - String Length (High)
**File:** `/home/user/PRFactory/src/PRFactory.Api/Models/CreateTicketRequest.cs`  
**Severity:** HIGH  
**Type:** Input Validation

```csharp
[Required]
[JsonPropertyName("title")]
public string Title { get; set; } = string.Empty;  // NO LENGTH LIMIT!

[Required]
[JsonPropertyName("description")]
public string Description { get; set; } = string.Empty;  // NO LENGTH LIMIT!
```

**Issue:**
- No `StringLength` or `MaxLength` attributes
- Attacker can send unlimited-length strings
- Causes database bloat, memory exhaustion, or DoS

**Attack:**
```bash
POST /api/tickets
{
  "title": "A" * 10000000,  # 10MB string
  "description": "B" * 100000000,  # 100MB string
  ...
}
```

**Remediation:**
```csharp
[Required]
[StringLength(200, MinimumLength = 3)]
public string Title { get; set; } = string.Empty;

[Required]
[StringLength(5000, MinimumLength = 10)]
public string Description { get; set; } = string.Empty;
```

---

### 6. CORS Misconfiguration (High)
**File:** `/home/user/PRFactory/src/PRFactory.Api/Program.cs:80-93`  
**Severity:** HIGH  
**Type:** CORS Security

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()      // ✗ ALLOWS DELETE, PATCH, etc!
              .AllowAnyHeader()      // ✗ NO VALIDATION!
              .AllowCredentials();   // Dangerous with AllowAny!
    });
});
```

**Issues:**
1. `AllowAnyMethod()` - allows DELETE, PATCH, PUT, TRACE
2. `AllowAnyHeader()` - accepts any headers, including `Authorization: Bearer invalid`
3. `AllowCredentials()` combined with broad origins

**Correct Implementation:**
```csharp
policy.WithOrigins(allowedOrigins)
      .WithMethods("GET", "POST", "OPTIONS")
      .WithHeaders("Content-Type", "Authorization")
      .AllowCredentials();
```

---

### 7. Missing Webhook Replay Protection (High)
**File:** `/home/user/PRFactory/src/PRFactory.Api/Middleware/JiraWebhookAuthenticationMiddleware.cs`  
**Severity:** HIGH  
**Type:** Webhook Security

**Issue:**
- Signature validation exists (good: HMAC-SHA256)
- BUT no nonce/timestamp validation
- Attacker can replay old webhook events indefinitely

**Attack:**
1. Capture legitimate webhook: `{"webhookEvent": "issue_created", ...}` + valid signature
2. Replay same webhook 1000 times
3. Creates 1000 duplicate tickets

**Remediation:**
```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (context.Request.Path.StartsWithSegments("/api/webhooks/jira"))
    {
        // ... existing signature validation ...

        // ADD: Check Jira timestamp (from payload)
        if (!long.TryParse(context.Request.Headers["X-Jira-Timestamp"], out var timestamp))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Missing timestamp" });
            return;
        }

        // Reject if older than 5 minutes
        var webhookTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
        if (DateTimeOffset.UtcNow - webhookTime > TimeSpan.FromMinutes(5))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Webhook timestamp expired" });
            return;
        }

        // ADD: Store processed webhook ID to prevent replays
        var webhookId = context.Request.Headers["X-Jira-Webhook-Id"];
        if (await _processedWebhooks.ContainsAsync(webhookId))
        {
            context.Response.StatusCode = 400;  // Silently ignore duplicate
            return;
        }

        await _processedWebhooks.AddAsync(webhookId, TimeSpan.FromHours(24));
    }

    await _next(context);
}
```

---

### 8. Workspace Not Isolated by Tenant (High)
**File:** `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/RepositoryCloneAgent.cs:51`  
**Severity:** HIGH  
**Type:** Multi-Tenant Isolation

```csharp
var localPath = Path.Combine(_workspaceBasePath, repositoryId.ToString());
                                               // ↑ Only repositoryId, not tenantId!
```

**Issue:**
- Repository workspace path uses only `repositoryId`
- Does NOT include `tenantId`
- With hardcoded TenantId, all repos share same base path anyway
- If TenantId is later fixed, repos still not tenant-isolated

**Expected:**
```csharp
var localPath = Path.Combine(_workspaceBasePath, 
                             context.TenantId.ToString(),
                             repositoryId.ToString());
```

**Also affects:**
- `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/LocalGitService.cs:62`

---

### 9. Jira Webhook Secret Can Be Empty (High)
**File:** `/home/user/PRFactory/src/PRFactory.Api/Middleware/JiraWebhookAuthenticationMiddleware.cs:33-38`  
**Severity:** HIGH  
**Type:** Weak Secret Handling

```csharp
var webhookSecret = _configuration["Jira:WebhookSecret"];

// Skip validation if no secret is configured (development mode)
if (string.IsNullOrEmpty(webhookSecret))
{
    _logger.LogWarning("Jira webhook secret is not configured. Skipping HMAC validation.");
    await _next(context);
    return;
}
```

**Issue:**
- If `Jira:WebhookSecret` not configured, signature validation is BYPASSED
- Anyone can send fake webhook events
- appsettings.json has empty string `"WebhookSecret": ""`

**Impact:**
- Attacker can trigger arbitrary workflows
- Create fake tickets, approve plans, trigger implementations

**Remediation:**
```csharp
var webhookSecret = _configuration["Jira:WebhookSecret"];

if (string.IsNullOrEmpty(webhookSecret))
{
    _logger.LogError("SECURITY: Jira webhook secret not configured!");
    throw new InvalidOperationException(
        "Jira:WebhookSecret is required. Set via appsettings or environment variable.");
}
```

---

## MEDIUM SEVERITY

### 10. Missing Rate Limiting (Medium)
**Location:** Application-wide  
**Severity:** MEDIUM  
**Type:** Denial of Service

**Issue:**
- No rate limiting on any API endpoints
- Attackers can hammer `/api/tickets` with unlimited requests
- No protection against API key bruteforce
- Claude API calls not rate-limited per-tenant

**Remediation:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ticket-creation", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 2;
    });
});

app.UseRateLimiter();

// On TicketController
[HttpPost]
[RequireRateLimiting("ticket-creation")]
public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
```

---

### 11. HMAC Signature Comparison Length Check Missing (Medium)
**File:** `/home/user/PRFactory/src/PRFactory.Api/Middleware/JiraWebhookAuthenticationMiddleware.cs:76-96`  
**Severity:** MEDIUM  
**Type:** Timing Attack (Minor)

```csharp
private static bool ValidateSignature(string body, string signature, string secret)
{
    try
    {
        if (!signature.StartsWith("sha256="))
        {
            return false;
        }

        var signatureHash = signature.Substring(7);
        
        // ✓ GOOD: Uses FixedTimeEquals
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signatureHash),
            Encoding.UTF8.GetBytes(expectedSignature));
    }
    catch (Exception)
    {
        return false;
    }
}
```

**Issue:**
- Signature length not validated before FixedTimeEquals
- If lengths differ significantly, comparison is inefficient
- Minor timing leak possible

**Remediation:**
```csharp
if (signatureHash.Length != expectedSignature.Length)
{
    return false;  // Early exit without timing leak
}

return CryptographicOperations.FixedTimeEquals(
    Encoding.UTF8.GetBytes(signatureHash),
    Encoding.UTF8.GetBytes(expectedSignature));
```

---

### 12. Exception Details Logged in Development (Medium)
**File:** `/home/user/PRFactory/src/PRFactory.Infrastructure/Agents/Configuration/AgentConfiguration.cs`  
**Severity:** MEDIUM  
**Type:** Information Disclosure

```csharp
public bool SanitizeErrorMessages { get; set; } = true;  // Good default
```

**Issue:**
- If disabled, full exception stack traces logged and returned to client
- Can leak internal paths, database schema, code structure
- Development mode might be running in production

**Mitigation:**
- Ensure `SanitizeErrorMessages = true` in Production
- Never disable in production configs

---

### 13. No Secrets Rotation Policy (Medium)
**Severity:** MEDIUM  
**Type:** Credential Management

**Issue:**
- No mechanism to rotate API keys
- No expiration dates on stored credentials
- If compromised, admin must manually update database
- Encryption key not rotatable (AES-256 with single key)

**Remediation:**
1. Implement key rotation mechanism
2. Add credential expiration timestamps
3. Audit log for credential access
4. Create automated rotation pipelines

---

## LOW SEVERITY

### 14. Encryption Key Management (Low)
**File:** `/home/user/PRFactory/src/PRFactory.Infrastructure/Persistence/Encryption/AesEncryptionService.cs:21-42`  
**Severity:** LOW  
**Type:** Key Management

**Issue:**
- Encryption key passed as base64 string in constructor
- Key likely stored in `appsettings.json` or environment variable
- If server compromised, key accessible
- No key derivation (direct AES key usage)

**Current State (acceptable for MVP):**
```csharp
public AesEncryptionService(string base64Key, ILogger<AesEncryptionService> logger)
{
    _encryptionKey = Convert.FromBase64String(base64Key);
    if (_encryptionKey.Length != 32) // 256 bits - GOOD!
    {
        throw new ArgumentException("Encryption key must be 256 bits");
    }
}
```

**Good aspects:**
- ✓ Uses AES-256-GCM (authenticated encryption)
- ✓ Random 12-byte nonce per encryption
- ✓ 16-byte authentication tag (GMAC)
- ✓ Proper constant-time comparison for decryption

**Future improvements:**
- Use Azure Key Vault instead of environment variables
- Implement key versioning
- Add PBKDF2 key derivation if using password-based keys

---

### 15. Git Credentials in Memory (Low)
**File:** `/home/user/PRFactory/src/PRFactory.Infrastructure/Git/LocalGitService.cs:74-78`  
**Severity:** LOW  
**Type:** Credentials in Memory

```csharp
cloneOptions.FetchOptions.CredentialsProvider = (url, user, cred) => 
    new UsernamePasswordCredentials
    {
        Username = "oauth2",
        Password = accessToken  // ← In memory until GC runs
    };
```

**Issue:**
- Access token held in memory as string (unencrypted)
- Not cleared immediately after use
- Dump of process memory exposes token

**Mitigation:**
- AccessToken already encrypted in database (good)
- Consider using SecureString if supported by LibGit2Sharp
- This is acceptable risk for CLI operations

---

### 16. No Security Headers (Low)
**Severity:** LOW  
**Type:** HTTP Security Headers

**Missing Headers:**
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000
Content-Security-Policy: ...
```

**Remediation:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    await next();
});
```

---

## SUMMARY TABLE

| Issue | Severity | Type | Status |
|-------|----------|------|--------|
| Configuration Debug Logging | CRITICAL | Secrets Exposure | Must Fix |
| Missing API Authentication | CRITICAL | Missing Auth | Must Fix |
| Hardcoded Demo Tenant | CRITICAL | Multi-Tenant Bypass | Must Fix |
| Path Traversal | HIGH | Injection | Should Fix |
| Missing String Validation | HIGH | Input Validation | Should Fix |
| CORS Misconfiguration | HIGH | CORS | Should Fix |
| Webhook Replay Attacks | HIGH | Logic Flaw | Should Fix |
| Workspace Not Tenant-Isolated | HIGH | Multi-Tenant Bypass | Should Fix |
| Empty Webhook Secret Bypass | HIGH | Weak Secret | Should Fix |
| Missing Rate Limiting | MEDIUM | DoS | Consider |
| HMAC Length Check | MEDIUM | Timing | Minor |
| Exception Details Logging | MEDIUM | Info Disclosure | Monitor |
| No Key Rotation | MEDIUM | Key Mgmt | Future |
| Encryption Key Management | LOW | Key Mgmt | Future |
| Git Creds in Memory | LOW | Creds Exposure | Minor |
| Missing Security Headers | LOW | Headers | Nice to Have |

---

## RECOMMENDATIONS

### Immediate (Before Any Production Use):
1. **CRITICAL**: Remove `GetDebugView()` from logging
2. **CRITICAL**: Implement proper API authentication
3. **CRITICAL**: Fix TenantContext to extract tenant from request
4. **HIGH**: Add path traversal validation to file operations
5. **HIGH**: Add StringLength validation to API models
6. **HIGH**: Fix CORS configuration
7. **HIGH**: Add webhook replay protection
8. **HIGH**: Require non-empty Jira webhook secret

### Short Term (Sprint 1-2):
1. Implement rate limiting on API endpoints
2. Fix workspace tenant isolation
3. Add security headers
4. Implement webhook nonce/timestamp validation

### Medium Term (Sprint 3-4):
1. Implement credential rotation mechanisms
2. Integrate Azure Key Vault for key management
3. Add request signing for webhook idempotency
4. Implement audit logging for sensitive operations

### Long Term:
1. Regular security audits
2. OWASP compliance checklist
3. Penetration testing
4. Bug bounty program for production

