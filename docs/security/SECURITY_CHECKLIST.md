# Security Fix Checklist

## CRITICAL - Block Production Deployment

### [ ] 1. Remove Configuration Debug Logging
- **File:** `src/PRFactory.Api/Program.cs:226`
- **Change:** Remove `builder.Configuration.GetDebugView()` from logging
- **Why:** Exposes all API keys, tokens, and secrets in logs
- **Estimated Time:** 5 minutes

```csharp
// BEFORE (VULNERABLE):
Log.Information("Configuration loaded from: {ConfigurationSource}",
    builder.Configuration.GetDebugView());

// AFTER (FIXED):
Log.Information("Configuration loaded successfully");
```

---

### [ ] 2. Implement API Authentication
- **File:** `src/PRFactory.Api/Program.cs`
- **Changes:**
  1. Enable authentication service in DI
  2. Add authorization middleware
  3. Add `[Authorize]` to `TicketController`
  4. Add `[AllowAnonymous]` to webhook endpoint only
- **Why:** Currently all API endpoints are unprotected
- **Estimated Time:** 2-3 hours

```csharp
// ADD to services:
builder.Services.AddAuthentication()
    .AddBearerToken();
builder.Services.AddAuthorization();

// ADD to middleware (after CORS, before endpoints):
app.UseAuthentication();
app.UseAuthorization();
```

---

### [ ] 3. Fix Multi-Tenant Context
- **File:** `src/PRFactory.Infrastructure/Application/TenantContext.cs`
- **Change:** Extract tenant ID from HTTP request context instead of hardcoding
- **Why:** All tenants currently share same demo tenant ID
- **Estimated Time:** 1-2 hours

```csharp
// BEFORE (VULNERABLE):
private static readonly Guid DemoTenantId = new Guid("00000000-0000-0000-0000-000000000001");
public Guid GetCurrentTenantId() => DemoTenantId;

// AFTER (FIXED):
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public Guid GetCurrentTenantId()
    {
        var claim = _httpContextAccessor.HttpContext?.User
            .FindFirst("tenant_id");
        if (claim != null && Guid.TryParse(claim.Value, out var tenantId))
            return tenantId;
        throw new UnauthorizedAccessException("Tenant ID not found");
    }
}
```

---

## HIGH PRIORITY - Fix Before Production

### [ ] 4. Add Path Traversal Validation
- **Files:**
  - `src/PRFactory.Infrastructure/Claude/ContextBuilder.cs:267-276`
  - `src/PRFactory.Infrastructure/Git/LocalGitService.cs:126`
  - `src/PRFactory.Infrastructure/Agents/GitPlanAgent.cs:85`
- **Change:** Validate file paths with `Path.GetFullPath()` and check they're within base directory
- **Why:** Prevents reading files outside repository directory
- **Estimated Time:** 1 hour

```csharp
private async Task<string?> TryReadFileAsync(string repoPath, string relativePath)
{
    var fullPath = Path.GetFullPath(Path.Combine(repoPath, relativePath));
    var baseRepoPath = Path.GetFullPath(repoPath);
    
    if (!fullPath.StartsWith(baseRepoPath + Path.DirectorySeparatorChar))
    {
        _logger.LogWarning("Path traversal attempt: {Path}", relativePath);
        return null;
    }
    
    if (!File.Exists(fullPath))
        return null;

    return await File.ReadAllTextAsync(fullPath);
}
```

---

### [ ] 5. Add StringLength Validation to API Models
- **File:** `src/PRFactory.Api/Models/CreateTicketRequest.cs`
- **Changes:** Add `StringLength` or `MaxLength` attributes
- **Why:** Prevents unlimited-size input causing DoS
- **Estimated Time:** 30 minutes

```csharp
[Required]
[StringLength(200, MinimumLength = 3)]
public string Title { get; set; } = string.Empty;

[Required]
[StringLength(5000, MinimumLength = 10)]
public string Description { get; set; } = string.Empty;
```

---

### [ ] 6. Fix CORS Configuration
- **File:** `src/PRFactory.Api/Program.cs:80-93`
- **Changes:** Replace `AllowAnyMethod()` and `AllowAnyHeader()` with specific values
- **Why:** Overly permissive CORS allows DELETE, PATCH, and other dangerous methods
- **Estimated Time:** 15 minutes

```csharp
policy.WithOrigins(allowedOrigins)
      .WithMethods("GET", "POST", "OPTIONS")
      .WithHeaders("Content-Type", "Authorization")
      .AllowCredentials();
```

---

### [ ] 7. Add Webhook Replay Protection
- **File:** `src/PRFactory.Api/Middleware/JiraWebhookAuthenticationMiddleware.cs`
- **Changes:**
  1. Validate webhook timestamp (reject if >5 minutes old)
  2. Track processed webhook IDs
  3. Reject duplicates
- **Why:** Prevents replaying captured webhooks to create duplicate events
- **Estimated Time:** 2 hours

**See full implementation in `/docs/SECURITY_REVIEW.md`**

---

### [ ] 8. Require Non-Empty Webhook Secret
- **File:** `src/PRFactory.Api/Middleware/JiraWebhookAuthenticationMiddleware.cs:33-38`
- **Change:** Throw exception if webhook secret is empty
- **Why:** Empty secret bypasses HMAC signature validation
- **Estimated Time:** 5 minutes

```csharp
var webhookSecret = _configuration["Jira:WebhookSecret"];

if (string.IsNullOrEmpty(webhookSecret))
{
    throw new InvalidOperationException(
        "SECURITY ERROR: Jira:WebhookSecret is required. " +
        "Set via appsettings or JIRA_WEBHOOK_SECRET environment variable.");
}
```

---

### [ ] 9. Fix Workspace Tenant Isolation
- **Files:**
  - `src/PRFactory.Infrastructure/Agents/RepositoryCloneAgent.cs:51`
  - `src/PRFactory.Infrastructure/Git/LocalGitService.cs:62`
- **Change:** Include `TenantId` in workspace path
- **Why:** Repos from different tenants could access shared directories
- **Estimated Time:** 30 minutes

```csharp
// BEFORE:
var localPath = Path.Combine(_workspaceBasePath, repositoryId.ToString());

// AFTER:
var localPath = Path.Combine(_workspaceBasePath, 
                             context.TenantId.ToString(),
                             repositoryId.ToString());
```

---

## MEDIUM PRIORITY - Sprint 1-2

### [ ] 10. Implement Rate Limiting
- **File:** `src/PRFactory.Api/Program.cs`
- **Why:** Protects against API brute force and DoS attacks
- **Estimated Time:** 1-2 hours

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ticket-creation", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(1);
    });
});
```

---

### [ ] 11. Add Security Headers
- **File:** `src/PRFactory.Api/Program.cs`
- **Why:** Adds HTTP security headers (X-Frame-Options, etc.)
- **Estimated Time:** 30 minutes

---

### [ ] 12. Validate HMAC Length Before Comparison
- **File:** `src/PRFactory.Api/Middleware/JiraWebhookAuthenticationMiddleware.cs:76-96`
- **Why:** Improves timing attack resistance
- **Estimated Time:** 15 minutes

---

## TRACKING

### Testing & Verification

- [ ] All endpoints require authentication (except `/api/webhooks/jira`)
- [ ] Webhook signature validation cannot be bypassed
- [ ] Path traversal rejected (e.g., `../../../etc/passwd`)
- [ ] Large input strings rejected
- [ ] Cross-tenant data access prevented
- [ ] Rate limiting blocks excessive requests
- [ ] Configuration secrets not in logs

### Code Review Checklist

- [ ] No `GetDebugView()` in logs
- [ ] No hardcoded tenant IDs
- [ ] All file paths validated
- [ ] Input lengths validated
- [ ] CORS restricted to specific origins/methods
- [ ] Authentication enabled on all sensitive endpoints

---

## Dependencies

### Required NuGet Updates

If implementing JWT authentication:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="*" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="*" />
```

### Build Verification

After changes, run:
```bash
dotnet build
dotnet test
```

Ensure:
- ✓ All projects compile without errors
- ✓ All tests pass
- ✓ No compilation warnings related to security

---

## Deployment Checklist

Before deploying to production:

- [ ] All CRITICAL issues marked as FIXED
- [ ] All HIGH severity issues marked as FIXED
- [ ] Security review re-run and passed
- [ ] Penetration testing completed (recommended)
- [ ] Security team approval obtained
- [ ] Incident response plan in place
- [ ] Audit logging configured

---

## Questions?

See full details in `/docs/SECURITY_REVIEW.md`

